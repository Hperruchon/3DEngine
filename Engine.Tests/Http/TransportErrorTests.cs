using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

public class TransportErrorTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TransportErrorTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCommands_With_Malformed_Json_Returns_400_With_E_API_BAD_REQUEST()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("not-json", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/commands", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("E-API-BAD-REQUEST", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostCommands_With_Missing_Name_Returns_400_With_E_API_BAD_REQUEST()
    {
        var client = _factory.CreateClient();
        var content = new StringContent(
            "{\"schemaVersion\":1,\"parameters\":{}}",
            Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/commands", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("E-API-BAD-REQUEST", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostCommands_With_Wrong_Content_Type_Returns_415()
    {
        var client = _factory.CreateClient();
        var content = new StringContent(
            "{\"name\":\"NoOp\",\"schemaVersion\":1,\"parameters\":{\"echo\":\"hi\"}}",
            Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        var response = await client.PostAsync("/commands", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task GetCommands_Returns_405()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/commands");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_Route_Returns_404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/nope");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(text).RootElement.Clone();
    }
}
