using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

// Finding E17 of the codebase review of 2026-09-30, part 3 (register entry R-0028, TASK-0481): a refused body of /commands,
// /queries and /proposals names no internal type of the surface, and an unknown charset gets HTTP 415 with the envelope.
// Until TASK-0481 the text of an unknown member named "Engine.Api.Http.Endpoints.CommandsEndpoint+CommandRequest", and an
// unknown charset gave HTTP 500 at /commands and /queries and a false text at /proposals.
public class RequestTextTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RequestTextTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/commands", """{"name":"NoOp","schemaVersion":1,"parameters":{},"wibble":1}""", "$.wibble")]
    [InlineData("/commands", """{"name":"NoOp","schemaVersion":"one","parameters":{}}""", "$.schemaVersion")]
    [InlineData("/queries", """{"name":"GetBodies","schemaVersion":1,"parameters":{},"wibble":1}""", "$.wibble")]
    [InlineData("/proposals", """{"summary":"s","commands":[],"wibble":1}""", "$.wibble")]
    public async Task A_Refused_Body_Names_The_Place_And_No_Type_Of_The_Surface(string route, string body, string place)
    {
        using var http = _factory.CreateClient();

        var (status, code, message) = await Post(http, route, body, "application/json");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal("E-API-BAD-REQUEST", code);
        Assert.StartsWith($"Malformed JSON at {place}", message, StringComparison.Ordinal);
        NamesNoType(message);
    }

    // A text that is not JSON keeps the reason of the reader, with its place.
    [Fact]
    public async Task A_Body_That_Is_Not_Json_Gives_The_Reason_Of_The_Reader()
    {
        using var http = _factory.CreateClient();

        var (status, _, message) = await Post(http, "/queries", """{"name":"GetBodies" "schemaVersion":1}""", "application/json");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.StartsWith("Malformed JSON at $", message, StringComparison.Ordinal);
        Assert.Contains("line 1, byte", message, StringComparison.Ordinal);
        Assert.Contains("is invalid after a value", message, StringComparison.Ordinal);
        Assert.DoesNotContain("LineNumber", message, StringComparison.Ordinal);
        NamesNoType(message);
    }

    // An envelope of a proposal with an unknown member: the refusal of the envelope names no type either.
    [Fact]
    public async Task A_Refused_Envelope_Of_A_Proposal_Names_No_Type_Of_The_Surface()
    {
        using var http = _factory.CreateClient();

        var (status, _, message) = await Post(http, "/proposals",
            """{"summary":"s","commands":[{"name":"NoOp","schemaVersion":1,"parameters":{},"wibble":1}]}""", "application/json");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.StartsWith("commands[0]: Malformed envelope at $.wibble", message, StringComparison.Ordinal);
        NamesNoType(message);
    }

    // Finding E252 of the second codebase review of 2026-11-28 (TASK-0488): utf-7, which .NET refuses with NotSupportedException,
    // and a quoted charset, which the read does not take, get the same answer as an unknown name.
    [Theory]
    [InlineData("/commands", """{"name":"NoOp","schemaVersion":1,"parameters":{}}""", "wibble")]
    [InlineData("/queries", """{"name":"GetBodies","schemaVersion":1,"parameters":{}}""", "wibble")]
    [InlineData("/proposals", """{"summary":"s","commands":[]}""", "wibble")]
    [InlineData("/commands", """{"name":"NoOp","schemaVersion":1,"parameters":{}}""", "utf-7")]
    [InlineData("/queries", """{"name":"GetBodies","schemaVersion":1,"parameters":{}}""", "utf-7")]
    [InlineData("/proposals", """{"summary":"s","commands":[]}""", "utf-7")]
    [InlineData("/commands", """{"name":"NoOp","schemaVersion":1,"parameters":{}}""", "\"utf-8\"")]
    [InlineData("/queries", """{"name":"GetBodies","schemaVersion":1,"parameters":{}}""", "\"utf-8\"")]
    [InlineData("/proposals", """{"summary":"s","commands":[]}""", "\"utf-8\"")]
    public async Task A_Body_Whose_Charset_Is_Not_Known_Gets_415_With_The_Envelope(string route, string body, string charset)
    {
        using var http = _factory.CreateClient();

        var (status, code, message) = await Post(http, route, body, $"application/json; charset={charset}");

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, status);
        Assert.Equal("E-API-BAD-REQUEST", code);
        Assert.Equal($"The charset '{charset}' of the content type is not known. Send UTF-8.", message);
    }

    // A known charset other than UTF-8 still reads, as ReadFromJsonAsync reads it.
    [Fact]
    public async Task A_Body_In_A_Known_Charset_Reads()
    {
        using var http = _factory.CreateClient();
        using var content = new ByteArrayContent(Encoding.Unicode.GetBytes("""{"name":"GetBodies","schemaVersion":1,"parameters":{}}"""));
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-16");

        using var response = await http.PostAsync("/queries", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static void NamesNoType(string message)
    {
        Assert.DoesNotContain("Engine.", message, StringComparison.Ordinal);
        Assert.DoesNotContain("System.", message, StringComparison.Ordinal);
        Assert.DoesNotContain("Request", message, StringComparison.Ordinal);
    }

    private static async Task<(HttpStatusCode Status, string? Code, string Message)> Post(HttpClient http, string route, string body, string type)
    {
        using var content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(type);
        using var response = await http.PostAsync(route, content);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = json.RootElement.GetProperty("error");
        return (response.StatusCode, error.GetProperty("code").GetString(), error.GetProperty("message").GetString() ?? string.Empty);
    }
}
