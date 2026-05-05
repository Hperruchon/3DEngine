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

        var found = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var sourcePath in DiagnosticsScanner.EnumerateEngineSources(repoRoot))
        {
            var text = File.ReadAllText(sourcePath);
            foreach (var code in DiagnosticsScanner.ExtractCodes(text))
            {
                if (!found.TryGetValue(code, out var refs))
                {
                    refs = new SortedSet<string>(StringComparer.Ordinal);
                    found[code] = refs;
                }
                refs.Add(Path.GetRelativePath(repoRoot, sourcePath).Replace('\\', '/'));
            }
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
            Assert.DoesNotContain("bin", segments);
            Assert.DoesNotContain("obj", segments);
        }
    }
}
