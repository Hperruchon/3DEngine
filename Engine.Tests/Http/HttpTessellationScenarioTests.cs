using System.Net.Http.Json;
using System.Text.Json;
using Engine.Tests.Geometry; // NativeManifoldFact
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

// The query GetTessellation through the HTTP surface (ADR-0018, TASK-0028). Until
// TASK-0028 the surface read each query result as an Aabb, so a result of another
// type did not reach the client (ADR-0018 item 7).
public class HttpTessellationScenarioTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [NativeManifoldFact]
    public async Task A_CreateBox_Then_A_GetTessellation_Give_12_Triangles()
    {
        var http = factory.CreateClient();
        var created = await http.PostAsJsonAsync("/commands", new
        {
            name = "CreateBox", schemaVersion = 1,
            parameters = new { sizeX = 2.0, sizeY = 3.0, sizeZ = 4.0 },
        });
        created.EnsureSuccessStatusCode();
        var bodyId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
            .RootElement.GetProperty("outputs").GetProperty("bodyId").GetGuid();

        var response = await http.PostAsJsonAsync("/queries", new
        {
            name = "GetTessellation", schemaVersion = 1,
            parameters = new { bodyId },
        });
        response.EnsureSuccessStatusCode();
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(JsonValueKind.Null, root.GetProperty("error").ValueKind);
        var result = root.GetProperty("result");
        Assert.Equal(12, result.GetProperty("triangleCount").GetInt32());
        Assert.Equal(8, result.GetProperty("vertexCount").GetInt32());
        Assert.Equal(24, result.GetProperty("positions").GetArrayLength());
        Assert.Equal(36, result.GetProperty("indices").GetArrayLength());
    }

    [Fact]
    public async Task The_Schema_Of_GetTessellation_Gives_The_Four_Result_Fields()
    {
        var http = factory.CreateClient();

        var root = JsonDocument.Parse(await http.GetStringAsync("/schema/queries/GetTessellation@1")).RootElement;

        var result = root.GetProperty("result");
        Assert.Equal("integer", result.GetProperty("vertexCount").GetProperty("type").GetString());
        Assert.Equal("integer", result.GetProperty("triangleCount").GetProperty("type").GetString());
        Assert.Equal("array", result.GetProperty("positions").GetProperty("type").GetString());
        Assert.Equal("array", result.GetProperty("indices").GetProperty("type").GetString());
    }
}
