using Engine.Api.Http.WebSockets;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Hosting;
using Engine.Core.Persistence;

namespace Engine.Api.Http;

// The document session that the surface serves, and what the surface needs to
// serve it. A host composes it and mounts the surface on it (ADR-0019 items 1 and
// 3, TASK-0038): engine-api-http in its program, and the desktop host in its own
// process (TASK-0054). The host owns the session, so a host can also send typed
// commands and queries to it; each one goes through the same bus as a command
// over HTTP (ADR-0019 item 2).
//
// The host chooses the backend and gives it here with its name and version. The
// library references Engine.Core and Engine.Contracts only (ADR-0019 item 3), so
// it cannot name the native backend; that choice belongs to a composition root
// (ADR-0014 section 4). Until TASK-0038 this class chose the backend itself.
//
// The session gets a decorated sink, so that each committed event reaches each
// WebSocket subscriber (TASK-0010 section 2). Each endpoint reaches the engine
// through Session (TASK-0034): commands, queries and the WebSocket handshake
// enter one serial section.
public sealed class EngineHost : IDisposable
{
    // ADR-0015 item 10: a host with a file opens it, or makes it when it does not
    // exist, before its surface serves a request; a host with an unsaved folder
    // writes there from the first command. A host with no file keeps the document in
    // memory only, as each test that makes a host. A file that does not open throws
    // a DocumentFileException, and the backend is freed.
    public EngineHost(IGeometryBackend backend, string backendName, string backendVersion, DocumentFileOptions? file = null)
    {
        ArgumentNullException.ThrowIfNull(backend);

        Backend = backend;
        BackendName = backendName;
        BackendVersion = backendVersion;
        Broadcaster = new EventBroadcaster();

        // One composition point for every other part, shared with the CLI.
        // Each registered handler comes from HandlerCatalog per ADR-0016.
        _unsavedFolder = file?.Path is null && file?.UnsavedFolder is { } unsaved ? Path.GetFullPath(unsaved) : null;
        var opens = file?.Path is { } existing && File.Exists(existing);
        EngineKit kit;
        try
        {
            kit = EngineHosting.CreateDefault(Backend, opens ? DocumentFile.ReadLengthUnit(file!.Path!) : Document.Millimetre);
        }
        catch
        {
            (Backend as IDisposable)?.Dispose();
            throw;
        }

        Document = kit.Document;
        CommandRegistry = kit.CommandRegistry;
        QueryRegistry = kit.QueryRegistry;
        Session = new DocumentSession(kit, new BroadcastingEventSink(kit.Events, Broadcaster));

        try
        {
            if (file?.Path is { } path)
            {
                if (opens)
                    OpenWarnings = DocumentFile.Open(Session, path).GetAwaiter().GetResult();
                else
                    DocumentFile.Create(Session, path);
            }
            else if (file?.UnsavedFolder is { } folder)
            {
                DocumentFile.Unsaved(Session, folder);
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private readonly string? _unsavedFolder;

    // ADR-0030 item 4: the name of the document file, or null for a host with no file
    // and for a document that is still in the folder of unsaved documents.
    public string? FileName
        => Session.FilePath is { } path && !string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), _unsavedFolder, StringComparison.Ordinal)
            ? Path.GetFileName(path)
            : null;

    // The warnings of the open of the file (W-IO-TRUNCATED, W-IO-BACKEND-VERSION),
    // which the host writes on its error output.
    public IReadOnlyList<Diagnostic> OpenWarnings { get; } = Array.Empty<Diagnostic>();

    public DocumentSession Session { get; }

    // The name and the version of the backend that the host composed, for
    // /schema/backend. Engine.Contracts does not change for them.
    public string BackendName { get; }

    public string BackendVersion { get; }

    public CommandRegistry CommandRegistry { get; }

    public QueryRegistry QueryRegistry { get; }

    // Internal, so that a host reaches the backend through the bus only (ADR-0019
    // item 2). It was public until TASK-0062 (finding E56 of the review of
    // 2026-10-11).
    internal IGeometryBackend Backend { get; }

    internal EventBroadcaster Broadcaster { get; }

    // For the tests only. A read of the Document here has no lock, therefore an
    // endpoint must read through Session.Read. The identifier does not change,
    // and a test reads the version only when no command runs.
    internal Document Document { get; }

    // Gives each later event of the session to the action, after its commit and in
    // the order of the commits: the same events that /events gives (ADR-0024 item 1,
    // ADR-0019 item 2). The action runs inside the commit, so it must not call the
    // session and must not block; a host puts the event in a queue. An action that
    // throws is removed, with the warning W-API-SUBSCRIPTION-REMOVED on the error
    // output. The payload of each event is read-only. To read the live bodies and
    // subscribe with no gap, call this inside Session.Read. Dispose the result to stop;
    // an event of a commit in progress can still arrive after Dispose returns.
    public IDisposable Subscribe(Action<EventRecord> action) => Broadcaster.Listen(action);

    // The session closes first and waits for a command that still runs, so the
    // backend is never freed under it (finding E66 of the codebase review of
    // 2026-10-17, TASK-0079). The container of a host calls this after the stop.
    public void Dispose()
    {
        Session.Close();
        (Backend as IDisposable)?.Dispose();
    }
}

// ADR-0015 item 10: the document file of a host. Path names the file, which the host
// opens, or makes when it does not exist. With no path, UnsavedFolder is the folder of
// unsaved documents, and with neither the host has no file.
public sealed record DocumentFileOptions(string? Path, string? UnsavedFolder = null);
