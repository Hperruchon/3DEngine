using System.Text.RegularExpressions;

namespace Engine.Tests.Governance;

// Finding T167 of the codebase review of 2026-11-07 (TASK-0263): the affects fields of ADR-0082 and ADR-0083 did not name
// nine files that their decisions change, one review after T164 found the same defect in ADR-0076 and ADR-0077. The
// affects field decides which task must read an ADR (TaskGovernanceGateTests), so a file that is not in it lets a later
// task change the decision without the ADR. A code file whose comment cites an ADR implements a part of that ADR, so it
// must match the affects field of that ADR.
//
// The rule holds from ADR-0082. On 2026-11-07 the earlier ADRs had 379 such citations in 525 code files, many of them a
// reference in passing ("as ADR-0030 item 2 says"); a rule for them is a separate decision of the owner.
public class AffectsCitationGateTests
{
    // The first ADR that this rule holds.
    private const int FirstAdr = 82;

    private static readonly Regex Citation = new(@"ADR-(\d{4})", RegexOptions.Compiled);

    // Finding T171 of the codebase review of 2026-11-07 (b) (TASK-0272): the shaders and the PowerShell files cite ADRs too.
    private static readonly string[] CodeExtensions = [".cs", ".csproj", ".js", ".sh", ".yml", ".props", ".targets", ".frag", ".vert", ".ps1"];

    private static readonly string[] SkippedFolders = [".git", "bin", "obj", "node_modules", "docs", "tasks"];

    [Fact]
    public void Each_Code_File_That_Cites_An_Adr_From_0082_Matches_Its_Affects_Field()
    {
        var files = CodeFiles(RepositoryFiles.Root)
            .Select(path => (Path: RepositoryFiles.Relative(path), Text: File.ReadAllText(path)))
            .ToList();
        var offences = Offences(files, AffectsFields());

        Assert.True(offences.Count == 0,
            "A code file that cites an ADR in a comment must match the affects field of that ADR (finding T167). Add the path "
            + "to the affects field, with a line in the history of the ADR.\n  " + string.Join("\n  ", offences));
    }

    // The method on a small repository: a citation outside the field is an offence, a path in the field or in a
    // pattern of it is not, an ADR before 0082 is not held, and a test project is not read.
    [Fact]
    public void A_Citation_Outside_The_Affects_Field_Is_An_Offence()
    {
        var adrs = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["0083"] = ["Engine.Core/Import/StlReader.cs", "Engine.Core/Commands/**"],
        };
        (string, string)[] files =
        [
            ("Engine.Core/Import/StlReader.cs", "// ADR-0083 item 3"),
            ("Engine.Core/Commands/ImportStlCommandHandler.cs", "// ADR-0083 item 1"),
            ("Engine.Core/Export/StepExport.cs", "// ADR-0083 item 4"),
            ("Engine.Core/Queries/GetFacesQueryHandler.cs", "// ADR-0077 item 6"),
            ("Engine.Tests/Import/StlImportTests.cs", "// ADR-0083 item 3"),
            ("3DEngine.Vulkan/Shaders/ui.frag", "// ADR-0099 item 1"),
        ];

        Assert.Equal(
            ["Engine.Core/Export/StepExport.cs cites ADR-0083, whose affects field does not match it", "3DEngine.Vulkan/Shaders/ui.frag cites ADR-0099, which has no file"],
            Offences(files, adrs));
    }

    internal static List<string> Offences(IEnumerable<(string Path, string Text)> files, IReadOnlyDictionary<string, IReadOnlyList<string>> adrs)
    {
        var offences = new List<string>();
        foreach (var (path, text) in files.Where(file => !IsTest(file.Path)))
        {
            foreach (var id in Citation.Matches(text).Select(match => match.Groups[1].Value).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            {
                if (int.Parse(id, System.Globalization.CultureInfo.InvariantCulture) < FirstAdr)
                    continue;
                // T171 (TASK-0272): a citation of an ADR that has no file is an offence.
                if (!adrs.TryGetValue(id, out var affects))
                {
                    offences.Add($"{path} cites ADR-{id}, which has no file");
                    continue;
                }

                if (!affects.Any(pattern => RepositoryFiles.PathPattern(pattern).IsMatch(path)))
                    offences.Add($"{path} cites ADR-{id}, whose affects field does not match it");
            }
        }

        return offences;
    }

    // A test cites the ADR that it holds; the field enforced-by names the test, not the field affects.
    private static bool IsTest(string path) => path.Split('/')[0].EndsWith("Tests", StringComparison.Ordinal);

    private static Dictionary<string, IReadOnlyList<string>> AffectsFields()
    {
        var adrs = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(RepositoryFiles.Path("docs", "adr"), "0*.md"))
        {
            var fields = RepositoryFiles.ReadFrontMatter(File.ReadAllText(file));
            if (fields is null)
                continue;
            adrs[fields.GetValueOrDefault("id", Path.GetFileName(file)[..4])] = fields.GetValueOrDefault("affects", string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return adrs;
    }

    private static IEnumerable<string> CodeFiles(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            if (CodeExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                yield return file;
        }

        foreach (var folder in Directory.EnumerateDirectories(directory))
        {
            if (SkippedFolders.Contains(Path.GetFileName(folder), StringComparer.OrdinalIgnoreCase))
                continue;
            foreach (var file in CodeFiles(folder))
                yield return file;
        }
    }
}
