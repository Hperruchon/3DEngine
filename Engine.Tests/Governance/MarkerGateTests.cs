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
//
// The identifier must be an entry of docs/register.md. Until TASK-0043 any
// four digits passed (codebase review of 2026-09-30, finding T4).
//
// Finding T4 of the codebase review of 2026-09-30 and finding T13 of the codebase
// review of 2026-10-04 (R-0031, TASK-0320): the identifier must be an entry of the
// section "Open" or of the section "Accepted compromises". A closed entry has no
// limit, and R-0000 is the template of the section "Entry format". Until TASK-0320
// each R-nnnn anywhere in the register passed.
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
        var known = LiveEntries(RepositoryFiles.Read("docs", "register.md"));
        Assert.True(known.Count > 0, "docs/register.md holds no entry in the sections \"Open\" and \"Accepted compromises\".");

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
                var cited = RegisterId.Matches(window).Select(m => m.Value).ToArray();

                if (cited.Length == 0)
                    offences.Add($"{relative}:{i + 1} says '{match.Value}' with no R-nnnn");
                else if (!cited.Any(known.Contains))
                    offences.Add($"{relative}:{i + 1} says '{match.Value}' and cites {string.Join(", ", cited)}, which is not an open or an accepted entry of docs/register.md");
            }
        }

        Assert.True(
            offences.Count == 0,
            "Register rule 5: a word that declares a thing temporary must carry a register "
            + "identifier, therefore the thing has a date and a limit. Add an entry to "
            + "docs/register.md and cite it on the same line or just above it. Offences:\n  "
            + string.Join("\n  ", offences));
    }

    // The identifier of each entry heading in the sections "Open" and "Accepted
    // compromises" (TASK-0320). A line in a code block is not a heading.
    private static HashSet<string> LiveEntries(string register)
    {
        var entries = new HashSet<string>(StringComparer.Ordinal);
        var live = false;
        var inCodeBlock = false;

        foreach (var raw in register.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            if (inCodeBlock)
                continue;

            if (line.StartsWith("## ", StringComparison.Ordinal))
                live = line.TrimEnd() is "## Open" or "## Accepted compromises";
            else if (live && Regex.Match(line, @"^### (R-\d{4})\b") is { Success: true } heading)
                entries.Add(heading.Groups[1].Value);
        }

        return entries;
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

        // Each tracked text file outside docs/ and tasks/: the configuration, each workflow, each
        // script, shader source and MSBuild file, the solution, the lists of eng/ and the notes at
        // the root. Finding T4 of the codebase review of 2026-09-30 (R-0031, TASK-0320):
        // nuget/.gitignore called the folder feed an interim bootstrap with no identifier. Finding
        // T13 of the codebase review of 2026-10-04 (TASK-0451): a TODO in a script, a project file
        // or a shader passed. TASK-0461 (finding T196 of the codebase review of 2026-11-23): the
        // gate read three named configuration files, the workflows and the files of a list of
        // extensions, so a TODO with no identifier in the root .gitignore, in eng/android/*.ere, in
        // eng/*.txt, in .github/CODEOWNERS or in the solution passed. The list of git is the list
        // of the repository, so a folder that git ignores is not read. A file with a byte 0 in its
        // first 8000 bytes is not text, as git decides, so a font or a compiled shader is not read.
        foreach (var relative in DependencyDirectionGateTests.TrackedFiles().Where(IsReadAsText))
        {
            var file = RepositoryFiles.Path(relative.Split('/'));
            if (File.Exists(file) && IsText(file))
                yield return file;
        }
    }

    // The folders of the records and of the definitions, which the gate does not read (see SCOPE),
    // and the folder of the worktrees of the agent sessions.
    private static readonly string[] NotRead = ["docs/", "tasks/", ".claude/", "Engine.Tests/Governance/"];

    // A tracked file that the gate reads as text: each file outside the folders of NotRead, except
    // a document (a Markdown file, such as CLAUDE.md, which defines the rule, or a note of a
    // project). A C# source is read with the project that holds it, above, so it is not read a
    // second time here.
    internal static bool IsReadAsText(string relative)
        => !NotRead.Any(folder => relative.StartsWith(folder, StringComparison.Ordinal))
        && !relative.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
        && !relative.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

    private static bool IsText(string file)
    {
        using var stream = File.OpenRead(file);
        var head = new byte[8000];
        var read = stream.Read(head, 0, head.Length);
        return Array.IndexOf(head, (byte)0, 0, read) < 0;
    }

    // TASK-0451 and TASK-0461: the files of the forms of T13 and of T196 are read, and a record,
    // a document and a C# source of this list are not.
    [Theory]
    [InlineData("eng/report-failed-tests.js", true)]
    [InlineData("eng/manifold-native/build.sh", true)]
    [InlineData("eng/manifold-native/build.ps1", true)]
    [InlineData("Engine.Core/Engine.Core.csproj", true)]
    [InlineData("Engine.Core/Directory.Build.props", true)]
    [InlineData("3DEngine.Vulkan/Shaders/mesh.frag", true)]
    [InlineData(".gitignore", true)]
    [InlineData("nuget/.gitignore", true)]
    [InlineData("eng/android/dotnet-android.ere", true)]
    [InlineData("eng/test-floor.txt", true)]
    [InlineData(".github/CODEOWNERS", true)]
    [InlineData(".github/workflows/ci.yml", true)]
    [InlineData("3DEngine.sln", true)]
    [InlineData("docs/register.md", false)]
    [InlineData("CLAUDE.md", false)]
    [InlineData("3DEngine/Documentation/Architecture.md", false)]
    [InlineData("tasks/TASK-0461-the-findings-of-the-review-of-2026-11-23.md", false)]
    [InlineData("Engine.Tests/Governance/MarkerGateTests.cs", false)]
    [InlineData("Engine.Core/CommandBus.cs", false)]
    [InlineData(".claude/worktrees/a/eng/count-tests.js", false)]
    public void The_Gate_Reads_Each_Tracked_Text_File_Outside_The_Records(string relative, bool read)
    {
        Assert.Equal(read, IsReadAsText(relative));
    }
}
