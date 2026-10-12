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
//   write-set-check --gate-sources <base>       prints "judged" when a commit after
//                                               <base> changes a source of the gate
//                                               program, and "main" when none does
//
// Exit code 0: each change is inside its write set, or the answer of
// --gate-sources is printed. 1: a change is outside it, or git failed. 2: the
// arguments are wrong.

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

// The switch of the step "Run the repository gates" of the pipeline. The list
// of the sources is Judge.IsGateProgramSource, which the gate files share; until
// TASK-0066 the workflow held its own list (finding T44 of the codebase review
// of 2026-10-12). Each changed source is printed on the error output.
if (args is ["--gate-sources", var gateBase])
{
    var changed = Git(["diff", "--name-only", "-z", "--no-renames", gateBase, "HEAD"]);
    if (changed is null)
        return Judge.Report(Console.Out, $"the files changed after {gateBase}", ["git cannot list them"]);
    var sources = Judge.ParseGitPaths(changed).Where(Judge.IsGateProgramSource).Order(StringComparer.Ordinal).ToList();
    foreach (var source in sources)
        Console.Error.WriteLine($"a source of the gate program changed: {source}");
    Console.WriteLine(sources.Count > 0 ? "judged" : "main");
    return 0;
}

Console.Error.WriteLine("usage: write-set-check --range <commit> [--strict] | --merge-base <reference> | --staged --task TASK-nnnn | --gate-sources <commit>");
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

        // git merge-tree ends with exit code 1 when the automatic merge has a
        // conflict, and its output is still complete (TASK-0066).
        var complete = process.ExitCode == 0 || (process.ExitCode == 1 && arguments is ["merge-tree", ..]);
        return complete ? output.Result : null;
    }
    catch (System.ComponentModel.Win32Exception)
    {
        return null;
    }
}
