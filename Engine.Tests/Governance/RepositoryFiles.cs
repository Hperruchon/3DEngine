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

    public static IEnumerable<string> TaskFiles() => WriteSetCheck.TaskFiles.TaskFilePaths(Root);

    public static List<TaskRecord> Tasks() => WriteSetCheck.TaskFiles.Tasks(Root);

    public static TaskRecord? ParseTask(string file, string text) => WriteSetCheck.TaskFiles.ParseTask(file, text);

    public static Regex PathPattern(string pattern) => WriteSetCheck.TaskFiles.PathPattern(pattern);

    public static bool PatternsIntersect(string first, string second) => WriteSetCheck.TaskFiles.PatternsIntersect(first, second);
}
