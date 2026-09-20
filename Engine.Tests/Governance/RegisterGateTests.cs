using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace Engine.Tests.Governance;

// The gate for docs/register.md. Its rules say that a due date is a stop and
// not a reminder, therefore the build must fail when a date passes.
//
// Without this gate the register is the instrument that docs/register.md itself
// warns about: the Blender design document of the year 2000 listed problems
// that took eleven years to correct.
public class RegisterGateTests
{
    private sealed record Entry(
        string Id,
        string Class,
        DateOnly Opened,
        DateOnly Due,
        string Extended,
        int Line);

    private const int OpenLimit = 20;

    private static readonly string[] ValidClasses = ["risk", "question", "debt", "interim"];

    private static readonly Dictionary<string, int> LifetimeDays = new(StringComparer.Ordinal)
    {
        ["risk"] = 30,
        ["question"] = 90,
        ["debt"] = 180,
        ["interim"] = 365,
    };

    [Fact]
    public void No_Open_Entry_Is_Past_Its_Due_Date()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdue = ParseOpen()
            .Where(e => e.Due < today)
            .Select(e => $"{e.Id} was due {e.Due:yyyy-MM-dd} (line {e.Line})")
            .ToArray();

        Assert.True(
            overdue.Length == 0,
            "A due date passed. Use one of the four exits in docs/register.md: resolve, decide, "
            + "accept, or extend once. Overdue:\n  " + string.Join("\n  ", overdue));
    }

    [Fact]
    public void No_Entry_Is_Extended_More_Than_Once()
    {
        // The rule permits one extension. A second one must force a resolve, a
        // decide or an accept.
        var twice = ParseOpen()
            .Where(e => CountExtensions(e.Extended) > 1)
            .Select(e => $"{e.Id} (line {e.Line})")
            .ToArray();

        Assert.True(
            twice.Length == 0,
            "An entry is extended more than once. Resolve it, decide it, or accept it. Entries:\n  "
            + string.Join("\n  ", twice));
    }

    [Fact]
    public void The_Open_Count_Is_At_Or_Below_The_Limit()
    {
        var open = ParseOpen().Count;

        Assert.True(
            open <= OpenLimit,
            $"The register holds {open} open entries and the limit is {OpenLimit}. "
            + "Close an entry before you add one. Growth is the failure condition.");
    }

    [Fact]
    public void Each_Entry_Declares_A_Valid_Class_And_Two_Dates()
    {
        var problems = new List<string>();

        foreach (var entry in ParseOpen())
        {
            if (!ValidClasses.Contains(entry.Class))
                problems.Add($"{entry.Id}: class '{entry.Class}' is not one of {string.Join(", ", ValidClasses)}");

            if (entry.Due < entry.Opened)
                problems.Add($"{entry.Id}: the due date is before the opened date");
        }

        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
    }

    [Fact]
    public void A_Due_Date_Is_Not_Later_Than_The_Lifetime_Of_Its_Class()
    {
        // An entry that was never extended must not give itself more time than
        // its class permits. An EARLIER date is a deliberate tightening and is
        // permitted. A LATER date is an extension that nobody recorded, and the
        // previous test bounds a recorded extension to one.
        var problems = new List<string>();

        foreach (var entry in ParseOpen())
        {
            if (CountExtensions(entry.Extended) > 0)
                continue;
            if (!LifetimeDays.TryGetValue(entry.Class, out var days))
                continue;

            var latest = entry.Opened.AddDays(days);
            if (entry.Due > latest)
            {
                problems.Add(
                    $"{entry.Id}: class '{entry.Class}' permits {latest:yyyy-MM-dd} at the latest, "
                    + $"the entry gives {entry.Due:yyyy-MM-dd}. Record an extension or correct the date.");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
    }

    private static int CountExtensions(string extended)
        => extended.Trim().Equals("no", StringComparison.OrdinalIgnoreCase) ? 0
            : extended.Count(c => c == '(');

    private static List<Entry> ParseOpen()
    {
        var text = RepositoryFiles.Read("docs", "register.md");
        var lines = text.Split('\n');

        var entries = new List<Entry>();
        var inOpenSection = false;
        var inCodeBlock = false;

        string? id = null, cls = null, opened = null, due = null, extended = null;
        var startLine = 0;

        void Flush()
        {
            if (id is null) return;
            entries.Add(new Entry(
                id,
                cls ?? "(absent)",
                ParseDate(opened),
                ParseDate(due),
                extended ?? "no",
                startLine));
            id = cls = opened = due = extended = null;
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }
            if (inCodeBlock) continue;

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                Flush();
                inOpenSection = line.StartsWith("## Open", StringComparison.Ordinal);
                continue;
            }

            if (!inOpenSection) continue;

            var header = Regex.Match(line, @"^### (R-\d{4})");
            if (header.Success)
            {
                Flush();
                id = header.Groups[1].Value;
                startLine = i + 1;
                continue;
            }

            if (id is null) continue;

            if (line.StartsWith("- class:", StringComparison.Ordinal)) cls = After(line);
            else if (line.StartsWith("- opened:", StringComparison.Ordinal)) opened = After(line);
            else if (line.StartsWith("- due:", StringComparison.Ordinal)) due = After(line);
            else if (line.StartsWith("- extended:", StringComparison.Ordinal)) extended = After(line);
        }

        Flush();
        return entries;
    }

    private static string After(string line) => line[(line.IndexOf(':') + 1)..].Trim();

    private static DateOnly ParseDate(string? value)
        => DateOnly.TryParseExact(
            (value ?? string.Empty).Split(' ')[0],
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : DateOnly.MinValue;
}
