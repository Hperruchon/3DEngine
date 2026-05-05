using System.Text.Json;
using Engine.Core;

namespace Engine.Tests.Cli;

public class CliQueryTests
{
    [Fact]
    public void Query_Anything_Returns_Exit1_And_Json_Error_E_QRY_UNKNOWN()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            new[] { "query", "GetEntity", "--param", "id=42" }, stdout, stderr);

        Assert.Equal(1, exit);
        Assert.Equal(string.Empty, stderr.ToString());

        using var doc = JsonDocument.Parse(stdout.ToString());
        var root = doc.RootElement;
        Assert.Equal("GetEntity", root.GetProperty("queryName").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("result").ValueKind);
        Assert.Equal(
            DiagnosticCodes.QueryUnknown,
            root.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public void Query_With_No_Name_Returns_Exit2_With_Usage_On_Stderr()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(new[] { "query" }, stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("query requires a query name", stderr.ToString());
    }
}
