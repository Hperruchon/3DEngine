using Engine.Tests.Governance;

namespace Engine.Tests.Pipeline;

// Finding T184 of the codebase review of 2026-11-20 (TASK-0426): a crash of the test host gave no cause. In attempt 1 of run
// 44758589835 the test host of 3DEngine.ImageTests stopped on Windows, and the step "Test" kept no dump. The step now collects
// a mini dump of a crash on Windows, and a failed job on Windows keeps each dump as an artifact. The test reads the workflow as
// text, because the pipeline is the only place where the step runs.
public sealed class TestStepTests
{
    // The text of one step of the job "gate", from its name to the next step.
    private static string Step(string name)
    {
        var lines = RepositoryFiles.Read(".github", "workflows", "ci.yml").Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        var start = lines.FindIndex(line => line == $"      - name: {name}");
        Assert.True(start >= 0, $"ci.yml has no step \"{name}\".");
        var end = lines.FindIndex(start + 1, line => line.StartsWith("      - name: ", StringComparison.Ordinal) || (line.StartsWith("  ", StringComparison.Ordinal) && !line.StartsWith("   ", StringComparison.Ordinal)));
        return string.Join("\n", lines.Skip(start).Take((end < 0 ? lines.Count : end) - start));
    }

    // On Windows the step "Test" adds --blame-crash with a mini dump to the options of the hang; on the other runners it does not.
    [Fact]
    public void The_Test_Step_Collects_A_Crash_Dump_On_Windows()
    {
        var test = Step("Test");
        Assert.Contains("if [ \"$RUNNER_OS\" = \"Windows\" ]; then crash=(--blame-crash --blame-crash-dump-type mini); fi", test, StringComparison.Ordinal);
        Assert.Contains("--blame-hang-timeout 2m --blame-hang-dump-type none \"${crash[@]}\"", test, StringComparison.Ordinal);
    }

    // A failed job on Windows keeps each dump of the results as an artifact, and a job with no dump does not fail for it.
    [Fact]
    public void A_Failed_Job_On_Windows_Keeps_The_Dump()
    {
        var keep = Step("Keep the crash dump of the test host");
        Assert.Contains("if: failure() && runner.os == 'Windows'", keep, StringComparison.Ordinal);
        Assert.Contains("uses: actions/upload-artifact@v4", keep, StringComparison.Ordinal);
        Assert.Contains("path: ${{ runner.temp }}/test-results/**/*.dmp", keep, StringComparison.Ordinal);
        Assert.Contains("if-no-files-found: ignore", keep, StringComparison.Ordinal);
    }
}
