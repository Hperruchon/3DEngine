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
        var kept = TaskFiles.PatternsKept;
        var before = TaskFiles.PatternsBuiltOnThisThread;
        new TaskGovernanceGateTests().Each_Open_Task_Lists_Exactly_The_Adrs_Whose_Affects_Intersect_Its_Write_Set();
        var built = TaskFiles.PatternsBuiltOnThisThread - before;
        var added = TaskFiles.PatternsKept - kept;

        Assert.True(
            built <= added,
            $"The gate of governed-by built {built} expressions, and the cache kept {added} new patterns. Build the "
            + "expression of a pattern once (TaskFiles.PathPattern).");
    }
}
