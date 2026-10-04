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
//     variable from `git diff-tree`, and it sets WRITE_SET_TASK from the
//     trailer line of the commit message, of the form TASK-nnnn. The test then
//     verifies that each changed file is inside the write set of that task.
//
// One implementation serves both. A person runs the dynamic part locally by
// setting both variables, and the logic that continuous integration uses is the
// logic that `dotnet test` covers.
//
// Three rules came from the codebase review of 2026-09-30, finding T3. A commit
// that names a task also changes the file of that task, so a closed task cannot
// lend its permits to a later commit. A gate file must be named exactly, so a
// pattern such as Engine.Tests/** does not permit a change to a gate. The
// cut-off commit is held here, so a commit cannot move it.
public class WriteSetGateTests
{
    // Task files 0001 to 0013 predate docs/templates.md and carry no front
    // matter. The gate exempts them and fails when the count grows, in the same
    // way as the budget for an unenforced ADR. A new task must declare its
    // write set.
    private const int TasksWithoutFrontMatterBudget = 13;

    // TASK-0026 turned the gate on at this commit. eng/write-set-cutoff.txt
    // gives the same value to the workflow. The two must agree, so that a
    // commit cannot move the cut-off past itself.
    private const string CutOff = "0609f13070d6917ee80f3aa14ecb553972b5efcf";

    [Fact]
    public void The_Cut_Off_Commit_Is_The_One_That_Turned_The_Gate_On()
    {
        var value = RepositoryFiles.Read("eng", "write-set-cutoff.txt")
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .ToArray();

        Assert.True(
            value.SequenceEqual([CutOff]),
            $"eng/write-set-cutoff.txt must hold the one commit {CutOff}. A commit that moves the "
            + "cut-off exempts itself and each commit before it. Found: " + string.Join(", ", value));
    }

    [Fact]
    public void The_Count_Of_Tasks_Without_A_Write_Set_Does_Not_Grow()
    {
        var bare = RepositoryFiles.TaskFiles()
            .Where(f => RepositoryFiles.ReadFrontMatter(File.ReadAllText(f)) is null)
            .Select(RepositoryFiles.Relative)
            .ToArray();

        Assert.True(
            bare.Length <= TasksWithoutFrontMatterBudget,
            $"The count of task files with no front matter is {bare.Length} and the budget is "
            + $"{TasksWithoutFrontMatterBudget}. A new task must declare a write set. See "
            + $"docs/templates.md, section 2. Files:\n  {string.Join("\n  ", bare)}");
    }

    [Fact]
    public void Every_Task_With_Front_Matter_Declares_A_Write_Set()
    {
        var problems = RepositoryFiles.Tasks()
            .Where(t => t.Create.Count == 0 && t.Modify.Count == 0)
            .Select(t => $"{t.Id}: the writes block names no file to create and no file to modify")
            .ToArray();

        Assert.True(problems.Length == 0, string.Join("\n  ", problems));
    }

