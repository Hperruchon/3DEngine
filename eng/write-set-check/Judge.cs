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
// the code that judges it. The rules did not change in the move.
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

    // A trailer line names the governing task: TASK-nnnn, optionally followed by
    // ' · ADR-nnnn' or ' · TASK-nnnn'. A prose line that starts with an
    // identifier is not a trailer (TASK-0039).
    private static readonly Regex Trailer = new(
        @"^TASK-[0-9]{4}( · (ADR|TASK)-[0-9]{4})*[ \t]*$", RegexOptions.Multiline | RegexOptions.Compiled);

    private readonly string _root;
    private readonly Func<string[], string?> _git;

    // The root is the repository under judgment. The git function runs git in
    // that root and gives its standard output, or null when git fails; a test
    // gives a function with fixed answers.
    public Judge(string root, Func<string[], string?> git)
    {
        _root = root;
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

    // The governing task that the commit message names, or null.
    public static string? TrailerTask(string message)
    {
        var match = Trailer.Match(message.Replace("\r", string.Empty, StringComparison.Ordinal));
        return match.Success ? match.Value[..9] : null;
    }

    // A list of changed paths, one per line. A rename can arrive as
    // "old -> new", which `git status --porcelain` writes and which a person
    // pastes. Both sides are a change and the write set must cover both,
    // therefore the judge splits the arrow.
    public static HashSet<string> ParseChangedFiles(string raw)
        => raw
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(line => line.Split(" -> ", StringSplitOptions.TrimEntries))
            .Where(path => path.Length > 0)
            .Select(path => path.Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);

    // Each problem of one change, or none. The commit is null for a change that
    // is not yet a commit; the judge then reads the index and HEAD.
    public IReadOnlyList<string> Check(string? commit, IReadOnlyCollection<string> changed, string? namedTask)
    {
        var set = changed.ToHashSet(StringComparer.Ordinal);
        if (set.Count == 0)
            return [];

        var (governing, problem) = GoverningTask(commit, set, namedTask);
        if (governing is null)
            return [problem!];

        var outside = FilesOutsideTheWriteSet(governing, set);
        if (outside.Count == 0)
            return [];

        return
        [
            "Register entry R-0017: the writes block of the governing task must cover each changed "
            + "file. Add the path to the create list or the modify list, or do not change the file. "
            + $"Governing task: {governing.Id}. Problems:\n  " + string.Join("\n  ", outside),
        ];
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

    // The task that governs a change. The commit message names it, as a trailer
    // line of the form TASK-nnnn. The commit also changes the file of that task,
    // so a closed task cannot lend its permits to a later commit. When no name
    // is given, the one task file that the change touches governs it. A change
    // that touches several task files and names none is refused: register entry
    // R-0026 recorded that such a change received the permits of each task, so
    // a commit that planned three tasks could also change
    // Engine.Core/CommandBus.cs with no report.
    private (TaskRecord? Task, string? Problem) GoverningTask(string? commit, HashSet<string> changed, string? named)
    {
        var tasks = TaskFiles.Tasks(_root);

        if (!string.IsNullOrEmpty(named))
        {
            var task = tasks.FirstOrDefault(t => t.Id == named);
            if (task is null)
                return (null, $"The commit names {named}, and no task file with a write set has that identifier. "
                    + "Name a task with front matter, or correct the identifier.");

            if (!changed.Contains(Relative(task.File)))
                return (null, $"The commit names {named} and does not change its file. A commit records its progress "
                    + "in the task that governs it, with one line under \"Progress\" at least, so that a "
                    + "closed task cannot lend its permits to a later commit.");

            return AtTheCommit(commit, task);
        }

        var touched = tasks.Where(t => changed.Contains(Relative(t.File))).ToArray();

        if (touched.Length == 0)
            return (null, "This change names no task and touches no task file, therefore no write set governs it. "
                + "Name the task in the commit message, as a trailer line of the form TASK-nnnn, or add the "
                + "task file to the change.\n  Changed files:\n  " + string.Join("\n  ", changed));

        if (touched.Length > 1)
            return (null, "This change touches several task files and names none. Only one task governs a commit. "
                + "Name it in the commit message, as a trailer line of the form TASK-nnnn. Touched: "
                + string.Join(", ", touched.Select(t => t.Id)));

        return AtTheCommit(commit, touched[0]);
    }

    // The governing task as the commit saw it, and not as the tip of the branch
    // sees it (codebase review of 2026-10-04, finding T11). Before a commit there
    // is no hash, and the judge reads the index and HEAD.
    //
    // A task that is Done before the commit and after it governs nothing: a
    // correction reopens the task with the status Active and closes it again
    // (question Q3 of the same review, which the owner accepted).
    private (TaskRecord? Task, string? Problem) AtTheCommit(string? commit, TaskRecord tip)
    {
        var path = Relative(tip.File);

        string? after, before;
        if (!string.IsNullOrEmpty(commit))
        {
            if (_git(["merge-base", "--is-ancestor", commit, CommitRulesFrom]) is not null)
                return (tip, null);

            after = _git(["show", $"{commit}:{path}"]);
            before = _git(["show", $"{commit}^:{path}"]);
            if (after is null)
                return (null, $"The judge cannot read {path} at the commit {commit}.");
        }
        else
        {
            after = _git(["show", $":{path}"]) ?? File.ReadAllText(tip.File);
            before = _git(["show", $"HEAD:{path}"]);
        }

        var task = TaskFiles.ParseTask(tip.File, after) ?? tip;
        var statusBefore = before is null ? null : TaskFiles.ParseTask(tip.File, before)?.Status;

        if (statusBefore == "Done" && task.Status == "Done")
            return (null, $"{task.Id} is Done before this commit and after it, therefore it governs no change. To "
                + "correct the work of a closed task, set its status to Active in the commit that changes "
                + "the work, and to Done again when the correction is complete. See docs/templates.md, "
                + "section 3.");

        return (task, null);
    }

    private string Relative(string absolute) => Path.GetRelativePath(_root, absolute).Replace('\\', '/');
}
