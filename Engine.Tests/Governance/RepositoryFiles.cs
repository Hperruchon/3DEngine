using Engine.Tests.Diagnostics;

namespace Engine.Tests.Governance;

// Shared helpers for each governance gate. A gate reads a document in the
// repository, therefore each gate needs the repository root and a small
// front-matter reader.
internal static class RepositoryFiles
{
    public static string Root => DiagnosticsScanner.FindRepoRoot(AppContext.BaseDirectory);

    public static string Path(params string[] parts)
        => System.IO.Path.Combine(new[] { Root }.Concat(parts).ToArray());

    public static string Read(params string[] parts) => File.ReadAllText(Path(parts));

    // Each tracked text file that a governance rule applies to. The gate reads
    // the working tree, not git, so it needs no process and no repository state.
    public static IEnumerable<string> TrackedTextFiles()
    {
        string[] roots =
        [
            "Engine.Contracts", "Engine.Core", "Engine.Cli", "Engine.Api.Http",
            "Engine.Geometry.Manifold", "Engine.Tests", "docs", "tasks", "eng",
        ];

        string[] extensions = [".cs", ".md", ".csproj", ".json", ".yml"];

        foreach (var relative in roots)
        {
            var directory = Path(relative);
            if (!Directory.Exists(directory))
                continue;

            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                if (file.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}")
                    || file.Contains($"{System.IO.Path.DirectorySeparatorChar}bin{System.IO.Path.DirectorySeparatorChar}"))
                    continue;

                if (extensions.Contains(System.IO.Path.GetExtension(file)))
                    yield return file;
            }
        }

        foreach (var name in new[] { "CLAUDE.md", "global.json", "nuget.config" })
        {
            var file = Path(name);
            if (File.Exists(file))
                yield return file;
        }
    }

    public static string Relative(string absolute)
        => System.IO.Path.GetRelativePath(Root, absolute).Replace('\\', '/');

    // A minimal front-matter reader. It handles a scalar, an inline list and a
    // block list, which is every shape that docs/templates.md uses. It is
    // deliberately not a YAML parser.
    public static Dictionary<string, string>? ReadFrontMatter(string text)
    {
        if (!text.StartsWith("---", StringComparison.Ordinal))
            return null;

        var lines = text.Split('\n');
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        string? currentKey = null;
        var blockItems = new List<string>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            if (line.StartsWith("---", StringComparison.Ordinal))
            {
                if (currentKey is not null)
                    fields[currentKey] = string.Join(",", blockItems);
                return fields;
            }

            if (line.StartsWith("  - ", StringComparison.Ordinal))
            {
                blockItems.Add(line[4..].Trim());
                continue;
            }

            if (currentKey is not null)
            {
                fields[currentKey] = string.Join(",", blockItems);
                currentKey = null;
                blockItems = [];
            }

            var colon = line.IndexOf(':');
            if (colon <= 0)
                continue;

            var key = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();

            if (value.Length == 0)
            {
                currentKey = key;
                blockItems = [];
            }
            else
            {
                fields[key] = value;
            }
        }

        return fields;
    }

    // Reads an inline list such as ['0004'] or [] into its members.
    public static IReadOnlyList<string> ParseInlineList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "[]")
            return [];

        return value.Trim('[', ']')
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Trim('\'', '"'))
            .Where(item => item.Length > 0)
            .ToArray();
    }
}
