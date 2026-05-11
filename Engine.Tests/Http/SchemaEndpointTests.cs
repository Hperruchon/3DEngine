using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

public class SchemaEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SchemaEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Schema_Commands_Index_Lists_Registered_Commands()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/schema/commands");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);

        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Single(body.EnumerateArray());
        var noop = body.EnumerateArray().First();
        Assert.Equal("NoOp", noop.GetProperty("name").GetString());
        Assert.Equal(1, noop.GetProperty("schemaVersion").GetInt32());
    }

    [Fact]
    public async Task Schema_Commands_Item_Returns_NoOp_Schema()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/schema/commands/NoOp@1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);

        Assert.Equal("NoOp", body.GetProperty("name").GetString());
        Assert.Equal(1, body.GetProperty("schemaVersion").GetInt32());
        var echo = body.GetProperty("parameters").GetProperty("echo");
        Assert.Equal("string", echo.GetProperty("type").GetString());
        Assert.True(echo.GetProperty("required").GetBoolean());
        Assert.Equal(
            "string",
            body.GetProperty("outputs").GetProperty("echo").GetProperty("type").GetString());
    }

    [Fact]
    public async Task Schema_Commands_Item_Unknown_Returns_404_With_Api_Error_Envelope()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/schema/commands/Unknown@1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(
            "E-API-BAD-REQUEST",
            body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Schema_Queries_Index_Is_Empty_Today()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/schema/queries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Empty(body.EnumerateArray());
    }

    [Fact]
    public async Task Schema_Queries_Item_Unknown_Returns_404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/schema/queries/Anything@1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(
            "E-API-BAD-REQUEST",
            body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Schema_Events_Lists_Documented_Event_Kinds()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/schema/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        var kinds = body.EnumerateArray().Select(e => e.GetProperty("kind").GetString()).ToList();

        // Spot-check the documented kinds from ADR-0005 §7.
        Assert.Contains("command.applied", kinds);
        Assert.Contains("command.rejected", kinds);
        Assert.Contains("command.cancelled", kinds);
        Assert.Contains("heartbeat", kinds);
        Assert.Contains("subscription.resume", kinds);
        Assert.Contains("subscription.reset", kinds);
    }

    [Fact]
    public async Task Schema_Diagnostics_Lists_All_Registered_Codes()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/schema/diagnostics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        var codes = body.EnumerateArray().Select(e => e.GetProperty("code").GetString()!).ToList();

        Assert.Contains("E-CMD-UNKNOWN", codes);
        Assert.Contains("E-CMD-VERSION-STALE", codes);
        Assert.Contains("E-CMD-BUS-BUSY", codes);
        Assert.Contains("E-QRY-UNKNOWN", codes);
        Assert.Contains("E-API-BAD-REQUEST", codes);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(text).RootElement.Clone();
    }
}
