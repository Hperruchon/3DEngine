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
    // A review is due when more than this quantity of milestones follows the
    // ledger version that the last review examined. The plan examination in
    // CLAUDE.md uses three, and a review costs more, therefore six.
    private const int ReviewInterval = 6;

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

        foreach (var review in Reviews())
        {
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

            foreach (var measure in RequiredMeasures)
            {
                if (!measures.Contains($"| {measure} |", StringComparison.Ordinal))
                    problems.Add($"{review.Name}: the measures table has no row \"{measure}\"");
            }
        }

        Assert.True(
            problems.Count == 0,
            "Each review repeats the same measures, so that the next review can compare. See "
            + "docs/templates.md, section 7.\n  " + string.Join("\n  ", problems));
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

    [Fact]
    public void A_Review_Is_Not_Overdue()
    {
        var latest = Reviews().OrderBy(r => ParseVersion(r.Fields.GetValueOrDefault("ledger", string.Empty)) ?? (0, 0)).LastOrDefault();
        Assert.NotNull(latest);

        var reviewed = ParseVersion(latest!.Fields.GetValueOrDefault("ledger", string.Empty));
        Assert.True(reviewed is not null, $"{latest.Name}: 'ledger' is absent or not a version");

        var ledger = RepositoryFiles.Read("docs", "CURRENT-STATE.md");
        var after = LedgerHeading.Matches(ledger)
            .Select(m => (Major: int.Parse(m.Groups[1].Value), Minor: int.Parse(m.Groups[2].Value)))
            .Where(v => v.CompareTo(reviewed!.Value) > 0)
            .Distinct()
            .ToList();

        Assert.True(
            after.Count <= ReviewInterval,
            $"A codebase review is due. The last review, {latest.Name}, examined "
            + $"v{reviewed!.Value.Major}.{reviewed.Value.Minor:00}, and {after.Count} milestones followed it. "
            + $"The limit is {ReviewInterval}. Write the next review with the form in docs/templates.md, "
            + "section 7.");
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
