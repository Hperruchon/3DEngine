using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Engine.Tests.Governance;

// The gate for the `writes` block of a task file. Register entry R-0017
// recorded the gap: each task declares a create list, a modify list and a forbid
// list, and nothing read that block. An agent could change a forbidden file and
// no check found the error. The declaration was a wish and not a rule.
//
// The gate has two parts.
//
//   - Each static test always runs. It reads every task file and verifies that
//     the declaration is well formed and that it agrees with the working tree.
//   - The dynamic test runs when the environment variable WRITE_SET_FILES gives
//     a list of changed paths, one per line. Continuous integration sets that
//     variable from `git diff --name-only`. The test then verifies that each
//     changed file is inside the write set of a task that the same change
//     touches.
//
// One implementation serves both. A person can run the dynamic part locally by
// setting the variable, and the logic that continuous integration uses is the
// logic that `dotnet test` covers.
public class WriteSetGateTests
{
    // Task files 0001 to 0013 predate docs/templates.md and carry no front
    // matter. The gate exempts them and fails when the count grows, in the same
    // way as the budget for an unenforced ADR. A new task must declare its
    // write set.
    private const int TasksWithoutFrontMatterBudget = 13;

    private sealed record TaskWriteSet(
        string File,
        string Id,
        string Status,
        IReadOnlyList<string> Create,
        IReadOnlyList<string> Modify,
        IReadOnlyList<string> Forbid)
    {
        public IEnumerable<string> Written => Create.Concat(Modify);
    }

    [Fact]
    public void The_Count_Of_Tasks_Without_A_Write_Set_Does_Not_Grow()
    {
        var bare = TaskFiles()
            .Where(f => RepositoryFiles.ReadFrontMatter(File.ReadAllText(f)) is null)
            .Select(RepositoryFiles.Relative)
            .ToArray();

        Assert.True(
            bare.Length <= TasksWithoutFrontMatterBudget,
            $"The count of task files with no front matter is {bare.Length} and the budget is "
            + $"{TasksWithoutFrontMatterBudget}. A new task must declare a write set. See "
            + $"docs/templates.md, section 3. Files:\n  {string.Join("\n  ", bare)}");
    }

    [Fact]
    public void Every_Task_With_Front_Matter_Declares_A_Write_Set()
    {
        var problems = Parse()
            .Where(t => t.Create.Count == 0 && t.Modify.Count == 0)
            .Select(t => $"{t.Id}: the writes block names no file to create and no file to modify")
            .ToArray();

        Assert.True(problems.Length == 0, string.Join("\n  ", problems));
    }

