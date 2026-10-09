using WriteSetCheck;
using Xunit;

namespace Engine.Tests.Governance;

// The rules of the judge of the write set, eng/write-set-check (TASK-0053). The
// pipeline runs the judge from a build of main; these tests hold its rules. A
// gate job clones one commit only, so each test gives the judge a fake git with
// fixed answers and reads the task files of the working tree.
public class WriteSetJudgeTests
{
    private static readonly TaskRecord Task = new(
        "tasks/TASK-9999-example.md",
        "TASK-9999",
        "Active",
        new Dictionary<string, string>(),
        Create: ["tasks/TASK-9999-example.md", "Engine.Tests/Governance/ExactGateTests.cs"],
        Modify: ["Engine.Tests/**", "Engine.Core/Commands/**"],
        Forbid: ["Engine.Contracts/**"]);

    [Fact]
    public void A_Pattern_Does_Not_Permit_A_Gate_File()
    {
        var problems = Judge.FilesOutsideTheWriteSet(Task, [
            "Engine.Tests/Governance/OtherGateTests.cs",
            "Engine.Tests/Engine.Tests.csproj",
            "eng/write-set-check/Judge.cs",
            ".github/workflows/ci.yml",
        ]);

        Assert.Equal(4, problems.Count);
        Assert.All(problems, p => Assert.Contains("is a gate file", p, StringComparison.Ordinal));
    }

    [Fact]
    public void An_Exact_Name_Permits_A_Gate_File_And_A_Pattern_Permits_Another_File()
    {
        var problems = Judge.FilesOutsideTheWriteSet(Task, [
            "Engine.Tests/Governance/ExactGateTests.cs",
            "Engine.Tests/Commands/SomeTests.cs",
            "Engine.Core/Commands/SomeHandler.cs",
        ]);

        Assert.Empty(problems);
    }

    [Fact]
    public void A_Forbidden_Path_Names_Its_Pattern_And_Another_Path_Names_The_Lists()
    {
        var problems = Judge.FilesOutsideTheWriteSet(Task, ["Engine.Contracts/Document.cs", "docs/roadmap.md"]);

        Assert.Equal(
            [
                "Engine.Contracts/Document.cs matches the forbid pattern 'Engine.Contracts/**' of TASK-9999",
                "docs/roadmap.md is in no create list and in no modify list of TASK-9999",
            ],
            problems);
    }

    [Theory]
    [InlineData("eng/write-set-check/Program.cs")]
    [InlineData("eng/write-set-check/WriteSetCheck.csproj")]
    [InlineData("eng/write-set-cutoff.txt")]
    [InlineData("Directory.Build.props")]
    [InlineData("Engine.Core/Directory.Packages.props")]
    [InlineData("Engine.Tests/Diagnostics/DiagnosticsScanner.cs")]
    public void Each_File_That_Can_Change_A_Judgement_Is_A_Gate_File(string file)
    {
        Assert.True(Judge.IsGateFile(file));
    }

    [Theory]
    [InlineData("Subject\n\nBody.\n\nTASK-0053\n\nCo-Authored-By: X <x@example.com>\n", "TASK-0053")]
    [InlineData("Subject\r\n\r\nTASK-0040 · ADR-0021\r\n", "TASK-0040")]
    [InlineData("Subject\n\nTASK-0039 added a rule in this prose line.\n", null)]
    [InlineData("Subject with no trailer\n", null)]
    public void The_Trailer_Line_Names_The_Task_And_A_Prose_Line_Does_Not(string message, string? expected)
    {
        Assert.Equal(expected, Judge.TrailerTask(message));
    }

    [Fact]
    public void A_Rename_Arrow_And_A_Backslash_Give_Two_Paths_With_The_Forward_Slash()
    {
        var files = Judge.ParseChangedFiles("docs\\a.md -> docs/b.md\n\nEngine.Core/c.cs\r\n");

        Assert.Equal(["Engine.Core/c.cs", "docs/a.md", "docs/b.md"], files.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void A_Task_That_Is_Done_Before_And_After_The_Commit_Governs_Nothing()
    {
        var file = TaskFile("TASK-0049");
        var judge = new Judge(RepositoryFiles.Root, FakeGit(before: File.ReadAllText(file), after: File.ReadAllText(file)));

        var problems = judge.Check("abc1234", [RepositoryFiles.Relative(file)], "TASK-0049");

        Assert.Contains("is Done before this commit and after it", Assert.Single(problems), StringComparison.Ordinal);
    }

    [Fact]
    public void A_Commit_Before_The_Rules_Of_TASK_0047_Keeps_The_Earlier_Rule()
    {
        var file = TaskFile("TASK-0049");
        var judge = new Judge(RepositoryFiles.Root, FakeGit(File.ReadAllText(file), File.ReadAllText(file), beforeTheRules: true));

        Assert.Empty(judge.Check("abc1234", [RepositoryFiles.Relative(file)], "TASK-0049"));
    }

    [Fact]
    public void A_Task_That_Closes_In_The_Commit_Governs_It()
    {
        var file = TaskFile("TASK-0049");
        var closed = File.ReadAllText(file);
        var judge = new Judge(RepositoryFiles.Root, FakeGit(before: closed.Replace("status: Done", "status: Active"), after: closed));

        Assert.Empty(judge.Check("abc1234", [RepositoryFiles.Relative(file), "eng/manifold-native/build.sh"], "TASK-0049"));
    }

    [Fact]
    public void A_Named_Task_Must_Exist_And_The_Commit_Must_Change_Its_File()
    {
        var judge = new Judge(RepositoryFiles.Root, FakeGit(null, null));

        var unknown = judge.Check("abc1234", ["docs/roadmap.md"], "TASK-9998");
        var untouched = judge.Check("abc1234", ["docs/roadmap.md"], "TASK-0049");

        Assert.Contains("no task file with a write set has that identifier", Assert.Single(unknown), StringComparison.Ordinal);
        Assert.Contains("does not change its file", Assert.Single(untouched), StringComparison.Ordinal);
    }

    [Fact]
    public void With_No_Name_Exactly_One_Task_File_Must_Be_Touched()
    {
        var judge = new Judge(RepositoryFiles.Root, FakeGit(null, null));

        var none = judge.Check("abc1234", ["docs/roadmap.md"], null);
        var two = judge.Check("abc1234", [RepositoryFiles.Relative(TaskFile("TASK-0048")), RepositoryFiles.Relative(TaskFile("TASK-0049"))], null);

        Assert.Contains("names no task and touches no task file", Assert.Single(none), StringComparison.Ordinal);
        Assert.Contains("touches several task files and names none", Assert.Single(two), StringComparison.Ordinal);
    }

    private static string TaskFile(string id)
        => RepositoryFiles.TaskFiles().Single(f => Path.GetFileName(f).StartsWith(id + "-", StringComparison.Ordinal));

    // A git that answers the two questions of the judge: is the commit before
    // the rules of TASK-0047, and what is the task file after and before the
    // commit. Each other question gets no answer.
    private static Func<string[], string?> FakeGit(string? before, string? after, bool beforeTheRules = false)
        => arguments => arguments switch
        {
            ["merge-base", "--is-ancestor", _, Judge.CommitRulesFrom] => beforeTheRules ? string.Empty : null,
            ["show", var spec] when spec.Contains("^:", StringComparison.Ordinal) => before,
            ["show", _] => after,
            _ => null,
        };
}
