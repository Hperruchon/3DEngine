using System.Text.RegularExpressions;

namespace WriteSetCheck;

// The judge of the write set. Register entry R-0017 recorded the gap that it
// closes: each task declares a create list, a modify list and a forbid list, and
// each change must stay inside the write set of the task that governs it.
//
// Until TASK-0053 these rules were a test in Engine.Tests, and the pipeline ran
// that test from the build of the commit that it judged. A file under
// Engine.Tests/ could clear the variable that the test read, so a commit could
// switch off its own judge (codebase review of 2026-10-08, finding T14). Now the
// pipeline builds this program from main, and a commit on a branch cannot change
// the code that judges it.
//
// Three rules came from the codebase review of 2026-09-30, finding T3. A commit
// that names a task also changes the file of that task, so a closed task cannot
// lend its permits to a later commit. A gate file must be named exactly, so a
// pattern such as Engine.Tests/** does not permit a change to a gate. The
// cut-off commit is held here, so a commit cannot move it.
//
// TASK-0047 added two rules from the review of 2026-10-04: the judge reads the
// governing task at the commit, and a task that is Done before and after the
// commit governs nothing.
//
// TASK-0057 closed the holes of the review of 2026-10-09: git comes from outside
// the judged checkout (T29); a commit with no parent fails, and a merge is judged
// against its first parent (T30); a test file cannot hold a call that redirects a
// gate (T31); a task is found by its identifier at the commit (T33); and each
// commit that changes Engine.Contracts/ also changes docs/adr/ (T26).
public sealed class Judge
{
    // TASK-0026 turned the gate on at this commit. eng/write-set-cutoff.txt
    // gives the same value, and a static test holds the two equal.
    public const string CutOff = "0609f13070d6917ee80f3aa14ecb553972b5efcf";

    // The two rules of AtTheCommit apply to each commit after this one, the merge
    // of the codebase review of 2026-10-04. Three earlier commits changed a task
    // that was already Done: f2d96a4, 7a37bc7 and 4715b88. They keep the earlier
    // rule.
    public const string CommitRulesFrom = "67c564ff65d0fcd8490d7be01183d31d28513e27";

    // A test file that holds one of these texts can point a gate of Engine.Tests
    // at another folder or switch off a check before the gate runs (codebase
    // review of 2026-10-09, finding T31; the owner chose this option on
    // 2026-10-10). Each text is in two parts, so that this file does not hold it.
    public static readonly IReadOnlyList<string> RedirectingCalls =
    [
        "Module" + "Initializer",
        "AppContext" + ".SetData",
        "SetEnvironment" + "Variable",
        "SetCurrent" + "Directory",
    ];

    // A trailer line names the governing task: TASK-nnnn, optionally followed by
    // ' · ADR-nnnn' or ' · TASK-nnnn'. A prose line that starts with an
    // identifier is not a trailer (TASK-0039).
    private static readonly Regex Trailer = new(
        @"^TASK-[0-9]{4}( · (ADR|TASK)-[0-9]{4})*[ \t]*$", RegexOptions.Multiline | RegexOptions.Compiled);

    private readonly Func<string[], string?> _git;

    // The git function runs git on the repository under judgment and gives its
    // standard output, or null when git fails; a test gives a function with
    // fixed answers.
    public Judge(Func<string[], string?> git)
    {
        _git = git;
    }

    // A file under Engine.Tests/Governance/, or a test class whose name ends in
    // GateTests, or the shared helpers of the gates. Also each file that can turn
    // a gate off without a change to a gate: the project file of the tests,
    // which can remove a gate from the compilation; a Directory.Build or
    // Directory.Packages file, which MSBuild reads for each project below it;
    // the cut-off of this gate; each workflow (codebase review of 2026-10-04,
    // finding T9); and each file of this program (TASK-0053).
    public static bool IsGateFile(string file)
    {
        var name = file[(file.LastIndexOf('/') + 1)..];

        return file.StartsWith("Engine.Tests/Governance/", StringComparison.Ordinal)
            || (file.StartsWith("Engine.Tests/", StringComparison.Ordinal) && file.EndsWith("GateTests.cs", StringComparison.Ordinal))
            || file.StartsWith("Engine.Tests/Diagnostics/", StringComparison.Ordinal)
            || file == "Engine.Tests/Engine.Tests.csproj"
            || name.StartsWith("Directory.Build.", StringComparison.Ordinal)
            || name == "Directory.Packages.props"
            || file == "eng/write-set-cutoff.txt"
            || file.StartsWith("eng/write-set-check/", StringComparison.Ordinal)
            || file.StartsWith(".github/workflows/", StringComparison.Ordinal);
    }

