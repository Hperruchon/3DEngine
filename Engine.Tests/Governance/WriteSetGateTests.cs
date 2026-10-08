using WriteSetCheck;
using Xunit;

namespace Engine.Tests.Governance;

// The static gate for the `writes` block of a task file. Register entry R-0017
// recorded the gap: each task declares a create list, a modify list and a forbid
// list, and nothing read that block.
//
// Each test here reads every task file and verifies that the declaration is well
// formed and that it agrees with the working tree. The judge of each change, the
// rule that each changed file is inside the write set of its task, is the program
// eng/write-set-check, which the pipeline builds from main (TASK-0053). Until
// that task the judge was a test in this class, and a file under Engine.Tests/
// could switch it off for its own commit (codebase review of 2026-10-08, finding
// T14). WriteSetJudgeTests tests the rules of the judge.
public class WriteSetGateTests
{
    // Task files 0001 to 0013 predate docs/templates.md and carry no front
    // matter. The gate exempts them and fails when the count grows, in the same
    // way as the budget for an unenforced ADR. A new task must declare its
    // write set.
    private const int TasksWithoutFrontMatterBudget = 13;

    [Fact]
    public void The_Cut_Off_Commit_Is_The_One_That_Turned_The_Gate_On()
    {
        var value = RepositoryFiles.Read("eng", "write-set-cutoff.txt")
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .ToArray();

        Assert.True(
            value.SequenceEqual([Judge.CutOff]),
            $"eng/write-set-cutoff.txt must hold the one commit {Judge.CutOff}. A commit that moves the "
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
}
