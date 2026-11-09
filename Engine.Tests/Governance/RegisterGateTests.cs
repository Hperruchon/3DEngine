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
//
// The parser skips a code block, because the entry form sits in one. An open
// fence therefore hid each entry and each test passed on an empty list
// (codebase review of 2026-09-30, finding T5). The parser now proves that it
// found each heading of the section.
public class RegisterGateTests
{
    private sealed record Entry(
        string Id,
        string Class,
        DateOnly Opened,
        DateOnly Due,
        string Extended,
        int Line,
        IReadOnlyList<string> Repeated);

    // Register rule 4. The limit was 20 to hold the entries from the architecture
    // investigation. Register entry R-0016 asked whether 20 is too high to have an
    // effect. The owner chose 15 on 2026-09-21; see TASK-0021.
    private const int OpenLimit = 15;

    private static readonly string[] ValidClasses = ["risk", "question", "debt", "interim"];

    private static readonly Dictionary<string, int> LifetimeDays = new(StringComparer.Ordinal)
    {
        ["risk"] = 30,
        ["question"] = 90,
        ["debt"] = 180,
        ["interim"] = 365,
    };

    // Finding T10 of the codebase review of 2026-10-04 (R-0031, TASK-0320): the
    // cross-check used the pattern of the parser, so a heading "#### R-0031" was
    // hidden from both, and the fields of R-0031 replaced the due date of R-0030.
    // The cross-check now counts each heading line that holds R- and a number, and
    // each one must have the exact form. Until TASK-0320 the two used one pattern.
    private static readonly Regex AnyEntryHeading = new(@"^[ \t]*#[^\n]*\bR-\d+[^\n]*", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex ExactEntryHeading = new(@"^### R-\d{4} · \S", RegexOptions.Compiled);

    [Fact]
    public void The_Parser_Finds_Each_Entry_Of_The_Open_Section()
    {
        var text = RepositoryFiles.Read("docs", "register.md").Replace("\r", string.Empty);
        var start = text.IndexOf("\n## Open", StringComparison.Ordinal);
        Assert.True(start >= 0, "docs/register.md has no section \"## Open\".");

        var end = text.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        var section = end < 0 ? text[start..] : text[start..end];
        var headings = AnyEntryHeading.Matches(section).Select(m => m.Value).ToArray();
        var parsed = ParseOpen().Count;

        var wrong = headings.Where(h => !ExactEntryHeading.IsMatch(h)).ToArray();
        Assert.True(
            wrong.Length == 0,
            "Each entry heading has the form \"### R-nnnn · <title>\". The parser does not see these lines:\n  "
            + string.Join("\n  ", wrong));

        Assert.True(
            headings.Length == parsed,
            $"The section \"Open\" holds {headings.Length} entry headings and the parser found {parsed}. An "
            + "open code fence hides an entry from each test below. Close the fence.");
    }

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

            // Finding T10 of the codebase review of 2026-10-04 (R-0031, TASK-0320): a second
            // line of a field replaced the first with no error. Until TASK-0320 the last one counted.
            foreach (var field in entry.Repeated.Distinct(StringComparer.Ordinal))
                problems.Add($"{entry.Id} (line {entry.Line}): the field '{field}' occurs more than once; an entry gives each field one time");
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
        var repeated = new List<string>();

        void Flush()
        {
            if (id is null) return;
            entries.Add(new Entry(
                id,
                cls ?? "(absent)",
                ParseDate(opened),
                ParseDate(due),
                extended ?? "no",
                startLine,
                repeated));
            id = cls = opened = due = extended = null;
            repeated = [];
        }

        // The value of a field, with a note when the entry gave the field before (T10).
        string Field(string? current, string name, string line)
        {
            if (current is not null)
                repeated.Add(name);
            return After(line);
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

            if (line.StartsWith("- class:", StringComparison.Ordinal)) cls = Field(cls, "class", line);
            else if (line.StartsWith("- opened:", StringComparison.Ordinal)) opened = Field(opened, "opened", line);
            else if (line.StartsWith("- due:", StringComparison.Ordinal)) due = Field(due, "due", line);
            else if (line.StartsWith("- extended:", StringComparison.Ordinal)) extended = Field(extended, "extended", line);
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