    [Fact]
    public void No_Task_Forbids_A_Path_That_It_Also_Writes()
    {
        // A declaration that contradicts itself gives no protection. The gate
        // must refuse it, otherwise the forbid list means nothing.
        var problems = new List<string>();

        foreach (var task in Parse())
        {
            var forbidden = task.Forbid.Select(ToRegex).ToArray();

            foreach (var path in task.Written)
            {
                if (forbidden.Any(rule => rule.IsMatch(path)))
                    problems.Add($"{task.Id}: the block writes '{path}' and also forbids it");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
    }

    [Fact]
    public void Every_Declared_Path_Uses_The_Forward_Slash_And_No_Leading_Slash()
    {
        var problems = new List<string>();

        foreach (var task in Parse())
        {
            foreach (var path in task.Written.Concat(task.Forbid))
            {
                if (path.Contains('\\'))
                    problems.Add($"{task.Id}: '{path}' uses a backslash");

                if (path.StartsWith('/'))
                    problems.Add($"{task.Id}: '{path}' starts with a slash; give a path from the root");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
    }

    [Fact]
    public void Each_Exact_Path_That_A_Done_Task_Creates_Exists()
    {
        // A task with the status Done claims that it made each file in its
        // create list. This test finds a claim that the working tree denies.
        // A pattern is skipped, because a pattern names a set and not a file.
        var problems = new List<string>();

        foreach (var task in Parse().Where(t => t.Status == "Done"))
        {
            foreach (var path in task.Create.Where(p => !p.Contains('*')))
            {
                if (!File.Exists(RepositoryFiles.Path(path.Split('/'))))
                    problems.Add($"{task.Id}: the task is Done and claims to create '{path}', which is absent");
            }
        }

        Assert.True(
            problems.Count == 0,
            "A task with the status Done names a file that it did not create. Correct the write set "
            + "or the status.\n  " + string.Join("\n  ", problems));
    }

    [Fact]
    public void Every_Changed_File_Is_Inside_The_Write_Set_Of_A_Changed_Task()
    {
        var changed = ChangedFiles();
        if (changed is null)
            return; // Continuous integration supplies the list. See the class comment.

        // Only a task that this change touches can govern this change. A task
        // from an older change must not authorise a file today.
        var governing = Parse()
            .Where(t => changed.Contains(RepositoryFiles.Relative(t.File), StringComparer.Ordinal))
            .ToArray();

        Assert.True(
            governing.Length > 0,
            "This change touches no task file, therefore no write set governs it. CLAUDE.md, section "
            + "\"Anti-patterns\", says: do not do work outside the scope of the active task. Add the "
            + "task file to the change.\n  Changed files:\n  " + string.Join("\n  ", changed));

        var permitted = governing.SelectMany(t => t.Written).Select(ToRegex).ToArray();
        var refused = governing
            .SelectMany(t => t.Forbid.Select(pattern => (t.Id, Rule: ToRegex(pattern), pattern)))
            .ToArray();

        var problems = new List<string>();

        foreach (var file in changed)
        {
            // A permit beats a forbid. A forbid binds the task that declares it
            // and no other task, because "Engine.Cli/** forbidden" on the
            // persistence task means that the persistence work must stay out of
            // the command line, and not that nobody may touch it.
            //
            // TASK-0022 had the opposite rule and called the strict reading the
            // safe one. That was wrong, and it only looked right because every
            // change tested against it carried one task. TASK-0026 corrected it
            // after the v0.20 commit failed: TASK-0018 modified
            // Engine.Cli/Cli.cs and declared it, and the forbid list of the
            // deferred TASK-0019 blocked that declaration.
            if (permitted.Any(rule => rule.IsMatch(file)))
                continue;

            // The file is in no list. A forbid that matches it gives the better
            // message, because it names the boundary that the author wrote.
            var blocked = refused.FirstOrDefault(r => r.Rule.IsMatch(file));

            problems.Add(blocked.Rule is not null
                ? $"{file} matches the forbid pattern '{blocked.pattern}' of {blocked.Id}, "
                    + "and no task in this change permits it"
                : $"{file} is in no create list and in no modify list");
        }

        Assert.True(
            problems.Count == 0,
            "Register entry R-0017: the writes block of a task must govern each changed file. Add the "
            + "path to the create list or the modify list, or do not change the file. Governing "
            + $"tasks: {string.Join(", ", governing.Select(t => t.Id))}. Problems:\n  "
            + string.Join("\n  ", problems));
    }

    // The list of changed paths, one per line, relative to the repository root
    // and with the forward slash. Null when the variable is absent.
    private static HashSet<string>? ChangedFiles()
    {
        var raw = Environment.GetEnvironmentVariable("WRITE_SET_FILES");
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // A rename can arrive as "old -> new", which `git status --porcelain`
        // writes and which a person pastes. Both sides are a change and the
        // write set must cover both, therefore the gate splits the arrow.
        // `git diff --name-only` gives one path per line and passes through.
        var files = raw
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(line => line.Split(" -> ", StringSplitOptions.TrimEntries))
            .Where(path => path.Length > 0)
            .Select(path => path.Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);

        return files.Count == 0 ? null : files;
    }

    // A path pattern, in the form that docs/templates.md uses. Two stars match
    // each character. One star matches each character except the separator.
    private static Regex ToRegex(string pattern)
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

    private static IEnumerable<string> TaskFiles()
        => Directory.EnumerateFiles(RepositoryFiles.Path("tasks"), "TASK-*.md").OrderBy(f => f, StringComparer.Ordinal);

    private static List<TaskWriteSet> Parse()
    {
        var tasks = new List<TaskWriteSet>();

        foreach (var file in TaskFiles())
        {
            var text = File.ReadAllText(file);
            var fields = RepositoryFiles.ReadFrontMatter(text);
            if (fields is null)
                continue;

            var (create, modify, forbid) = ReadWrites(text);

            tasks.Add(new TaskWriteSet(
                file,
                fields.TryGetValue("id", out var id) ? $"TASK-{id}" : Path.GetFileName(file),
                fields.TryGetValue("status", out var status) ? status : string.Empty,
                create,
                modify,
                forbid));
        }

        return tasks;
    }

    // RepositoryFiles.ReadFrontMatter is flat, and the writes block has two
    // levels. This reader handles that one shape and nothing else, in the same
    // spirit: it is not a YAML parser.
    private static (List<string> Create, List<string> Modify, List<string> Forbid) ReadWrites(string text)
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
}
