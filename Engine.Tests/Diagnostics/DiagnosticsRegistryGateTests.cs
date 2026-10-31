using System.Text;

namespace Engine.Tests.Diagnostics;

public class DiagnosticsRegistryGateTests
{
    [Fact]
    public void All_Diagnostic_Codes_In_Engine_Sources_Are_Registered()
    {
        var repoRoot = DiagnosticsScanner.FindRepoRoot(AppContext.BaseDirectory);
        var registry = DiagnosticsScanner.ParseRegistry(
            File.ReadAllText(Path.Combine(repoRoot, "docs", "diagnostics.md")));

        var sources = DiagnosticsScanner.EnumerateEngineSources(repoRoot)
            .Select(sourcePath => (Path: Path.GetRelativePath(repoRoot, sourcePath).Replace('\\', '/'), Text: File.ReadAllText(sourcePath)))
            .ToList();
        var uses = sources.SelectMany(source => DiagnosticsScanner.ExtractCodes(source.Text).Select(code => (source.Path, Code: code)))
            .Concat(DiagnosticsScanner.ConstantCodes(sources));

        var found = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var (path, code) in uses)
        {
            if (!found.TryGetValue(code, out var refs))
            {
                refs = new SortedSet<string>(StringComparer.Ordinal);
                found[code] = refs;
            }
            refs.Add(path);
        }

        var unregistered = found.Where(kv => !registry.Contains(kv.Key)).ToList();
        if (unregistered.Count == 0)
            return;

