using System.Diagnostics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core;
using Engine.Core.Persistence;
using Engine.Geometry.Manifold;

namespace ThreeDEngine.Desktop;

// ADR-0030 items 3 to 5 (TASK-0092): the document folder of the desktop, and an open
// that starts the program again. An open checks the file in a scratch session with its
// own backend first, so a file that does not open leaves the person with the window.
// Then the window loop ends, the surface frees the port, and the program starts again
// with --file and the same port, so an agent connects again to the same address.
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

    private async Task<ErrorDetail?> Reopen(string path, CancellationToken ct)
    {
        if (RequestedOpen is not null)
            return new ErrorDetail(DiagnosticCodes.IoLoadFailed, $"An open of {RequestedOpen} is already in progress.");

        using var backend = new ManifoldGeometryBackend();
        if (await DocumentFile.Check(path, backend, ct) is { } error)
            return error;

        Volatile.Write(ref _requested, path);
        return null;
    }

    // The arguments of the program with --file set to the path; each other argument
    // stays, the port too.
    public static string[] Arguments(IReadOnlyList<string> args, string path)
    {
        var kept = new List<string>();
        for (var index = 0; index < args.Count; index++)
        {
            if (args[index] == "--file")
            {
                index++;
                continue;
            }

            if (args[index].StartsWith("--file=", StringComparison.Ordinal))
                continue;

            kept.Add(args[index]);
        }

        kept.Add("--file");
        kept.Add(path);
        return [.. kept];
    }

    // Starts the program again with the arguments. A program that runs as
    // "dotnet 3DEngine.dll" starts the same way.
    public static void StartAgain(IReadOnlyList<string> args, string path)
    {
        var program = Environment.ProcessPath ?? throw new InvalidOperationException("The path of the program is not known.");
        var start = new ProcessStartInfo(program) { UseShellExecute = false };
        if (string.Equals(Path.GetFileNameWithoutExtension(program), "dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
        foreach (var argument in Arguments(args, path))
            start.ArgumentList.Add(argument);
        Process.Start(start)?.Dispose();
    }
}
