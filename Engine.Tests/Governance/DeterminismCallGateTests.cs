using System.Text.RegularExpressions;

namespace Engine.Tests.Governance;

// The gate for determinism rules 1, 3, 4 and 5 in CLAUDE.md. Finding T2 of the
// codebase review of 2026-09-23: no gate scanned for the calls that the rules
// forbid, and the rules review of 2026-09-25 made it finding F20. Rules 2 and 6
// need judgement and stay as text.
//
// Rule 1 forbids a transcendental function in code that writes to the log.
// Rule 3 forbids Math.FusedMultiplyAdd. Rule 4 forbids the five functions
// whose guarantee covers one process only. Rule 5 forbids a 32-bit x86
// runtime identifier. The names below are the rule; CLAUDE.md gives the
// reason for each one.
//
// The gate reads each source as one text with its comments removed, so a call
// with the parenthesis on the next line is seen and a comment that names a
// function is not. It refuses the same functions on double, float and Half,
// and it refuses a static import of System.Math, because a bare name after
// that import has no form that a scan can see (codebase review of
// 2026-09-30, finding T4).
public class DeterminismCallGateTests
{
    // The projects whose code writes to the log or computes what the log
    // records. The render side is presentation and may use an angle.
    private static readonly string[] LogPathProjects =
        ["Engine.Contracts", "Engine.Core", "Engine.Geometry.Manifold"];

    private const string ForbiddenNames =
        "Sin|Cos|Tan|Asin|Acos|Atan|Atan2|Sinh|Cosh|Tanh|Asinh|Acosh|Atanh|SinCos"
        + "|Exp|Log|Log2|Log10|Pow|Cbrt|FusedMultiplyAdd";

    private static readonly Regex ForbiddenCall = new(
        @"\b(Math|MathF|double|float|Half)\.(" + ForbiddenNames + @")\s*\("
        + @"|\b(MultiplyAddEstimate|MinNative|MaxNative|ShuffleNative|ConvertToIntegerNative)\s*\(",
        RegexOptions.Compiled);

    private static readonly Regex StaticImport = new(
        @"\busing\s+static\s+(global::)?System\.(Math|MathF)\s*;", RegexOptions.Compiled);

    private static readonly Regex BlockComment = new(@"/\*[\s\S]*?\*/", RegexOptions.Compiled);

    private static readonly Regex LineComment = new(@"//[^\n]*", RegexOptions.Compiled);

    private static readonly Regex RuntimeIdentifier = new(
        @"<RuntimeIdentifiers?(\s[^>]*)?>(?<value>[^<]*)</RuntimeIdentifiers?>", RegexOptions.Compiled);

    [Fact]
    public void No_Source_In_The_Log_Path_Calls_A_Function_That_The_Determinism_Rules_Forbid()
    {
        var offences = new List<string>();

        foreach (var project in LogPathProjects)
        {
            var directory = RepositoryFiles.Path(project);
            Assert.True(Directory.Exists(directory), $"{project} was not found.");

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relative = RepositoryFiles.Relative(file);
                if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal))
                    continue;

                var text = WithoutComments(File.ReadAllText(file).Replace("\r", string.Empty));

                foreach (Match match in StaticImport.Matches(text))
                    offences.Add($"{relative}:{LineOf(text, match.Index)} imports System.{match.Groups[2].Value} statically");

                foreach (Match match in ForbiddenCall.Matches(text))
                    offences.Add($"{relative}:{LineOf(text, match.Index)} calls {match.Value.TrimEnd('(', ' ', '\n')}");
            }
        }

        Assert.True(
            offences.Count == 0,
            "CLAUDE.md, section \"Determinism rules\": a replay must give the same result on each "
            + "platform, and these functions do not. Use +, -, *, / and sqrt, or record the number "
            + "that the client computed. Offences:\n  " + string.Join("\n  ", offences));
    }

    [Fact]
    public void No_Project_Ships_A_32_Bit_Runtime_Identifier()
    {
        var offences = new List<string>();

        foreach (var file in Directory.EnumerateFiles(RepositoryFiles.Root, "*.csproj", SearchOption.AllDirectories))
        {
            var relative = RepositoryFiles.Relative(file);
            if (relative.Contains("/obj/", StringComparison.Ordinal)
                || relative.Contains("/bin/", StringComparison.Ordinal)
                || relative.StartsWith(".claude/", StringComparison.Ordinal))
                continue;

            foreach (Match match in RuntimeIdentifier.Matches(File.ReadAllText(file)))
            {
                foreach (var identifier in match.Groups["value"].Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (identifier.EndsWith("-x86", StringComparison.OrdinalIgnoreCase))
                        offences.Add($"{relative} names the runtime identifier {identifier}");
                }
            }
        }

        Assert.True(
            offences.Count == 0,
            "CLAUDE.md, determinism rule 5: do not ship a 32-bit x86 runtime identifier. Offences:\n  "
            + string.Join("\n  ", offences));
    }

    // Each comment becomes blank, and each line break stays, so that a line
    // number in a report is the line number in the file.
    private static string WithoutComments(string text)
    {
        text = BlockComment.Replace(text, m => new string('\n', m.Value.Count(c => c == '\n')));
        return LineComment.Replace(text, string.Empty);
    }

    private static int LineOf(string text, int index)
        => text.AsSpan(0, index).Count('\n') + 1;
}
