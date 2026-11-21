using System.Diagnostics;
using Engine.Tests.Governance;

namespace Engine.Tests.Pipeline;

// TASK-0435 (phase P0.173): eng/report-failed-tests.js names each test that was running at a crash of the test host, from the
// sequence file, with the cause and the dumps. In run 44869058349 the test host of Engine.Tests crashed on Linux, and the
// annotation held the first 2000 characters of the error output of the test host: the output of the tests, and not the report
// of the runtime at its end. The tests give the script a folder of results in the form of the test platform.
public sealed class CrashReportTests
{
    private const string Output = "engine-api-http has no document file: the document is in memory only, and a stop loses it.";

    // The text of the platform for a crash: the output of the tests, then the report of the runtime.
    private static readonly string Crash = "The active test run was aborted. Reason: Test host process crashed : " +
        string.Concat(Enumerable.Repeat(Output + "\n", 40)) +
        "Unhandled exception. System.InvalidOperationException: probe of the crash\n   at Engine.Tests.Probe.Run()\n";

    // The annotations that the script writes for a folder with one .trx file, the given sequence and the given dumps.
    private static string Report(string reason, string sequence, params string[] dumps)
    {
        var folder = Path.Combine(Path.GetTempPath(), $"crash-report-{Guid.NewGuid():N}");
        var attachments = Path.Combine(folder, Guid.NewGuid().ToString());
        Directory.CreateDirectory(attachments);
        try
        {
            File.WriteAllText(Path.Combine(folder, "run.trx"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">\n" +
                "  <Results />\n  <ResultSummary outcome=\"Failed\">\n    <RunInfos>\n" +
                $"      <RunInfo computerName=\"runner\" outcome=\"Error\" timestamp=\"2026-11-21T00:00:00\">\n        <Text>{System.Security.SecurityElement.Escape(reason)}</Text>\n      </RunInfo>\n" +
                "    </RunInfos>\n  </ResultSummary>\n</TestRun>\n");
            if (sequence.Length > 0)
                File.WriteAllText(Path.Combine(attachments, $"Sequence_{Guid.NewGuid():N}.xml"), $"<?xml version=\"1.0\"?>\n<TestSequence>\n{sequence}</TestSequence>\n");
            foreach (var dump in dumps)
                File.WriteAllText(Path.Combine(attachments, dump), "dump");

            var start = new ProcessStartInfo("node")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            start.ArgumentList.Add(RepositoryFiles.Path("eng", "report-failed-tests.js"));
            start.ArgumentList.Add(folder);
            start.Environment["RUNNER_OS"] = "Linux";
            start.Environment.Remove("GITHUB_STEP_SUMMARY");
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"node eng/report-failed-tests.js gave {process.ExitCode}: {error}");
            return output;
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // A crash during a test: the annotation names the test that did not complete, gives the dump, and gives the report of the
    // runtime before the cut of 2000 characters, not the output of the tests.
    [Fact]
    public void A_Crash_Names_The_Running_Test_The_Dump_And_The_Report_Of_The_Runtime()
    {
        var report = Report(Crash,
            "  <Test Name=\"Engine.Tests.Done.Passes\" Source=\"Engine.Tests.dll\" Completed=\"True\" />\n" +
            "  <Test Name=\"Engine.Tests.Probe.Run\" Source=\"Engine.Tests.dll\" Completed=\"False\" />\n",
            "testhost_4242_20261121T000000_crashdump.dmp");

        var line = Assert.Single(report.Split('\n'), l => l.StartsWith("::error ", StringComparison.Ordinal));
        Assert.StartsWith("::error title=Failed test on Linux%3A Engine.Tests.Probe.Run::The test did not complete: it was running when the test platform stopped the run. Dumps of the crash: testhost_4242_20261121T000000_crashdump.dmp. Cause: Unhandled exception. System.InvalidOperationException: probe of the crash%0A", line, StringComparison.Ordinal);
        Assert.DoesNotContain(Output, line, StringComparison.Ordinal);
    }

    // A crash with no report of the runtime and no running test: the annotation of the step gives the last characters of the
    // error output, where the last line is.
    [Fact]
    public void A_Crash_With_No_Report_Of_The_Runtime_Gives_The_End_Of_The_Error_Output()
    {
        var reason = "The active test run was aborted. Reason: Test host process crashed : " +
            string.Concat(Enumerable.Repeat(Output + "\n", 40)) + "the last line of the test host";

        var report = Report(reason, "");

        var line = Assert.Single(report.Split('\n'), l => l.StartsWith("::error ", StringComparison.Ordinal));
        Assert.StartsWith("::error title=Test step failed on Linux::", line, StringComparison.Ordinal);
        Assert.Contains(" Cause: ... ", line, StringComparison.Ordinal);
        Assert.EndsWith("the last line of the test host", line.TrimEnd('\r'), StringComparison.Ordinal);
    }
}
