using System.Text;
using System.Text.RegularExpressions;
using Engine.Tests.Diagnostics;

namespace Engine.Tests.Governance;

// Shared helpers for each governance gate. A gate reads a document in the
// repository, therefore each gate needs the repository root, a small
// front-matter reader, the task files and the path patterns of a write set.
internal static class RepositoryFiles
{
    public static string Root => DiagnosticsScanner.FindRepoRoot(AppContext.BaseDirectory);

    public static string Path(params string[] parts)
        => System.IO.Path.Combine(new[] { Root }.Concat(parts).ToArray());

    public static string Read(params string[] parts) => File.ReadAllText(Path(parts));

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

    // One task file with front matter. Task files 0001 to 0013 carry none and
    // are not returned; WriteSetGateTests bounds their count.
    public sealed record TaskRecord(
        string File,
        string Id,
        string Status,
        Dictionary<string, string> Fields,
        IReadOnlyList<string> Create,
        IReadOnlyList<string> Modify,
        IReadOnlyList<string> Forbid)
    {
        public IEnumerable<string> Written => Create.Concat(Modify);
    }

    public static IEnumerable<string> TaskFiles()
        => Directory.EnumerateFiles(Path("tasks"), "TASK-*.md").OrderBy(f => f, StringComparer.Ordinal);

    public static List<TaskRecord> Tasks()
    {
        var tasks = new List<TaskRecord>();

        foreach (var file in TaskFiles())
        {
            var text = File.ReadAllText(file);
            var fields = ReadFrontMatter(text);
            if (fields is null)
                continue;

            var (create, modify, forbid) = ReadWrites(text);

            tasks.Add(new TaskRecord(
                file,
                fields.TryGetValue("id", out var id) ? $"TASK-{id}" : System.IO.Path.GetFileName(file),
                fields.TryGetValue("status", out var status) ? status : string.Empty,
                fields,
                create,
                modify,
                forbid));
        }

        return tasks;
    }

    // ReadFrontMatter is flat, and the writes block has two levels. This reader
    // handles that one shape and nothing else, in the same spirit: it is not a
    // YAML parser.
    public static (List<string> Create, List<string> Modify, List<string> Forbid) ReadWrites(string text)
    {
        List<string> create = [], modify = [], forbid = [];
        List<string>? current = null;
        var inWrites = false;

        foreach (var raw in text.Split('\n').Skip(1))
        {
            var line = raw.TrimEnd('\r');

            if (line.StartsWith("---", StringComparison.Ordinal))
                break;

            if (line.StartsWith("writes:", StringComparison.Ordinal))
            {
                inWrites = true;
                continue;
            }

            if (!inWrites)
                continue;

            // A key at the left margin ends the block.
            if (line.Length > 0 && !char.IsWhiteSpace(line[0]))
                break;

            var trimmed = line.Trim();

            if (trimmed is "create:" or "modify:" or "forbid:")
            {
                current = trimmed switch
                {
                    "create:" => create,
                    "modify:" => modify,
                    _ => forbid,
                };
                continue;
            }

            if (current is not null && trimmed.StartsWith("- ", StringComparison.Ordinal))
                current.Add(trimmed[2..].Trim().Trim('\'', '"'));
        }

        return (create, modify, forbid);
    }

    // A path pattern, in the form that docs/templates.md uses. Two stars match
    // each character. One star matches each character except the separator.
    public static Regex PathPattern(string pattern)
    {
        var expression = new StringBuilder("^");

        for (var i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == '*')
            {
                if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                {
                    expression.Append(".*");
                    i++;
                }
                else
                {
                    expression.Append("[^/]*");
                }

                continue;
            }

            expression.Append(Regex.Escape(pattern[i].ToString()));
        }

        return new Regex(expression.Append('$').ToString(), RegexOptions.Compiled);
    }

    // Two patterns intersect when one of them matches a sample path of the
    // other. A sample replaces each star with a name. This is enough for the
    // shapes that a write set and an affects field use.
    public static bool PatternsIntersect(string first, string second)
        => PathPattern(first).IsMatch(Sample(second)) || PathPattern(second).IsMatch(Sample(first));

    private static string Sample(string pattern)
        => pattern.Replace("**", "x/x", StringComparison.Ordinal).Replace("*", "x", StringComparison.Ordinal);
}