        var message = new StringBuilder();
        message.AppendLine("Diagnostic code(s) used in source but not registered in docs/diagnostics.md:");
        foreach (var (code, refs) in unregistered)
        {
            message.Append("  ").AppendLine(code);
            foreach (var path in refs)
                message.Append("    ").AppendLine(path);
        }
        message.AppendLine("Add the code to docs/diagnostics.md before merging.");
        Assert.Fail(message.ToString());
    }

    [Fact]
    public void Registry_Parser_Extracts_All_Seed_Codes()
    {
        var repoRoot = DiagnosticsScanner.FindRepoRoot(AppContext.BaseDirectory);
        var registry = DiagnosticsScanner.ParseRegistry(
            File.ReadAllText(Path.Combine(repoRoot, "docs", "diagnostics.md")));

        Assert.Contains("E-CMD-UNKNOWN", registry);
        Assert.Contains("E-CMD-VERSION-STALE", registry);
        Assert.Contains("E-CMD-BUS-BUSY", registry);
        Assert.Contains("E-QRY-UNKNOWN", registry);
    }

    [Fact]
    public void Scanner_Extracts_Code_From_Sample_Source()
    {
        const string sample = """
            namespace Sample;
            public static class Codes { public const string X = "E-FOO-BAR"; }
            """;

        var codes = DiagnosticsScanner.ExtractCodes(sample).ToList();
        Assert.Contains("E-FOO-BAR", codes);
    }

    [Theory]
    [InlineData("E-foo-bar")]
    [InlineData("EE-CMD-XX")]
    [InlineData("E--CMD-XX")]
    [InlineData("CMD-XX")]
    [InlineData("E-CMD-")]
    public void Scanner_Ignores_Tokens_That_Do_Not_Match_Code_Shape(string token)
    {
        var sample = $"// reference: {token} end";
        Assert.DoesNotContain(token, DiagnosticsScanner.ExtractCodes(sample));
    }

    [Fact]
    public void Source_Enumeration_Skips_Bin_And_Obj()
    {
        var repoRoot = DiagnosticsScanner.FindRepoRoot(AppContext.BaseDirectory);
        var sources = DiagnosticsScanner.EnumerateEngineSources(repoRoot).ToList();

        Assert.NotEmpty(sources);
        Assert.Contains(sources, p =>
            p.EndsWith(Path.Combine("Engine.Core", "CommandBus.cs"), StringComparison.Ordinal));

        foreach (var path in sources)
        {
            var segments = Path.GetRelativePath(repoRoot, path)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Assert.NotEqual("bin", segments[1]);
            Assert.NotEqual("obj", segments[1]);
        }
    }

    // Finding T81 of the codebase review of 2026-10-29 (TASK-0107): a folder bin/ below the
    // root of a project is a folder that MSBuild compiles, so the scanner reads it.
    [Fact]
    public void Source_Enumeration_Reads_A_Bin_Folder_Below_The_Root_Of_A_Project()
    {
        var root = Directory.CreateTempSubdirectory("diagnostics-").FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Engine.Probe", "Sketch", "bin"));
            Directory.CreateDirectory(Path.Combine(root, "Engine.Probe", "bin"));
            File.WriteAllText(Path.Combine(root, "Engine.Probe", "Engine.Probe.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(root, "Engine.Probe", "Sketch", "bin", "ProbeHidden.cs"), "class A { }");
            File.WriteAllText(Path.Combine(root, "Engine.Probe", "bin", "Output.cs"), "class B { }");

            var sources = DiagnosticsScanner.EnumerateEngineSources(root).Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'));

            Assert.Equal(["Engine.Probe/Sketch/bin/ProbeHidden.cs"], sources);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Finding T91 of the codebase review of 2026-10-30 (TASK-0114): no engine source holds a part
    // of a code, so no code is built at run time.
    [Fact]
    public void No_Engine_Source_Holds_A_Part_Of_A_Code()
    {
        var repoRoot = DiagnosticsScanner.FindRepoRoot(AppContext.BaseDirectory);
        var sources = DiagnosticsScanner.EnumerateEngineSources(repoRoot)
            .Select(sourcePath => (Path: Path.GetRelativePath(repoRoot, sourcePath).Replace('\\', '/'), Text: File.ReadAllText(sourcePath)));

        var parts = DiagnosticsScanner.PartsOfCodes(sources).Select(part => $"{part.Path}:{part.Line} holds '{part.Value}'").ToList();

        Assert.True(parts.Count == 0, "Write each diagnostic code as one constant (finding T91):\n  " + string.Join("\n  ", parts));
    }

    [Theory]
    [InlineData("static class P { static readonly string Tag = \"HIDDEN\"; static string Code() => \"E-SKETCH-\" + Tag; }")]
    [InlineData("static class P { static string Code() => string.Concat(\"W-SKETCH-\", \"JOINED\"); }")]
    [InlineData("static class P { const string Start = \"I-SKETCH\"; }")]
    // The forms of finding T99 (TASK-0123).
    [InlineData("static class P { static readonly string Tag = \"HIDDEN\"; static string Code() => $\"E-SKETCH-{Tag}\"; }")]
    [InlineData("static class P { static readonly string Tag = \"HIDDEN\"; static string Code() => \"E\" + (\"-SKETCH-\" + Tag); }")]
    [InlineData("static class P { static readonly string Tag = \"HIDDEN\"; static string Code() => string.Join(\"-\", \"I\", \"SKETCH\", Tag); }")]
    [InlineData("static class P { static string Code() => \"e-sketch-hidden\".ToUpperInvariant(); }")]
    // The forms of finding T109 (TASK-0132).
    [InlineData("static class P { static readonly string Tag = \"HIDDEN\"; static string Code() => string.Join(\"-\", new[] { \"E\", \"SKETCH\", Tag }); }")]
    [InlineData("static class P { const string Sev = \"E\"; static readonly string Tag = \"HIDDEN\"; static string Code() => string.Join(\"-\", Sev, \"SKETCH\", Tag); }")]
    [InlineData("static class P { static string Code() => string.Concat(\"E\", \"-\", \"SKETCH-X\"); }")]
    [InlineData("static class P { static readonly string Sev = \"x\"; static readonly string Tag = \"HIDDEN\"; static string Code() => $\"{Sev}-SKETCH{\"-\"}{Tag}\"; }")]
    [InlineData("static class P { static readonly string Tag = \"HIDDEN\"; static string Code() => 'E' + \"-x\" + Tag; }")]
    // The form of finding T116 (TASK-0143): a severity in a const char.
    [InlineData("static class P { const char S = 'E'; static readonly string Tag = \"HIDDEN\"; static string Code() => S + \"-x\" + Tag; }")]
    // The forms of finding T120 of the codebase review of 2026-10-31 (c) (TASK-0150): a severity in a
    // const char in an interpolation and in a call.
    [InlineData("static class P { const char S = 'E'; static readonly string Tag = \"HIDDEN\"; static string Code() => $\"{S}-{\"SKETCH\"}-{Tag}\"; }")]
    [InlineData("static class P { const char S = 'W'; static readonly string Tag = \"HIDDEN\"; static string Code() => string.Concat(S, \"-\", \"SKETCH\", \"-\", Tag); }")]
    public void A_Part_Of_A_Code_Is_An_Offence(string source)
    {
        Assert.NotEmpty(DiagnosticsScanner.PartsOfCodes([("Engine.Core/Probe.cs", source)]));
        Assert.Empty(DiagnosticsScanner.PartsOfCodes([("Engine.Core/Probe.cs", "static class P { const string Code = \"E-SKETCH-HIDDEN\"; }")]));
    }

    // Finding T110 of the codebase review of 2026-10-31 (b) (TASK-0143): the scanner parses with no
    // preprocessor symbol, and the build defines NET, so a code inside #if NET was invisible to the
    // gate. A file with disabled text fails each reader of constants.
    [Fact]
    public void Code_That_A_Preprocessor_Condition_Hides_Fails_The_Scanner()
    {
        const string Hidden = "static class P {\n#if NET\n    internal const string Code = \"E-CLI-\" + \"HIDDEN\";\n#endif\n}\n";

        var error = Assert.Throws<InvalidOperationException>(() => DiagnosticsScanner.ConstantCodes([("Engine.Cli/Probe.cs", Hidden)]).ToList());
        Assert.Contains("Engine.Cli/Probe.cs:3", error.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => DiagnosticsScanner.PartsOfCodes([("Engine.Cli/Probe.cs", Hidden)]).ToList());
        Assert.Contains("E-CLI-HIDDEN", DiagnosticsScanner.ConstantCodes([("Engine.Cli/Probe.cs", Hidden.Replace("#if NET\n", string.Empty, StringComparison.Ordinal).Replace("#endif\n", string.Empty, StringComparison.Ordinal))]).Select(use => use.Code));
    }

    // Finding T90 of the codebase review of 2026-10-30 (TASK-0114): two internal types of one name
    // in two projects made the one compilation ambiguous, and a joined code passed. Each file binds
    // in the compilation of its own project.
    [Fact]
    public void A_Type_Of_The_Same_Name_In_Another_Project_Hides_No_Code()
    {
        (string, string)[] files =
        [
            ("Engine.Core/ProbeShared.cs", "namespace Engine.Probe; internal static class Shared { internal const string Part = \"E-SKETCH\"; }"),
            ("Engine.Cli/ProbeShared.cs", "namespace Engine.Probe; internal static class Shared { internal const string Part = \"X\"; }"),
            ("Engine.Core/ProbeCode.cs", "namespace Engine.Probe; internal static class Codes { internal const string Duplicate = Shared.Part + \"-DUP\"; }"),
        ];

        Assert.Contains("E-SKETCH-DUP", DiagnosticsScanner.ConstantCodes(files).Select(use => use.Code));
    }

    // Finding T97 of the codebase review of 2026-10-30 (b) (TASK-0123): the same form through a
    // project that the project of the code references. The files of both projects went into one
    // compilation, and the two types gave error CS0101 there.
    [Fact]
    public void A_Type_Of_The_Same_Name_In_A_Referenced_Project_Hides_No_Code()
    {
        (string, string)[] files =
        [
            ("Engine.Cli/ProbeShared.cs", "namespace Engine.Probe; internal static class Shared { internal const string Part = \"E\"; }"),
            ("Engine.Cli/ProbeCode.cs", "namespace Engine.Probe; internal static class Codes { internal const string Duplicate = Shared.Part + \"-SKETCH-DUPX\"; }"),
            ("Engine.Core/ProbeShared.cs", "namespace Engine.Probe; internal static class Shared { internal const string Part = \"X\"; }"),
        ];

        Assert.Contains("E-SKETCH-DUPX", DiagnosticsScanner.ConstantCodes(files).Select(use => use.Code));
    }

    // Finding T98 (TASK-0123): the references of a project come from each file that MSBuild reads
    // for it, not from its project file only. A reference in a Directory.Build.props file is one.
    [Fact]
    public void A_Reference_In_Another_File_Of_MsBuild_Is_A_Reference()
    {
        var props = Path.Combine(Path.GetTempPath(), "t98-" + Guid.NewGuid().ToString("N") + ".props");
        File.WriteAllText(props, "<Project><ItemGroup><ProjectReference Include=\"../Engine.Core/Engine.Core.csproj\" /></ItemGroup></Project>");
        try
        {
            Assert.Contains("Engine.Core", Engine.Tests.Governance.ConstantStrings.References("Engine.Cli", file => file.EndsWith("Engine.Cli.csproj", StringComparison.Ordinal) ? [props] : []));
            Assert.Empty(Engine.Tests.Governance.ConstantStrings.References("Engine.Cli", _ => []));
        }
        finally
        {
            File.Delete(props);
        }
    }

    // Finding T104 of the codebase review of 2026-10-31 (TASK-0132): a name that only an implicit using
    // of the SDK binds gave no constant, so the joined code passed.
    [Fact]
    public void A_Name_Of_An_Implicit_Using_Hides_No_Code()
    {
        (string, string)[] files = [("Engine.Core/ProbeZ1.cs", "static class Z { const string Code = \"E\" + (\"-SKETCH\" + (\"-\" + nameof(GC))); }")];

        Assert.Contains("E-SKETCH-GC", DiagnosticsScanner.ConstantCodes(files).Select(use => use.Code));
    }

    // Finding T105 (TASK-0132): each compilation granted its internals to each project, so an internal
    // and a public type of one name were ambiguous, and the build, which grants Engine.Core to
    // Engine.Tests only, takes the public one.
    [Fact]
    public void A_Grant_That_The_Build_Does_Not_Give_Hides_No_Code()
    {
        (string, string)[] files =
        [
            ("Engine.Core/ProbeZA.cs", "namespace Engine.Core.ProbeZA; internal static class Tag { internal const string Part = \"X\", Mid = \"-Y\", End = \"-Z\"; }"),
            ("Engine.Core/ProbeZB.cs", "namespace Engine.Core.ProbeZB; public static class Tag { public const string Part = \"E\", Mid = \"-SKETCH\", End = \"-AMBIG\"; }"),
            ("Engine.Cli/ProbeZC.cs", "using Engine.Core.ProbeZA; using Engine.Core.ProbeZB; static class C { const string Code = Tag.Part + Tag.Mid + Tag.End; }"),
        ];

        Assert.Contains("E-SKETCH-AMBIG", DiagnosticsScanner.ConstantCodes(files).Select(use => use.Code));
    }

    // Finding T81: the compiler joins a code from two strings and from a constant of another
    // file, and the scanner reads the value.
    [Fact]
    public void Scanner_Reads_A_Code_That_The_Compiler_Joins()
    {
        (string, string)[] files =
        [
            ("Engine.Core/Probe.cs", "static class P { const string A = \"E-SKETCH-\" + \"HIDDEN\"; const string B = Q.Part + \"-ALSO\"; }"),
            ("Engine.Core/Other.cs", "static class Q { internal const string Part = \"W-SKETCH\"; }"),
        ];

        var codes = DiagnosticsScanner.ConstantCodes(files).Select(use => use.Code).ToHashSet();

        Assert.Superset(new HashSet<string> { "E-SKETCH-HIDDEN", "W-SKETCH-ALSO" }, codes);
    }
}
