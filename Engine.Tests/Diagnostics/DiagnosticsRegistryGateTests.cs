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
