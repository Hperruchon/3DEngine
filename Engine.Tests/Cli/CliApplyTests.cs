using System.Text.Json;
using Engine.Cli;
using Engine.Core;

namespace Engine.Tests.Cli;

public class CliApplyTests
{
    [Fact]
    public void Apply_NoOp_With_Echo_Returns_Exit0_And_Json_Status_Applied_With_Echo_Output()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "NoOp", "--param", "echo=hello" }, stdout, stderr);

        Assert.Equal(0, exit);
        Assert.Equal(string.Empty, stderr.ToString());

        using var doc = JsonDocument.Parse(stdout.ToString());
        var root = doc.RootElement;
        Assert.Equal("Applied", root.GetProperty("status").GetString());
        Assert.Equal("NoOp", root.GetProperty("commandName").GetString());
        Assert.Equal("hello", root.GetProperty("outputs").GetProperty("echo").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("error").ValueKind);
    }

    [Fact]
    public void Apply_Unknown_Command_Returns_Exit1_And_Json_Error_E_CMD_UNKNOWN()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "Wibble" }, stdout, stderr);

        Assert.Equal(1, exit);
        Assert.Equal(string.Empty, stderr.ToString());

        using var doc = JsonDocument.Parse(stdout.ToString());
        var root = doc.RootElement;
        Assert.Equal("Rejected", root.GetProperty("status").GetString());
        Assert.Equal("Wibble", root.GetProperty("commandName").GetString());
        Assert.Equal(
            DiagnosticCodes.CommandUnknown,
            root.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public void Apply_NoOp_Missing_Echo_Returns_Exit2_With_Usage_On_Stderr()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "NoOp" }, stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("NoOp requires --param echo", stderr.ToString());
        Assert.Contains("Usage:", stderr.ToString());
    }

    [Fact]
    public void Apply_NoOp_Malformed_Param_Returns_Exit2_With_Usage_On_Stderr()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "NoOp", "--param", "echo" }, stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("Expected key=value", stderr.ToString());
    }

    [Fact]
    public void Apply_NoOp_Duplicate_Param_Returns_Exit2_With_Usage_On_Stderr()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "apply", "NoOp", "--param", "echo=a", "--param", "echo=b" },
            stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Contains("Duplicate --param key", stderr.ToString());
    }
}
