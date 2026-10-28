using Engine.Contracts;
using Engine.Core.Persistence;

namespace Engine.Api.Http;

// ADR-0030 item 2: the one folder where a save and an open by name go, and the
// function of a host that can start itself again with another file (item 4). A host
// registers its own folder before AddEngineSurface; with none, the surface reads the
// setting Engine:DocumentFolder, or takes the default.
public sealed class DocumentFolder
{
    public DocumentFolder(string path, Func<string, CancellationToken, Task<ErrorDetail?>>? reopen = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        // A folder with a final separator refused each name until TASK-0099, because
        // GetDirectoryName gives no final separator (finding E79 of the second codebase
        // review of 2026-10-28).
        FullPath = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(path));
        Reopen = reopen;
    }

    // <documents of the account>/3DEngine.
    public static string Default
        => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "3DEngine");

    public string FullPath { get; }

    // Checks the file at the path and starts the host again with it, or gives the
    // error; null on a host that cannot start itself again (engine-api-http).
    public Func<string, CancellationToken, Task<ErrorDetail?>>? Reopen { get; }

    // ADR-0030 item 4: true after an open that the host accepted. The surface then refuses
    // each command, save and open with HTTP 503, because the host starts again. Until
    // TASK-0099 a command after the reply 202 changed the old document (finding V37 of the
    // second codebase review of 2026-10-28).
    public bool OpenAccepted => Volatile.Read(ref _openAccepted);

    private bool _openAccepted;

    // Checks the file and starts the host again with it (Reopen), and records an accepted
    // open. Each open of a host, by HTTP or by its window, goes through here.
    public async Task<ErrorDetail?> Open(string path, CancellationToken ct)
    {
        if (Reopen is not { } reopen)
            throw new InvalidOperationException("This host cannot start itself again, so it does not open a file.");

        var error = await reopen(path, ct).ConfigureAwait(false);
        if (error is null)
            Volatile.Write(ref _openAccepted, true);
        return error;
    }

    // ADR-0030 item 2: a file name has at most 255 characters on each file system.
    public const int MaximumFileNameLength = 255;

    // The names that Windows keeps for a device, with or without an extension.
    private static readonly HashSet<string> ReservedNames = new(
        ["CON", "PRN", "AUX", "NUL", .. Enumerable.Range(1, 9).Select(n => $"COM{n}"), .. Enumerable.Range(1, 9).Select(n => $"LPT{n}")],
        StringComparer.OrdinalIgnoreCase);

    // The path in the folder of a plain file name, or null with the reason. A name has
    // no folder part, no drive, no control character, and is not . or ..; the host adds
    // the extension .3de when it is absent. The same rules hold on each operating system,
    // so that a file can move between them (ADR-0030 item 2): no character < > " | ? *, no
    // reserved name of Windows, no final space or dot, a name before .3de, and a file name
    // of 255 characters at most. Until TASK-0099 a save made "CON" and "a " on Windows, and
    // the other characters gave HTTP 409 (finding E81 of the second codebase review of
    // 2026-10-28).
    public string? Resolve(string? name, out string? refusal)
    {
        refusal = name switch
        {
            null or "" => "A name is required.",
            "." or ".." => $"The name '{name}' is not a file name.",
            _ when name.Any(c => c is '/' or '\\' or ':' || char.IsControl(c)) =>
                $"The name '{name}' has a folder part, a drive or a control character; a name is a plain file name in the document folder.",
            _ when name.Any(c => c is '<' or '>' or '"' or '|' or '?' or '*') =>
                $"The name '{name}' has one of the characters < > \" | ? *, which a file name cannot have on Windows.",
            _ => null,
        };
        if (refusal is not null)
            return null;

        var file = name!.EndsWith(DocumentFile.Extension, StringComparison.OrdinalIgnoreCase) ? name : name + DocumentFile.Extension;
        var stem = file[..^DocumentFile.Extension.Length];
        refusal = stem switch
        {
            "" => $"The name '{name}' has no name before {DocumentFile.Extension}.",
            // A name with a final space or dot has no extension, so its stem is the name.
            _ when stem.EndsWith(' ') || stem.EndsWith('.') => $"The name '{name}' ends with a space or a dot, which Windows removes from a file name.",
            _ when ReservedNames.Contains(stem.Split('.')[0].TrimEnd(' ')) => $"The name '{name}' is a name that Windows keeps for a device.",
            _ when file.Length > MaximumFileNameLength => $"The file name has {file.Length} characters, and a file name has {MaximumFileNameLength} at most.",
            _ => null,
        };
        if (refusal is not null)
            return null;

        var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(FullPath, file));
        if (!string.Equals(System.IO.Path.GetDirectoryName(path), FullPath, StringComparison.Ordinal))
        {
            refusal = $"The name '{name}' leaves the document folder.";
            return null;
        }

        return path;
    }
}
