using System.Net;
using System.Text;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Core;
using Engine.Core.Geometry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Engine.Tests.Http;

// Findings E72 and E73 of the codebase review of 2026-10-28 (TASK-0090): a field of type
// string takes only a JSON string, and a string that is not well-formed UTF-16 is a
// refusal and not HTTP 500.
public class StringFieldTests
{
    private static async Task<WebApplication> StartSurface(EngineHost host)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddEngineSurface(_ => host);
        var app = builder.Build();
        app.MapEngineSurface();
        await app.StartAsync();
        return app;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> Rename(HttpClient http, Guid bodyId, string name)
    {
        var json = $"{{\"name\":\"RenameBody\",\"schemaVersion\":1,\"parameters\":{{\"bodyId\":\"{bodyId}\",\"name\":{name}}}}}";
        using var response = await http.PostAsync("/commands", new StringContent(json, Encoding.UTF8, "application/json"));
        return (response.StatusCode, JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone());
    }

    [Theory]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("{\"a\":1}")]
    [InlineData("[1,2]")]
    [InlineData("\"\\ud800\"")]
    public async Task A_Name_That_Is_Not_A_Well_Formed_Json_String_Is_Refused(string name)
    {
        var host = new EngineHost(new InProcessMeshBackend());
        await using var app = await StartSurface(host);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        var box = await http.PostAsync("/commands", new StringContent(
            "{\"name\":\"CreateBox\",\"schemaVersion\":1,\"parameters\":{\"sizeX\":1,\"sizeY\":1,\"sizeZ\":1}}", Encoding.UTF8, "application/json"));
        var bodyId = JsonDocument.Parse(await box.Content.ReadAsStringAsync()).RootElement.GetProperty("outputs").GetProperty("bodyId").GetGuid();

        var (status, body) = await Rename(http, bodyId, name);
        var (accepted, applied) = await Rename(http, bodyId, "\"Plate\"");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(DiagnosticCodes.ApiBadRequest, body.GetProperty("error").GetProperty("code").GetString());
        Assert.Contains("'name'", body.GetProperty("error").GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, accepted);
        Assert.Equal("Applied", applied.GetProperty("status").GetString());
        Assert.Equal(2, await host.Session.Read((document, _) => document.Version));
    }
}
