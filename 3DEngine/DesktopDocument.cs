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
// the port, and the next session holds the file on the same port, so an agent connects again
// to the same address. Since R14b (ADR-0061 item 9, TASK-0194) the next session is in the
// same process; until then the program started again with --file.
internal sealed class DesktopDocument
{
    private string? _requested;

    public DesktopDocument(string folder)
    {
        Folder = new DocumentFolder(folder, Reopen);
    }

    public DocumentFolder Folder { get; }

    // The path of the file to open after the window closes, or null.
    public string? RequestedOpen => Volatile.Read(ref _requested);

    // The file that the session of this desktop holds, or null; the host sets it after it
    // makes the session.
    public Func<string?>? HeldFile { get; set; }

    private async Task<ErrorDetail?> Reopen(string path, CancellationToken ct)
    {
        if (RequestedOpen is not null)
            return new ErrorDetail(DiagnosticCodes.IoLoadFailed, $"An open of {RequestedOpen} is already in progress.");

        // ADR-0030 item 5: the check holds the file as a journal does, so a file that another
        // program holds fails it (V31). The file that this desktop holds is the exception: the
        // old process frees it before the new program starts.
        using var backend = new ManifoldGeometryBackend();
        if (await DocumentFile.Check(path, backend, hold: !SameFile(path, HeldFile?.Invoke()), ct) is { } error)
            return error;

        Volatile.Write(ref _requested, path);
        return null;
    }

    // True when the two paths name one file. Windows and macOS compare names with no case.
    internal static bool SameFile(string path, string? other)
        => other is not null && string.Equals(
            Path.GetFullPath(path),
            Path.GetFullPath(other),
            OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
}
