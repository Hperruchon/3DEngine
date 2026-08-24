using System.Net.Http.Json;
using System.Text.Json;
using Engine.Tests.Geometry; // NativeManifoldFact
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

// The HTTP host keeps one Document for the process lifetime (ADR-0011 §1), so this
// is where the full create -> translate -> subtract -> query chain runs end to end.
// The successful path is native-only (the managed stub has no transform/boolean
// capability), so it is gated with [NativeManifoldFact].
public class HttpBooleanTransformScenarioTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HttpBooleanTransformScenarioTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static async Task<Guid> ApplyCreatingBody(HttpClient http, object command)
    {
        var response = await http.PostAsJsonAsync("/commands", command);
        response.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Applied", json.GetProperty("status").GetString());
        return json.GetProperty("outputs").GetProperty("bodyId").GetGuid();
    }

    [NativeManifoldFact]
    public async Task CreateBox_Translate_Subtract_Then_Query_Shows_Trimmed_Aabb()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        var http = factory.CreateClient();

        // Two identical origin-centered 10-cubes (each -5..5 on every axis).
        var a = await ApplyCreatingBody(http, new
        {
            name = "CreateBox", schemaVersion = 1,
            parameters = new { sizeX = 10.0, sizeY = 10.0, sizeZ = 10.0 },
        });
        var b = await ApplyCreatingBody(http, new
        {
            name = "CreateBox", schemaVersion = 1,
            parameters = new { sizeX = 10.0, sizeY = 10.0, sizeZ = 10.0 },
        });

        // Move B to +x so it overlaps only the +x half of A (B' spans 0..10 on x).
        var bMoved = await ApplyCreatingBody(http, new
        {
            name = "Translate", schemaVersion = 1,
            parameters = new { bodyId = b.ToString(), dx = 5.0, dy = 0.0, dz = 0.0 },
        });

        // A - B' removes A's +x half, leaving x in [-5, 0].
        var diff = await ApplyCreatingBody(http, new
        {
            name = "Subtract", schemaVersion = 1,
            parameters = new { minuendBodyId = a.ToString(), subtrahendBodyId = bMoved.ToString() },
        });

        var queryResponse = await http.PostAsJsonAsync("/queries", new
        {
            name = "GetBoundingBox", schemaVersion = 1,
            parameters = new { bodyId = diff.ToString() },
        });
        queryResponse.EnsureSuccessStatusCode();
        var queryBody = JsonDocument.Parse(await queryResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(JsonValueKind.Null, queryBody.GetProperty("error").ValueKind);
        var aabb = queryBody.GetProperty("result");

        Assert.Equal(-5.0, aabb.GetProperty("minX").GetDouble(), 6);
        Assert.Equal(0.0,  aabb.GetProperty("maxX").GetDouble(), 6); // trimmed from 5 to 0
        Assert.Equal(-5.0, aabb.GetProperty("minY").GetDouble(), 6);
        Assert.Equal(5.0,  aabb.GetProperty("maxY").GetDouble(), 6);
        Assert.Equal(-5.0, aabb.GetProperty("minZ").GetDouble(), 6);
        Assert.Equal(5.0,  aabb.GetProperty("maxZ").GetDouble(), 6);
    }

    [Fact]
    public async Task Subtract_With_Unknown_Bodies_Returns_E_GEOM_BODY_NOT_FOUND()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        var http = factory.CreateClient();

        var response = await http.PostAsJsonAsync("/commands", new
        {
            name = "Subtract", schemaVersion = 1,
            parameters = new
            {
                minuendBodyId = Guid.NewGuid().ToString(),
                subtrahendBodyId = Guid.NewGuid().ToString(),
            },
        });
        response.EnsureSuccessStatusCode();
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Rejected", body.GetProperty("status").GetString());
        Assert.Equal(
            "E-GEOM-BODY-NOT-FOUND",
            body.GetProperty("error").GetProperty("code").GetString());
    }
}
