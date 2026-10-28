using Engine.Contracts;
using Engine.Core.Hosting;

namespace Engine.Core;

// One Document, its backend, its command bus, its query bus and its event sink
// behind one serial section (TASK-0034).
//
// A command, a query and a read never overlap, so a reader never sees a commit
// in the middle. ADR-0008 section 6 says that a query is serialized against
// commands, and ADR-0014 section 3 says that the backend is single-threaded from
// the view of the engine. Before this type the Document, the backends and the
// readers had no common lock, and a probe gave thousands of exceptions
// (DocumentSessionConcurrencyTests).
//
// The order of the locks is fixed: this section, then the semaphore of the bus,
// then the lock of the sink, then the lock of a host (for example the
// broadcaster of the HTTP host). A host that reads the Document and also takes a
// lock of its own must do it inside Read, so that it keeps the same order.
//
// One queue for each kind of access is the accepted cost: a long query delays a
// command. TASK-0034, section "Scope (out)", gives the reason not to add a
// reader-writer lock or snapshot isolation now.
public sealed class DocumentSession
{
    private readonly SemaphoreSlim _serial = new(1, 1);
    private readonly Document _document;
    private readonly IEventSink _events;
    private readonly CommandBus _commands;
    private readonly QueryBus _queries;

    // The sink is the decorated sink when a host decorates one, and the sink of
    // the kit when a host does not. The session builds the one command bus of
    // the Document; a second bus on the same Document is refused (CommandBus).
    public DocumentSession(EngineKit kit, IEventSink? events = null)
    {
        ArgumentNullException.ThrowIfNull(kit);

        _document = kit.Document;
        _events = events ?? kit.Events;
        _commands = kit.CreateCommandBus(_events);
        _queries = kit.CreateQueryBus();
        CommandRegistry = kit.CommandRegistry;
        QueryRegistry = kit.QueryRegistry;
        BackendIdentity = kit.Backend.Identity;
    }

    // The registries are filled before the session exists and do not change
    // after, so a host can read them with no lock.
    public CommandRegistry CommandRegistry { get; }

    public QueryRegistry QueryRegistry { get; }

    // The identifier does not change for the life of the Document.
    public Guid DocumentId => _document.DocumentId;

    // ADR-0015 item 7: the unit does not change for the life of the Document.
    public string LengthUnit => _document.LengthUnit;

    // ADR-0015 item 5: the backend that each log entry of this session records.
    public Engine.Contracts.Geometry.BackendIdentity BackendIdentity { get; }

    // The path of the document file, or null for a session with no file. It changes
    // at a save.
    public string? FilePath => _commands.Journal?.FilePath;