    // The full path of git from the folders of PATH, never from the judged
    // checkout or from a relative folder (codebase review of 2026-10-09, finding
    // T29). A program that starts "git" by its name finds a program of that name
    // in the current folder first, on Windows and with .NET on Linux, and a
    // commit could carry such a program. Null when no git is found.
    public static string? FindGit(string root, string? pathVariable)
    {
        var name = OperatingSystem.IsWindows() ? "git.exe" : "git";
        var inside = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (var folder in (pathVariable ?? string.Empty).Split(Path.PathSeparator))
        {
            var trimmed = folder.Trim().Trim('"');
            if (trimmed.Length == 0 || !Path.IsPathFullyQualified(trimmed))
                continue;

            var full = Path.GetFullPath(trimmed).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (full.StartsWith(inside, comparison))
                continue;

            var candidate = Path.Combine(full, name);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    // The governing task that the commit message names, or null.
    public static string? TrailerTask(string message)
    {
        var match = Trailer.Match(message.Replace("\r", string.Empty, StringComparison.Ordinal));
        return match.Success ? match.Value[..9] : null;
    }

    // The paths of a git command that ran with -z: each path ends with a NUL
    // character, and git does not quote it (codebase review of 2026-10-09,
    // finding T35).
    public static HashSet<string> ParseGitPaths(string raw)
        => raw.Split('\0', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);

    // Each commit after the base up to HEAD, one at a time. A commit before the
    // cut-off is not judged. A commit with no parent fails, because it can carry
    // any tree. A merge is judged on each file that differs from its first
    // parent and that no commit on the side of its second parent changed: that is
    // the change that the merge made itself (finding T30). Until TASK-0057 a
    // merge was read with `git diff-tree --cc`, which hides each file that equals
    // one parent, so a merge that took the tree of an old commit passed.
    //
    // A strict range fails when the base is not known; without it, an unknown
    // base falls back to the cut-off. Exit code 0 or 1.
    public int CheckRange(string? before, bool strict, TextWriter output)
    {
        var known = !string.IsNullOrWhiteSpace(before) && before.Trim('0').Length > 0
            && _git(["cat-file", "-e", before + "^{commit}"]) is not null;
        if (!known)
        {
            if (strict)
            {
                output.WriteLine($"FAIL  the base of the range, '{before}', is not known, so the judge cannot say which commits are new");
                output.WriteLine($"::error title=Write set::the base of the range, '{before}', is not known");
                return 1;
            }

            before = CutOff;
        }

        output.WriteLine($"range: {before}..HEAD");

        var list = _git(["rev-list", "--reverse", $"{before}..HEAD"]);
        if (list is null)
            return Fail(output, $"git rev-list on {before}..HEAD", "git failed");

        var failed = 0;
        foreach (var commit in list.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var subject = _git(["log", "-1", "--format=%h %s", commit])?.Trim() ?? commit;

            if (_git(["merge-base", "--is-ancestor", commit, CutOff]) is not null)
            {
                output.WriteLine($"skip  {subject}  (before the cut-off)");
                continue;
            }

            var parents = _git(["rev-list", "--parents", "-n", "1", commit]);
            if (parents is null)
            {
                failed |= Fail(output, subject, "git cannot read the parents of the commit");
                continue;
            }

            var count = parents.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length - 1;
            if (count == 0)
            {
                failed |= Fail(output, subject, "the commit has no parent, so it can carry any tree; a commit after the cut-off must have a parent");
                continue;
            }

            if (count > 2)
            {
                failed |= Fail(output, subject, "the merge has more than two parents; merge one branch at a time");
                continue;
            }

            string? files;
            if (count == 2)
            {
                var own = _git(["diff", "--name-only", "-z", "--no-renames", $"{commit}^1", commit]);
                var brought = _git(["diff", "--name-only", "-z", "--no-renames", $"{commit}^1...{commit}^2"]);
                if (own is null || brought is null)
                {
                    failed |= Fail(output, subject, "the two parents of the merge have no merge base, or git failed; a merge of unrelated history is refused");
                    continue;
                }

                var mergeOwn = ParseGitPaths(own);
                mergeOwn.ExceptWith(ParseGitPaths(brought));
                if (mergeOwn.Count == 0)
                {
                    output.WriteLine($"pass  {subject}  (a merge with no change of its own)");
                    continue;
                }

                files = string.Join('\0', mergeOwn);
            }
            else
            {
                files = _git(["diff-tree", "--no-commit-id", "--name-only", "-r", "-z", "--no-renames", commit]);
                if (files is null)
                {
                    failed |= Fail(output, subject, "git cannot list the files of the commit");
                    continue;
                }
            }

            var message = _git(["log", "-1", "--format=%B", commit]);
            if (message is null)
            {
                failed |= Fail(output, subject, "git cannot read the message of the commit");
                continue;
            }

            var task = TrailerTask(message);
            failed |= Report(output, $"{subject}  (task: {task ?? "none named"})", Check(commit, ParseGitPaths(files), task));
        }

        return failed;
    }

    // The commit where the current branch left the given reference, for a run on
    // a branch: each commit of the branch is judged, and not only the commits of
    // the last push (finding T32). Null when git cannot find it.
    public string? MergeBase(string reference) => _git(["merge-base", reference, "HEAD"])?.Trim();

    // Each problem of one change, or none. The commit is null for a change that
    // is not yet a commit; the judge then reads the index and HEAD.
    public IReadOnlyList<string> Check(string? commit, IReadOnlyCollection<string> changed, string? namedTask)
    {
        var set = changed.ToHashSet(StringComparer.Ordinal);
        if (set.Count == 0)
            return [];

        var problems = new List<string>();

        // CLAUDE.md, section "Stop and ask": a change to Engine.Contracts needs an
        // ADR in the same commit. Until TASK-0057 a job of the workflow checked a
        // range of commits, so one ADR change in a push let each contract change
        // pass (finding T26).
        if (set.Any(f => f.StartsWith("Engine.Contracts/", StringComparison.Ordinal))
            && !set.Any(f => f.StartsWith("docs/adr/", StringComparison.Ordinal)))
        {
            problems.Add("The change modifies Engine.Contracts/ and no file under docs/adr/. A change to the public "
                + "shape of Engine.Contracts needs an ADR in the same commit (CLAUDE.md, section \"Stop and ask\").");
        }

        problems.AddRange(RedirectingTestFiles(commit, set));

        var (governing, problem) = GoverningTask(commit, set, namedTask);
        if (governing is null)
        {
            problems.Add(problem!);
            return problems;
        }

        var outside = FilesOutsideTheWriteSet(governing, set);
        if (outside.Count > 0)
        {
            problems.Add(
                "Register entry R-0017: the writes block of the governing task must cover each changed "
                + "file. Add the path to the create list or the modify list, or do not change the file. "
                + $"Governing task: {governing.Id}. Problems:\n  " + string.Join("\n  ", outside));
        }

        return problems;
    }

    // Each changed file that the write set does not permit, with the reason.
    public static List<string> FilesOutsideTheWriteSet(TaskRecord governing, IEnumerable<string> changed)
    {
        var permitted = governing.Written.Select(TaskFiles.PathPattern).ToArray();
        var refused = governing.Forbid.Select(pattern => (Rule: TaskFiles.PathPattern(pattern), pattern)).ToArray();
        var problems = new List<string>();

        foreach (var file in changed.OrderBy(f => f, StringComparer.Ordinal))
        {
            // A gate file is named exactly or not at all. A pattern that covers
            // the tests must not let a task change the gate that reads it.
            if (IsGateFile(file))
            {
                if (!governing.Written.Contains(file, StringComparer.Ordinal))
                    problems.Add($"{file} is a gate file, and {governing.Id} does not name it exactly; a pattern does not permit a gate file");
                continue;
            }

            // A permit beats a forbid inside one task, because a task that both
            // permits and forbids a path is refused by a static test. A forbid
            // gives the better message when a file is in no list, because it
            // names the boundary that the author wrote.
            if (permitted.Any(rule => rule.IsMatch(file)))
                continue;

            var blocked = refused.FirstOrDefault(r => r.Rule.IsMatch(file));

            problems.Add(blocked.Rule is not null
                ? $"{file} matches the forbid pattern '{blocked.pattern}' of {governing.Id}"
                : $"{file} is in no create list and in no modify list of {governing.Id}");
        }

        return problems;
    }

    // Each changed file under Engine.Tests/ that holds a redirecting call, read at
    // the commit, or in the index before a commit. A deleted file holds nothing.
    private IEnumerable<string> RedirectingTestFiles(string? commit, HashSet<string> changed)
    {
        foreach (var file in changed.Where(f => f.StartsWith("Engine.Tests/", StringComparison.Ordinal)).OrderBy(f => f, StringComparer.Ordinal))
        {
            var text = _git(["show", $"{Spec(commit)}{file}"]);
            if (text is null)
                continue;

            foreach (var call in RedirectingCalls.Where(c => text.Contains(c, StringComparison.Ordinal)))
                yield return $"{file} holds {call}, which can redirect a gate of Engine.Tests before it runs (finding T31 of the review of 2026-10-09). Remove it.";
        }
    }

    // The task that governs a change. The commit message names it, as a trailer
    // line of the form TASK-nnnn. The commit also changes the file of that task,
    // so a closed task cannot lend its permits to a later commit. When no name
    // is given, the one task file that the change touches governs it. A change
    // that touches several task files and names none is refused: register entry
    // R-0026 recorded that such a change received the permits of each task.
    //
    // The task files come from the commit, and not from the tip, so a later
    // rename of a task file does not fail the earlier commits of its task
    // (finding T33).
    private (TaskRecord? Task, string? Problem) GoverningTask(string? commit, HashSet<string> changed, string? named)
    {
        var listing = commit is null
            ? _git(["ls-files", "-z", "--", "tasks"])
            : _git(["ls-tree", "-r", "-z", "--name-only", commit, "--", "tasks"]);
        if (listing is null)
            return (null, "git cannot list the task files of the commit.");

        var taskPaths = ParseGitPaths(listing)
            .Where(p => Regex.IsMatch(p, @"^tasks/TASK-[0-9]{4}-[^/]*\.md$"))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        string path;
        if (!string.IsNullOrEmpty(named))
        {
            var found = taskPaths.Where(p => p.StartsWith($"tasks/{named}-", StringComparison.Ordinal)).ToArray();
            if (found.Length != 1)
                return (null, $"The commit names {named}, and no task file with that identifier, or more than one, exists at "
                    + "the commit. Name a task with front matter, or correct the identifier.");

            path = found[0];
            if (!changed.Contains(path))
                return (null, $"The commit names {named} and does not change its file. A commit records its progress "
                    + "in the task that governs it, with one line under \"Progress\" at least, so that a "
                    + "closed task cannot lend its permits to a later commit.");
        }
        else
        {
            var touched = taskPaths.Where(changed.Contains).ToArray();

            if (touched.Length == 0)
                return (null, "This change names no task and touches no task file, therefore no write set governs it. "
                    + "Name the task in the commit message, as a trailer line of the form TASK-nnnn, or add the "
                    + "task file to the change.\n  Changed files:\n  " + string.Join("\n  ", changed));

            if (touched.Length > 1)
                return (null, "This change touches several task files and names none. Only one task governs a commit. "
                    + "Name it in the commit message, as a trailer line of the form TASK-nnnn. Touched: "
                    + string.Join(", ", touched));

            path = touched[0];
        }

        return AtTheCommit(commit, path);
    }

    // The governing task as the commit saw it (codebase review of 2026-10-04,
    // finding T11). Before a commit there is no hash, and the judge reads the
    // index and HEAD.
    //
    // A task that is Done before the commit and after it governs nothing: a
    // correction reopens the task with the status Active and closes it again
    // (question Q3 of the same review, which the owner accepted).
    private (TaskRecord? Task, string? Problem) AtTheCommit(string? commit, string path)
    {
        var after = _git(["show", $"{Spec(commit)}{path}"]);
        var task = after is null ? null : TaskFiles.ParseTask(path, after);
        if (task is null)
            return (null, $"The judge cannot read the front matter of {path} at the commit.");

        if (commit is not null && _git(["merge-base", "--is-ancestor", commit, CommitRulesFrom]) is not null)
            return (task, null);

        var before = _git(["show", commit is null ? $"HEAD:{path}" : $"{commit}^:{path}"]);
        var statusBefore = before is null ? null : TaskFiles.ParseTask(path, before)?.Status;

        if (statusBefore == "Done" && task.Status == "Done")
            return (null, $"{task.Id} is Done before this commit and after it, therefore it governs no change. To "
                + "correct the work of a closed task, set its status to Active in the commit that changes "
                + "the work, and to Done again when the correction is complete. See docs/templates.md, "
                + "section 3.");

        return (task, null);
    }

    private static string Spec(string? commit) => commit is null ? ":" : $"{commit}:";

    private static int Fail(TextWriter output, string what, string problem)
        => Report(output, what, [problem]);

    // One line for each change, and a public annotation for each problem, so
    // that a person with no sign-in can read the reason.
    public static int Report(TextWriter output, string what, IReadOnlyList<string> problems)
    {
        if (problems.Count == 0)
        {
            output.WriteLine($"pass  {what}");
            return 0;
        }

        output.WriteLine($"FAIL  {what}");
        foreach (var problem in problems)
        {
            output.WriteLine("  " + problem);
            output.WriteLine($"::error title=Write set::{what}: {problem.Split('\n')[0]}");
        }

        return 1;
    }
}
