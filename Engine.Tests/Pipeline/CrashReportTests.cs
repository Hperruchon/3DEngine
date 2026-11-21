using System.Diagnostics;
using Engine.Tests.Governance;

namespace Engine.Tests.Pipeline;

// TASK-0435 (phase P0.173): eng/report-failed-tests.js names each test that was running at a crash of the test host, from the
// sequence file, with the cause and the dumps. In run 44869058349 the test host of Engine.Tests crashed on Linux, and the
// annotation held the first 2000 characters of the error output of the test host: the output of the tests, and not the report
// of the runtime at its end. The tests give the script a folder of results in the form of the test platform.
// TASK-0440 (phase P0.175): in run 44908442870 the message of the annotation named no test, and its cause was the output of
// other tests. The message now starts with the name of the running test, the cause leaves out the output of the tests, and a
// crash with no known running test says so.
public sealed class CrashReportTests
{
    private const string Output = "engine-api-http has no document file: the document is in memory only, and a stop loses it.";

    private const string Head = "The active test run was aborted. Reason: Test host process crashed : ";

    // The text of the platform for a crash: the output of the tests, the lines of createdump, then the report of the runtime.
    private static readonly string Crash = Head +
        string.Concat(Enumerable.Repeat(Output + "\n", 40)) +
        "[createdump] Gathering state for process 4242 testhost\n[createdump] Dump successfully written in 312ms\n" +
        "Unhandled exception. System.InvalidOperationException: probe of the crash\n   at Engine.Tests.Probe.Run()\n";

    // The facts of run 44908442870 (windows-latest, step "Test"): two lines of the output of other tests, repeated, with no
    // report of the runtime, and a second reason of the data collection. The artifact held the sequence file and no dump.
    private const string ImageOutput =
        "3DEngine does not import bracket.stl, because its new part did not start\n" +
        "The file C:\\Users\\runneradmin\\AppData\\Local\\Temp\\3dengine-stl-open-c6340258207b497f99826736d1e51600\\documents\\first.3de " +
        "did not exist, so the host made a new empty document file there. A save moves the file to its new name, so a start again " +
        "with the same --file gives an empty document (ADR-0015 items 9 and 10).\n";

    private const string ChannelFailed = "Data collection : Test case event channel failed: Unable to read data from the transport " +
        "connection: An existing connection was forcibly closed by the remote host..";

    private const string ImageTest =
        "ThreeDEngine.ImageTests.ResponsiveLayoutTests.Each_Size_Of_A_Resize_From_The_Minimum_Keeps_Each_Control_Free";

    private const string ProbeRunning = "  <Test Name=\"Engine.Tests.Done.Passes\" Source=\"Engine.Tests.dll\" Completed=\"True\" />\n" +
        "  <Test Name=\"Engine.Tests.Probe.Run\" Source=\"Engine.Tests.dll\" Completed=\"False\" />\n";

    private const string Dump = "testhost_4242_20261121T000000_crashdump.dmp";

    private static string RunInfo(string outcome, string text) =>
        $"      <RunInfo computerName=\"runner\" outcome=\"{outcome}\" timestamp=\"2026-11-21T00:00:00\">\n" +
        $"        <Text>{System.Security.SecurityElement.Escape(text)}</Text>\n      </RunInfo>\n";

    private static string Report(string reason, string sequence, params string[] dumps) =>
        ReportWith(RunInfo("Error", reason), "", sequence, dumps);

