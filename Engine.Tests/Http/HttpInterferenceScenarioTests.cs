using System.Net.Http.Json;
using System.Text.Json;
using Engine.Tests.Geometry; // NativeManifoldFact
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

// Row K11 (TASK-0436, ADR-0182 item 1): an agent sends the pair check CheckInterference through the one route /queries of the
// surface, with its two lists as text, and /schema/queries gives its parameters. The host of the test starts the native
// backend, so the test needs it.
public class HttpInterferenceScenarioTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static async Task<JsonElement> Post(HttpClient http, string route, object body)
    {
        var response = await http.PostAsJsonAsync(route, body);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    private static async Task<string> Cube(HttpClient http, double dx)
    {
        var box = await Post(http, "/commands", new { name = "CreateBox", schemaVersion = 1, parameters = new { sizeX = 10.0, sizeY = 10.0, sizeZ = 10.0 } });
        var id = box.GetProperty("outputs").GetProperty("bodyId").GetGuid().ToString();
        var moved = await Post(http, "/commands", new { name = "Translate", schemaVersion = 1, parameters = new { bodyId = id, dx, dy = 0.0, dz = 0.0 } });
        Assert.True(moved.GetProperty("status").GetString() == "Applied", moved.ToString());
        return moved.GetProperty("outputs").GetProperty("bodyId").GetGuid().ToString();
    }

    [NativeManifoldFact]
    public async Task An_Agent_Checks_The_Pairs_Of_Bodies_Through_The_Route_Of_The_Queries()
    {
        using var host = factory.WithWebHostBuilder(_ => { });
        var http = host.CreateClient();
        var (a, b, c) = (await Cube(http, 0), await Cube(http, 10), await Cube(http, 15));

        var check = await Post(http, "/queries", new
        {
            name = "CheckInterference", schemaVersion = 1,
            parameters = new { bodyIds = $"{a},{b},{c}", intendedPairs = $"{a}:{b}" },
        });
        Assert.Equal(JsonValueKind.Null, check.GetProperty("error").ValueKind);
        var pairs = check.GetProperty("result").GetProperty("pairs").EnumerateArray().ToList();
        Assert.Equal(3, pairs.Count);

        var touch = pairs[0];
        Assert.Equal((a, b), (touch.GetProperty("bodyIdA").GetString(), touch.GetProperty("bodyIdB").GetString()));
        Assert.Equal(("Touching", true, false, "Guaranteed"), (touch.GetProperty("class").GetString(), touch.GetProperty("intended").GetBoolean(), touch.GetProperty("fault").GetBoolean(), touch.GetProperty("basis").GetString()));
        Assert.Equal(JsonValueKind.Null, touch.GetProperty("box").ValueKind);

        var apart = pairs[1];
        Assert.Equal(("Apart", 5.0, 5.0, 10.0), (apart.GetProperty("class").GetString(), apart.GetProperty("gap").GetDouble(), apart.GetProperty("pointA").GetProperty("x").GetDouble(), apart.GetProperty("pointB").GetProperty("x").GetDouble()));

        var overlap = pairs[2];
        Assert.Equal(("Overlapping", 500.0, true, false), (overlap.GetProperty("class").GetString(), overlap.GetProperty("volume").GetDouble(), overlap.GetProperty("fault").GetBoolean(), overlap.GetProperty("meshOnly").GetBoolean()));
        Assert.Equal((10.0, 15.0), (overlap.GetProperty("box").GetProperty("minX").GetDouble(), overlap.GetProperty("box").GetProperty("maxX").GetDouble()));

        var refused = await Post(http, "/queries", new { name = "CheckInterference", schemaVersion = 1, parameters = new { bodyIds = a } });
        Assert.Equal("E-CHECK-PAIR-INVALID", refused.GetProperty("error").GetProperty("code").GetString());

        var schema = JsonDocument.Parse(await http.GetStringAsync("/schema/queries/CheckInterference@1")).RootElement;
        Assert.Equal("string", schema.GetProperty("parameters").GetProperty("bodyIds").GetProperty("type").GetString());
    }
}
