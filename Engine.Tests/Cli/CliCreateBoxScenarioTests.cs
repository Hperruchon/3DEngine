using System.Globalization;
using System.Text.Json;

namespace Engine.Tests.Cli;

// CLI is embedded mode per ADR-0011 §5: each invocation builds a fresh
// engine. A CreateBox in one invocation does NOT persist to the next —
// we can verify the round-trip within a single Apply invocation (Output
// returns the bodyId), but cross-invocation chaining requires the HTTP
// host (TASK-0011 §6 HttpCreateBoxScenarioTests).
public class CliCreateBoxScenarioTests
{
    [Fact]
    public void Apply_CreateBox_Returns_Applied_With_BodyId_Output()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "CreateBox",
                "--param", "sizeX=10",
                "--param", "sizeY=20",
                "--param", "sizeZ=30" },
            stdout, stderr);

        Assert.Equal(0, exit);
        Assert.Equal(string.Empty, stderr.ToString());

        using var doc = JsonDocument.Parse(stdout.ToString());
        var root = doc.RootElement;
        Assert.Equal("Applied", root.GetProperty("status").GetString());
        Assert.Equal("CreateBox", root.GetProperty("commandName").GetString());

        var bodyId = root.GetProperty("outputs").GetProperty("bodyId").GetString();
        Assert.False(string.IsNullOrEmpty(bodyId));
        Assert.True(Guid.TryParse(bodyId, out _));
    }

    [Fact]
    public void Apply_CreateBox_Missing_Size_Returns_Exit2_With_Usage()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "CreateBox", "--param", "sizeX=1", "--param", "sizeY=1" },
            stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Contains("sizeZ", stderr.ToString());
    }

    [Fact]
    public void Apply_CreateBox_NonNumeric_Param_Returns_Exit2()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "CreateBox",
                "--param", "sizeX=abc",
                "--param", "sizeY=1",
                "--param", "sizeZ=1" },
            stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Contains("not a valid number", stderr.ToString());
    }

    [Fact]
    public void Apply_CreateBox_Zero_Size_Returns_Exit1_With_E_GEOM_INVALID_PARAM()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "CreateBox",
                "--param", "sizeX=0",
                "--param", "sizeY=1",
                "--param", "sizeZ=1" },
            stdout, stderr);

        Assert.Equal(1, exit);
        using var doc = JsonDocument.Parse(stdout.ToString());
        Assert.Equal("Rejected", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "E-GEOM-INVALID-PARAM",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public void Query_GetBoundingBox_Without_Preceding_CreateBox_Returns_BodyNotFound()
    {
        // Embedded CLI: each invocation has a fresh Document with no bodies,
        // so any GetBoundingBox returns E-GEOM-BODY-NOT-FOUND.
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "query", "GetBoundingBox",
                "--param", "bodyId=00000000-0000-0000-0000-000000000001" },
            stdout, stderr);

        Assert.Equal(1, exit);
        using var doc = JsonDocument.Parse(stdout.ToString());
        Assert.Equal(
            "E-GEOM-BODY-NOT-FOUND",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }
}
