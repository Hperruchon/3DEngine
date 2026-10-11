using System.Text.Json;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;

namespace Engine.Tests.Cli;

// The query GetTessellation through the command line (ADR-0018 item 7,
// TASK-0028). Each invocation of the command line builds a new engine with an
// empty Document (ADR-0011 §5), so a query there never finds a body. These tests
// hold the two parts that the command line can show: the query reaches its
// handler, and the renderer writes the fields of a record result that the
// command line holds as an object.
public class CliTessellationTests
{
    [Fact]
    public void GetTessellation_Reaches_Its_Handler_And_Gives_E_GEOM_BODY_NOT_FOUND()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(
            ["query", "GetTessellation", "--param", $"bodyId={Guid.NewGuid()}"], stdout, stderr);

        Assert.Equal(1, exit);
        using var doc = JsonDocument.Parse(stdout.ToString());
        Assert.Equal(
            DiagnosticCodes.GeomBodyNotFound,
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public void The_Renderer_Writes_The_Fields_Of_A_Tessellation_Held_As_An_Object()
    {
        var result = new QueryResult<object>(
            QueryName: "GetTessellation",
            AsOfDocumentVersion: 1,
            Result: new Tessellation([0, 0, 0, 1, 0, 0, 0, 1, 0], [0, 1, 2]),
            Diagnostics: Array.Empty<Diagnostic>(),
            Error: null,
            DurationMs: 0);
        var stdout = new StringWriter();

        global::Engine.Cli.JsonRenderer.WriteQueryResult(result, stdout);

        using var doc = JsonDocument.Parse(stdout.ToString());
        var written = doc.RootElement.GetProperty("result");
        Assert.Equal(1, written.GetProperty("triangleCount").GetInt32());
        Assert.Equal(3, written.GetProperty("vertexCount").GetInt32());
        Assert.Equal(9, written.GetProperty("positions").GetArrayLength());
        Assert.Equal(3, written.GetProperty("indices").GetArrayLength());
    }
}
