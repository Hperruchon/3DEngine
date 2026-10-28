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
    [InlineData("eng/repo-gates/Program.cs")]
    [InlineData("eng/repo-gates/RepoGates.csproj")]
    [InlineData("eng/write-set-cutoff.txt")]
    [InlineData("Directory.Build.props")]
    [InlineData("Engine.Core/Directory.Packages.props")]
    [InlineData("Engine.Tests/Diagnostics/DiagnosticsScanner.cs")]
    // Findings T41 and T44 of the codebase review of 2026-10-12: each input of a
    // build (TASK-0066).
    [InlineData("global.json")]
    [InlineData("tools/global.json")]
    [InlineData("nuget.config")]
    [InlineData("NuGet.Config")]
    [InlineData("nuget/Engine.Geometry.Manifold.Native.3.5.2.1.nupkg")]
    // Finding T48 of the codebase review of 2026-10-13: each file that decides which
    // projects a run tests (TASK-0073).
    [InlineData("3DEngine.sln")]
    [InlineData("Directory.Solution.targets")]
    [InlineData("Directory.Solution.props")]
    [InlineData("after.3DEngine.sln.targets")]
    [InlineData("before.3DEngine.sln.targets")]
    [InlineData("Engine.Tests/Engine.Tests.csproj.user")]
    // Finding T69 of the second codebase review of 2026-10-28 (TASK-0097): the project
    // file of each test project.
    [InlineData("3DEngine.Tests/3DEngine.Tests.csproj")]
    [InlineData("3DEngine.ImageTests/3DEngine.ImageTests.csproj")]
    [InlineData("3dengine.tests/3DEngine.Tests.csproj")]
    [InlineData("directory.build.props")]
    [InlineData("eng/count-tests.js")]
    [InlineData("eng/test-floor.txt")]
    public void Each_File_That_Can_Change_A_Judgement_Is_A_Gate_File(string file)
    {
        Assert.True(Judge.IsGateFile(file));
    }

    // The second line of T48: the step "Test" fails when fewer tests ran than the
    // floor, or when no result exists (TASK-0073).
    // Since TASK-0078 each test project has an exact count, and the folder holds one
    // result file for each project and no other. A project that leaves the run while
    // another one grows, and a result file that a test writes, each passed the one
    // floor of TASK-0073 (findings T54 and T58 of the codebase review of 2026-10-17).
    [Theory]
    [InlineData("exact", "windows", 0)]
    [InlineData("the count of one platform", "linux", 0)]
    [InlineData("one test fewer", "windows", 1)]
    [InlineData("one test more", "windows", 1)]
    [InlineData("a project left the run", "windows", 1)]
    [InlineData("a second result of one project", "windows", 1)]
    [InlineData("a result of another assembly", "windows", 1)]
    [InlineData("a result of two assemblies", "windows", 1)]
    [InlineData("no folder", "windows", 1)]
    public void The_Count_Of_The_Tests_Is_Exact_For_Each_Project(string scenario, string platform, int expectedExit)
    {
        static string Trx(string assembly, int executed, string? second = null)
            => $"<TestRun><TestDefinitions><UnitTest storage=\"c:/work/{assembly}.dll\" />"
            + (second is null ? "" : $"<UnitTest storage=\"c:/work/{second}.dll\" />")
            + $"</TestDefinitions><ResultSummary><Counters total=\"{executed}\" executed=\"{executed}\" passed=\"{executed}\" /></ResultSummary></TestRun>";

        var folder = Directory.CreateTempSubdirectory("count-tests-").FullName;
        try
        {
            var results = Path.Combine(folder, "results");
            Directory.CreateDirectory(Path.Combine(results, "b"));
            File.WriteAllText(Path.Combine(folder, "counts.txt"), "# a comment\nA.Tests 3\nB.Tests 4 linux=5\n");

            if (scenario != "a project left the run")
                File.WriteAllText(Path.Combine(results, "a.trx"), Trx("A.Tests", scenario switch { "one test fewer" => 2, "one test more" => 4, _ => 3 }));
            File.WriteAllText(Path.Combine(results, "b", "b.trx"), Trx("B.Tests", platform == "linux" ? 5 : 4));
            if (scenario == "a second result of one project")
                File.WriteAllText(Path.Combine(results, "a2.trx"), Trx("A.Tests", 3));
            if (scenario == "a result of another assembly")
                File.WriteAllText(Path.Combine(results, "extra.trx"), Trx("Forged.Tests", 1000));
            if (scenario == "a result of two assemblies")
                File.WriteAllText(Path.Combine(results, "two.trx"), Trx("A.Tests", 3, "B.Tests"));

            var folderToRead = scenario == "no folder" ? Path.Combine(folder, "absent") : results;
            Assert.Equal(expectedExit, RunCount(folderToRead, Path.Combine(folder, "counts.txt"), platform));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Theory]
    [InlineData("docs/Directory.Packages.txt")]
    [InlineData("docs/global.json.md")]
    [InlineData("Engine.Core/Directory.cs")]
    public void A_Name_Like_An_Input_Of_A_Build_Is_Not_A_Gate_File(string file)
    {
        Assert.False(Judge.IsGateFile(file));
        Assert.False(Judge.IsGateProgramSource(file));
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

    // Git quotes a path with a space or a character outside ASCII in its normal
    // output, and the judge read the quoted form as the name (finding T35 of the
    // review of 2026-10-09). With -z git gives each path as it is.
    [Fact]
    public void A_List_Of_Git_With_Nul_Characters_Gives_Each_Path_As_It_Is()
    {
        var files = Judge.ParseGitPaths("docs/a b.md\0docs/café.md\0Engine.Core/c.cs\0");

        Assert.Equal(["Engine.Core/c.cs", "docs/a b.md", "docs/café.md"], files.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void A_Task_That_Is_Done_Before_And_After_The_Commit_Governs_Nothing()
    {
        var file = TaskFile("TASK-0049");
        var judge = new Judge(FakeGit(before: File.ReadAllText(file), after: File.ReadAllText(file)));

        var problems = judge.Check("abc1234", [RepositoryFiles.Relative(file)], "TASK-0049");

        Assert.Contains("is Done before this commit and after it", Assert.Single(problems), StringComparison.Ordinal);
    }

    [Fact]
    public void A_Commit_Before_The_Rules_Of_TASK_0047_Keeps_The_Earlier_Rule()
    {
        var file = TaskFile("TASK-0049");
        var judge = new Judge(FakeGit(File.ReadAllText(file), File.ReadAllText(file), beforeTheRules: true));

        Assert.Empty(judge.Check("abc1234", [RepositoryFiles.Relative(file)], "TASK-0049"));
    }

    [Fact]
    public void A_Task_That_Closes_In_The_Commit_Governs_It()
    {
        var file = TaskFile("TASK-0049");
        var closed = File.ReadAllText(file);
        var judge = new Judge(FakeGit(before: closed.Replace("status: Done", "status: Active"), after: closed));

        Assert.Empty(judge.Check("abc1234", [RepositoryFiles.Relative(file), "eng/manifold-native/build.sh"], "TASK-0049"));
    }

    [Fact]
    public void A_Named_Task_Must_Exist_And_The_Commit_Must_Change_Its_File()
    {
        var judge = new Judge(FakeGit(null, null));

        var unknown = judge.Check("abc1234", ["docs/roadmap.md"], "TASK-9998");
        var untouched = judge.Check("abc1234", ["docs/roadmap.md"], "TASK-0049");

        Assert.Contains("no task file with that identifier", Assert.Single(unknown), StringComparison.Ordinal);
        Assert.Contains("does not change its file", Assert.Single(untouched), StringComparison.Ordinal);
    }

    [Fact]
    public void With_No_Name_Exactly_One_Task_File_Must_Be_Touched()
    {
        var judge = new Judge(FakeGit(null, null));

        var none = judge.Check("abc1234", ["docs/roadmap.md"], null);
        var two = judge.Check("abc1234", [RepositoryFiles.Relative(TaskFile("TASK-0048")), RepositoryFiles.Relative(TaskFile("TASK-0049"))], null);

        Assert.Contains("names no task and touches no task file", Assert.Single(none), StringComparison.Ordinal);
        Assert.Contains("touches several task files and names none", Assert.Single(two), StringComparison.Ordinal);
    }

    // Finding T40 of the codebase review of 2026-10-11: a failed read of the task
    // file before the commit gave null, which the judge took for a task that did
    // not exist, so the rule for a task that is Done did not apply (TASK-0061).
    [Fact]
    public void A_Failed_Read_Of_The_Task_Before_The_Commit_Is_A_Failure()
    {
        var file = TaskFile("TASK-0049");
        var after = File.ReadAllText(file);
        Func<string[], string?> git = arguments => arguments switch
        {
            ["ls-tree", "-r", "-z", "--name-only", _, "--", "tasks"]
                => string.Concat(RepositoryFiles.TaskFiles().Select(f => RepositoryFiles.Relative(f) + "\0")),
            ["ls-tree", "-z", "--name-only", var tree, "--", _] when tree.EndsWith('^') => null,
            ["show", _] => after,
            _ => null,
        };

        var problems = new Judge(git).Check("abc1234", [RepositoryFiles.Relative(file)], "TASK-0049");

        Assert.Contains("cannot read", Assert.Single(problems), StringComparison.Ordinal);
    }

    // The same finding: a folder of PATH outside the checkout can be a link into it.
    // On Windows the test makes a junction, which needs no special right; on Linux
    // and macOS it makes a symbolic link.
    [Fact]
    public void A_Link_Into_The_Checkout_Does_Not_Give_Git()
    {
        var root = Directory.CreateTempSubdirectory("judge-root-").FullName;
        var link = Path.Combine(Path.GetTempPath(), "judge-link-" + Guid.NewGuid().ToString("N"));
        try
        {
            File.WriteAllText(Path.Combine(root, OperatingSystem.IsWindows() ? "git.exe" : "git"), "not a program");
            if (OperatingSystem.IsWindows())
            {
                using var mklink = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{root}\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                })!;
                mklink.StandardOutput.ReadToEnd();
                mklink.WaitForExit();
                Assert.True(mklink.ExitCode == 0, "mklink /J failed");
            }
            else
            {
                Directory.CreateSymbolicLink(link, root);
            }

            var git = Judge.FindGit(root, link);

            Assert.Null(git);
        }
        finally
        {
            if (Directory.Exists(link))
                Directory.Delete(link);
            Directory.Delete(root, recursive: true);
        }
    }

    private static int RunCount(string folder, string countFile, string platform)
    {
        var start = new System.Diagnostics.ProcessStartInfo("node")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(RepositoryFiles.Path("eng", "count-tests.js"));
        start.ArgumentList.Add(folder);
        start.ArgumentList.Add(countFile);
        start.ArgumentList.Add(platform);
        using var process = System.Diagnostics.Process.Start(start)!;
        process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode;
    }

    private static string TaskFile(string id)
        => RepositoryFiles.TaskFiles().Single(f => Path.GetFileName(f).StartsWith(id + "-", StringComparison.Ordinal));

    // A git that answers the three questions of the judge: which task files the
    // commit holds (the files of the working tree), is the commit before the
    // rules of TASK-0047, and what is the task file after and before the commit.
    // Each other question gets no answer.
    private static Func<string[], string?> FakeGit(string? before, string? after, bool beforeTheRules = false)
        => arguments => arguments switch
        {
            ["ls-tree", "-r", "-z", "--name-only", _, "--", "tasks"]
                => string.Concat(RepositoryFiles.TaskFiles().Select(f => RepositoryFiles.Relative(f) + "\0")),
            ["ls-tree", "-z", "--name-only", var tree, "--", var path]
                => (tree.EndsWith('^') ? before : after) is null ? string.Empty : path + "\0",
            ["merge-base", "--is-ancestor", _, Judge.CommitRulesFrom] => beforeTheRules ? string.Empty : null,
            ["show", var spec] when spec.Contains("^:", StringComparison.Ordinal) => before,
            ["show", _] => after,
            _ => null,
        };
}
