using System.Diagnostics;
using WriteSetCheck;

// The write-set check (TASK-0053, TASK-0057). Run it in the root of the
// repository that it judges.
//
//   write-set-check --range <base> [--strict]   each commit after <base> up to HEAD;
//                                               without --strict an empty, zero or
//                                               unknown base falls back to the
//                                               cut-off commit
//   write-set-check --merge-base <reference>    each commit after the point where
//                                               HEAD left <reference>, for a branch
//   write-set-check --staged --task TASK-nnnn   the staged change, before a commit
//
// Exit code 0: each change is inside its write set. 1: a change is outside it,
// or git failed. 2: the arguments are wrong.

var root = Directory.GetCurrentDirectory();
var gitPath = Judge.FindGit(root, Environment.GetEnvironmentVariable("PATH"));
if (gitPath is null)
{
    Console.WriteLine("FAIL  no git in a folder of PATH outside the judged checkout");
    return 1;
}

var judge = new Judge(Git);

if (args is ["--range", var start])
    return judge.CheckRange(start, strict: false, Console.Out);

if (args is ["--range", var strictStart, "--strict"])
    return judge.CheckRange(strictStart, strict: true, Console.Out);

if (args is ["--merge-base", var reference])
{
    var mergeBase = judge.MergeBase(reference);
    if (mergeBase is null)
        return Judge.Report(Console.Out, $"the merge base of HEAD and {reference}", ["git cannot find it"]);
    return judge.CheckRange(mergeBase, strict: true, Console.Out);
}

if (args is ["--staged", "--task", var task])
{
    var staged = Git(["diff", "--cached", "--name-only", "-z", "--no-renames"]);
    if (staged is null)
        return Judge.Report(Console.Out, "the staged change", ["git cannot list the staged files"]);
    return Judge.Report(Console.Out, "the staged change", judge.Check(null, Judge.ParseGitPaths(staged), task));
}

Console.Error.WriteLine("usage: write-set-check --range <commit> [--strict] | --merge-base <reference> | --staged --task TASK-nnnn");
return 2;

// The standard output of git on the judged repository, or null when git fails.
// Git runs by its full path, and `-C` names the repository, so no program in the
// judged checkout runs (finding T29).
string? Git(string[] arguments)
{
    var start = new ProcessStartInfo(gitPath)
    {
        WorkingDirectory = Path.GetTempPath(),
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        StandardOutputEncoding = System.Text.Encoding.UTF8,
    };
    start.ArgumentList.Add("-C");
    start.ArgumentList.Add(root);
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
