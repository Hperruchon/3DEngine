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
internal sealed class DesktopDocument
{
    private string? _requested;

    // The path of the open that a check examines or that the check accepted, or null.
    private string? _pending;

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
        // Finding E149 of the codebase review of 2026-11-06 (TASK-0197): the open read the pending open, awaited the check
        // and then wrote, so two opens at the same time were both accepted. An open takes the pending place before the
        // check, and gives it back when the check fails.
        if (Interlocked.CompareExchange(ref _pending, path, null) is { } pending)
            return new ErrorDetail(DiagnosticCodes.IoLoadFailed, $"An open of {pending} is already in progress.");

        var accepted = false;
        try
        {
            // ADR-0030 item 5: the check holds the file as a journal does, so a file that another
            // program holds fails it (V31). The file that this desktop holds is the exception: the
            // host disposes the session of that file before the next session holds it.
            using var backend = new ManifoldGeometryBackend();
            if (await DocumentFile.Check(path, backend, hold: !SameFile(path, HeldFile?.Invoke()), ct) is { } error)
                return error;

            Volatile.Write(ref _requested, path);
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
