using System.Net.Http.Json;
using System.Text.Json;
using Engine.Tests.Geometry; // NativeManifoldFact
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

// Row K16 (TASK-0433, ADR-0181 item 8): an agent reaches Union and Intersect through the route /commands of HTTP, with no
// new route (ADR-0016), and /schema/commands gives their parameters.
public class HttpUnionIntersectScenarioTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static async Task<JsonElement> Post(HttpClient http, string route, object body)
    {
        var response = await http.PostAsJsonAsync(route, body);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    private static async Task<Guid> Body(HttpClient http, string name, object parameters)
    {
        var json = await Post(http, "/commands", new { name, schemaVersion = 1, parameters });
        Assert.True(json.GetProperty("status").GetString() == "Applied", json.ToString());
        return json.GetProperty("outputs").GetProperty("bodyId").GetGuid();
    }

    private static async Task<Guid> Cube(HttpClient http, double dx)
    {
        var box = await Body(http, "CreateBox", new { sizeX = 10.0, sizeY = 10.0, sizeZ = 10.0 });
        return await Body(http, "Translate", new { bodyId = box.ToString(), dx, dy = 0.0, dz = 0.0 });
    }

    private static async Task<(double Volume, double MinX, double MaxX)> Measure(HttpClient http, Guid body)
    {
        var mass = (await Post(http, "/queries", new { name = "GetMassProperties", schemaVersion = 1, parameters = new { bodyId = body.ToString() } })).GetProperty("result");
        var box = (await Post(http, "/queries", new { name = "GetBoundingBox", schemaVersion = 1, parameters = new { bodyId = body.ToString() } })).GetProperty("result");
        return (mass.GetProperty("volume").GetDouble(), box.GetProperty("minX").GetDouble(), box.GetProperty("maxX").GetDouble());
    }

    [NativeManifoldFact]
    public async Task An_Agent_Unites_And_Intersects_Two_Bodies_Through_The_Route_Of_The_Commands()
    {
        using var host = factory.WithWebHostBuilder(_ => { });
        var http = host.CreateClient();

        var union = await Body(http, "Union", new { firstBodyId = (await Cube(http, 0)).ToString(), secondBodyId = (await Cube(http, 5)).ToString() });
        var common = await Body(http, "Intersect", new { firstBodyId = (await Cube(http, 0)).ToString(), secondBodyId = (await Cube(http, 5)).ToString() });

        Assert.Equal((1500.0, -5.0, 10.0), await Measure(http, union));
        Assert.Equal((500.0, 0.0, 5.0), await Measure(http, common));

        var refused = await Post(http, "/commands", new { name = "Intersect", schemaVersion = 1, parameters = new { firstBodyId = union.ToString(), secondBodyId = union.ToString() } });
        Assert.Equal("Rejected", refused.GetProperty("status").GetString());
        Assert.Equal("E-GEOM-INVALID-PARAM", refused.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task The_Schema_Gives_The_Two_Bodies_Of_Each_Command()
    {
        var http = factory.CreateClient();
        foreach (var name in new[] { "Union", "Intersect" })
        {
            var schema = JsonDocument.Parse(await http.GetStringAsync($"/schema/commands/{name}@1")).RootElement.ToString();
            Assert.Contains("firstBodyId", schema, StringComparison.Ordinal);
            Assert.Contains("secondBodyId", schema, StringComparison.Ordinal);
            Assert.Contains("bodyId", schema, StringComparison.Ordinal);
        }
    }
}