    // ADR-0015 items 8 to 10: a host gives the session its file before the first
    // command, after an open applied each entry with no write.
    internal void Attach(Persistence.DocumentJournal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);
        if (_commands.Journal is not null)
            throw new InvalidOperationException("The session has a document file.");
        _commands.Journal = journal;
    }

    // ADR-0015 item 9: moves the document file to a new name, in the serial section,
    // and later entries go there. The target must not exist. It gives null, or the
    // error E-IO-SAVE-FAILED, and the file then stays where it was.
    public async Task<ErrorDetail?> SaveAs(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        RefuseReentry();
        RefuseClosed();
        await _serial.WaitAsync(ct).ConfigureAwait(false);
        Hold();
        try
        {
            if (_commands.Journal is not { } journal)
                return new ErrorDetail(DiagnosticCodes.IoSaveFailed, "The session has no document file.");
            if (journal.Broken)
                return new ErrorDetail(DiagnosticCodes.IoSaveFailed, $"An earlier write of the document file {journal.FilePath} failed.");

            try
            {
                journal.MoveTo(Path.GetFullPath(path));
                return null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return new ErrorDetail(DiagnosticCodes.IoSaveFailed, $"The save to {path} failed: {ex.Message}");
            }
        }
        finally
        {
            Release();
        }
    }

    // A cancellation stops the command while it waits for the section. After
    // the bus starts the commit, it does not stop (ADR-0006 section 4).
    public Task<CommandResult> Apply(Command command, CancellationToken ct = default)
        => Apply(command, entry: false, ct);

    // ADR-0015 item 8: an open applies each entry of the file one time, with no
    // look-up in the idempotency cache (finding E75).
    internal Task<CommandResult> ApplyEntry(Command command, CancellationToken ct = default)
        => Apply(command, entry: true, ct);

    private async Task<CommandResult> Apply(Command command, bool entry, CancellationToken ct)
    {
        RefuseReentry();
        RefuseClosed();
        await _serial.WaitAsync(ct).ConfigureAwait(false);
        Hold();
        try
        {
            return await (entry ? _commands.ApplyEntry(command, ct) : _commands.Apply(command, ct)).ConfigureAwait(false);
        }
        finally
        {
            Release();
        }
    }

    public async Task<QueryResult<T>> Query<T>(Query query, CancellationToken ct = default)
    {
        RefuseReentry();
        RefuseClosed();
        await _serial.WaitAsync(ct).ConfigureAwait(false);
        Hold();
        try
        {
            return await _queries.Query<T>(query, ct).ConfigureAwait(false);
        }
        finally
        {
            Release();
        }
    }

    // A read of the Document and of the event sink in the section. The function
    // must not keep a reference to the Document after it returns: a reference
    // that leaves the section is a reader with no lock again.
    public async Task<T> Read<T>(Func<Document, IEventSink, T> read, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(read);

        RefuseReentry();
        RefuseClosed();
        await _serial.WaitAsync(ct).ConfigureAwait(false);
        Hold();
        try
        {
            return read(_document, _events);
        }
        finally
        {
            Release();
        }
    }

    // A call into the session from code that runs inside the section, an event
    // sink or a read function, waited for the section that its own flow holds,
    // and each other client waited behind it (finding E20 of the review of
    // 2026-10-04, TASK-0051). The flow that holds the section carries a token in
    // an AsyncLocal, and the session refuses a call that carries the token of the
    // current holder. A task that code inside the section starts inherits the
    // token, so it is refused too while the section is held. After the section
    // ends the token is stale, and the task waits like each other caller.
    //
    // Two limits (findings E42 and E43 of the review of 2026-10-09, TASK-0058):
    //   - A task that a sink starts and does not wait for is refused or accepted
    //     by timing: refused while the section is held, accepted after. Such a
    //     task cannot wait for itself, so a refusal of it is a false alarm. Start
    //     detached work inside ExecutionContext.SuppressFlow, so that it carries
    //     no token and waits like each other caller.
    //   - A flow that carries no execution context, for example a dedicated
    //     thread, carries no token, so the session does not see its call back.
    //     If the sink waits for such a flow, the call waits for ever.
    // The rule for a sink therefore is: do not wait for work on another thread.
    private readonly AsyncLocal<object?> _flow = new();
    private object? _holder;

    private void RefuseReentry()
    {
        var token = _flow.Value;
        if (token is not null && ReferenceEquals(token, Volatile.Read(ref _holder)))
            throw new InvalidOperationException(
                "A call entered the DocumentSession from inside its own serial section: from an event sink, "
                + "from a read function, or from a task that one of them started. The call would wait for "
                + "itself. Make the call after the section ends.");
    }

    // The end of the session (finding E66 of the codebase review of 2026-10-17,
    // TASK-0079). A host calls this before it frees the backend. The session refuses
    // each later call, and Close returns when the serial section is free, so that no
    // command, query or read still runs on the backend. Until TASK-0079 the stop of a
    // host ended its wait for an open request after about 3 s, and the backend was
    // freed under a command that still ran.
    //
    // Close has no time limit. A commit does not stop (ADR-0006 section 4), and a
    // backend that is freed under a native call can corrupt memory, so a command that
    // never ends holds the stop of its host. A caller that waits for the section when
    // Close starts gets the section after it, sees the flag and is refused.
    public void Close()
    {
        RefuseReentry();
        Volatile.Write(ref _closed, true);
        _serial.Wait();
        try
        {
            // ADR-0015: no command runs, so the file closes after its last line.
            _commands.Journal?.Dispose();
        }
        finally
        {
            _serial.Release();
        }
    }

    private bool _closed;

    private void RefuseClosed()
    {
        if (Volatile.Read(ref _closed))
            throw new InvalidOperationException(
                "The DocumentSession is closed: its host stopped, and its backend is freed or about to be freed.");
    }

    // Not async, so that the value of the AsyncLocal stays in the caller and
    // reaches the code that the caller runs in the section. A caller that waited
    // for the section while Close ran leaves it at once.
    private void Hold()
    {
        if (Volatile.Read(ref _closed))
        {
            _serial.Release();
            RefuseClosed();
        }

        var token = new object();
        Volatile.Write(ref _holder, token);
        _flow.Value = token;
    }

    private void Release()
    {
        Volatile.Write(ref _holder, null);
        _serial.Release();
    }
}
