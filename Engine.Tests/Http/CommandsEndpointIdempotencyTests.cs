using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

public class CommandsEndpointIdempotencyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CommandsEndpointIdempotencyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCommands_With_Same_CommandId_Returns_Cached_Result()
    {
        var client = _factory.WithWebHostBuilder(_ => { }).CreateClient();
        var commandId = Guid.Parse("DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD");

        var first = await client.PostAsJsonAsync("/commands", new
        {
            name = "NoOp",
            schemaVersion = 1,
            parameters = new { echo = "first" },
            commandId,
        });
        var firstBody = await ReadJsonAsync(first);

        var second = await client.PostAsJsonAsync("/commands", new
        {
            name = "NoOp",
            schemaVersion = 1,
            parameters = new { echo = "second-should-be-ignored" },
            commandId,
        });
        var secondBody = await ReadJsonAsync(second);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        // Same CommandId -> same cached CommandResult. Second body's "echo"
        // parameter is ignored because the bus already answered.
        Assert.Equal(
            firstBody.GetProperty("appliedAtSeq").GetInt64(),
            secondBody.GetProperty("appliedAtSeq").GetInt64());
        Assert.Equal(
            firstBody.GetProperty("documentVersion").GetInt64(),
            secondBody.GetProperty("documentVersion").GetInt64());
        Assert.Equal(
            firstBody.GetProperty("outputs").GetProperty("echo").GetString(),
            secondBody.GetProperty("outputs").GetProperty("echo").GetString());
        Assert.Equal("first",
            secondBody.GetProperty("outputs").GetProperty("echo").GetString());
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(text).RootElement.Clone();
    }
}
