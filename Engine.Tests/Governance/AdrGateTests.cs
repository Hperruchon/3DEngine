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

    // The heading of the list of archived numbers in docs/adr/README.md. A
    // withdrawn or superseded record moves to docs/adr/archive/, and its number
    // stays in that list (docs/templates.md, section 1, rule 4). The gate does not
    // read the archive: an archived record is history, and no rule applies to its
    // text. Until TASK-0040 the gate had no archive, therefore a withdrawn record
    // stayed in the index and a reader had to skip it.
    private const string ArchivedHeading = "## Archived";

    // The count of ADRs with no enforcement, at the moment this gate was built.
    // The gate fails when the count grows. It does not fail on the present count,
    // because two records describe work that is deliberately not built.
    private const int UnenforcedBudget = 1;

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
        var archived = IndexRows().Archived.Select(RowId).ToHashSet(StringComparer.Ordinal);
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
                    // An archived record is not read, therefore its side of the
                    // pair is not checked. ADR-0019 supersedes the archived ADR-0011.
                    if (!archived.Contains(to))
                        problems.Add($"{from}: {field} names {to}, which is not in docs/adr/ and is not archived");
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
        var (inForce, archived) = IndexRows();
        var problems = new List<string>();

        foreach (var adr in Parse())
        {
            var row = inForce
                .FirstOrDefault(l => l.StartsWith($"| [{adr.Id}]", StringComparison.Ordinal));

            if (row is null)
            {
                problems.Add($"{adr.Id}: the index has no row");
                continue;
            }

            if (!row.Contains($"| {adr.Status} |", StringComparison.Ordinal))
                problems.Add($"{adr.Id}: the index row does not give the status {adr.Status}");
        }

        foreach (var id in inForce.Select(RowId))
        {
            if (Parse().All(a => a.Id != id))
                problems.Add($"the index has a row for {id}, which has no file");
        }

        foreach (var id in archived.Select(RowId))
        {
            if (Parse().Any(a => a.Id == id))
                problems.Add($"{id}: the index lists it under \"Archived\", but its file is still in docs/adr/; move the file to docs/adr/archive/");

            if (inForce.Any(l => RowId(l) == id))
                problems.Add($"{id}: the index lists it in force and under \"Archived\"");
        }

        problems.AddRange(ArchiveProblems(archived, ArchivedStatus));

        Assert.True(problems.Count == 0, string.Join("\n  ", problems));
    }

    // docs/templates.md, section 1, rule 4: a record that no decision needs
    // leaves docs/adr/, so that each record a reader opens there is in force or
    // proposed. Until TASK-0040 the rule was text only, and ADR-0003 stayed in
    // docs/adr/ with the status Withdrawn.
    [Fact]
    public void No_Record_In_The_Adr_Directory_Is_Withdrawn_Or_Superseded()
    {
        var problems = Parse()
            .Where(a => a.Status is "Withdrawn" or "Superseded")
            .Select(a => $"{a.Id}: the status is {a.Status}; move the file to docs/adr/archive/ and list "
                + "the number under \"Archived\" in docs/adr/README.md")
            .ToArray();

        Assert.True(problems.Length == 0, string.Join("\n  ", problems));
    }

    // The rows of the index in docs/adr/README.md: the rows above the heading
    // "Archived" are the records in docs/adr/, and the rows below it are the
    // archived numbers.
    private static (string[] InForce, string[] Archived) IndexRows()
    {
        var lines = RepositoryFiles.Read("docs", "adr", "README.md").Replace("\r", string.Empty).Split('\n');
        var heading = Array.FindIndex(lines, l => l.TrimEnd() == ArchivedHeading);
        var end = heading < 0 ? lines.Length : heading;

        static bool IsRow(string line) => line.StartsWith("| [0", StringComparison.Ordinal);
        return (lines[..end].Where(IsRow).ToArray(), lines[end..].Where(IsRow).ToArray());
    }

    private static string RowId(string row) => Regex.Match(row, @"\[(\d{4})\]").Groups[1].Value;

    private static IEnumerable<string> AdrFiles()
        => Directory.EnumerateFiles(RepositoryFiles.Path("docs", "adr"), "0*.md").OrderBy(f => f);

    // Finding T65 of the codebase review of 2026-10-28 (TASK-0089): an archived number must
    // have its file in docs/adr/archive/, with the status Withdrawn or Superseded, and the row
    // must give that status. Until TASK-0089 a deleted record with its row moved under
    // "Archived" passed the gate. The function is pure, so a test gives it each form.
    internal static IEnumerable<string> ArchiveProblems(IEnumerable<string> archivedRows, Func<string, string?> statusOfArchiveFile)
    {
        foreach (var row in archivedRows)
        {
            var id = RowId(row);
            var status = statusOfArchiveFile(id);
            if (status is null)
                yield return $"{id}: the index lists it under \"Archived\", and docs/adr/archive/ has no file of it";
            else if (status is not ("Withdrawn" or "Superseded"))
                yield return $"{id}: its file in docs/adr/archive/ has the status {status}; an archived record is Withdrawn or Superseded";
            else if (!row.Contains($"| {status} |", StringComparison.Ordinal))
                yield return $"{id}: the row under \"Archived\" does not give the status {status} of its file";
        }
    }

    // The status of the file of a number in docs/adr/archive/, or null when it has none.
    private static string? ArchivedStatus(string id)
    {
        var folder = RepositoryFiles.Path("docs", "adr", "archive");
        var file = Directory.Exists(folder) ? Directory.EnumerateFiles(folder, $"{id}-*.md").FirstOrDefault() : null;
        return file is null ? null : RepositoryFiles.ReadFrontMatter(File.ReadAllText(file))?.GetValueOrDefault("status") ?? string.Empty;
    }

    [Theory]
    [InlineData("| [0042](archive/0042-x.md) | X | Withdrawn | — | today |", null, "has no file of it")]
    [InlineData("| [0042](archive/0042-x.md) | X | Withdrawn | — | today |", "Accepted", "has the status Accepted")]
    [InlineData("| [0042](archive/0042-x.md) | X | Withdrawn | — | today |", "Superseded", "does not give the status Superseded")]
    public void Each_Form_Of_Finding_T65_Is_A_Problem(string row, string? status, string expected)
    {
        var problems = ArchiveProblems([row], _ => status).ToList();

        Assert.Contains(problems, problem => problem.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void An_Archived_Record_With_Its_File_And_Its_Status_Is_No_Problem()
    {
        Assert.Empty(ArchiveProblems(["| [0042](archive/0042-x.md) | X | Withdrawn | — | today |"], _ => "Withdrawn"));
    }

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
