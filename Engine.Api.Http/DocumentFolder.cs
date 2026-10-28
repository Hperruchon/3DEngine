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

    // The path in the folder of a plain file name, or null with the reason. A name has
    // no folder part, no drive, no control character, and is not . or ..; the host adds
    // the extension .3de when it is absent.
    public string? Resolve(string? name, out string? refusal)
    {
        refusal = name switch
        {
            null or "" => "A name is required.",
            "." or ".." => $"The name '{name}' is not a file name.",
            _ when name.Any(c => c is '/' or '\\' or ':' || char.IsControl(c)) =>
                $"The name '{name}' has a folder part, a drive or a control character; a name is a plain file name in the document folder.",
            _ => null,
        };
        if (refusal is not null)
            return null;

        var file = name!.EndsWith(DocumentFile.Extension, StringComparison.OrdinalIgnoreCase) ? name : name + DocumentFile.Extension;
        var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(FullPath, file));
        if (!string.Equals(System.IO.Path.GetDirectoryName(path), FullPath, StringComparison.Ordinal))
        {
            refusal = $"The name '{name}' leaves the document folder.";
            return null;
        }

        return path;
    }
}
