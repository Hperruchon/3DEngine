using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

public class CommandsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CommandsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCommands_NoOp_With_Echo_Returns_200_And_Json_Status_Applied()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/commands", new
        {
            name = "NoOp",
            schemaVersion = 1,
            parameters = new { echo = "hello" },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);

        Assert.Equal("Applied", body.GetProperty("status").GetString());
        Assert.Equal("hello", body.GetProperty("outputs").GetProperty("echo").GetString());
        Assert.True(body.GetProperty("error").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task PostCommands_Unknown_Command_Returns_200_And_Json_Status_Rejected_E_CMD_UNKNOWN()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/commands", new
        {
            name = "Unknown",
            schemaVersion = 1,
            parameters = new { },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);

        Assert.Equal("Rejected", body.GetProperty("status").GetString());
        Assert.Equal("E-CMD-UNKNOWN", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostCommands_With_ExpectedDocumentVersion_Mismatch_Returns_200_And_Rejected_E_CMD_VERSION_STALE()
    {
        var factory = _factory.WithWebHostBuilder(_ => { });
        var client = factory.CreateClient();

        // First call lands at Version 1.
        var first = await client.PostAsJsonAsync("/commands", new
        {
            name = "NoOp",
            schemaVersion = 1,
            parameters = new { echo = "first" },
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Second call claims it expects Version 0 — stale by one.
        var second = await client.PostAsJsonAsync("/commands", new
        {
            name = "NoOp",
            schemaVersion = 1,
            parameters = new { echo = "second" },
            expectedDocumentVersion = 0,
        });

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var body = await ReadJsonAsync(second);
        Assert.Equal("Rejected", body.GetProperty("status").GetString());
        Assert.Equal("E-CMD-VERSION-STALE", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostCommands_Echoes_Client_Supplied_CommandId()
    {
        var client = _factory.CreateClient();
        var clientCommandId = Guid.Parse("ABABABAB-ABAB-ABAB-ABAB-ABABABABABAB");
        var response = await client.PostAsJsonAsync("/commands", new
        {
            name = "NoOp",
            schemaVersion = 1,
            parameters = new { echo = "trace" },
            commandId = clientCommandId,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(
            clientCommandId,
            Guid.Parse(body.GetProperty("commandId").GetString()!));
    }

    [Fact]
    public async Task PostCommands_Generates_CommandId_When_Body_Omits_It()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/commands", new
        {
            name = "NoOp",
            schemaVersion = 1,
            parameters = new { echo = "auto" },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        var idText = body.GetProperty("commandId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(idText));
        Assert.True(Guid.TryParse(idText, out var parsed));
        Assert.NotEqual(Guid.Empty, parsed);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(text).RootElement.Clone();
    }
}
