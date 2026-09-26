namespace Engine.Tests.Governance;

// The gate for two task fields in docs/templates.md. The field governed-by
// must equal the set of ADRs in force whose affects patterns intersect the
// write set of the task, so that an absent ADR is an error and not an
// assumption. The field depends-on must name a task that exists. Until this
// gate, "a gate can verify this" was a sentence in the template.
//
// Only a Ready or Active task is checked for governed-by. A closed task is a
// record. The rules review of 2026-09-25 found nine closed tasks that differ
// from the rule, because ADR-0018 to ADR-0021 arrived after those tasks closed.
public class TaskGovernanceGateTests
{
    private static readonly string[] NotInForce = ["Proposed", "Withdrawn", "Rejected"];

    private static readonly string[] OpenStatuses = ["Ready", "Active"];

    private sealed record Adr(string Id, IReadOnlyList<string> Affects);

    [Fact]
    public void Each_Open_Task_Lists_Exactly_The_Adrs_Whose_Affects_Intersect_Its_Write_Set()
    {
        var adrs = AdrsInForce();
        var problems = new List<string>();

        foreach (var task in RepositoryFiles.Tasks().Where(t => OpenStatuses.Contains(t.Status)))
        {
            var written = task.Written.ToArray();
            var expected = adrs
                .Where(a => a.Affects.Any(pattern => written.Any(path => RepositoryFiles.PatternsIntersect(pattern, path))))
                .Select(a => a.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            var declared = RepositoryFiles.ParseInlineList(task.Fields.GetValueOrDefault("governed-by"))
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            var missing = expected.Except(declared, StringComparer.Ordinal).ToArray();
            var extra = declared.Except(expected, StringComparer.Ordinal).ToArray();

            if (missing.Length > 0)
                problems.Add($"{task.Id}: governed-by lacks {string.Join(", ", missing)}, whose affects intersect the write set");

            if (extra.Length > 0)
                problems.Add($"{task.Id}: governed-by names {string.Join(", ", extra)}, which affect no path in the write set or are not in force");
        }

        Assert.True(
            problems.Count == 0,
            "governed-by must equal the ADRs in force whose affects intersect writes. See docs/templates.md, "
            + "section 2. Correct the field, the write set, or the affects field of the ADR.\n  "
            + string.Join("\n  ", problems));
    }

    [Fact]
    public void Each_Depends_On_Names_A_Task_That_Exists()
    {
        var tasks = RepositoryFiles.Tasks();
        var known = tasks.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);

        // Task files 0001 to 0013 carry no front matter and are still tasks.
        foreach (var file in RepositoryFiles.TaskFiles())
            known.Add(Path.GetFileName(file)[..9]);

        var problems = tasks
            .SelectMany(t => RepositoryFiles.ParseInlineList(t.Fields.GetValueOrDefault("depends-on"))
                .Where(id => !known.Contains($"TASK-{id}"))
                .Select(id => $"{t.Id}: depends-on names {id}, which is not a task file"))
            .ToArray();

        Assert.True(problems.Length == 0, string.Join("\n  ", problems));
    }

    private static List<Adr> AdrsInForce()
    {
        var adrs = new List<Adr>();

        foreach (var file in Directory.EnumerateFiles(RepositoryFiles.Path("docs", "adr"), "0*.md"))
        {
            var fields = RepositoryFiles.ReadFrontMatter(File.ReadAllText(file));
            if (fields is null || NotInForce.Contains(fields.GetValueOrDefault("status", string.Empty)))
                continue;

            var affects = fields.GetValueOrDefault("affects", string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            adrs.Add(new Adr(fields.GetValueOrDefault("id", Path.GetFileName(file)[..4]), affects));
        }

        return adrs;
    }
}
