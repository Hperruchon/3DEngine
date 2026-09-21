using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace Engine.Tests.Governance;

// The gate for each ADR rule in docs/templates.md. Register entry R-0006
// recorded the defect that made this gate necessary: ADR-0011 contradicted the
// deployment default in ADR-0004 and the index recorded nothing, and ADR-0012
// carried an in-file amendment that the index did not mention.
//
// The research behind docs/working-agreement.md gives the reason. An ADR with
// no enforcement decays without a signal, and a hand-maintained index drifts.
public class AdrGateTests
{
    // The set agrees with docs/templates.md, section "Field rules", and with the
    // legend in docs/adr/README.md. Superseded was absent until TASK-0021, and
    // A_Status_Agrees_With_Its_Supersession_And_Amendment_Fields already required
    // that value, therefore the first superseded record would have failed one test
    // whichever document was right.
    private static readonly string[] ValidStatuses =
        ["Accepted", "Amended", "Superseded", "Proposed", "Withdrawn", "Rejected"];

    private sealed record Adr(
        string File,
        string Id,
        string Title,
        string Status,
        string Date,
        IReadOnlyList<string> Supersedes,
        IReadOnlyList<string> SupersededBy,
        IReadOnlyList<string> Amends,
        IReadOnlyList<string> AmendedBy,
        string Affects,
        string EnforcedBy);

    // The count of ADRs with no enforcement, at the moment this gate was built.
    // The gate fails when the count grows. It does not fail on the present count,
    // because two records describe work that is deliberately not built.
    private const int UnenforcedBudget = 2;

