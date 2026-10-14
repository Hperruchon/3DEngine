using System.Text.RegularExpressions;
using System.Xml.Linq;
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
//
// Finding T49 of the codebase review of 2026-10-13 showed five more forms that
// compile and passed the gate: code that a preprocessor condition hid, a file in
// a folder obj/ below the root of a project, a Compile item from outside the
// project, a creator of System.Numerics that calls a transcendental function, and
// reflection on Math. Since TASK-0073 the gate refuses each one.
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

        // The members of System.Numerics that call a transcendental function
        // inside the framework (T49).
        "CreateRotation", "CreateRotationX", "CreateRotationY", "CreateRotationZ", "CreateFromAxisAngle",
        "CreateFromYawPitchRoll", "CreateSkew", "CreatePerspectiveFieldOfView",
        "CreatePerspectiveFieldOfViewLeftHanded", "Slerp", "FromPolarCoordinates", "Phase",

        // Reflection, which calls a method that the source does not name (T49).
        "GetMethod", "GetMethods", "GetMember", "GetMembers", "FindMembers", "GetRuntimeMethod",
        "GetRuntimeMethods", "GetDeclaredMethod", "GetDeclaredMethods", "InvokeMember", "CreateDelegate",
        "GetMethodFromHandle", "Call",
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
        var sources = new List<(string Path, string Source)>();

        foreach (var project in LogPathProjects)
        {
            var directory = RepositoryFiles.Path(project);
            Assert.True(Directory.Exists(directory), $"{project} was not found.");

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relative = RepositoryFiles.Relative(file);
                if (IsCompiled(project, relative))
                    sources.Add((relative, File.ReadAllText(file)));
            }
        }

        var offences = CallOffences(sources);

        Assert.True(
            offences.Count == 0,
            "CLAUDE.md, section \"Determinism rules\": a replay must give the same result on each "
            + "platform, and these functions do not. Use +, -, *, / and sqrt, or record the number "
            + "that the client computed. Offences:\n  " + string.Join("\n  ", offences));
    }

    // The forms of finding T49 that a source holds. Each one passed the gate of
    // main and must be an offence (TASK-0073).
    [Theory]
    [InlineData("internal static class H\n{\n#if NET\n    internal static double F(double x) => Math.Sin(x);\n#endif\n}\n", "Probe.cs:4 holds code that a preprocessor condition hides")]
    [InlineData("using System.Numerics;\ninternal static class C { internal static Matrix4x4 R() => Matrix4x4.CreateRotationZ(1f); }", "calls System.Numerics.Matrix4x4.CreateRotationZ")]
    [InlineData("using System.Numerics;\ninternal static class C { internal static Complex P() => Complex.FromPolarCoordinates(1, 2); }", "calls System.Numerics.Complex.FromPolarCoordinates")]
    [InlineData("using System.Numerics;\ninternal static class C { internal static double A() => new Complex(1, 1).Phase; }", "calls System.Numerics.Complex.Phase")]
    [InlineData("internal static class R { internal static object? S() => typeof(Math).GetMethod(\"Sin\")!.Invoke(null, [1.0]); }", "calls System.Type.GetMethod")]
    [InlineData("using System.Linq.Expressions;\ninternal static class R { internal static Expression E() => Expression.Call(typeof(Math), \"Cos\", null, Expression.Constant(1.0)); }", "calls System.Linq.Expressions.Expression.Call")]
    public void Each_Form_Of_Finding_T49_In_A_Source_Is_An_Offence(string source, string expected)
    {
        var offences = CallOffences([("Probe.cs", source)]);

        Assert.True(
            offences.Any(o => o.Contains(expected, StringComparison.Ordinal)),
            $"Expected an offence with '{expected}'. Offences:\n  " + string.Join("\n  ", offences));
    }

    // MSBuild leaves out the folders obj/ and bin/ at the root of the project
    // only. Until TASK-0073 the gate skipped such a folder at each depth, and a
    // file Geometry/obj/Trig.cs compiled with no check (T49).
    [Fact]
    public void A_Folder_Obj_Below_The_Root_Of_A_Project_Is_Read()
    {
        Assert.True(IsCompiled("Engine.Core", "Engine.Core/Geometry/obj/Trig.cs"));
        Assert.True(IsCompiled("Engine.Core", "Engine.Core/Geometry/bin/Trig.cs"));
        Assert.False(IsCompiled("Engine.Core", "Engine.Core/obj/Debug/Engine.Core.AssemblyInfo.cs"));
        Assert.False(IsCompiled("Engine.Core", "Engine.Core/bin/Trig.cs"));
    }

    private static bool IsCompiled(string project, string relative)
        => !relative.StartsWith($"{project}/obj/", StringComparison.Ordinal)
        && !relative.StartsWith($"{project}/bin/", StringComparison.Ordinal);

    // Each offence of the sources: a static import of Math, code that a
    // preprocessor condition hides, and a name of the list that binds to a member
    // of the framework or binds to nothing.
    private static List<string> CallOffences(IEnumerable<(string Path, string Source)> sources)
    {
        var offences = new List<string>();
        var trees = new List<SyntaxTree>();
        var options = new CSharpParseOptions(LanguageVersion.Preview);

        foreach (var (relative, source) in sources)
        {
            var tree = CSharpSyntaxTree.ParseText(source, options, path: relative);
            trees.Add(tree);

            // The gate parses with no preprocessor symbol, and the build defines
            // several. Code that a condition leaves out of the parse is code that
            // the gate cannot read; until TASK-0073 a call inside #if NET passed
            // (T49).
            foreach (var hidden in tree.GetRoot().DescendantTrivia().Where(t => t.IsKind(SyntaxKind.DisabledTextTrivia)))
                offences.Add($"{relative}:{tree.GetLineSpan(hidden.Span).StartLinePosition.Line + 1} holds code that a preprocessor condition hides from the gate");

            var text = WithoutComments(source.Replace("\r", string.Empty));
            foreach (Match match in StaticImport.Matches(text))
                offences.Add($"{relative}:{LineOf(text, match.Index)} imports System.{match.Groups[2].Value} statically");
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
                else if (symbols.Where(s => s is IMethodSymbol or IPropertySymbol).FirstOrDefault(IsFrameworkMember) is { } member)
                    offences.Add($"{tree.FilePath}:{line} calls {member.ContainingType.ToDisplayString()}.{member.Name}");
            }
        }

        return offences;
    }

    // A Compile item adds a source that the gate does not read, and an Analyzer
    // item adds a source generator whose output the gate does not see. Each file
    // that MSBuild reads for a project of the log path must hold neither. Until
    // TASK-0073 a Compile Include of a file outside the project passed (T49).
    [Fact]
    public void No_File_That_MsBuild_Reads_For_The_Log_Path_Adds_A_Source()
    {
        var offences = new List<string>();

        foreach (var project in LogPathProjects)
        {
            var projectFile = RepositoryFiles.Path(project, $"{project}.csproj");
            Assert.True(File.Exists(projectFile), $"{project}.csproj was not found.");

            foreach (var file in DependencyDirectionGateTests.FilesThatMsBuildReads(projectFile))
                offences.AddRange(SourceItemOffences(RepositoryFiles.Relative(file), XDocument.Load(file)));
        }

        Assert.True(
            offences.Count == 0,
            "CLAUDE.md, section \"Determinism rules\": the gate reads the files *.cs in the folder of each "
            + "project of the log path, and nothing else. Put the source in that folder. Offences:\n  "
            + string.Join("\n  ", offences));
    }

    // The form of finding T49 that a project file holds (TASK-0073).
    [Theory]
    [InlineData("<Project><ItemGroup><Compile Include=\"..\\docs\\Outside.cs\" /></ItemGroup></Project>", "adds the item Compile")]
    [InlineData("<Project><ItemGroup><Analyzer Include=\"..\\docs\\Generator.dll\" /></ItemGroup></Project>", "adds the item Analyzer")]
    [InlineData("<Project><Target Name=\"T\"><CreateItem Include=\"x.cs\"><Output TaskParameter=\"Include\" ItemName=\"Compile\" /></CreateItem></Target></Project>", "makes Compile items")]
    public void Each_Form_Of_Finding_T49_In_A_Project_File_Is_An_Offence(string project, string expected)
    {
        var offences = SourceItemOffences("Probe.csproj", XDocument.Parse(project));

        Assert.True(
            offences.Any(o => o.Contains(expected, StringComparison.Ordinal)),
            $"Expected an offence with '{expected}'. Offences:\n  " + string.Join("\n  ", offences));
    }

    private static IEnumerable<string> SourceItemOffences(string relative, XDocument document)
    {
        foreach (var element in document.Descendants())
        {
            var name = element.Name.LocalName;
            if (name is "Compile" or "Analyzer" && element.Attribute("Include") is not null)
                yield return $"{relative} adds the item {name} {element.Attribute("Include")!.Value}";
            else if (name == "Output" && element.Attribute("ItemName")?.Value is "Compile" or "Analyzer")
                yield return $"{relative} makes {element.Attribute("ItemName")!.Value} items in a task";
        }
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

    // A member of each namespace of the framework. Until TASK-0073 the list held
    // System, System.Numerics and System.Runtime.Intrinsics; reflection and
    // System.Linq.Expressions are in other namespaces (T49).
    private static bool IsFrameworkMember(ISymbol member)
    {
        var space = member.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        return space == "System" || space.StartsWith("System.", StringComparison.Ordinal);
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
