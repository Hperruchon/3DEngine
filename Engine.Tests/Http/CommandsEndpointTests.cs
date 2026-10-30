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

    // Finding E16 of the codebase review of 2026-09-30 (TASK-0115): a typing error in a member of
    // the envelope removed the version check with no error. Each unknown member gives HTTP 400.
    // The envelope with no unknown member is the control: it gives no HTTP 400.
    [Theory]
    [InlineData("/commands", "{\"name\":\"NoOp\",\"schemaVersion\":1,\"parameters\":{\"echo\":\"a\"}", ",\"expectedDocumentVersoin\":5")]
    [InlineData("/commands", "{\"name\":\"NoOp\",\"schemaVersion\":1,\"parameters\":{\"echo\":\"a\"}", ",\"extra\":1")]
    [InlineData("/queries", "{\"name\":\"GetBodyInfo\",\"schemaVersion\":1,\"parameters\":{\"bodyId\":\"7a7a7a7a-0000-0000-0000-000000000000\"}", ",\"expectedDocumentVersion\":1")]
    public async Task An_Unknown_Member_Of_The_Envelope_Gives_400(string route, string envelope, string unknown)
    {
        var client = _factory.CreateClient();
        using var valid = new StringContent(envelope + "}", System.Text.Encoding.UTF8, "application/json");
        using var wrong = new StringContent(envelope + unknown + "}", System.Text.Encoding.UTF8, "application/json");

        Assert.NotEqual(HttpStatusCode.BadRequest, (await client.PostAsync(route, valid)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(route, wrong)).StatusCode);
    }

    // Finding E11 (TASK-0115): CreateBox with 1e999 was applied, and a query of the body gave
    // HTTP 500. The binder refuses it, so the host answers HTTP 400 and the log holds nothing.
    [Fact]
    public async Task A_Number_That_Is_Not_Finite_Is_Rejected()
    {
        var client = _factory.CreateClient();
        using var content = new StringContent("{\"name\":\"CreateBox\",\"schemaVersion\":1,\"parameters\":{\"sizeX\":1e999,\"sizeY\":1,\"sizeZ\":1}}", System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/commands", content);

        var body = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("E-API-BAD-REQUEST", body.GetProperty("error").GetProperty("code").GetString());
        Assert.Contains("expected a finite number", body.GetProperty("error").GetProperty("message").GetString());
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