    [Fact]
    public void No_Task_Forbids_A_Path_That_It_Also_Writes()
    {
        // A declaration that contradicts itself gives no protection. The gate
        // must refuse it, otherwise the forbid list means nothing. Two patterns
        // intersect when one matches a sample of the other, so a wide permit
        // over a narrow forbid is refused too.
        var problems = new List<string>();

        foreach (var task in RepositoryFiles.Tasks())
        {
            // An open task must not permit what it forbids, in any form. A closed
            // task is a record and keeps the older, literal rule.
            var open = task.Status is "Ready" or "Active";

            foreach (var path in task.Written)
            {
                foreach (var forbid in task.Forbid)
                {
                    var conflict = open
                        ? RepositoryFiles.PatternsIntersect(forbid, path)
                        : RepositoryFiles.PathPattern(forbid).IsMatch(path);

                    if (conflict)
                        problems.Add($"{task.Id}: the block writes '{path}' and forbids '{forbid}', which intersect");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
    }

    [Fact]
    public void Every_Declared_Path_Uses_The_Forward_Slash_And_No_Leading_Slash()
    {
        var problems = new List<string>();

        foreach (var task in RepositoryFiles.Tasks())
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

        foreach (var task in RepositoryFiles.Tasks().Where(t => t.Status == "Done"))
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
    public void Every_Changed_File_Is_Inside_The_Write_Set_Of_The_Governing_Task()
    {
        var changed = ChangedFiles();
        if (changed is null)
            return; // Continuous integration supplies the list. See the class comment.

        var governing = GoverningTask(changed);

        var permitted = governing.Written.Select(RepositoryFiles.PathPattern).ToArray();
        var refused = governing.Forbid.Select(pattern => (Rule: RepositoryFiles.PathPattern(pattern), pattern)).ToArray();

        var problems = new List<string>();

        foreach (var file in changed)
        {
            // A gate file is named exactly or not at all. A pattern that covers
            // the tests must not let a task change the gate that reads it.
            if (IsGateFile(file))
            {
                if (!governing.Written.Contains(file, StringComparer.Ordinal))
                    problems.Add($"{file} is a gate file, and {governing.Id} does not name it exactly; a pattern does not permit a gate file");
                continue;
            }

            // A permit beats a forbid inside one task, because a task that both
            // permits and forbids a path is refused by a static test above. A
            // forbid gives the better message when a file is in no list,
            // because it names the boundary that the author wrote.
            if (permitted.Any(rule => rule.IsMatch(file)))
                continue;

            var blocked = refused.FirstOrDefault(r => r.Rule.IsMatch(file));

            problems.Add(blocked.Rule is not null
                ? $"{file} matches the forbid pattern '{blocked.pattern}' of {governing.Id}"
                : $"{file} is in no create list and in no modify list of {governing.Id}");
        }

        Assert.True(
            problems.Count == 0,
            "Register entry R-0017: the writes block of the governing task must cover each changed "
            + "file. Add the path to the create list or the modify list, or do not change the file. "
            + $"Governing task: {governing.Id}. Problems:\n  " + string.Join("\n  ", problems));
    }

    // A file under Engine.Tests/Governance/, or a test class whose name ends
    // in GateTests, or the shared helpers of the gates. Also each file that can
    // turn a gate off without a change to a gate: the project file of the
    // tests, which can remove a gate from the compilation; a Directory.Build or
    // Directory.Packages file, which MSBuild reads for each project below it;
    // the cut-off of this gate; and each workflow (codebase review of
    // 2026-10-04, finding T9).
    private static bool IsGateFile(string file)
    {
        var name = file[(file.LastIndexOf('/') + 1)..];

        return file.StartsWith("Engine.Tests/Governance/", StringComparison.Ordinal)
            || (file.StartsWith("Engine.Tests/", StringComparison.Ordinal) && file.EndsWith("GateTests.cs", StringComparison.Ordinal))
            || file.StartsWith("Engine.Tests/Diagnostics/", StringComparison.Ordinal)
            || file == "Engine.Tests/Engine.Tests.csproj"
            || name.StartsWith("Directory.Build.", StringComparison.Ordinal)
            || name == "Directory.Packages.props"
            || file == "eng/write-set-cutoff.txt"
            || file.StartsWith(".github/workflows/", StringComparison.Ordinal);
    }

    // The task that governs a change. The commit message names it, as a trailer
    // line of the form TASK-nnnn, and continuous integration passes the name
    // in WRITE_SET_TASK. The commit also changes the file of that task, so a
    // closed task cannot lend its permits to a later commit. When no name is
    // given, the one task file that the change touches governs it. A change
    // that touches several task files and names none is refused: register
    // entry R-0026 recorded that such a change received the permits of each
    // task, so a commit that planned three tasks could also change
    // Engine.Core/CommandBus.cs with no report.
    private static RepositoryFiles.TaskRecord GoverningTask(HashSet<string> changed)
    {
        var tasks = RepositoryFiles.Tasks();
        var named = Environment.GetEnvironmentVariable("WRITE_SET_TASK")?.Trim();

        if (!string.IsNullOrEmpty(named))
        {
            var task = tasks.FirstOrDefault(t => t.Id == named);
            Assert.True(
                task is not null,
                $"The commit names {named}, and no task file with a write set has that identifier. "
                + "Name a task with front matter, or correct the identifier.");

            Assert.True(
                changed.Contains(RepositoryFiles.Relative(task!.File), StringComparer.Ordinal),
                $"The commit names {named} and does not change its file. A commit records its progress "
                + "in the task that governs it, with one line under \"Progress\" at least, so that a "
                + "closed task cannot lend its permits to a later commit.");

            return task;
        }

        var touched = tasks
            .Where(t => changed.Contains(RepositoryFiles.Relative(t.File), StringComparer.Ordinal))
            .ToArray();

        Assert.True(
            touched.Length > 0,
            "This change names no task and touches no task file, therefore no write set governs it. "
            + "Name the task in the commit message, as a trailer line of the form TASK-nnnn, or add the "
            + "task file to the change.\n  Changed files:\n  " + string.Join("\n  ", changed));

        Assert.True(
            touched.Length == 1,
            "This change touches several task files and names none. Only one task governs a commit. "
            + "Name it in the commit message, as a trailer line of the form TASK-nnnn. Touched: "
            + string.Join(", ", touched.Select(t => t.Id)));

        return touched[0];
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
}
