using Engine.Contracts;
using Engine.Core;

namespace ThreeDEngine.Desktop;

// ADR-0072 item 2 (phase R14e1, TASK-0222): the document session of a host that draws. The application reads the
// Document and sends each command through Session, and it gets each committed event through Subscribe. The desktop
// gives the EngineHost of the surface library through HostedSession; the phone composes its own session, because
// Android has no runtime pack of ASP.NET Core (ADR-0061 item 4).
internal interface ISessionHost
{
    DocumentSession Session { get; }

    // The name of the document file, or null for a document with no file and for a document that is still in the
    // folder of unsaved documents (ADR-0030 item 4).
    string? FileName { get; }

    // Gives each later event of the session to the action, after its commit and in the order of the commits, as
    // EngineHost.Subscribe gives it (ADR-0024 item 1). The action runs inside the commit, so it must not call the session
    // and must not block. Dispose the result to stop.
    IDisposable Subscribe(Action<EventRecord> action);
}

// ADR-0072 item 3: the files of a host that draws, for the actions Save, Open and Export of the panel. The desktop
// gives its document folder (ADR-0030 item 2); the phone gives the picker of Android in a later slice of R14e.
internal interface IDocumentFiles
{
    // The path of a plain file name in the folder of the host, or null with the reason.
    string? Resolve(string name, out string? refusal);

    // Checks the file at the path and gives it to the host for its next session, or gives the error (ADR-0030 item 5).
    Task<ErrorDetail?> Open(string path, CancellationToken ct);
}
