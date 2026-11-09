using System.Text.RegularExpressions;
using Engine.Tests.Diagnostics;
using WriteSetCheck;

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

    // The reader of the task files and of the path patterns lives in the judge of
    // the write set, so that the judge and the gates read a write set in one way
    // (TASK-0053). These members keep the names that each gate uses.
    public static Dictionary<string, string>? ReadFrontMatter(string text) => WriteSetCheck.TaskFiles.ReadFrontMatter(text);

    public static IReadOnlyList<string> ParseInlineList(string? value) => WriteSetCheck.TaskFiles.ParseInlineList(value);

    // Finding T5 of the codebase review of 2026-09-30 (R-0031, TASK-0320): the two
    // readers of the field affects split the value at each comma. A pattern kept the
    // quotes of a block-list item or the brackets of an inline list, and it matched no
    // path, so the ADR governed no task with no error. Until TASK-0320 each reader had
    // its own split. The patterns now come from ParseInlineList, which takes off the
    // quotes and the brackets as it does for the other list fields, and a quote or a
    // bracket that stays in a pattern is an error.
    public static IReadOnlyList<string> AffectsPatterns(string? value, string source)
    {
        var patterns = ParseInlineList(value);
        var wrong = patterns.FirstOrDefault(pattern => pattern.IndexOfAny(['\'', '"', '[', ']']) >= 0);
        if (wrong is not null)
            throw new InvalidOperationException($"{source}: the affects pattern {wrong} holds a quote or a bracket and matches no path. Write each pattern with no quote and no bracket.");

        return patterns;
    }

    public static IEnumerable<string> TaskFiles() => WriteSetCheck.TaskFiles.TaskFilePaths(Root);

    public static List<TaskRecord> Tasks() => WriteSetCheck.TaskFiles.Tasks(Root);

    public static TaskRecord? ParseTask(string file, string text) => WriteSetCheck.TaskFiles.ParseTask(file, text);

    public static Regex PathPattern(string pattern) => WriteSetCheck.TaskFiles.PathPattern(pattern);

    public static bool PatternsIntersect(string first, string second) => WriteSetCheck.TaskFiles.PatternsIntersect(first, second);
}
