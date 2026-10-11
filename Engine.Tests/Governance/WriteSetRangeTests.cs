using System.Diagnostics;
using WriteSetCheck;
using Xunit;

namespace Engine.Tests.Governance;

// The steps of the judge of the write set over a range of commits (TASK-0057).
// Each test makes a scratch repository with git, makes commits in it, and runs
// the real program, eng/write-set-check, on it. A gate job clones one commit
// only, so the tests need no history of this repository.
//
// The codebase review of 2026-10-09 found each attack below with a run of a
// review agent (T26 and T28 to T35). Before TASK-0057 each test of an attack
// failed: the judge passed the change.
public class WriteSetRangeTests
{
    // The judge refuses this word in a test file, so the source of this test
    // holds it in two parts (T31).
    private const string Initializer = "Module" + "Initializer";

    [Fact]
    public void A_Forbidden_Change_Fails_And_A_Permitted_Change_Passes()
    {
        using var repo = ScratchRepository.WithTask();
        var start = repo.Head;
        repo.CommitUnderTask("Permitted", ("src/a.txt", "a"));
        repo.CommitUnderTask("Forbidden", ("Engine.Core/b.cs", "b"));

        var (exit, output) = repo.Judge("--range", start);

        Assert.Equal(1, exit);
        Assert.Contains("pass  ", output, StringComparison.Ordinal);
        Assert.Contains("Engine.Core/b.cs matches the forbid pattern", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_Commit_With_No_Parent_And_A_Merge_Of_Unrelated_History_Fail()
    {
        using var repo = ScratchRepository.WithTask();
        var start = repo.Head;
        repo.Git("checkout", "-q", "--orphan", "orphan");
        repo.Git("rm", "-rq", "--cached", ".");
        repo.Write("Engine.Core/c.cs", "c");
        repo.Git("add", "Engine.Core/c.cs");
        repo.Git("commit", "-qm", "An orphan commit with no trailer");
        repo.Git("checkout", "-qf", "main");
        repo.Git("merge", "-q", "--no-edit", "--allow-unrelated-histories", "orphan");

        var (exit, output) = repo.Judge("--range", start);

        Assert.Equal(1, exit);
        Assert.Contains("FAIL  ", output, StringComparison.Ordinal);
        Assert.Contains("no parent", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_Merge_That_Takes_The_Tree_Of_An_Old_Commit_Fails()
    {
        // Two commits before the range change a file that the task forbids. The
        // judge does not read them.
        using var repo = ScratchRepository.WithTask();
        repo.Write("Engine.Core/x.cs", "old");
        repo.Git("add", "-A");
        repo.Git("commit", "-qm", "Before the range: the old version");
        var old = repo.Head;
        repo.Write("Engine.Core/x.cs", "new");
        repo.Git("add", "-A");
        repo.Git("commit", "-qm", "Before the range: the new version");
        var start = repo.Head;
        repo.CommitUnderTask("A permitted change", ("src/a.txt", "a"));
        var tip = repo.Head;

        // A merge of the tip and the old commit with the tree of the old commit: it puts back the
        // old version of the forbidden file, and --cc lists nothing for it.
        var tree = repo.Git("rev-parse", old + "^{tree}").Trim();
        var merge = repo.Git("commit-tree", tree, "-p", tip, "-p", old, "-m", "Merge an old commit").Trim();
        repo.Git("reset", "-q", "--hard", merge);

        var (exit, output) = repo.Judge("--range", start);

        Assert.Equal(1, exit);
        Assert.Contains("FAIL  ", output, StringComparison.Ordinal);
        Assert.Contains("Merge an old commit", output, StringComparison.Ordinal);
        Assert.Contains("Engine.Core/x.cs", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_Test_File_That_Can_Redirect_A_Gate_Fails()
    {
        using var repo = ScratchRepository.WithTask();
        var start = repo.Head;
        repo.CommitUnderTask("A test file", ("Engine.Tests/Probe.cs", "[" + Initializer + "] internal static void Run() { }"));

        var (exit, output) = repo.Judge("--range", start);

        Assert.Equal(1, exit);
        Assert.Contains("Engine.Tests/Probe.cs", output, StringComparison.Ordinal);
        Assert.Contains(Initializer, output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_Contract_Change_With_No_Adr_In_The_Same_Commit_Fails()
    {
        using var repo = ScratchRepository.WithTask();
        var start = repo.Head;
        repo.CommitUnderTask("An ADR", ("docs/adr/0001-a.md", "a"));
        repo.CommitUnderTask("A contract", ("Engine.Contracts/A.cs", "a"));

        var (exit, output) = repo.Judge("--range", start);

        Assert.Equal(1, exit);
        Assert.Contains("Engine.Contracts", output, StringComparison.Ordinal);
        Assert.Contains("ADR", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_Renamed_Task_File_Does_Not_Fail_The_Earlier_Commits_Of_Its_Task()
    {
        using var repo = ScratchRepository.WithTask();
        var start = repo.Head;
        repo.CommitUnderTask("Work", ("src/a.txt", "a"));
        repo.RenameTaskFile("tasks/TASK-0001-renamed.md");

        var (exit, output) = repo.Judge("--range", start);

        Assert.True(exit == 0, output);
    }

    [Fact]
    public void On_A_Branch_The_Judge_Starts_At_The_Merge_Base()
    {
        using var repo = ScratchRepository.WithTask();
        repo.Git("checkout", "-qb", "work");
        repo.CommitUnderTask("Forbidden", ("Engine.Core/b.cs", "b"));
        var red = repo.Head;
        repo.CommitUnderTask("A later push", ("src/a.txt", "a"));

        var later = repo.Judge("--range", red);
        var branch = repo.Judge("--merge-base", "main");

        Assert.Equal(0, later.Exit);
        Assert.Equal(1, branch.Exit);
        Assert.Contains("Engine.Core/b.cs", branch.Output, StringComparison.Ordinal);
    }

    // Finding T36 of the codebase review of 2026-10-11: the judge of TASK-0057 read
    // a merge on its own change minus the files that the side of its second parent
    // changed. In a merge of main into a branch that side is main, so an edit of a
    // file of main inside the merge was not judged. The old judge, which read a merge
    // with --cc, failed it (TASK-0061).
    [Fact]
    public void A_Merge_Of_Main_Into_A_Branch_That_Rewrites_A_File_Of_Main_Fails()
    {
        using var repo = ScratchRepository.WithTask();
        repo.Git("checkout", "-qb", "work");
        repo.CommitUnderTask("Work on the branch", ("src/a.txt", "a"));
        repo.Git("checkout", "-q", "main");
        repo.AddCoreTask();
        repo.Git("checkout", "-q", "work");
        repo.Git("merge", "-q", "--no-commit", "--no-ff", "main");
        repo.Write("Engine.Core/x.cs", "EVIL");
        repo.Git("add", "-A");
        repo.Git("commit", "-qm", "Merge main into work");

        var (exit, output) = repo.Judge("--merge-base", "main");

        Assert.Equal(1, exit);
        Assert.Contains("Merge main into work", output, StringComparison.Ordinal);
        Assert.Contains("Engine.Core/x.cs", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_Merge_That_Changes_Again_A_File_Of_Its_Second_Parent_Meets_Each_Rule()
    {
        using var repo = ScratchRepository.WithTask();
        var start = repo.Head;
        repo.Git("checkout", "-qb", "work");
        repo.CommitUnderTask("A test, a contract and an ADR",
            ("Engine.Tests/Foo.cs", "class Foo { }"),
            ("Engine.Contracts/C.cs", "class C { }"),
            ("docs/adr/0001-a.md", "a"));
        repo.Git("checkout", "-q", "main");
        repo.Git("merge", "-q", "--no-commit", "--no-ff", "work");
        repo.Write("Engine.Tests/Foo.cs", "[" + Initializer + "] class Foo { }");
        repo.Write("Engine.Contracts/C.cs", "class C { int Field; }");
        repo.CommitUnderTask("Merge work");

        var (exit, output) = repo.Judge("--range", start);

        Assert.Equal(1, exit);
        Assert.Contains(Initializer, output, StringComparison.Ordinal);
        Assert.Contains("Engine.Contracts/ and no file under docs/adr/", output, StringComparison.Ordinal);
    }

    // The test above of an orphan commit also fails on the commit with no parent, so
    // it cannot see the loss of the rule for a merge of unrelated history (finding
    // T37). Here the merge names its task, changes its task file, and brings only a
    // permitted file, so only that rule can fail it.
    [Fact]
    public void A_Merge_Of_Unrelated_History_Fails_By_Itself()
    {
        using var repo = ScratchRepository.WithTask();
        var start = repo.Head;
        repo.Git("checkout", "-q", "--orphan", "orphan");
        repo.Git("rm", "-rq", "--cached", ".");
        repo.Write("src/o.txt", "o");
        repo.Git("add", "src/o.txt");
        repo.Git("commit", "-qm", "An orphan commit");
        repo.Git("checkout", "-qf", "main");
        repo.Git("merge", "-q", "--no-commit", "--allow-unrelated-histories", "orphan");
        repo.CommitUnderTask("Merge unrelated history");

        var (exit, output) = repo.Judge("--range", start);

        Assert.Equal(1, exit);
        Assert.Contains(
            output.Split('\n'),
            line => line.StartsWith("FAIL  ", StringComparison.Ordinal) && line.Contains("Merge unrelated history", StringComparison.Ordinal));
    }

    [Fact]
    public void The_Staged_Change_Is_Judged_Before_A_Commit()
    {
        using var repo = ScratchRepository.WithTask();
        repo.Write("src/a.txt", "a");
        repo.AppendProgress();
        repo.Git("add", "-A");

        var permitted = repo.Judge("--staged", "--task", "TASK-0001");
        repo.Write("Engine.Core/b.cs", "b");
        repo.Git("add", "-A");
        var forbidden = repo.Judge("--staged", "--task", "TASK-0001");

        Assert.Equal(0, permitted.Exit);
        Assert.Equal(1, forbidden.Exit);
        Assert.Contains("Engine.Core/b.cs matches the forbid pattern", forbidden.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void An_Unknown_Previous_Tip_Fails_When_The_Range_Is_Strict()
    {
        using var repo = ScratchRepository.WithTask();

        var (exit, output) = repo.Judge("--range", "0123456789abcdef0123456789abcdef01234567", "--strict");

        Assert.Equal(1, exit);
        Assert.Contains("not known", output, StringComparison.Ordinal);
    }

    [Fact]
    public void The_Judge_Takes_Git_From_Outside_The_Judged_Checkout()
    {
        using var repo = ScratchRepository.WithTask();
        var fake = Path.Combine(repo.Root, OperatingSystem.IsWindows() ? "git.exe" : "git");
        File.WriteAllText(fake, "not a program");
        var path = repo.Root + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");

        var git = Judge.FindGit(repo.Root, path);

        Assert.NotNull(git);
        Assert.False(git!.StartsWith(repo.Root, StringComparison.OrdinalIgnoreCase), git);
    }

    [Fact]
    public void A_Program_Named_Git_In_The_Root_Does_Not_Replace_Git()
    {
        // A program that exits with 0 and writes nothing. On Windows such a
        // program needs a build; the test of FindGit covers Windows.
        if (OperatingSystem.IsWindows())
            return;

        using var repo = ScratchRepository.WithTask();
        var start = repo.Head;
        repo.CommitUnderTask("Forbidden", ("Engine.Core/b.cs", "b"));
        var fake = Path.Combine(repo.Root, "git");
        File.WriteAllText(fake, "#!/bin/sh\nexit 0\n");
        File.SetUnixFileMode(fake, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        var (exit, output) = repo.Judge("--range", start);

        Assert.Equal(1, exit);
        Assert.Contains("Engine.Core/b.cs", output, StringComparison.Ordinal);
    }

    // A scratch repository with one task, TASK-0001, which permits src/**,
    // Engine.Tests/**, Engine.Contracts/** and docs/adr/**, and forbids
    // Engine.Core/**. Each commit under the task adds a progress line to the
    // task file and names the task in a trailer.
    private sealed class ScratchRepository : IDisposable
    {
        private string _taskFile = "tasks/TASK-0001-example.md";
        private int _progress;

        private ScratchRepository()
        {
            Root = Path.Combine(Path.GetTempPath(), "write-set-range-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string Head => Git("rev-parse", "HEAD").Trim();

        public static ScratchRepository WithTask()
        {
            var repo = new ScratchRepository();
            repo.Git("init", "-q", "-b", "main");
            repo.Git("config", "user.name", "Test");
            repo.Git("config", "user.email", "test@example.com");
            repo.Git("config", "core.autocrlf", "false");
            repo.Write(repo._taskFile, """
                ---
                id: 0001
                title: An example
                status: Active
                writes:
                  create:
                    - tasks/TASK-0001-example.md
                    - tasks/TASK-0001-renamed.md
                  modify:
                    - src/**
                    - Engine.Tests/**
                    - Engine.Contracts/**
                    - docs/adr/**
                  forbid:
                    - Engine.Core/**
                ---

                # TASK-0001

                ## Progress

                """);
            repo.Git("add", "-A");
            repo.Git("commit", "-qm", "The task\n\nTASK-0001");
            return repo;
        }

        public void Write(string path, string text)
        {
            var full = Path.Combine(Root, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, text.Replace("\r\n", "\n"));
        }

        public void CommitUnderTask(string subject, params (string Path, string Text)[] files)
        {
            foreach (var (path, text) in files)
                Write(path, text);
            AppendProgress();
            Git("add", "-A");
            Git("commit", "-qm", $"{subject}\n\nTASK-0001");
        }

        public void AppendProgress() => File.AppendAllText(Path.Combine(Root, _taskFile), $"- step {++_progress}\n");

        // A second task, TASK-0002, which permits Engine.Core/**, and one commit
        // under it that changes Engine.Core/x.cs.
        public void AddCoreTask()
        {
            Write("tasks/TASK-0002-core.md", """
                ---
                id: 0002
                title: A change of the core
                status: Active
                writes:
                  create:
                    - tasks/TASK-0002-core.md
                  modify:
                    - Engine.Core/**
                ---

                # TASK-0002

                """);
            Write("Engine.Core/x.cs", "the version of main");
            Git("add", "-A");
            Git("commit", "-qm", "A change of the core\n\nTASK-0002");
        }

        public void RenameTaskFile(string newPath)
        {
            Git("mv", _taskFile, newPath);
            _taskFile = newPath;
            CommitUnderTask("Rename the task file");
        }

        public string Git(params string[] arguments)
        {
            var (exit, output) = Run("git", arguments);
            Assert.True(exit == 0, $"git {string.Join(' ', arguments)} failed: {output}");
            return output;
        }

        public (int Exit, string Output) Judge(params string[] arguments)
            => Run("dotnet", [Path.Combine(AppContext.BaseDirectory, "write-set-check.dll"), .. arguments]);

        private (int Exit, string Output) Run(string program, string[] arguments)
        {
            var start = new ProcessStartInfo(program)
            {
                WorkingDirectory = Root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (var argument in arguments)
                start.ArgumentList.Add(argument);

            using var process = Process.Start(start)!;
            var error = process.StandardError.ReadToEndAsync();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, output + error.Result);
        }

        public void Dispose()
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);
                Directory.Delete(Root, recursive: true);
            }
            catch (IOException)
            {
                // A file that another process still holds stays in the temporary folder.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
