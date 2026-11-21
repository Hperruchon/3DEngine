using WriteSetCheck;
using Xunit;

namespace Engine.Tests.Governance;

// The work of the path patterns, as a count and not as a time (TASK-0442, R-0074, ADR-0173
// item 4). Until TASK-0442 TaskFiles.PathPattern compiled a new expression at each call.
// The gate of governed-by compiled tens of thousands of expressions and took 38 to 100
// seconds on the computer of the owner, near the hang limit of 2 minutes of the step "Test".
// The counter of TaskFiles counts the expressions of one thread only, so a test of another
// class that runs at the same time does not change the count.
public class PathPatternWorkTests
{
    [Fact]
    public void A_Pattern_Gives_One_Expression_That_Is_Built_Once()
    {
        // A pattern that only this test uses, so that this thread builds it.
        const string Pattern = "eng/path-pattern-work-probe/*.cs";
        var before = TaskFiles.PatternsBuiltOnThisThread;

        var first = TaskFiles.PathPattern(Pattern);
        var second = TaskFiles.PathPattern(Pattern);

        Assert.Same(first, second);
        Assert.Equal(1, TaskFiles.PatternsBuiltOnThisThread - before);
    }

    [Fact]
    public void The_Gate_Of_Governed_By_Builds_At_Most_One_Expression_For_Each_Pattern()
    {
        // Each pattern that the gate can match: each affects pattern of each ADR, and each
        // path of the write set of each open task.
        var patterns = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(RepositoryFiles.Path("docs", "adr"), "0*.md"))
        {
            var fields = RepositoryFiles.ReadFrontMatter(File.ReadAllText(file));
            if (fields is not null)
                patterns.UnionWith(RepositoryFiles.AffectsPatterns(fields.GetValueOrDefault("affects"), Path.GetFileName(file)));
        }

        foreach (var task in RepositoryFiles.Tasks().Where(t => t.Status is "Ready" or "Active"))
            patterns.UnionWith(task.Written);

        var before = TaskFiles.PatternsBuiltOnThisThread;
        new TaskGovernanceGateTests().Each_Open_Task_Lists_Exactly_The_Adrs_Whose_Affects_Intersect_Its_Write_Set();
        var built = TaskFiles.PatternsBuiltOnThisThread - before;

        Assert.True(
            built <= patterns.Count,
            $"The gate of governed-by built {built} expressions for {patterns.Count} patterns. Build the expression "
            + "of a pattern once (TaskFiles.PathPattern).");
    }
}
