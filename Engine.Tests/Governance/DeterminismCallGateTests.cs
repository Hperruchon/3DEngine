using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
// The gate compiles the sources of the log path with the C# compiler (Roslyn),
// with no build and no run, and binds each name of the list below. A name that
// binds to a method of the framework (a namespace System, System.Numerics or
// System.Runtime.Intrinsics) is an offence, whatever its form: Math.Sin,
// Double.Sin, an alias of System.Math, a method group, a static import or a
// generic method of an interface of System.Numerics. A name of the list that
// does not bind to a symbol is also an offence, because the gate cannot see what
// it calls. Until TASK-0066 the gate matched a call as text, and eight forms
// that compile passed it (finding T45 of the codebase review of 2026-10-12). It
// still refuses a static import of System.Math as text (codebase review of
// 2026-09-30, finding T4).
public class DeterminismCallGateTests
{
    // The projects whose code writes to the log or computes what the log
    // records. The render side is presentation and may use an angle.
    private static readonly string[] LogPathProjects =
        ["Engine.Contracts", "Engine.Core", "Engine.Geometry.Manifold"];

    // Rule 1: each transcendental function, and each function whose result is an
    // estimate or is not one correctly rounded result. Rule 3: FusedMultiplyAdd.
    // Rule 4: the five functions whose guarantee covers one process only.
    private static readonly HashSet<string> ForbiddenMethods = new(StringComparer.Ordinal)
    {
        "Sin", "Cos", "Tan", "Asin", "Acos", "Atan", "Atan2", "Sinh", "Cosh", "Tanh", "Asinh", "Acosh",
        "Atanh", "SinCos", "SinPi", "CosPi", "TanPi", "AsinPi", "AcosPi", "AtanPi", "Atan2Pi", "SinCosPi",
        "Exp", "Exp2", "Exp10", "ExpM1", "Exp2M1", "Exp10M1", "Log", "Log2", "Log10", "LogP1", "Log2P1",
        "Log10P1", "Pow", "Cbrt", "Hypot", "RootN", "ReciprocalEstimate", "ReciprocalSqrtEstimate",
        "FusedMultiplyAdd",
        "MultiplyAddEstimate", "MinNative", "MaxNative", "ShuffleNative", "ConvertToIntegerNative",
    };

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
        var trees = new List<SyntaxTree>();
        var options = new CSharpParseOptions(LanguageVersion.Preview);

        foreach (var project in LogPathProjects)
        {
            var directory = RepositoryFiles.Path(project);
            Assert.True(Directory.Exists(directory), $"{project} was not found.");

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relative = RepositoryFiles.Relative(file);
                if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal))
                    continue;

                var source = File.ReadAllText(file);
                trees.Add(CSharpSyntaxTree.ParseText(source, options, path: relative));

                var text = WithoutComments(source.Replace("\r", string.Empty));
                foreach (Match match in StaticImport.Matches(text))
                    offences.Add($"{relative}:{LineOf(text, match.Index)} imports System.{match.Groups[2].Value} statically");
            }
        }

        // The global usings that the SDK writes for a project with
        // ImplicitUsings, which each project of the log path enables. Without
        // them, Math and Double do not bind.
        trees.Add(CSharpSyntaxTree.ParseText(
            "global using System; global using System.Collections.Generic; global using System.IO; "
            + "global using System.Linq; global using System.Net.Http; global using System.Threading; "
            + "global using System.Threading.Tasks;",
            options,
            path: "ImplicitUsings.g.cs"));

        // One compilation of the three projects, with the assemblies of the
        // framework that runs the gate. It does not need to succeed: an error, for
        // example a method that a source generator completes, does not stop the
        // binding of the other names.
        var compilation = CSharpCompilation.Create(
            "LogPath",
            trees,
            FrameworkReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var name in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (!ForbiddenMethods.Contains(name.Identifier.ValueText))
                    continue;

                var info = model.GetSymbolInfo(name);
                ISymbol[] symbols = info.Symbol is { } symbol ? [symbol] : [.. info.CandidateSymbols];
                var line = tree.GetLineSpan(name.Span).StartLinePosition.Line + 1;

                if (symbols.Length == 0)
                    offences.Add($"{tree.FilePath}:{line} names {name.Identifier.ValueText}, which the gate cannot bind");
                else if (symbols.OfType<IMethodSymbol>().FirstOrDefault(IsFrameworkMethod) is { } method)
                    offences.Add($"{tree.FilePath}:{line} calls {method.ContainingType.ToDisplayString()}.{method.Name}");
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

        // Each MSBuild file and not each project file only: a Directory.Build.props
        // with the identifier passed until TASK-0047 (codebase review of
        // 2026-10-04, finding T9).
        var files = new[] { "*.csproj", "*.props", "*.targets" }
            .SelectMany(pattern => Directory.EnumerateFiles(RepositoryFiles.Root, pattern, SearchOption.AllDirectories));

        foreach (var file in files)
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

    private static bool IsFrameworkMethod(IMethodSymbol method)
    {
        var space = method.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        return space is "System" or "System.Numerics" || space.StartsWith("System.Runtime.Intrinsics", StringComparison.Ordinal);
    }

    // The assemblies of the framework that runs the gate, from the list of the
    // host. A source of the judged checkout cannot change this list.
    private static IEnumerable<MetadataReference> FrameworkReferences()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(path => Path.GetFileName(path) is var name
                && (name.StartsWith("System.", StringComparison.Ordinal) || name is "mscorlib.dll" or "netstandard.dll"))
            .Select(path => MetadataReference.CreateFromFile(path));

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
