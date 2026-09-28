using System.Text.RegularExpressions;

namespace Engine.Tests.Governance;

// The gate for the field enforced-by of an ADR in force. AdrGateTests reads
// only the prefix UNENFORCED, therefore an accepted ADR could name a test that
// did not exist and the budget of unenforced ADRs did not see it. On
// 2026-09-25 four accepted ADRs named a test that a later task creates. The
// rules review of that date recorded it as finding F17.
//
// The rule: an ADR in force names a file or a directory that exists. It may
// instead name a file and the task that creates it, in the form
// "path (TASK-nnnn creates it)", while that task has the status Ready or
// Active. When the task closes, the file must exist.
public class AdrEnforcementExistsGateTests
{
    private static readonly string[] NotInForce = ["Proposed", "Withdrawn", "Rejected"];

    private static readonly string[] OpenStatuses = ["Ready", "Active"];

    private static readonly Regex CreatedByTask = new(
        @"^(?<path>\S+)\s*\(TASK-(?<task>\d{4}) creates it\)$", RegexOptions.Compiled);

    [Fact]
    public void Each_Adr_In_Force_Names_An_Enforcement_That_Exists_Or_A_Task_That_Creates_It()
    {
        var tasks = RepositoryFiles.Tasks().ToDictionary(t => t.Id, t => t.Status, StringComparer.Ordinal);
        var problems = new List<string>();

        foreach (var file in Directory.EnumerateFiles(RepositoryFiles.Path("docs", "adr"), "0*.md").OrderBy(f => f, StringComparer.Ordinal))
        {
            var fields = RepositoryFiles.ReadFrontMatter(File.ReadAllText(file));
            if (fields is null)
                continue;

            var id = fields.GetValueOrDefault("id", Path.GetFileName(file));
            var status = fields.GetValueOrDefault("status", string.Empty);
            var value = fields.GetValueOrDefault("enforced-by", string.Empty).Trim();

            if (NotInForce.Contains(status) || value.StartsWith("UNENFORCED", StringComparison.Ordinal))
                continue;

            var match = CreatedByTask.Match(value);
            var path = match.Success ? match.Groups["path"].Value : value;

            if (Exists(path))
                continue;

            if (!match.Success)
            {
                problems.Add($"ADR-{id}: enforced-by names '{path}', which does not exist");
                continue;
            }

            var task = $"TASK-{match.Groups["task"].Value}";
            if (!tasks.TryGetValue(task, out var taskStatus))
                problems.Add($"ADR-{id}: enforced-by names '{path}' and {task}, and that task does not exist");
            else if (!OpenStatuses.Contains(taskStatus))
                problems.Add($"ADR-{id}: enforced-by names '{path}' and {task}, whose status is {taskStatus}; the file must exist");
        }

        Assert.True(
            problems.Count == 0,
            "An ADR in force must name a test that exists, or a test and the Ready task that creates it. "
            + "See docs/templates.md, section 1.\n  " + string.Join("\n  ", problems));
    }

    private static bool Exists(string path)
    {
        var relative = path.EndsWith("/**", StringComparison.Ordinal) ? path[..^3] : path;
        var absolute = RepositoryFiles.Path(relative.Split('/'));
        return File.Exists(absolute) || Directory.Exists(absolute);
    }
}