    // The annotations that the script writes for a folder with one .trx file (the given run infos and results), the given
    // sequence and the given dumps.
    private static string ReportWith(string runInfos, string results, string sequence, params string[] dumps)
    {
        var folder = Path.Combine(Path.GetTempPath(), $"crash-report-{Guid.NewGuid():N}");
        var attachments = Path.Combine(folder, Guid.NewGuid().ToString());
        Directory.CreateDirectory(attachments);
        try
        {
            File.WriteAllText(Path.Combine(folder, "run.trx"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">\n" +
                $"  <Results>{results}</Results>\n  <ResultSummary outcome=\"Failed\">\n    <RunInfos>\n" + runInfos +
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

    private static string[] Errors(string report) =>
        report.Split('\n').Where(l => l.StartsWith("::error ", StringComparison.Ordinal)).Select(l => l.TrimEnd('\r')).ToArray();

    // A crash during a test: the message starts with the test that did not complete, gives the dump, the lines of createdump
    // and the report of the runtime, and leaves out the output of the tests.
    [Fact]
    public void A_Crash_Names_The_Running_Test_The_Dump_And_The_Report_Of_The_Runtime()
    {
        var report = Report(Crash, ProbeRunning, Dump);

        var line = Assert.Single(Errors(report));
        Assert.StartsWith("::error title=Failed test on Linux%3A Engine.Tests.Probe.Run::Engine.Tests.Probe.Run: the test did not " +
            "complete, because the test host stopped while it ran. Dumps of the crash: testhost_4242_20261121T000000_crashdump.dmp. " +
            "Cause: The active test run was aborted. Reason: Test host process crashed. [createdump] Gathering state for process " +
            "4242 testhost [createdump] Dump successfully written in 312ms 40 lines of the output of the tests are left out. " +
            "Unhandled exception. System.InvalidOperationException: probe of the crash%0A", line, StringComparison.Ordinal);
        Assert.DoesNotContain(Output, line, StringComparison.Ordinal);
    }

    // The facts of run 44908442870: the message starts with the running test from the sequence file, says that there is no
    // dump, and gives the words of the platform and the reason of the data collection, and not the output of other tests.
    [Fact]
    public void A_Crash_Like_Run_44908442870_Names_The_Running_Test_First_And_Leaves_Out_The_Output_Of_Other_Tests()
    {
        var reason = Head + string.Concat(Enumerable.Repeat(ImageOutput, 20));
        var sequence = "  <Test Name=\"ThreeDEngine.ImageTests.SheetPageTests.A_Drag_Gives_One_Move\" Source=\"3DEngine.ImageTests.dll\" Completed=\"True\" />\n" +
            $"  <Test Name=\"{ImageTest}\" Source=\"3DEngine.ImageTests.dll\" Completed=\"False\" />\n";

        var report = ReportWith(RunInfo("Error", reason) + RunInfo("Error", ChannelFailed), "", sequence);

        var line = Assert.Single(Errors(report));
        Assert.StartsWith($"::error title=Failed test on Linux%3A {ImageTest}::{ImageTest}: the test did not complete, because the " +
            "test host stopped while it ran. No dump of the crash. Cause: The active test run was aborted. Reason: Test host " +
            "process crashed. 40 lines of the output of the tests are left out. The error output holds no report of the runtime. " +
            ChannelFailed, line, StringComparison.Ordinal);
        Assert.DoesNotContain("bracket.stl", line, StringComparison.Ordinal);
        Assert.DoesNotContain("did not exist", line, StringComparison.Ordinal);
    }

    // TASK-0441 (phase P0.176): the stop of attempt 1 of run 44758589835 and of run 44908442870 was the hang limit of 2 minutes
    // of the step "Test", and not a crash. The blame data collector says so in a message with the outcome Warning. A local run
    // of the test with a limit of 20 seconds gave the same reason, the sequence file, no dump, and that message. The annotation
    // names the hang limit, and it does not say that a dump of a crash is missing.
    [Fact]
    public void A_Stop_At_The_Hang_Limit_Names_The_Limit_And_Not_A_Crash()
    {
        var reason = Head + string.Concat(Enumerable.Repeat(ImageOutput, 20));
        var hang = "Data collector 'Blame' message: The specified inactivity time of 2 minutes has elapsed. Collecting hang dumps " +
            "from testhost and its child processes.";
        var sequence = $"  <Test Name=\"{ImageTest}\" Source=\"3DEngine.ImageTests.dll\" Completed=\"False\" />\n";

        var report = ReportWith(RunInfo("Error", reason) + RunInfo("Error", ChannelFailed) + RunInfo("Warning", hang), "", sequence);

        var line = Assert.Single(Errors(report));
        Assert.StartsWith($"::error title=Failed test on Linux%3A {ImageTest}::{ImageTest}: the test did not complete, because the " +
            "test host stopped while it ran. A test ran longer than the hang limit of 2 minutes, so the test platform stopped the " +
            "test host: a hang, not a crash. Cause: The active test run was aborted. Reason: Test host process crashed.", line,
            StringComparison.Ordinal);
        Assert.DoesNotContain("No dump of the crash", line, StringComparison.Ordinal);
    }

    // A crash with no sequence file and no dump: the annotation of the step says that the crashed test is not known, and the
    // cause leaves out the output of the tests.
    [Fact]
    public void A_Crash_With_No_Sequence_File_Says_That_The_Crashed_Test_Is_Not_Known()
    {
        var reason = Head + string.Concat(Enumerable.Repeat(Output + "\n", 40)) + "the last line of the test host";

        var report = Report(reason, "");

        var line = Assert.Single(Errors(report));
        Assert.StartsWith("::error title=Test step failed on Linux::The test host stopped, and no .trx file in '", line, StringComparison.Ordinal);
        Assert.Contains("' names a failed test. No sequence file: the crashed test is not known. Files read: 1. No dump of the crash. " +
            "Cause: The active test run was aborted. Reason: Test host process crashed. 41 lines of the output of the tests are left " +
            "out. The error output holds no report of the runtime.", line, StringComparison.Ordinal);
        Assert.DoesNotContain(Output, line, StringComparison.Ordinal);
        Assert.DoesNotContain("the last line of the test host", line, StringComparison.Ordinal);
    }

    // A crash whose sequence file names no running test says that, and not that there is no sequence file.
    [Fact]
    public void A_Crash_Whose_Sequence_Names_No_Running_Test_Says_So()
    {
        var report = Report(Crash, "  <Test Name=\"Engine.Tests.Done.Passes\" Source=\"Engine.Tests.dll\" Completed=\"True\" />\n", Dump);

        var line = Assert.Single(Errors(report));
        Assert.Contains("' names a failed test. No sequence file names a running test: the crashed test is not known. Files read: 1. " +
            "Dumps of the crash: testhost_4242_20261121T000000_crashdump.dmp. Cause: ", line, StringComparison.Ordinal);
    }

    // With no sequence file, the running test that the blame data collector names in the .trx file is the name.
    [Fact]
    public void A_Crash_Whose_Trx_Names_The_Running_Test_Names_It_With_No_Sequence_File()
    {
        var blame = "Data collector 'Blame' message: The test running when the crash occurred: \nEngine.Tests.Probe.Run\n" +
            "This test may, or may not be the source of the crash.";

        var report = ReportWith(RunInfo("Error", Crash) + RunInfo("Warning", blame), "", "");

        var line = Assert.Single(Errors(report));
        Assert.StartsWith("::error title=Failed test on Linux%3A Engine.Tests.Probe.Run::Engine.Tests.Probe.Run: the test did not " +
            "complete, because the test host stopped while it ran. No dump of the crash. Cause: ", line, StringComparison.Ordinal);
    }

    // A failed test beside a crash with no known running test: the crash gets its own annotation first, and the failed test
    // keeps its annotation.
    [Fact]
    public void A_Failed_Test_Beside_A_Crash_With_No_Sequence_File_Gives_The_Crash_Its_Own_Annotation()
    {
        var failed = "<UnitTestResult testName=\"Engine.Tests.Other.Fails\" outcome=\"Failed\"><Output><ErrorInfo>" +
            "<Message>expected 1</Message></ErrorInfo></Output></UnitTestResult>";

        var report = ReportWith(RunInfo("Error", Crash), failed, "");

        var lines = Errors(report);
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("::error title=Test host stopped on Linux::No sequence file: the crashed test is not known. No dump of the " +
            "crash. Cause: The active test run was aborted.", lines[0], StringComparison.Ordinal);
        Assert.StartsWith("::error title=Failed test on Linux%3A Engine.Tests.Other.Fails::expected 1", lines[1], StringComparison.Ordinal);
    }

    // TASK-0449 (finding T191 of the third codebase review of 2026-11-21): with ten failed tests or more, the test that was
    // running at the stop is the first failure, so its message has the first annotation and does not go into "N more" as a
    // bare name.
    [Fact]
    public void With_Ten_Failed_Tests_The_Running_Test_Of_A_Stop_Has_The_First_Annotation()
    {
        var failed = string.Concat(Enumerable.Range(0, 10).Select(i =>
            $"<UnitTestResult testName=\"Engine.Tests.Other.Fails{i}\" outcome=\"Failed\"><Output><ErrorInfo>" +
            "<Message>expected 1</Message></ErrorInfo></Output></UnitTestResult>"));

        var report = ReportWith(RunInfo("Error", Crash), failed, ProbeRunning, Dump);

        var lines = Errors(report);
        Assert.Equal(10, lines.Length);
        Assert.StartsWith("::error title=Failed test on Linux%3A Engine.Tests.Probe.Run::Engine.Tests.Probe.Run: the test did not " +
            "complete, because the test host stopped while it ran.", lines[0], StringComparison.Ordinal);
        Assert.StartsWith("::error title=2 more failed tests on Linux::Engine.Tests.Other.Fails8%0AEngine.Tests.Other.Fails9", lines[^1], StringComparison.Ordinal);
    }

    // A long report of the runtime is cut, so that the reason of the data collection after it stays inside the cut of the
    // message.
    [Fact]
    public void Each_Cause_Is_Cut_So_That_A_Second_Cause_Stays_In_The_Message()
    {
        var reason = Head + "Unhandled exception. System.InvalidOperationException: deep\n" +
            string.Concat(Enumerable.Repeat("   at Engine.Tests.Probe.Deep()\n", 200));

        var report = ReportWith(RunInfo("Error", reason) + RunInfo("Error", ChannelFailed), "", ProbeRunning);

        var line = Assert.Single(Errors(report));
        Assert.StartsWith("::error title=Failed test on Linux%3A Engine.Tests.Probe.Run::Engine.Tests.Probe.Run: ", line, StringComparison.Ordinal);
        Assert.Contains(" ... " + ChannelFailed, line, StringComparison.Ordinal);
    }
}
