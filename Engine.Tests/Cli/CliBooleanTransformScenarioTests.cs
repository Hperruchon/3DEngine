using System.Text.Json;

namespace Engine.Tests.Cli;

// CLI is embedded/one-shot (ADR-0011 §5): each invocation has a fresh Document
// with no bodies. A Translate/Subtract that references a body therefore always
// resolves to a structured E-GEOM-BODY-NOT-FOUND — and because operand existence
// is checked before backend capability, that result is the same whether the
// native backend or the managed stub is active, so these tests are platform
// stable. The real create -> translate -> subtract -> query chain lives in the
// HTTP host (HttpBooleanTransformScenarioTests).
public class CliBooleanTransformScenarioTests
{
    private const string SomeGuid = "11111111-1111-1111-1111-111111111111";

    [Fact]
    public void Apply_Translate_On_Fresh_Engine_Returns_BodyNotFound()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "Translate",
                "--param", $"bodyId={SomeGuid}",
                "--param", "dx=5", "--param", "dy=0", "--param", "dz=0" },
            stdout, stderr);

        Assert.Equal(1, exit);
        Assert.Equal(string.Empty, stderr.ToString());
        using var doc = JsonDocument.Parse(stdout.ToString());
        Assert.Equal("Rejected", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "E-GEOM-BODY-NOT-FOUND",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public void Apply_Subtract_On_Fresh_Engine_Returns_BodyNotFound()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "Subtract",
                "--param", $"minuendBodyId={SomeGuid}",
                "--param", $"subtrahendBodyId={SomeGuid}" },
            stdout, stderr);

        Assert.Equal(1, exit);
        Assert.Equal(string.Empty, stderr.ToString());
        using var doc = JsonDocument.Parse(stdout.ToString());
        Assert.Equal("Rejected", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "E-GEOM-BODY-NOT-FOUND",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public void Apply_Translate_Missing_Param_Returns_Exit2()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "Translate",
                "--param", $"bodyId={SomeGuid}",
                "--param", "dx=5", "--param", "dy=0" }, // dz missing
            stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Contains("dz", stderr.ToString());
    }

    [Fact]
    public void Apply_Subtract_Missing_Param_Returns_Exit2()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "Subtract", "--param", $"minuendBodyId={SomeGuid}" }, // subtrahend missing
            stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Contains("subtrahendBodyId", stderr.ToString());
    }

    [Fact]
    public void Apply_Translate_Invalid_Guid_Returns_Exit2()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "Translate",
                "--param", "bodyId=not-a-guid",
                "--param", "dx=1", "--param", "dy=1", "--param", "dz=1" },
            stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Contains("not a valid GUID", stderr.ToString());
    }
}
