using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

// HTTP host is server-mode per ADR-0011 §1: one engine, many clients, state
// persists across requests within the process lifetime. This makes
// "apply then query" a real round-trip on the same Document.
public class HttpCreateBoxScenarioTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HttpCreateBoxScenarioTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Apply_CreateBox_Via_Post_Then_Query_GetBoundingBox_Via_Post_Returns_Expected_Aabb()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        var http = factory.CreateClient();

        // 1. POST /commands { CreateBox sizeX=10 sizeY=20 sizeZ=40 }
        var applyResponse = await http.PostAsJsonAsync("/commands", new
        {
            name = "CreateBox",
            schemaVersion = 1,
            parameters = new { sizeX = 10.0, sizeY = 20.0, sizeZ = 40.0 },
        });
        applyResponse.EnsureSuccessStatusCode();
        var applyBody = JsonDocument.Parse(
            await applyResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Applied", applyBody.GetProperty("status").GetString());
        var bodyId = applyBody.GetProperty("outputs").GetProperty("bodyId").GetGuid();

        // 2. POST /queries { GetBoundingBox bodyId=<above> }
        var queryResponse = await http.PostAsJsonAsync("/queries", new
        {
            name = "GetBoundingBox",
            schemaVersion = 1,
            parameters = new { bodyId = bodyId.ToString() },
        });
        queryResponse.EnsureSuccessStatusCode();
        var queryBody = JsonDocument.Parse(
            await queryResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(JsonValueKind.Null, queryBody.GetProperty("error").ValueKind);

        var aabb = queryBody.GetProperty("result");
        Assert.Equal(-5.0,  aabb.GetProperty("minX").GetDouble());
        Assert.Equal(-10.0, aabb.GetProperty("minY").GetDouble());
        Assert.Equal(-20.0, aabb.GetProperty("minZ").GetDouble());
        Assert.Equal(5.0,   aabb.GetProperty("maxX").GetDouble());
        Assert.Equal(10.0,  aabb.GetProperty("maxY").GetDouble());
        Assert.Equal(20.0,  aabb.GetProperty("maxZ").GetDouble());
    }

    [Fact]
    public async Task Query_GetBoundingBox_For_Unknown_Body_Returns_E_GEOM_BODY_NOT_FOUND()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        var http = factory.CreateClient();

        var unknown = Guid.NewGuid();
        var response = await http.PostAsJsonAsync("/queries", new
        {
            name = "GetBoundingBox",
            schemaVersion = 1,
            parameters = new { bodyId = unknown.ToString() },
        });
        response.EnsureSuccessStatusCode();
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(
            "E-GEOM-BODY-NOT-FOUND",
            body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateBox_Negative_Param_Returns_E_GEOM_INVALID_PARAM()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        var http = factory.CreateClient();

        var response = await http.PostAsJsonAsync("/commands", new
        {
            name = "CreateBox",
            schemaVersion = 1,
            parameters = new { sizeX = -1.0, sizeY = 1.0, sizeZ = 1.0 },
        });
        response.EnsureSuccessStatusCode();
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Rejected", body.GetProperty("status").GetString());
        Assert.Equal(
            "E-GEOM-INVALID-PARAM",
            body.GetProperty("error").GetProperty("code").GetString());
    }
}
