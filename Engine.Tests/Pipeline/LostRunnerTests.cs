using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.Tests.Governance;

namespace Engine.Tests.Pipeline;

// ADR-0173 (TASK-0417): the choice of eng/lost-runner.js, which the workflow rerun-lost-runner.yml runs after each run of "CI".
// The three saved runs are the first attempts of runs of 2026-11-18 from the public API: a lost macOS runner (run 44337062610),
// a failed test on Windows (run 44409491212) and a failed install of the emulator (run 44416434810). ADR-0173 names this file
// in its enforced-by field.
public sealed class LostRunnerTests
{
    private static string Saved(string name) => RepositoryFiles.Path("Engine.Tests", "Pipeline", "LostRunner", name + ".json");

    // The choice of the script for a saved run, from a file.
    private static (bool Rerun, string Reason) Decide(string file)
    {
        var start = new ProcessStartInfo("node")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(RepositoryFiles.Path("eng", "lost-runner.js"));
        start.ArgumentList.Add("--decide");
        start.ArgumentList.Add(file);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"node eng/lost-runner.js --decide gave {process.ExitCode}: {error}");
        using var choice = JsonDocument.Parse(output);
        return (choice.RootElement.GetProperty("rerun").GetBoolean(), choice.RootElement.GetProperty("reason").GetString()!);
    }

    // The choice for a saved run after a change of its fields.
    private static (bool Rerun, string Reason) DecideChanged(string name, Action<JsonNode> change)
    {
        var run = JsonNode.Parse(File.ReadAllText(Saved(name)))!;
        change(run);
        var file = Path.Combine(Path.GetTempPath(), $"lost-runner-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(file, run.ToJsonString());
            return Decide(file);
        }
        finally
        {
            File.Delete(file);
        }
    }

    // A run whose only failed job is a macOS job that no runner took gets one re-run.
    [Fact]
    public void A_Lost_Runner_Gets_A_Re_Run()
        => Assert.Equal((true, "GitHub gave no runner to \"Gate on macos-latest\""), Decide(Saved("lost-macos-runner")));

    // A failed test and a failed install of the emulator are failed steps, so the run stays red.
    [Theory]
    [InlineData("failed-test", "the job \"Gate on windows-latest\" failed in the step \"Test\"")]
    [InlineData("failed-emulator-install", "the job \"Start the APK on the emulator\" failed in the step \"Install the emulator and a system image of Android 14\"")]
    public void A_Failed_Step_Gets_No_Re_Run(string name, string reason)
        => Assert.Equal((false, reason), Decide(Saved(name)));

    // A second attempt, a run of another workflow, a run that did not fail, and a lost runner together with a failed step of
    // another job each get no re-run.
    [Fact]
    public void Only_A_First_Failed_Attempt_Of_CI_With_Lost_Runners_Only_Gets_A_Re_Run()
    {
        Assert.Equal((false, "the run is on its attempt 2; only a first attempt gets a re-run"),
            DecideChanged("lost-macos-runner", run => run["run"]!["run_attempt"] = 2));
        Assert.Equal((false, "the run is of the workflow \"Build manifold native\", not \"CI\""),
            DecideChanged("lost-macos-runner", run => run["run"]!["name"] = "Build manifold native"));
        Assert.Equal((false, "the run has the conclusion \"success\""),
            DecideChanged("lost-macos-runner", run => run["run"]!["conclusion"] = "success"));

        var both = DecideChanged("lost-macos-runner", run =>
        {
            var windows = run["jobs"]!.AsArray().Single(job => (string)job!["name"]! == "Gate on windows-latest")!;
            windows["conclusion"] = "failure";
            windows["steps"]!.AsArray().Single(step => (string)step!["name"]! == "Test")!["conclusion"] = "failure";
        });
        Assert.Equal((false, "the job \"Gate on windows-latest\" failed in the step \"Test\""), both);

        var noText = DecideChanged("lost-macos-runner", run =>
        {
            foreach (var (_, list) in run["annotations"]!.AsObject())
                list!.AsArray().Clear();
        });
        Assert.False(noText.Rerun, noText.Reason);
    }

    // The workflow runs the script from the default branch with a token that can start a re-run, so it runs no file of the
    // branch of the run: it checks out the default branch only, with no credentials, and it grants no more than it needs.
    [Fact]
    public void The_Workflow_Runs_The_Script_Of_The_Default_Branch_With_A_Small_Token()
    {
        var workflow = File.ReadAllText(RepositoryFiles.Path(".github", "workflows", "rerun-lost-runner.yml"));

        Assert.Contains("  workflow_run:\n    workflows: [CI]\n    types: [completed]", workflow.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Matches(@"permissions:\r?\n  actions: write\r?\n  checks: read\r?\n  contents: read\r?\n\r?\n", workflow);
        Assert.Contains("ref: ${{ github.event.repository.default_branch }}", workflow, StringComparison.Ordinal);
        Assert.Contains("persist-credentials: false", workflow, StringComparison.Ordinal);
        Assert.Contains("github.event.workflow_run.run_attempt == 1", workflow, StringComparison.Ordinal);
        Assert.Contains("node eng/lost-runner.js \"$RUN_ID\"", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("head_sha", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("head_branch", workflow, StringComparison.Ordinal);
    }
}