    [Fact]
    public void Every_Adr_Has_Front_Matter_With_Each_Required_Field()
    {
        var problems = new List<string>();

        foreach (var file in AdrFiles())
        {
            var fields = RepositoryFiles.ReadFrontMatter(File.ReadAllText(file));
            var name = Path.GetFileName(file);

            if (fields is null)
            {
                problems.Add($"{name}: no front matter");
                continue;
            }

            foreach (var required in new[] { "id", "title", "status", "date", "affects", "enforced-by" })
            {
                if (!fields.ContainsKey(required) || fields[required].Length == 0)
                    problems.Add($"{name}: field '{required}' is absent or empty");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
    }

    [Fact]
    public void Every_Status_Comes_From_The_Closed_Set()
    {
        var problems = Parse()
            .Where(a => !ValidStatuses.Contains(a.Status))
            .Select(a => $"{a.Id}: status '{a.Status}' is not one of {string.Join(", ", ValidStatuses)}")
            .ToArray();

        Assert.True(problems.Length == 0, string.Join("\n  ", problems));
    }

    [Fact]
    public void Each_Identifier_Matches_Its_File_Name_And_Is_Unique()
    {
        var problems = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var adr in Parse())
        {
            var fromName = Path.GetFileName(adr.File)[..4];
            if (adr.Id != fromName)
                problems.Add($"{Path.GetFileName(adr.File)}: front matter says id {adr.Id}");

            if (!seen.Add(adr.Id))
                problems.Add($"{adr.Id}: the identifier is used more than once");
        }

        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
    }

    [Fact]
    public void Each_Supersession_And_Each_Amendment_Is_Reciprocal()
    {
        // This check is the one that would have caught register entry R-0006.
        var byId = Parse().ToDictionary(a => a.Id, StringComparer.Ordinal);
        var problems = new List<string>();

        foreach (var adr in byId.Values)
        {
            Check(adr.Id, adr.Supersedes, "supersedes", target => target.SupersededBy, "superseded-by");
            Check(adr.Id, adr.SupersededBy, "superseded-by", target => target.Supersedes, "supersedes");
            Check(adr.Id, adr.Amends, "amends", target => target.AmendedBy, "amended-by");
            Check(adr.Id, adr.AmendedBy, "amended-by", target => target.Amends, "amends");
        }

        void Check(
            string from,
            IReadOnlyList<string> targets,
            string field,
            Func<Adr, IReadOnlyList<string>> reciprocal,
            string reciprocalField)
        {
            foreach (var to in targets)
            {
                if (!byId.TryGetValue(to, out var target))
                {
                    problems.Add($"{from}: {field} names {to}, which does not exist");
                    continue;
                }

                if (!reciprocal(target).Contains(from))
                    problems.Add($"{from}: {field} names {to}, but {to} does not list {from} in {reciprocalField}");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
    }

    [Fact]
    public void A_Status_Agrees_With_Its_Supersession_And_Amendment_Fields()
    {
        var problems = new List<string>();

        foreach (var adr in Parse())
        {
            if (adr.SupersededBy.Count > 0 && adr.Status != "Superseded")
                problems.Add($"{adr.Id}: superseded-by is set, therefore the status must be Superseded");

            if (adr.AmendedBy.Count > 0 && adr.Status is not ("Amended" or "Superseded"))
                problems.Add($"{adr.Id}: amended-by is set, therefore the status must be Amended");

            if (adr.Status == "Amended" && adr.AmendedBy.Count == 0)
                problems.Add($"{adr.Id}: the status is Amended, therefore amended-by must name an ADR");
        }

        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
    }

    // A record that is not in force cannot be enforced. A proposal describes work
    // that nobody built, and a withdrawn or rejected record describes work that
    // nobody will build. The budget counts a record in force only. Without this
    // distinction a proposal would consume the budget of an accepted decision.
    private static readonly string[] NotInForce = ["Proposed", "Withdrawn", "Rejected"];

    [Fact]
    public void The_Count_Of_Unenforced_Adrs_Does_Not_Grow()
    {
        var unenforced = Parse()
            .Where(a => !NotInForce.Contains(a.Status))
            .Where(a => a.EnforcedBy.StartsWith("UNENFORCED", StringComparison.Ordinal))
            .Select(a => a.Id)
            .ToArray();

        Assert.True(
            unenforced.Length <= UnenforcedBudget,
            $"The count of unenforced ADRs is {unenforced.Length} and the budget is {UnenforcedBudget}. "
            + "An ADR in force with no enforcement decays without a signal. Give it a test, or lower "
            + "the budget "
            + $"when you enforce one. Unenforced: {string.Join(", ", unenforced)}");
    }

    [Fact]
    public void The_Index_Lists_Each_Adr_With_Its_Status()
    {
        // docs/templates.md says that a generated index is the correct end state.
        // A tool does not exist yet, therefore this test verifies agreement. The
        // protection is the same: drift fails the build.
        var index = RepositoryFiles.Read("docs", "adr", "README.md");
        var problems = new List<string>();

        foreach (var adr in Parse())
        {
            var row = index.Split('\n')
                .FirstOrDefault(l => l.StartsWith($"| [{adr.Id}]", StringComparison.Ordinal));

            if (row is null)
            {
                problems.Add($"{adr.Id}: the index has no row");
                continue;
            }

            if (!row.Contains($"| {adr.Status} |", StringComparison.Ordinal))
                problems.Add($"{adr.Id}: the index row does not give the status {adr.Status}");
        }

        foreach (var row in index.Split('\n').Where(l => l.StartsWith("| [0", StringComparison.Ordinal)))
        {
            var id = Regex.Match(row, @"\[(\d{4})\]").Groups[1].Value;
            if (Parse().All(a => a.Id != id))
                problems.Add($"the index has a row for {id}, which has no file");
        }

        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
    }

    private static IEnumerable<string> AdrFiles()
        => Directory.EnumerateFiles(RepositoryFiles.Path("docs", "adr"), "0*.md").OrderBy(f => f);

    private static List<Adr> Parse()
    {
        var records = new List<Adr>();

        foreach (var file in AdrFiles())
        {
            var fields = RepositoryFiles.ReadFrontMatter(File.ReadAllText(file));
            if (fields is null)
                continue;

            records.Add(new Adr(
                file,
                Get("id"),
                Get("title"),
                Get("status"),
                Get("date"),
                RepositoryFiles.ParseInlineList(Get("supersedes")),
                RepositoryFiles.ParseInlineList(Get("superseded-by")),
                RepositoryFiles.ParseInlineList(Get("amends")),
                RepositoryFiles.ParseInlineList(Get("amended-by")),
                Get("affects"),
                Get("enforced-by")));

            string Get(string key) => fields.TryGetValue(key, out var value) ? value : string.Empty;
        }

        return records;
    }
}
