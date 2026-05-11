using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

public class QueriesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public QueriesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostQueries_Anything_Returns_200_And_Json_QueryResult_E_QRY_UNKNOWN()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/queries", new
        {
            name = "Anything",
            schemaVersion = 1,
            parameters = new { },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        var body = JsonDocument.Parse(text).RootElement;

        Assert.Equal("Anything", body.GetProperty("queryName").GetString());
        Assert.Equal("E-QRY-UNKNOWN", body.GetProperty("error").GetProperty("code").GetString());
    }
}
