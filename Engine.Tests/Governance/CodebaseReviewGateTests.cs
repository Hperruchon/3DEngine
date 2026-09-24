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

    private static readonly Regex ReviewName = new(
        @"^(\d{4}-\d{2}-\d{2})-codebase-review\.md$", RegexOptions.Compiled);

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
        var reviews = Reviews();
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

            if (previous.Date >= review.Date)
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

        Assert.True(
            problems.Count == 0,
            "A review must say what happened to each finding of the review before it: fixed, open, or "
            + "worse.\n  " + string.Join("\n  ", problems));
    }

    [Fact]
    public void A_Review_Is_Not_Overdue()
    {
        var latest = Reviews().OrderBy(r => r.Date).LastOrDefault();
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

    private static List<Review> Reviews()
    {
        var directory = RepositoryFiles.Path("docs", "reviews");
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
