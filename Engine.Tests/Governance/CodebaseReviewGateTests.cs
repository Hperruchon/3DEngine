using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace Engine.Tests.Governance;

// The gate for the periodic codebase review. The owner decided on 2026-09-23:
// "we will need to do it periodically so that we can keep track of how the code
// base evolve". A review that nobody schedules is a review that nobody does, so
// the schedule is a test, in the same way as the limit of the register.
//
// The period counts milestones and not days. Objective 12 says that time is
// irregular, and a calendar rule would fail after a break in which the code did
// not change. A milestone is a ledger entry in docs/CURRENT-STATE.md.
//
// docs/templates.md, section 7, gives the form that this gate reads.
public class CodebaseReviewGateTests
{
    // A suffix after the date, for example 2026-10-13-b, permits a second review on
    // one day. Until TASK-0073 the name held a date only, and a second review on one
    // day needed a false date (finding T53 of the review of 2026-10-13).
    private static readonly Regex ReviewName = new(
        @"^(\d{4}-\d{2}-\d{2})(?:-[a-z0-9]+)?-codebase-review\.md$", RegexOptions.Compiled);

    private static readonly Regex LedgerHeading = new(
        @"^## v(\d+)\.(\d+)\b", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex FindingHeading = new(
        @"^#### ([A-Z]\d+) ·", RegexOptions.Compiled | RegexOptions.Multiline);

    // Each review repeats these measures, so that two reviews compare.
    private static readonly string[] RequiredMeasures =
    [
        "Projects in the solution",
        "Production source lines",
        "Test source lines",
        "Tests",
        "Gate classes",
        "Build warnings (clean build)",
        "Open register entries",
        "ADRs",
        "Findings: critical",
        "Findings: high",
        "Findings: medium",
        "Findings: low",
    ];

    private sealed record Review(string File, string Name, DateOnly Date, Dictionary<string, string> Fields, string Text);

    [Fact]
    public void At_Least_One_Review_Exists()
    {
        Assert.True(Reviews().Count > 0, "docs/reviews/ holds no codebase review. See docs/templates.md, section 7.");
    }

    [Fact]
    public void Each_Review_Declares_Its_Date_Commit_Ledger_And_Previous_Review()
    {
        var problems = new List<string>();
        var versions = LedgerVersions(RepositoryFiles.Read("docs", "CURRENT-STATE.md"));

        foreach (var review in Reviews())
        {
            if (review.Fields.TryGetValue("ledger", out var declared) && LedgerRefusal(declared, versions) is { } refusal)
                problems.Add($"{review.Name}: {refusal}");

            foreach (var key in new[] { "date", "commit", "ledger", "previous" })
            {
                if (!review.Fields.TryGetValue(key, out var value) || value.Length == 0)
                    problems.Add($"{review.Name}: the front matter has no '{key}'");
            }

            if (review.Fields.TryGetValue("date", out var date) && date != review.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                problems.Add($"{review.Name}: the name gives {review.Date:yyyy-MM-dd} and the field 'date' gives {date}");

            if (review.Fields.TryGetValue("commit", out var commit) && !Regex.IsMatch(commit, "^[0-9a-f]{7,40}$"))
                problems.Add($"{review.Name}: 'commit' must be a commit hash, not '{commit}'");

            if (review.Fields.TryGetValue("ledger", out var ledger) && ParseVersion(ledger) is null)
                problems.Add($"{review.Name}: 'ledger' must be a ledger version such as v0.28, not '{ledger}'");
        }

        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
    }

    [Fact]
    public void Each_Review_Gives_Each_Required_Measure()
    {
        var problems = new List<string>();

        foreach (var review in Reviews())
        {
            var measures = Section(review.Text, "## Measures");
            if (measures is null)
            {
                problems.Add($"{review.Name}: no section \"## Measures\"");
                continue;
            }

            foreach (var measure in MissingMeasures(review.Fields.GetValueOrDefault("ledger", string.Empty), measures))
                problems.Add($"{review.Name}: the measures table has no row \"{measure}\"");
        }

        Assert.True(
            problems.Count == 0,
            "Each review repeats the same measures, so that the next review can compare. See "
            + "docs/templates.md, section 7.\n  " + string.Join("\n  ", problems));
    }

    // Finding T78 of the second codebase review of 2026-10-28 (TASK-0098): no codebase review
    // repeated the measures of the rules review, which docs/templates.md, section 6, rule 5
    // asks for. The owner chose A on 2026-10-28: a review after TASK-0098 (ledger v0.104 and
    // later) gives three rows with the method of docs/reviews/2026-09-25-rules-review.md,
    // section 1.
    private static readonly string[] RuleMeasures =
    [
        "Lines in the rule files",
        "Sentences with \"must\"",
        "Sentences with \"do not\"",
    ];

    private static readonly (int Major, int Minor) RuleMeasuresFrom = (0, 104);

    private static IEnumerable<string> MissingMeasures(string ledger, string measures)
    {
        var required = ParseVersion(ledger) is { } version && version.CompareTo(RuleMeasuresFrom) >= 0
            ? RequiredMeasures.Concat(RuleMeasures)
            : RequiredMeasures;
        return required.Where(measure => !measures.Contains($"| {measure} |", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("v0.103", 0)]
    [InlineData("v0.104", 3)]
    [InlineData("v0.120", 3)]
    public void A_Review_After_TASK_0098_Gives_The_Measures_Of_The_Rules(string ledger, int missing)
    {
        var measures = string.Concat(RequiredMeasures.Select(measure => $"| {measure} | 1 | A count. |\n"));

        Assert.Equal(missing, MissingMeasures(ledger, measures).Count());
        Assert.Empty(MissingMeasures(ledger, measures + string.Concat(RuleMeasures.Select(measure => $"| {measure} | 1 | A count. |\n"))));
    }

    [Fact]
    public void Each_Review_Gives_The_State_Of_Each_Finding_Of_The_Previous_Review()
    {
        var problems = PreviousReviewProblems(Reviews());

        Assert.True(
            problems.Count == 0,
            "A review must say what happened to each finding of the review before it: fixed, open, or "
            + "worse.\n  " + string.Join("\n  ", problems));
    }

    // Finding T53 of the codebase review of 2026-10-13: a second review on one day
    // needed a false date. Each review below passed or failed the gate of main
    // against the rule (TASK-0073).
    [Theory]
    [InlineData("2026-10-13-b-codebase-review.md", "v0.72", true)]
    [InlineData("2026-10-13-b-codebase-review.md", "v0.71", false)]
    [InlineData("2026-10-12-codebase-review.md", "v0.72", false)]
    public void A_Second_Review_On_One_Day_Is_Read_And_Ordered_By_Its_Ledger(string name, string ledger, bool accepted)
    {
        var folder = Directory.CreateTempSubdirectory("reviews-").FullName;
        try
        {
            File.WriteAllText(
                Path.Combine(folder, "2026-10-13-codebase-review.md"),
                "---\ndate: 2026-10-13\ncommit: afc1d92\nledger: v0.71\nprevious: none\n---\n\n#### T1 · Low · A finding\n");
            File.WriteAllText(
                Path.Combine(folder, name),
                $"---\ndate: {name[..10]}\ncommit: afc1d92\nledger: {ledger}\nprevious: 2026-10-13-codebase-review.md\n---\n\n"
                + "## Findings of the previous review\n\n- **T1** — Open.\n");

            var reviews = Reviews(folder);
            var problems = PreviousReviewProblems(reviews);

            Assert.Equal(2, reviews.Count);
            Assert.True(accepted == (problems.Count == 0), string.Join("\n  ", problems));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static List<string> PreviousReviewProblems(List<Review> reviews)
    {
        var byName = reviews.ToDictionary(r => r.Name, StringComparer.Ordinal);
        var problems = new List<string>();

        foreach (var review in reviews)
        {
            var previousName = review.Fields.GetValueOrDefault("previous", "none");
            if (previousName == "none")
                continue;

            if (!byName.TryGetValue(previousName, out var previous))
            {
                problems.Add($"{review.Name}: 'previous' names {previousName}, which is not in docs/reviews/");
                continue;
            }

            // The order of two reviews is the order of the ledger versions that they
            // examine; the dates may be equal (T53).
            var (previousLedger, ledger) = (ParseVersion(previous.Fields.GetValueOrDefault("ledger", string.Empty)), ParseVersion(review.Fields.GetValueOrDefault("ledger", string.Empty)));
            if (previous.Date > review.Date || previousLedger is null || ledger is null || previousLedger.Value.CompareTo(ledger.Value) >= 0)
                problems.Add($"{review.Name}: the previous review {previousName} is not earlier");

            var states = Section(review.Text, "## Findings of the previous review");
            if (states is null)
            {
                problems.Add($"{review.Name}: no section \"## Findings of the previous review\"");
                continue;
            }

            foreach (Match finding in FindingHeading.Matches(previous.Text))
            {
                var id = finding.Groups[1].Value;
                if (!Regex.IsMatch(states, $@"\b{id}\b"))
                    problems.Add($"{review.Name}: gives no state for finding {id} of {previousName}");
            }
        }

        return problems;
    }

    // The decision of the owner of 2026-10-27 (TASK-0083): a review is due when more than
    // four product milestones, or more than twelve entries of any kind, follow the ledger
    // version of the last review. A product milestone is an entry whose task the row of a
    // phase of track R, K, P or V in docs/roadmap.md names. Until TASK-0098 the phase came
    // from the task file, and the row had to name the same task (finding T73).
    // Until TASK-0083 each entry counted, with a limit of six, and the review and the record
    // of the answers of the owner used two of the six.
    private const int ProductMilestoneLimit = 4;
    private const int EntryLimit = 12;

    [Fact]
    public void A_Review_Is_Not_Overdue()
    {
        var latest = Reviews().OrderBy(r => ParseVersion(r.Fields.GetValueOrDefault("ledger", string.Empty)) ?? (0, 0)).LastOrDefault();
        Assert.NotNull(latest);

        var reviewed = ParseVersion(latest!.Fields.GetValueOrDefault("ledger", string.Empty));
        Assert.True(reviewed is not null, $"{latest.Name}: 'ledger' is absent or not a version");

        var (product, entries) = MilestonesAfter(
            reviewed!.Value,
            RepositoryFiles.Read("docs", "CURRENT-STATE.md"),
            RepositoryFiles.Read("docs", "roadmap.md"));

        Assert.True(
            product <= ProductMilestoneLimit && entries <= EntryLimit,
            $"A codebase review is due. The last review, {latest.Name}, examined "
            + $"v{reviewed.Value.Major}.{reviewed.Value.Minor:00}, and {product} product milestones and {entries} entries "
            + $"of any kind followed it. The limits are {ProductMilestoneLimit} and {EntryLimit}. Write the next review "
            + "with the form in docs/templates.md, section 7.");
    }

    // The cases of the method of the decision of 2026-10-27. The ledger starts at the
    // reviewed version v0.10; TASK-0001 is governance, TASK-0002 is R7 and its row names
    // it, TASK-0003 is R8 and its row names another task, TASK-0004 is P0.33 (track 0).
    [Theory]
    [InlineData("seven governance entries", false)]
    [InlineData("four product entries", false)]
    [InlineData("five product entries", true)]
    [InlineData("twelve track 0 entries", false)]
    [InlineData("thirteen track 0 entries", true)]
    [InlineData("five product entries under governance headings", true)]
    [InlineData("five entries of a phase whose row names another task", false)]
    [InlineData("thirteen entries with no task", true)]
    public void A_Review_Follows_The_Product_Milestones(string scenario, bool overdue)
    {
        var (count, heading) = scenario switch
        {
            "seven governance entries" => (7, "(governance, TASK-0001)"),
            "four product entries" => (4, "(R7, TASK-0002)"),
            "five product entries" => (5, "(R7, TASK-0002)"),
            "twelve track 0 entries" => (12, "(P0.33, TASK-0004)"),
            "thirteen track 0 entries" => (13, "(P0.33, TASK-0004)"),
            "five product entries under governance headings" => (5, "(governance, TASK-0002)"),
            "five entries of a phase whose row names another task" => (5, "(R8, TASK-0003)"),
            "thirteen entries with no task" => (13, "(governance)"),
            _ => throw new ArgumentException(scenario),
        };

        var ledger = "# Ledger\n\n## v0.9 — Before\n\n## v0.10 — The review (governance, TASK-0001)\n"
            + string.Concat(Enumerable.Range(11, count).Select(minor => $"\n## v0.{minor} — An entry {heading}\n"));
        var (product, entries) = MilestonesAfter((0, 10), ledger, Rows);

        Assert.Equal(overdue, product > ProductMilestoneLimit || entries > EntryLimit);
    }

    // A ledger heading and the task that it names, for example
    // "## v0.79 — A title (P0.30, TASK-0078)".
    private static readonly Regex LedgerEntry = new(
        @"^## v(\d+)\.(\d+)\b(?<rest>[^\n]*)$", RegexOptions.Compiled | RegexOptions.Multiline);

    // A phase of track R, K, P or V; a phase of track 0 starts with P0.
    private static readonly Regex ProductPhase = new(
        @"^(?:[RKV]\d+|P(?!0\.)\d+)(?:[a-z]|\.\d+)?$", RegexOptions.Compiled);

    // The count of product milestones and the count of entries after the reviewed version.
    // Finding T73 of the second codebase review of 2026-10-28 (TASK-0098): the counter
    // skipped a repeated version, took the last task of a heading only, and took the phase
    // from the task file, which is not a gate file. It now counts each heading, and an entry
    // is a product milestone when the row of a product phase in docs/roadmap.md names one of
    // its tasks. HeadingProblems refuses a repeated version and a heading with two tasks, and
    // UnnamedProductTasks refuses a task file whose phase is not the phase of its row.
    private static (int Product, int Entries) MilestonesAfter((int Major, int Minor) reviewed, string ledger, string roadmap)
    {
        var (product, entries) = (0, 0);

        foreach (Match entry in LedgerEntry.Matches(ledger.Replace("\r", string.Empty, StringComparison.Ordinal)))
        {
            var version = (int.Parse(entry.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(entry.Groups[2].Value, CultureInfo.InvariantCulture));
            if (version.CompareTo(reviewed) <= 0)
                continue;

            entries++;
            if (Regex.Matches(entry.Groups["rest"].Value, @"TASK-(\d{4})").Any(task => ProductRowPhase(roadmap, task.Groups[1].Value) is not null))
                product++;
        }

        return (product, entries);
    }

    // The product phase whose row in docs/roadmap.md names the task, or null.
    private static string? ProductRowPhase(string roadmap, string task)
        => roadmap.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n')
            .Where(row => row.StartsWith("| ", StringComparison.Ordinal) && row.Contains($"TASK-{task}", StringComparison.Ordinal))
            .Select(row => row[2..].Split(" |")[0])
            .FirstOrDefault(ProductPhase.IsMatch);

    // Finding T73: each ledger version is greater than the one before it, and each heading
    // after the reviewed version names one task.
    internal static IEnumerable<string> HeadingProblems((int Major, int Minor) reviewed, string ledger)
    {
        (int, int)? previous = null;
        foreach (Match entry in LedgerEntry.Matches(ledger.Replace("\r", string.Empty, StringComparison.Ordinal)))
        {
            var version = (int.Parse(entry.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(entry.Groups[2].Value, CultureInfo.InvariantCulture));
            if (previous is { } before && version.CompareTo(before) <= 0)
                yield return $"v{version.Item1}.{version.Item2:00} follows v{before.Item1}.{before.Item2:00}; each ledger version is greater than the one before it";
            previous = version;

            var tasks = Regex.Matches(entry.Groups["rest"].Value, @"TASK-\d{4}").Count;
            if (version.CompareTo(reviewed) > 0 && tasks != 1)
                yield return $"v{version.Item1}.{version.Item2:00} names {tasks} tasks in its heading; a heading after the last review names one";
        }
    }

    [Fact]
    public void Each_Ledger_Version_Rises_And_Each_Heading_Names_One_Task()
    {
        var latest = Reviews().OrderBy(r => ParseVersion(r.Fields.GetValueOrDefault("ledger", string.Empty)) ?? (0, 0)).Last();
        var problems = HeadingProblems(ParseVersion(latest.Fields["ledger"])!.Value, RepositoryFiles.Read("docs", "CURRENT-STATE.md")).ToList();

        Assert.True(problems.Count == 0, "The review counter reads each heading of the ledger (finding T73):\n  " + string.Join("\n  ", problems));
    }

    // The three forms of finding T73. Each one passed the gate of main.
    private static readonly string Rows = "| R7 | The intent layer. TASK-0002. | 5–7 |\n| R8 | Name and visibility. TASK-0009. | 4–6 |\n| P0.33 | The review rule. TASK-0004. | 1 |\n";

    [Fact]
    public void A_Heading_That_Names_A_Second_Task_Last_Counts_Its_Product_Task_And_Is_A_Problem()
    {
        var ledger = "## v0.10 — The review (governance, TASK-0001)\n\n## v0.11 — An entry (R7, TASK-0002, TASK-0001)\n";

        Assert.Equal(1, MilestonesAfter((0, 10), ledger, Rows).Product);
        Assert.Contains(HeadingProblems((0, 10), ledger), problem => problem.Contains("names 2 tasks", StringComparison.Ordinal));
    }

    [Fact]
    public void A_Product_Task_With_Another_Phase_In_Its_File_Counts_And_Is_A_Problem()
    {
        var ledger = "## v0.10 — The review (governance, TASK-0001)\n\n## v0.11 — An entry (P0.99, TASK-0002)\n";
        var phases = new Dictionary<string, string> { ["0001"] = "governance", ["0002"] = "P0.99" };

        Assert.Equal(1, MilestonesAfter((0, 10), ledger, Rows).Product);
        Assert.Contains(UnnamedProductTasks((0, 10), ledger, id => phases.GetValueOrDefault(id), Rows), problem => problem.Contains("P0.99", StringComparison.Ordinal));
    }

    [Fact]
    public void A_Repeated_Version_Counts_Each_Heading_And_Is_A_Problem()
    {
        var ledger = "## v0.10 — The review (governance, TASK-0001)\n\n## v0.11 — An entry (R7, TASK-0002)\n\n## v0.11 — An entry (R7, TASK-0002)\n";

        Assert.Equal((2, 2), MilestonesAfter((0, 10), ledger, Rows));
        Assert.Contains(HeadingProblems((0, 10), ledger), problem => problem.Contains("follows v0.11", StringComparison.Ordinal));
    }

    // Finding T66 of the codebase review of 2026-10-28 (TASK-0089): the counter counts a
    // product task only when its roadmap row names it (D3 of TASK-0083), so a row with no
    // name of its task made a milestone an entry only, with no change to a gate file. The
    // rule of D3 stays, and a product task after the last review whose row does not name it
    // is a problem.
    internal static IEnumerable<string> UnnamedProductTasks(
        (int Major, int Minor) reviewed, string ledger, Func<string, string?> phaseOfTask, string roadmap)
    {
        roadmap = roadmap.Replace("\r", string.Empty, StringComparison.Ordinal);
        foreach (Match entry in LedgerEntry.Matches(ledger.Replace("\r", string.Empty, StringComparison.Ordinal)))
        {
            var version = (int.Parse(entry.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(entry.Groups[2].Value, CultureInfo.InvariantCulture));
            if (version.CompareTo(reviewed) <= 0)
                continue;

            foreach (Match match in Regex.Matches(entry.Groups["rest"].Value, @"TASK-(\d{4})"))
            {
                var task = match.Groups[1].Value;
                var phase = phaseOfTask(task);
                var rowPhase = ProductRowPhase(roadmap, task);
                if (phase is not null && ProductPhase.IsMatch(phase)
                    && !roadmap.Split('\n').Any(row => row.StartsWith($"| {phase} |", StringComparison.Ordinal) && row.Contains($"TASK-{task}", StringComparison.Ordinal)))
                    yield return $"v{version.Item1}.{version.Item2:00}: TASK-{task} has the product phase {phase}, and the row of {phase} in docs/roadmap.md does not name it, so the review counter does not count it";
                // Finding T73 (TASK-0098): a task file is not a gate file, so its phase must be
                // the phase of the product row that names it.
                else if (rowPhase is not null && phase != rowPhase)
                    yield return $"v{version.Item1}.{version.Item2:00}: the row of {rowPhase} in docs/roadmap.md names TASK-{task}, and its task file gives the phase {phase ?? "none"}";
            }
        }
    }

    [Fact]
    public void Each_Product_Task_After_The_Last_Review_Is_Named_By_Its_Roadmap_Row()
    {
        var latest = Reviews().OrderBy(r => ParseVersion(r.Fields.GetValueOrDefault("ledger", string.Empty)) ?? (0, 0)).Last();
        var problems = UnnamedProductTasks(
            ParseVersion(latest.Fields["ledger"])!.Value,
            RepositoryFiles.Read("docs", "CURRENT-STATE.md"),
            PhaseOfTask,
            RepositoryFiles.Read("docs", "roadmap.md")).ToList();

        Assert.True(problems.Count == 0, "Name each product task in the row of its phase (finding T66):\n  " + string.Join("\n  ", problems));
    }

    [Fact]
    public void A_Product_Task_Whose_Row_Names_Another_Task_Is_A_Problem()
    {
        var ledger = "## v0.10 — The review (governance, TASK-0001)\n\n## v0.11 — An entry (R8, TASK-0003)\n\n## v0.12 — An entry (R7, TASK-0002)\n";
        var phases = new Dictionary<string, string> { ["0001"] = "governance", ["0002"] = "R7", ["0003"] = "R8" };
        var roadmap = "| R7 | The intent layer. TASK-0002. | 5–7 |\n| R8 | Name and visibility. TASK-0009. | 4–6 |\n";

        var problems = UnnamedProductTasks((0, 10), ledger, id => phases.GetValueOrDefault(id), roadmap).ToList();

        Assert.Equal("TASK-0003", Regex.Match(Assert.Single(problems), @"TASK-\d{4}").Value);
    }

    // The field 'phase' of the task file tasks/TASK-<id>-*.md, or null.
    private static string? PhaseOfTask(string id)
    {
        var file = Directory.EnumerateFiles(RepositoryFiles.Path("tasks"), $"TASK-{id}-*.md").FirstOrDefault();
        return file is null ? null : RepositoryFiles.ReadFrontMatter(File.ReadAllText(file).Replace("\r", string.Empty, StringComparison.Ordinal))?.GetValueOrDefault("phase");
    }

    private static List<Review> Reviews() => Reviews(RepositoryFiles.Path("docs", "reviews"));

    private static List<Review> Reviews(string directory)
    {
        if (!Directory.Exists(directory))
            return [];

        var reviews = new List<Review>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.md").OrderBy(f => f, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(file);
            var match = ReviewName.Match(name);
            if (!match.Success)
                continue;

            var text = File.ReadAllText(file).Replace("\r", string.Empty);
            var date = DateOnly.ParseExact(match.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var fields = RepositoryFiles.ReadFrontMatter(text) ?? new Dictionary<string, string>(StringComparer.Ordinal);
            reviews.Add(new Review(file, name, date, fields, text));
        }

        return reviews;
    }

    // Finding T61 of the codebase review of 2026-10-17: the gate took the declared
    // version of a review and did not check it, so a review that declared v0.99
    // silenced the rule of an overdue review. A declared version must have an entry in
    // the ledger (TASK-0078).
    [Theory]
    [InlineData("v0.2", true)]
    [InlineData("v0.3", false)]
    [InlineData("v0.99", false)]
    public void The_Ledger_Version_Of_A_Review_Must_Have_An_Entry(string declared, bool accepted)
    {
        var versions = LedgerVersions("# Ledger\n\n## v0.1 — The start\n\n## v0.2 — A review (TASK-0001)\n");

        Assert.Equal(accepted, LedgerRefusal(declared, versions) is null);
    }

    private static HashSet<(int Major, int Minor)> LedgerVersions(string ledger)
        => [.. LedgerHeading.Matches(ledger.Replace("\r", string.Empty, StringComparison.Ordinal))
            .Select(m => (int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)))];

    private static string? LedgerRefusal(string declared, IReadOnlySet<(int Major, int Minor)> versions)
        => ParseVersion(declared) is { } version && !versions.Contains(version)
            ? $"'ledger' gives {declared}, and docs/CURRENT-STATE.md has no entry with that version"
            : null;

    private static (int Major, int Minor)? ParseVersion(string text)
    {
        var match = Regex.Match(text, @"^v(\d+)\.(\d+)$");
        return match.Success ? (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value)) : null;
    }

    // The text from a level-2 heading to the next level-2 heading, or null.
    private static string? Section(string text, string heading)
    {
        var lines = text.Split('\n');
        var start = Array.FindIndex(lines, l => l.TrimEnd() == heading);
        if (start < 0)
            return null;

        var end = Array.FindIndex(lines, start + 1, l => l.StartsWith("## ", StringComparison.Ordinal));
        return string.Join("\n", lines[(start + 1)..(end < 0 ? lines.Length : end)]);
    }
}
