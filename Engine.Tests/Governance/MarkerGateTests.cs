using System.Text.RegularExpressions;
using Xunit;

namespace Engine.Tests.Governance;

// The gate for register rule 5. A word that declares a thing temporary must
// carry a register identifier, therefore the thing has a date and a limit.
//
// This rule exists because the repository already held several such words.
// nuget.config called its folder feed an interim bootstrap. ArgParser.cs said
// that a later task would replace it. The native build workflow said DRAFT for
// months after it ran. Each one became permanent quietly.
//
// SCOPE. The gate reads live code and live configuration. It does not read
// history and it does not read a definition, for two reasons:
//
//   - An ADR and a closed task are records, and a ledger entry is append-only.
//     A gate that demanded a change to one would set two rules against each
//     other.
//   - A document that defines the rule must name the words in order to define
//     them. A match there is a definition, not a declaration of impermanence.
public class MarkerGateTests
{
    private static readonly Regex Marker = new(
        @"\b(TODO|FIXME|HACK|XXX|DRAFT)\b|\b(interim|temporary)\b|\bfor now\b|\bbootstrap\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RegisterId = new(@"\bR-\d{4}\b", RegexOptions.Compiled);

    [Fact]
    public void Every_Impermanence_Marker_In_Live_Code_Carries_A_Register_Identifier()
    {
        var offences = new List<string>();

        foreach (var file in LiveFiles())
        {
            var relative = RepositoryFiles.Relative(file);
            var lines = File.ReadAllLines(file);

            for (var i = 0; i < lines.Length; i++)
            {
                var match = Marker.Match(lines[i]);
                if (!match.Success)
                    continue;

                // The identifier may sit on the same line or just above it, so a
                // comment can introduce a block and cite the entry once.
                var window = string.Join("\n", lines.Skip(Math.Max(0, i - 2)).Take(3));

                if (!RegisterId.IsMatch(window))
                    offences.Add($"{relative}:{i + 1} says '{match.Value}' with no R-nnnn");
            }
        }

        Assert.True(
            offences.Count == 0,
            "Register rule 5: a word that declares a thing temporary must carry a register "
            + "identifier, therefore the thing has a date and a limit. Add an entry to "
            + "docs/register.md and cite it on the same line or just above it. Offences:\n  "
            + string.Join("\n  ", offences));
    }

    private static IEnumerable<string> LiveFiles()
    {
        // Source in each project that has a project file, except the files
        // that define the rules. Until TASK-0039 the gate named nine projects,
        // and a tenth project was not read (rules review finding F22). The
        // project files give the list, in the same way as the dependency gate.
        var projectDirectories = Directory
            .EnumerateFiles(RepositoryFiles.Root, "*.csproj", SearchOption.AllDirectories)
            .Select(RepositoryFiles.Relative)
            .Where(relative => !relative.Contains("/obj/", StringComparison.Ordinal)
                && !relative.Contains("/bin/", StringComparison.Ordinal)
                && !relative.StartsWith(".claude/", StringComparison.Ordinal))
            .Select(relative => System.IO.Path.GetDirectoryName(relative)!.Replace('\\', '/'))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(directory => directory, StringComparer.Ordinal);

        foreach (var projectDirectory in projectDirectories)
        {
            var directory = RepositoryFiles.Path(projectDirectory.Split('/'));

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relative = RepositoryFiles.Relative(file);
                if (relative.Contains("/obj/", StringComparison.Ordinal)
                    || relative.Contains("/bin/", StringComparison.Ordinal)
                    || relative.StartsWith("Engine.Tests/Governance/", StringComparison.Ordinal))
                    continue;

                yield return file;
            }
        }

        // Live configuration at the root.
        foreach (var name in new[] { "nuget.config", "global.json" })
        {
            var file = RepositoryFiles.Path(name);
            if (File.Exists(file))
                yield return file;
        }

        // Each workflow. A stale DRAFT in a workflow header is the exact defect
        // that register entry R-0009 records.
        var workflows = RepositoryFiles.Path(".github", "workflows");
        if (Directory.Exists(workflows))
        {
            foreach (var file in Directory.EnumerateFiles(workflows, "*.yml"))
                yield return file;
        }
    }
}
