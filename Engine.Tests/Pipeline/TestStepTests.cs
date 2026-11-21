using Engine.Tests.Governance;

namespace Engine.Tests.Pipeline;

// Finding T184 of the codebase review of 2026-11-20 (TASK-0426): a crash of the test host gave no cause. In attempt 1 of run
// 44758589835 the test host of 3DEngine.ImageTests stopped on Windows, and the step "Test" kept no dump. The step collected a
// mini dump of a crash on Windows only. TASK-0435 (phase P0.173): in run 44869058349 the test host of Engine.Tests crashed on
// Linux, and the run kept no dump. The step now collects a mini dump on each system, and a failed job keeps each dump and each
// sequence file as an artifact with the name of its system. The test reads the workflow as text, because the pipeline is the
// only place where the step runs.
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

    // On each system the step "Test" gives --blame-crash with a mini dump after the options of the hang, with no condition on
    // the system, and the step runs with no condition.
    [Fact]
    public void The_Test_Step_Collects_A_Crash_Dump_On_Each_System()
    {
        var test = Step("Test");
        Assert.Contains("--blame-hang-timeout 2m --blame-hang-dump-type none --blame-crash --blame-crash-dump-type mini \\", test, StringComparison.Ordinal);
        Assert.DoesNotContain("RUNNER_OS", test, StringComparison.Ordinal);
        Assert.DoesNotContain("runner.os", test, StringComparison.Ordinal);
        Assert.DoesNotContain("if:", test, StringComparison.Ordinal);
    }

    // A failed job on each system keeps each dump and each sequence file of the results as an artifact with the name of its
    // system, so the three jobs of a run do not take one name; a job with no dump does not fail for it.
    [Fact]
    public void A_Failed_Job_On_Each_System_Keeps_The_Dump_And_The_Sequence()
    {
        var keep = Step("Keep the crash dump of the test host");
        Assert.Contains("        if: failure()\n", keep + "\n", StringComparison.Ordinal);
        Assert.Contains("uses: actions/upload-artifact@v4", keep, StringComparison.Ordinal);
        Assert.Contains("name: test-host-crash-${{ matrix.os }}", keep, StringComparison.Ordinal);
        Assert.Contains("${{ runner.temp }}/test-results/**/*.dmp", keep, StringComparison.Ordinal);
        Assert.Contains("${{ runner.temp }}/test-results/**/Sequence_*.xml", keep, StringComparison.Ordinal);
        Assert.Contains("if-no-files-found: ignore", keep, StringComparison.Ordinal);
    }
}
