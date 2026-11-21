using System.Net.Http.Json;
using System.Text.Json;
using Engine.Tests.Geometry; // NativeManifoldFact
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

// Row K15a (TASK-0432, ADR-0180 item 1): the three measure queries answer through the one route /queries of the surface,
// from the registry, as an agent sends them. The host of the test starts the native backend, so the test needs it.
public class HttpMeasureScenarioTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static async Task<JsonElement> Post(HttpClient http, string path, object body)
    {
        var response = await http.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    [NativeManifoldFact]
    public async Task The_Three_Measure_Queries_Answer_On_The_Route_Of_The_Queries()
    {
        using var host = factory.WithWebHostBuilder(_ => { });
        var http = host.CreateClient();
        var created = await Post(http, "/commands", new { name = "CreateBox", schemaVersion = 1, parameters = new { sizeX = 10.0, sizeY = 20.0, sizeZ = 40.0 } });
        Assert.Equal("Applied", created.GetProperty("status").GetString());
        var box = created.GetProperty("outputs").GetProperty("bodyId").GetGuid().ToString();

        var mass = await Post(http, "/queries", new { name = "GetMassProperties", schemaVersion = 1, parameters = new { bodyId = box } });
        Assert.Equal(JsonValueKind.Null, mass.GetProperty("error").ValueKind);
        Assert.Equal(8000, mass.GetProperty("result").GetProperty("volume").GetDouble());
        Assert.Equal(0, mass.GetProperty("result").GetProperty("centre").GetProperty("z").GetDouble());
        Assert.False(mass.GetProperty("result").GetProperty("facets").GetBoolean());

        var distance = await Post(http, "/queries", new
        {
            name = "GetDistance", schemaVersion = 1,
            parameters = new { bodyIdA = box, featureIdA = box, elementA = "+z", bodyIdB = box, featureIdB = box, elementB = "-z" },
        });
        Assert.Equal(40, distance.GetProperty("result").GetProperty("distance").GetDouble());
        Assert.Equal(20, distance.GetProperty("result").GetProperty("pointA").GetProperty("z").GetDouble());
        Assert.Equal(-20, distance.GetProperty("result").GetProperty("pointB").GetProperty("z").GetDouble());

        var angle = await Post(http, "/queries", new
        {
            name = "GetAngle", schemaVersion = 1,
            parameters = new { bodyIdA = box, featureIdA = box, elementA = "+x", bodyIdB = box, featureIdB = box, elementB = "-y" },
        });
        Assert.Equal(0, angle.GetProperty("result").GetProperty("cosine").GetDouble());
        Assert.Equal(1, angle.GetProperty("result").GetProperty("sine").GetDouble());

        var refused = await Post(http, "/queries", new
        {
            name = "GetAngle", schemaVersion = 1,
            parameters = new { bodyIdA = box, featureIdA = box, elementA = "+w", bodyIdB = box, featureIdB = box, elementB = "-y" },
        });
        Assert.Equal("E-MEASURE-NAME-NOT-FOUND", refused.GetProperty("error").GetProperty("code").GetString());
    }
}
