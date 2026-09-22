using System.Text.RegularExpressions;
using Xunit;

namespace Engine.Tests.Governance;

// The gate for the objectives in docs/CHARTER.md. CLAUDE.md cited "Objective 11"
// and docs/roadmap.md cited "Objective 15" for weeks, and no file defined either
// number. The owner confirmed the twenty objectives on 2026-09-20, but the list
// lived only in the transcript of that session. TASK-0029 recorded it.
//
// A citation that names no objective is a statement about the repository that
// did not come from the repository. This gate makes that condition loud.
public class ObjectiveReferenceGateTests
{
    private const int ObjectiveCount = 20;

    private static readonly Regex Item = new(@"^(\d+)\. ", RegexOptions.Compiled | RegexOptions.Multiline);

    // "Objective 11" and "objective 11", but not "anti-objective 11".
    private static readonly Regex ObjectiveCitation = new(
        @"(?<![Aa]nti-)\b[Oo]bjective (\d+)\b", RegexOptions.Compiled);

    private static readonly Regex AntiObjectiveCitation = new(
        @"\b[Aa]nti-objective (\d+)\b", RegexOptions.Compiled);

    [Fact]
    public void The_Charter_Numbers_Each_Objective_From_1_To_20()
    {
        var numbers = Numbers(Section(Charter(), "## The twenty objectives"));

        Assert.True(
            numbers.SequenceEqual(Enumerable.Range(1, ObjectiveCount)),
            $"docs/CHARTER.md, section \"The twenty objectives\", must number each objective from 1 to "
            + $"{ObjectiveCount} in order. Found: {string.Join(", ", numbers)}");
    }

    [Fact]
    public void Each_Cited_Objective_Exists()
    {
        var known = Numbers(Section(Charter(), "## The twenty objectives")).ToHashSet();
        var problems = Citations(ObjectiveCitation, known, "objective");

        Assert.True(
            problems.Count == 0,
            "A document cites an objective that docs/CHARTER.md does not define. Correct the number, "
            + "or record the objective with a decision from the owner.\n  " + string.Join("\n  ", problems));
    }

    [Fact]
    public void Each_Cited_Anti_Objective_Exists()
    {
        var known = Numbers(Section(Charter(), "## Anti-objectives")).ToHashSet();
        var problems = Citations(AntiObjectiveCitation, known, "anti-objective");

        Assert.True(
            problems.Count == 0,
            "A document cites an anti-objective that docs/CHARTER.md does not define.\n  "
            + string.Join("\n  ", problems));
    }

    private static List<string> Citations(Regex citation, HashSet<int> known, string kind)
    {
        var problems = new List<string>();

        foreach (var file in Documents())
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                foreach (Match match in citation.Matches(lines[i]))
                {
                    var number = int.Parse(match.Groups[1].Value);
                    if (!known.Contains(number))
                        problems.Add($"{RepositoryFiles.Relative(file)}:{i + 1} cites {kind} {number}");
                }
            }
        }

        return problems;
    }

    // Each document that a person or an agent reads for a decision. The archive
    // is excluded, because its header forbids its use.
    private static IEnumerable<string> Documents()
    {
        yield return RepositoryFiles.Path("CLAUDE.md");

        foreach (var root in new[] { "docs", "tasks" })
        {
            foreach (var file in Directory.EnumerateFiles(RepositoryFiles.Path(root), "*.md", SearchOption.AllDirectories))
            {
                if (!RepositoryFiles.Relative(file).StartsWith("docs/archive/", StringComparison.Ordinal))
                    yield return file;
            }
        }
    }

    private static string Charter() => RepositoryFiles.Read("docs", "CHARTER.md");

    // The text from a level-2 heading to the next level-2 heading.
    private static string Section(string text, string headingStart)
    {
        var lines = text.Replace("\r", string.Empty).Split('\n');
        var start = Array.FindIndex(lines, l => l.StartsWith(headingStart, StringComparison.Ordinal));
        Assert.True(start >= 0, $"docs/CHARTER.md has no heading that starts with \"{headingStart}\".");

        var end = Array.FindIndex(lines, start + 1, l => l.StartsWith("## ", StringComparison.Ordinal));
        return string.Join("\n", lines[(start + 1)..(end < 0 ? lines.Length : end)]);
    }

    private static List<int> Numbers(string section)
        => Item.Matches(section).Select(m => int.Parse(m.Groups[1].Value)).ToList();
}
