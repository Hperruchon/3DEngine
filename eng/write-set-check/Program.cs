using System.Diagnostics;
using WriteSetCheck;

// The write-set check (TASK-0053). Run it in the root of the repository that it
// judges.
//
//   write-set-check --range <before>        each commit after <before> up to HEAD;
//                                           the pipeline gives the previous tip of
//                                           the push, and an empty, zero or unknown
//                                           value falls back to the cut-off commit
//   write-set-check --staged --task TASK-n  the staged change, before a commit
//
// Exit code 0: each change is inside its write set. 1: a change is outside it.
// 2: the arguments are wrong.

var root = Directory.GetCurrentDirectory();
var judge = new Judge(root, Git);

if (args.Length == 2 && args[0] == "--range")
    return CheckRange(args[1]);

if (args.Length == 3 && args[0] == "--staged" && args[1] == "--task")
    return Report("the staged change", judge.Check(null, Judge.ParseChangedFiles(Git(["diff", "--cached", "--name-only", "--no-renames"]) ?? string.Empty), args[2]));

Console.Error.WriteLine("usage: write-set-check --range <commit> | --staged --task TASK-nnnn");
return 2;

// The same steps as the job of TASK-0026 to TASK-0047, which held them in the
// workflow: one commit at a time, a commit before the cut-off is not judged,
// and a merge is judged on the changes that the merge made itself, which
// `git diff-tree --cc` lists.
int CheckRange(string before)
{
    if (string.IsNullOrWhiteSpace(before) || before.Trim('0').Length == 0 || Git(["cat-file", "-e", before]) is null)
        before = Judge.CutOff;
    Console.WriteLine($"range: {before}..HEAD");

    var list = Git(["rev-list", "--reverse", $"{before}..HEAD"]);
    if (list is null)
    {
        Console.WriteLine($"FAIL: git rev-list on {before}..HEAD");
        return 1;
    }

    var failed = 0;
    foreach (var commit in list.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        var subject = Git(["log", "-1", "--format=%h %s", commit])?.Trim() ?? commit;

        if (Git(["merge-base", "--is-ancestor", commit, Judge.CutOff]) is not null)
        {
            Console.WriteLine($"skip  {subject}  (before the cut-off)");
            continue;
        }

        var parents = (Git(["rev-list", "--parents", "-n", "1", commit]) ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).Length - 1;

        string files;
        if (parents > 1)
        {
            files = Git(["diff-tree", "--cc", "--no-commit-id", "--name-only", "-r", commit]) ?? string.Empty;
            if (files.Trim().Length == 0)
            {
                Console.WriteLine($"pass  {subject}  (a merge with no change of its own)");
                continue;
            }
        }
        else
        {
            files = Git(["diff-tree", "--no-commit-id", "--name-only", "-r", commit]) ?? string.Empty;
        }

        var task = Judge.TrailerTask(Git(["log", "-1", "--format=%B", commit]) ?? string.Empty);
        failed |= Report($"{subject}  (task: {task ?? "none named"})", judge.Check(commit, Judge.ParseChangedFiles(files), task));
    }

    return failed;
}

static int Report(string what, IReadOnlyList<string> problems)
{
    if (problems.Count == 0)
    {
        Console.WriteLine($"pass  {what}");
        return 0;
    }

    Console.WriteLine($"FAIL  {what}");
    foreach (var problem in problems)
    {
        Console.WriteLine("  " + problem);
        // A public annotation names the commit for a person with no sign-in.
        Console.WriteLine($"::error title=Write set::{what}: {problem.Split('\n')[0]}");
    }

    return 1;
}

// The standard output of a git command in the root of the repository, or null
// when git fails or is absent.
string? Git(string[] arguments)
{
    var start = new ProcessStartInfo("git")
    {
        WorkingDirectory = root,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        StandardOutputEncoding = System.Text.Encoding.UTF8,
    };
    foreach (var argument in arguments)
        start.ArgumentList.Add(argument);

    try
    {
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 ? output.Result : null;
    }
    catch (System.ComponentModel.Win32Exception)
    {
        return null;
    }
}
