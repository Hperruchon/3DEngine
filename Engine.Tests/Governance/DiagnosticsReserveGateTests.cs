using System.Text.RegularExpressions;
using Xunit;

namespace Engine.Tests.Governance;

// The gate for register entry R-0011. A reserved code is a code that the
// registry holds and that no source raises. The entry recorded the risk in one
// sentence: an unlimited quantity of reserved codes makes the registry
// unreliable, because a reader cannot tell a live code from a placeholder.
//
// The exit was a decision, and a decision alone would come back as the same
// question at the next code. A budget makes the answer mechanical.
public class DiagnosticsReserveGateTests
{
    // The quantity at the moment of the decision, on 2026-09-22. A new reserved
    // code needs a decision that raises this number, and that decision leaves a
    // record in this file.
    private const int ReservedBudget = 2;

    private static readonly Regex Row = new(
        @"^\|\s*`([EWI]-[A-Z0-9-]+)`\s*\|\s*(.+?)\s*\|",
        RegexOptions.Compiled | RegexOptions.Multiline);

    [Fact]
    public void The_Quantity_Of_Reserved_Codes_Does_Not_Grow()
    {
        var reserved = Reserved();

        Assert.True(
            reserved.Count <= ReservedBudget,
            $"The quantity of reserved codes is {reserved.Count} and the budget is {ReservedBudget}. "
            + "A reserved code is a code that nothing raises, and an unlimited quantity of them makes "
            + "the registry unreliable. Raise a code, or lower the budget, or record a decision that "
            + $"raises it. Reserved: {string.Join(", ", reserved.Keys)}");
    }

    [Fact]
    public void Each_Reserved_Code_Gives_A_Reason()
    {
        var problems = Reserved()
            .Where(entry => entry.Value.Length < 40)
            .Select(entry => $"{entry.Key}: the reason is absent or too short")
            .ToArray();

        Assert.True(problems.Length == 0, string.Join("\n  ", problems));
    }

    [Fact]
    public void Each_Reserved_Code_Appears_In_The_Table_Of_Active_Codes()
    {
        // A reserved code is still a shipped code. It must stay in the registry,
        // because CLAUDE.md says that codes are stable and that this file takes
        // an addition only.
        var text = Registry();
        var active = Section(text, "## Active codes");

        var problems = Reserved().Keys
            .Where(code => !active.Contains($"`{code}`", StringComparison.Ordinal))
            .Select(code => $"{code}: the reserved table names it, the active table does not")
            .ToArray();

        Assert.True(problems.Length == 0, string.Join("\n  ", problems));
    }

    private static string Registry() => RepositoryFiles.Read("docs", "diagnostics.md");

    private static Dictionary<string, string> Reserved()
    {
        var section = Section(Registry(), "## Permanently reserved codes");
        var reserved = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (Match match in Row.Matches(section))
            reserved[match.Groups[1].Value] = match.Groups[2].Value;

        return reserved;
    }

    // The text from one heading to the next heading of the same level.
    private static string Section(string text, string heading)
    {
        var start = text.IndexOf(heading, StringComparison.Ordinal);
        if (start < 0)
            return string.Empty;

        var end = text.IndexOf("\n## ", start + heading.Length, StringComparison.Ordinal);
        return end < 0 ? text[start..] : text[start..end];
    }
}
