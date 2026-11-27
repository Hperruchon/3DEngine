using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.Tests.Governance;

namespace Engine.Tests.Pipeline;

// ADR-0173 (TASK-0417): the choice of eng/lost-runner.js, which the workflow rerun-lost-runner.yml runs after each run of "CI".
// The three saved runs are the first attempts of runs of 2026-11-18 from the public API: a lost macOS runner (run 44337062610),
// a failed test on Windows (run 44409491212) and a failed install of the emulator (run 44416434810). TASK-0435 (phase P0.173)
// adds two first attempts of 2026-11-21: a fault of the artifact service on macOS in the step "Keep the determinism hash"
// (run 44831092608) and a crash of the test host on Linux (run 44869058349). ADR-0173 names this file in its enforced-by field.
public sealed class LostRunnerTests
{
    private static string Saved(string name) => RepositoryFiles.Path("Engine.Tests", "Pipeline", "LostRunner", name + ".json");

    private const string CreateArtifactTimeout = "Failed to CreateArtifact: Failed to make request after 5 attempts: Request timeout: /twirp/github.actions.results.api.v1.ArtifactService/CreateArtifact";

    // The output of the script for the given arguments.
    private static string Script(params string[] arguments)
    {
        var start = new ProcessStartInfo("node")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(RepositoryFiles.Path("eng", "lost-runner.js"));
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"node eng/lost-runner.js {string.Join(' ', arguments)} gave {process.ExitCode}: {error}");
        return output;
    }

    // The choice of the script for a saved run, from a file.
    private static (bool Rerun, string Reason) Decide(string file)
    {
        using var choice = JsonDocument.Parse(Script("--decide", file));
        return (choice.RootElement.GetProperty("rerun").GetBoolean(), choice.RootElement.GetProperty("reason").GetString()!);
    }

    // The job of the given name in a saved run, and its list of annotations, which the change makes when it is absent.
    private static (JsonNode Job, JsonArray Annotations) Job(JsonNode run, string name)
    {
        var job = run["jobs"]!.AsArray().Single(job => (string)job!["name"]! == name)!;
        var id = job["id"]!.ToString();
        var all = run["annotations"]!.AsObject();
        if (all[id] is null)
            all[id] = new JsonArray();
        return (job, all[id]!.AsArray());
    }

    private static void Fail(JsonNode job, string step)
    {
        job["conclusion"] = "failure";
        job["steps"]!.AsArray().Single(s => (string)s!["name"]! == step)!["conclusion"] = "failure";
    }

    private static JsonObject Annotation(string level, string message) => new() { ["annotation_level"] = level, ["message"] = message };

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

    // A failed test, a failed install of the emulator and a crash of the test host are failed steps of the code, so the run
    // stays red.
    [Theory]
    [InlineData("failed-test", "the job \"Gate on windows-latest\" failed in the step \"Test\"")]
    [InlineData("failed-emulator-install", "the job \"Start the APK on the emulator\" failed in the step \"Install the emulator and a system image of Android 14\"")]
    [InlineData("crashed-test-host", "the job \"Gate on ubuntu-latest\" failed in the step \"Test\"")]
    public void A_Failed_Step_Gets_No_Re_Run(string name, string reason)
        => Assert.Equal((false, reason), Decide(Saved(name)));

    // TASK-0435: run 44831092608 failed only in the upload of the determinism hash on macOS, with the time-out of the artifact
    // service, so it gets the one re-run.
    [Fact]
    public void A_Fault_Of_The_Artifact_Service_Gets_A_Re_Run()
        => Assert.Equal((true, "a known fault of the GitHub service failed \"Gate on macos-latest\" in the step \"Keep the determinism hash\""),
            Decide(Saved("artifact-service-timeout")));

    // P0.189 (finding T202 of the codebase review of 2026-11-25): run 45707816188 failed only in the upload of the determinism
    // hash on Ubuntu, with a reset of the connection to the artifact service. The saved run with the text of that annotation gets
    // the one re-run; a reset in another step of the artifact, or another fault of the finalize, stays red.
    [Fact]
    public void A_Reset_Of_The_Artifact_Service_Gets_A_Re_Run()
    {
        static Action<JsonNode> Message(string text) => run =>
        {
            var (_, notes) = Job(run, "Gate on macos-latest");
            var note = notes.Single(note => (string)note!["message"]! == CreateArtifactTimeout)!;
            note["message"] = text;
        };

        Assert.Equal((true, "a known fault of the GitHub service failed \"Gate on macos-latest\" in the step \"Keep the determinism hash\""),
            DecideChanged("artifact-service-timeout", Message("Failed to FinalizeArtifact: Unable to make request: ECONNRESET")));
        Assert.False(DecideChanged("artifact-service-timeout", Message("Failed to FinalizeArtifact: Unable to make request: ETIMEDOUT")).Rerun);
        Assert.False(DecideChanged("artifact-service-timeout", Message("Failed to CreateArtifact: Unable to make request: ECONNRESET")).Rerun);
    }

    // TASK-0435: a fault of the service together with a fault of the code stays red: a failed test of another job, a failed test
    // of the same job, a crash, a failed upload with another message, a second annotation of a failure, an upload with no
    // annotation, a step that ci.yml does not give as an upload, a message with half of the known texts, and a second attempt.
    [Fact]
    public void A_Fault_Of_The_Service_With_A_Fault_Of_The_Code_Stays_Red()
    {
        const string Hash = "Keep the determinism hash";
        var red = new (string Reason, Action<JsonNode> Change)[]
        {
            ("the job \"Gate on windows-latest\" failed in the step \"Test\"", run =>
            {
                var (windows, notes) = Job(run, "Gate on windows-latest");
                Fail(windows, "Test");
                notes.Add(Annotation("failure", "Assert.True() Failure"));
            }),
            ("the job \"Gate on macos-latest\" failed in the step \"Test\"", run => Fail(Job(run, "Gate on macos-latest").Job, "Test")),
            ("the job \"Gate on macos-latest\" failed in the step \"Test\"", run =>
            {
                var (macos, notes) = Job(run, "Gate on macos-latest");
                Fail(macos, "Test");
                notes.Add(Annotation("failure", "Failed test on macOS: Engine.Tests.Probe.Run :: The test did not complete. Cause: Test host process crashed"));
            }),
            ($"the job \"Gate on macos-latest\" failed in the step \"{Hash}\" with no known fault of the service: \"No files were found with the provided path: /Users/runner/work/_temp/determinism/hash.txt. No artifacts will be uploaded.\"", run =>
            {
                var notes = Job(run, "Gate on macos-latest").Annotations;
                notes.Clear();
                notes.Add(Annotation("failure", "No files were found with the provided path: /Users/runner/work/_temp/determinism/hash.txt. No artifacts will be uploaded."));
            }),
            ($"the job \"Gate on macos-latest\" failed in the step \"{Hash}\" with no known fault of the service: \"Process completed with exit code 1.\"", run =>
                Job(run, "Gate on macos-latest").Annotations.Add(Annotation("failure", "Process completed with exit code 1."))),
            ($"the job \"Gate on macos-latest\" failed in the step \"{Hash}\" with no known fault of the service", run =>
                Job(run, "Gate on macos-latest").Annotations.Clear()),
            ("the job \"Gate on macos-latest\" failed in the step \"Keep the hash of the runner\"", run =>
                Job(run, "Gate on macos-latest").Job["steps"]!.AsArray().Single(s => (string)s!["name"]! == Hash)!["name"] = "Keep the hash of the runner"),
            ($"the job \"Gate on macos-latest\" failed in the step \"{Hash}\" with no known fault of the service: \"Request timeout: /twirp/github.actions.results.api.v1.ArtifactService/CreateArtifact\"", run =>
            {
                var notes = Job(run, "Gate on macos-latest").Annotations;
                notes.Clear();
                notes.Add(Annotation("failure", "Request timeout: /twirp/github.actions.results.api.v1.ArtifactService/CreateArtifact"));
            }),
            ("the run is on its attempt 2; only a first attempt gets a re-run", run => run["run"]!["run_attempt"] = 2),
        };

        foreach (var (reason, change) in red)
            Assert.Equal((false, reason), DecideChanged("artifact-service-timeout", change));
    }

    // TASK-0435: two faults of the service in one run, a lost runner and a time-out of the artifact service, get the one re-run;
    // the known message is the only annotation of a failure that the job of the upload has, with a warning and a notice.
    [Fact]
    public void A_Lost_Runner_And_A_Fault_Of_The_Artifact_Service_Get_A_Re_Run()
    {
        var both = DecideChanged("artifact-service-timeout", run =>
        {
            var (windows, notes) = Job(run, "Gate on windows-latest");
            windows["conclusion"] = "failure";
            windows["steps"] = new JsonArray();
            notes.Add(Annotation("failure", "The job was not acquired by Runner of type hosted even after multiple attempts"));
        });

        Assert.Equal((true, "GitHub gave no runner to \"Gate on windows-latest\"; a known fault of the GitHub service failed \"Gate on macos-latest\" in the step \"Keep the determinism hash\""), both);
        var saved = JsonNode.Parse(File.ReadAllText(Saved("artifact-service-timeout")))!;
        var levels = Job(saved, "Gate on macos-latest").Annotations.Select(a => ((string)a!["annotation_level"]!, (string)a["message"]!)).ToList();
        Assert.Contains(("failure", CreateArtifactTimeout), levels);
        Assert.Equal(["warning", "failure", "notice"], levels.Select(level => level.Item1));
    }

    // TASK-0435: the steps of the service are the steps of ci.yml that upload or download an artifact, and no step of the code.
    [Fact]
    public void The_Steps_Of_The_Service_Are_The_Uploads_And_Downloads_Of_The_Artifacts_Of_Ci()
    {
        var steps = JsonSerializer.Deserialize<string[]>(Script("--service-steps"))!;

        Assert.Equal(
            ["Keep the crash dump of the test host", "Keep the determinism hash", "Keep the picture of the image test", "Take the hash of each runner",
             "Keep the APK", "Take the APK", "Keep the determinism hash of the phone", "Keep the log of Android"],
            steps);
    }

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
        // TASK-0435: the script reads the steps of the service from ci.yml of the default branch.
        Assert.Matches(@"sparse-checkout: \|\r?\n            eng/lost-runner\.js\r?\n            \.github/workflows/ci\.yml\r?\n", workflow);
        Assert.Contains("persist-credentials: false", workflow, StringComparison.Ordinal);
        Assert.Contains("github.event.workflow_run.run_attempt == 1", workflow, StringComparison.Ordinal);
        Assert.Contains("node eng/lost-runner.js \"$RUN_ID\"", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("head_sha", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("head_branch", workflow, StringComparison.Ordinal);
    }
}
