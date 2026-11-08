using System.Diagnostics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core;
using Engine.Core.Persistence;
using Engine.Geometry.Manifold;

namespace ThreeDEngine.Desktop;

// ADR-0030 items 3 to 5 (TASK-0092): the document folder of the desktop, and an open. An
// open checks the file in a scratch session with its own backend first, so a file that does
// not open leaves the person with the window. Then the window loop ends, the surface frees
// the port, and the next session holds the file; with a fixed port it is the same port, so an
// agent connects again to the same address. Since R14b (ADR-0061 item 9, TASK-0194) the next
// session is in the same process; until then the program started again with --file.
internal sealed class DesktopDocument : IDocumentFiles
{
    private string? _requested;

    // The path of the open that a check examines or that the check accepted, or null.
    private string? _pending;

    // E167 (TASK-0262): the store of the desktop, which the check of an open reads for an import of ADR-0083.
    private readonly Engine.Core.Assets.IAssetSource? _assets;

    // ADR-0100 item 6 (Q2 A, TASK-0314): the recent projects of this desktop, or null for no list.
    private readonly RecentProjects? _recent;

    public DesktopDocument(string folder, Engine.Core.Assets.IAssetSource? assets = null, RecentProjects? recent = null)
    {
        _assets = assets;
        _recent = recent;
        Folder = new DocumentFolder(folder, Reopen);
    }

    public DocumentFolder Folder { get; }

    // ADR-0072 item 3 (TASK-0222): the files of the application are the names of the document folder.
    public string? Resolve(string name, out string? refusal) => Folder.Resolve(name, out refusal);

    public Task<ErrorDetail?> Open(string path, CancellationToken ct) => Folder.Open(path, ct);

    // The path of the file to open after the window closes, or null. NewDocument asks for a new part document.
    public string? RequestedOpen => Volatile.Read(ref _requested);

    // ADR-0083 D1 (TASK-0259): the path that asks the next session for a new part document, whatever the setting "file".
    public const string NewDocument = "";

    // Each session of the program makes its own DesktopDocument, so the import of the next session and the state of the home
    // screen belong to the process.
    private static StlImport? _import;
    private static bool _home = true;

    // ADR-0100 item 6 (Q2 A, TASK-0314): the recent projects in each folder. Until TASK-0314 the home screen gave the newest
    // files of the document folder only, so a project in another folder never showed.
    public IReadOnlyList<string> Recent(int count) => _recent?.Paths(count) ?? [];

    public void Remember(string path) => _recent?.Remember(path);

    // Finding V125 of the codebase review of 2026-11-07 (b) (TASK-0270): the request takes the place of an open, as Reopen
    // does (E149), so a request and an open of a file in one session cannot both be accepted, and the import of the request
    // reaches only its new part. Until TASK-0270 an open of an agent could replace the request and take its import.
    public ErrorDetail? RequestNew(StlImport? import)
    {
        if (Interlocked.CompareExchange(ref _pending, NewDocument, null) is { } pending)
            return InProgress(pending);

        Volatile.Write(ref _import, import);
        Volatile.Write(ref _requested, NewDocument);
        Folder.AcceptRequest();
        return null;
    }

    public StlImport? TakeImport() => Interlocked.Exchange(ref _import, null);

    // V125: the host starts the previous file again when a new part does not start (finding E150), and that session must not
    // take the import of the new part.
    internal static StlImport? DropImport() => Interlocked.Exchange(ref _import, null);

    private static ErrorDetail InProgress(string pending)
        => new(DiagnosticCodes.IoLoadFailed, pending == NewDocument
            ? "A new part document is already in progress."
            : $"An open of {pending} is already in progress.");

    // ADR-0083 D6: the first session of the program starts on the home screen; a choice there ends it for the program. A
    // choice is "Open file", "New part" or an accepted open of a file (V126, TASK-0270).
    public bool Home { get => Volatile.Read(ref _home); set => Volatile.Write(ref _home, value); }

    public bool HasFolder => true;

    // The file that the session of this desktop holds, or null; the host sets it after it
    // makes the session.
    public Func<string?>? HeldFile { get; set; }

    private async Task<ErrorDetail?> Reopen(string path, CancellationToken ct)
    {
        // Finding E149 of the codebase review of 2026-11-06 (TASK-0197): the open read the pending open, awaited the check
        // and then wrote, so two opens at the same time were both accepted. An open takes the pending place before the
        // check, and gives it back when the check fails.
        if (Interlocked.CompareExchange(ref _pending, path, null) is { } pending)
            return InProgress(pending);

        var accepted = false;
        try
        {
            // ADR-0030 item 5: the check holds the file as a journal does, so a file that another
            // program holds fails it (V31). The file that this desktop holds is the exception: the
            // host disposes the session of that file before the next session holds it.
            using var backend = new ManifoldGeometryBackend();
            if (await DocumentFile.Check(path, backend, hold: !SameFile(path, HeldFile?.Invoke()), ct, _assets) is { } error)
                return error;

            Volatile.Write(ref _requested, path);
            // Finding V126 of the codebase review of 2026-11-07 (b) (TASK-0270): an accepted open of a file, from a recent
            // row, the panel or an agent, ends the home screen; until TASK-0270 the document opened under it again.
            Volatile.Write(ref _home, false);
            accepted = true;
            return null;
        }
        finally
        {
            if (!accepted)
                Volatile.Write(ref _pending, null);
        }
    }

    // True when the two paths name one file. Windows and macOS compare names with no case.
    internal static bool SameFile(string path, string? other)
        => other is not null && string.Equals(
            Path.GetFullPath(path),
            Path.GetFullPath(other),
            OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
}
