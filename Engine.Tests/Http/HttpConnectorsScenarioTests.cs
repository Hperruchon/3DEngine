using System.Net.Http.Json;
using System.Text.Json;
using Engine.Tests.Geometry; // NativeManifoldFact
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

// Row K17 (TASK-0443, ADR-0183 item 1): an agent reads the connectors of a body through the one route /queries of the
// surface, and /schema/queries gives the parameter. The host of the test starts the native backend, so the test needs it.
public class HttpConnectorsScenarioTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static async Task<JsonElement> Post(HttpClient http, string route, object body)
    {
        var response = await http.PostAsJsonAsync(route, body);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    private static async Task<JsonElement> Applied(HttpClient http, object command)
    {
        var result = await Post(http, "/commands", command);
        Assert.True(result.GetProperty("status").GetString() == "Applied", result.ToString());
        return result.GetProperty("outputs");
    }

    // A box of 10 moved up by 5 has its top face at z = 10. The datum "top" on that face is its one named datum, and the
    // side of a cylinder gives its axis and its radius.
    [NativeManifoldFact]
    public async Task An_Agent_Reads_The_Connectors_Of_A_Body_Through_The_Route_Of_The_Queries()
    {
        using var host = factory.WithWebHostBuilder(_ => { });
        var http = host.CreateClient();
        var box = (await Applied(http, new { name = "CreateBox", schemaVersion = 1, parameters = new { sizeX = 10.0, sizeY = 10.0, sizeZ = 10.0 } })).GetProperty("bodyId").GetString();
        var datum = (await Applied(http, new { name = "CreateDatum", schemaVersion = 1, parameters = new { featureId = box, element = "+z", name = "top" } })).GetProperty("datumId").GetString();
        var raised = (await Applied(http, new { name = "Translate", schemaVersion = 1, parameters = new { bodyId = box, dx = 0.0, dy = 0.0, dz = 5.0 } })).GetProperty("bodyId").GetString();

        var answer = await Post(http, "/queries", new { name = "GetConnectors", schemaVersion = 1, parameters = new { bodyId = raised } });
        Assert.Equal(JsonValueKind.Null, answer.GetProperty("error").ValueKind);
        var result = answer.GetProperty("result");
        var named = Assert.Single(result.GetProperty("datums").EnumerateArray().ToList());
        Assert.Equal((datum, "top", "+z"), (named.GetProperty("datumId").GetString(), named.GetProperty("name").GetString(), named.GetProperty("reference").GetProperty("element").GetString()));
        Assert.Equal(1.0, named.GetProperty("frame").GetProperty("normal").GetProperty("z").GetDouble());

        var faces = result.GetProperty("faces").EnumerateArray().ToList();
        Assert.Equal(6, faces.Count);
        var top = Assert.Single(faces, face => face.GetProperty("reference").GetProperty("element").GetString() == "+z");
        Assert.Equal(("Plane", 10.0, 1.0), (top.GetProperty("kind").GetString(), top.GetProperty("point").GetProperty("z").GetDouble(), top.GetProperty("direction").GetProperty("z").GetDouble()));
        Assert.Equal(JsonValueKind.Null, top.GetProperty("radius").ValueKind);

        var cylinder = (await Applied(http, new { name = "CreateCylinder", schemaVersion = 1, parameters = new { radius = 4.0, height = 8.0 } })).GetProperty("bodyId").GetString();
        var round = await Post(http, "/queries", new { name = "GetConnectors", schemaVersion = 1, parameters = new { bodyId = cylinder } });
        var side = Assert.Single(round.GetProperty("result").GetProperty("faces").EnumerateArray().ToList(), face => face.GetProperty("kind").GetString() == "Cylinder");
        Assert.Equal(("side", 4.0, -4.0), (side.GetProperty("reference").GetProperty("element").GetString(), side.GetProperty("radius").GetDouble(), side.GetProperty("point").GetProperty("z").GetDouble()));

        var refused = await Post(http, "/queries", new { name = "GetConnectors", schemaVersion = 1, parameters = new { bodyId = box } });
        Assert.Equal("E-GEOM-BODY-NOT-FOUND", refused.GetProperty("error").GetProperty("code").GetString());

        var schema = JsonDocument.Parse(await http.GetStringAsync("/schema/queries/GetConnectors@1")).RootElement;
        Assert.Equal("guid", schema.GetProperty("parameters").GetProperty("bodyId").GetProperty("type").GetString());
    }
}
