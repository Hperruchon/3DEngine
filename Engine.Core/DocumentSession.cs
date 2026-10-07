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
    }

    // The registries are filled before the session exists and do not change
    // after, so a host can read them with no lock.
    public CommandRegistry CommandRegistry { get; }

    public QueryRegistry QueryRegistry { get; }

    // The identifier does not change for the life of the Document.
    public Guid DocumentId => _document.DocumentId;

    // A cancellation stops the command while it waits for the section. After
    // the bus starts the commit, it does not stop (ADR-0006 section 4).
    public async Task<CommandResult> Apply(Command command, CancellationToken ct = default)
    {
        RefuseReentry();
        await _serial.WaitAsync(ct).ConfigureAwait(false);
        Hold();
        try
        {
            return await _commands.Apply(command, ct).ConfigureAwait(false);
        }
        finally
        {
            Release();
        }
    }

    public async Task<QueryResult<T>> Query<T>(Query query, CancellationToken ct = default)
    {
        RefuseReentry();
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

    // Not async, so that the value of the AsyncLocal stays in the caller and
    // reaches the code that the caller runs in the section.
    private void Hold()
    {
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
