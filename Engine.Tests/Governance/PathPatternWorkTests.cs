using WriteSetCheck;
using Xunit;

namespace Engine.Tests.Governance;

// The work of the path patterns, as a count and not as a time (TASK-0442, R-0074, ADR-0173
// item 4). Until TASK-0442 TaskFiles.PathPattern compiled a new expression at each call.
// The gate of governed-by compiled tens of thousands of expressions and took 38 to 100
// seconds on the computer of the owner, near the hang limit of 2 minutes of the step "Test".
// The counter of TaskFiles counts the expressions of one thread only, so a test of another
// class that runs at the same time does not change the count. No bound reads a file of the
// repository a second time, so a change of a file during the run does not decide a result
// (TASK-0455).
public class PathPatternWorkTests
{
    [Fact]
    public void A_Pattern_Gives_One_Expression_That_Is_Built_Once()
    {
        // A pattern that only this call uses, so that this thread builds it. Until TASK-0455 the
        // pattern was the same at each call, and a second call of the test on another thread at
        // the same time could build it first.
        var pattern = $"eng/path-pattern-work-probe/{Guid.NewGuid():N}/*.cs";
        var before = TaskFiles.PatternsBuiltOnThisThread;

        var first = TaskFiles.PathPattern(pattern);
        var second = TaskFiles.PathPattern(pattern);

        Assert.Same(first, second);
        Assert.Equal(1, TaskFiles.PatternsBuiltOnThisThread - before);
    }

    [Fact]
    public void The_Gate_Of_Governed_By_Builds_At_Most_One_Expression_For_Each_Pattern()
    {
        // This thread builds an expression only for a pattern that the cache does not keep, and
        // the cache keeps that pattern before the call returns. So the expressions of this thread
        // are at most the new patterns of the cache. A test of another class that runs at the
        // same time can only add patterns. Until TASK-0455 the bound was the patterns of the ADR
        // and task files, read a second time before the gate: a change of a file during the run
        // gave the gate one pattern more than the bound, and the test failed once.
        //
        // TASK-0461 (finding T195 of the codebase review of 2026-11-23): each build goes into the cache, so a cache with a key
        // for each call or for each pair of a pattern and a path gave built == added and passed, and a warm cache passed with 0
        // and 0. So the expressions of this thread are also at most the distinct patterns that the gate gives to PathPattern in
        // this run, which PathPattern records in the same call, with no second read of a file. The growth of the cache is not
        // bounded by that count, because a test of another class can add patterns at the same time.
        //
        // The run 45359984114 of the close of TASK-0461 failed: with no Ready or Active task the gate gives no pattern. So the
        // work of the gate also covers the three newest tasks, of any status, and the count does not depend on an open task.
        // Until TASK-0473 this comment said "the three newest tasks of each status"; the code takes three tasks in all.
        //
        // TASK-0473 (finding T199 of the codebase review of 2026-11-25): with a warm cache this thread builds 0 expressions, and
        // the bound above passes for each form of the key; a call site that makes a distinct text for each path raises asked and
        // built together. So each text that the gate asks for must also be a text of the gate: a pattern of the field affects of
        // an ADR in force or a path that a kept task writes, from the same objects that the gate reads one time.
        // The identifiers come from the names of the files, which a run does not change. The test does not judge the problems
        // of the gate; the gate test judges the open tasks.
        var newest = RepositoryFiles.Tasks()
            .Select(task => task.Id)
            .OrderByDescending(id => id, StringComparer.Ordinal)
            .Take(3)
            .ToHashSet(StringComparer.Ordinal);
        var asked = new HashSet<string>(StringComparer.Ordinal);
        var kept = TaskFiles.PatternsKept;
        var before = TaskFiles.PatternsBuiltOnThisThread;
        var texts = new HashSet<string>(StringComparer.Ordinal);
        TaskFiles.PatternsAskedOnThisThread = asked;
        try
        {
            TaskGovernanceGateTests.GovernedByProblems(task => task.Status is "Ready" or "Active" || newest.Contains(task.Id), texts);
        }
        finally
        {
            TaskFiles.PatternsAskedOnThisThread = null;
        }

        var built = TaskFiles.PatternsBuiltOnThisThread - before;
        var added = TaskFiles.PatternsKept - kept;

        Assert.True(asked.Count > 0, "The gate of governed-by gave no pattern to PathPattern; an empty count is not evidence.");
        var strange = asked.Except(texts, StringComparer.Ordinal).Take(3).ToArray();
        Assert.True(
            strange.Length == 0,
            $"The gate of governed-by gave PathPattern {asked.Count} distinct texts, and {asked.Count(text => !texts.Contains(text))} of "
            + $"them are no pattern of an ADR in force and no path of a kept task, for example {string.Join(", ", strange)}. Give "
            + "PathPattern the pattern itself, so that the cache keeps one expression for each pattern.");
        Assert.True(
            built <= added && built <= asked.Count,
            $"The gate of governed-by built {built} expressions for {asked.Count} distinct patterns, and the cache kept {added} new "
            + "patterns. Build the expression of a pattern once (TaskFiles.PathPattern).");
    }
}
