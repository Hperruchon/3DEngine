using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Core.Geometry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Engine.Tests.Http;

// ADR-0051 over HTTP and the WebSocket (TASK-0166): an agent makes a box, a datum on its top face and
// a sketch on the datum, reads the frame with GetDatum, watches datum.created, and a new subscriber
// gets the datum in its snapshot. The surface needs no change for a new command (ADR-0016).
[Collection("real host")]
public sealed class HttpDatumScenarioTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task An_Agent_Makes_A_Datum_On_A_Face_And_A_Sketch_On_It_Over_Http()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddEngineSurface(_ => new EngineHost(new InProcessMeshBackend()));
        await using var app = builder.Build();
        app.MapEngineSurface();
        await app.StartAsync();
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        using var limit = new CancellationTokenSource(Limit);

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(app.Urls.First().Replace("http://", "ws://") + "/events"), limit.Token);
        await WebSocketTestClient.SendJsonAsync(socket, new { }, limit.Token);
        Assert.Equal("subscription.reset", (await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token)).GetProperty("kind").GetString());

        var box = await Output(http, "CreateBox", new { sizeX = 40.0, sizeY = 30.0, sizeZ = 10.0 }, "bodyId", limit.Token);
        var datumId = await Output(http, "CreateDatum", new { featureId = box, element = "+z", name = "Top" }, "datumId", limit.Token);
        var sketchId = await Output(http, "CreateSketch", new { datumId }, "sketchId", limit.Token);

        using (var query = await http.PostAsJsonAsync("/queries", new { name = "GetDatum", schemaVersion = 1, parameters = new { datumId } }, limit.Token))
        {
            var body = await query.Content.ReadAsStringAsync(limit.Token);
            Assert.True(query.IsSuccessStatusCode, body);
            var result = JsonDocument.Parse(body).RootElement.GetProperty("result");
            Assert.Equal("Top", result.GetProperty("name").GetString());
            Assert.Equal((box, "+z"), (result.GetProperty("reference").GetProperty("featureId").GetGuid(), result.GetProperty("reference").GetProperty("element").GetString()));
            var origin = result.GetProperty("frame").GetProperty("origin");
            Assert.Equal((0.0, 0.0, 5.0), (origin.GetProperty("x").GetDouble(), origin.GetProperty("y").GetDouble(), origin.GetProperty("z").GetDouble()));
            Assert.Equal(JsonValueKind.Null, result.GetProperty("fault").ValueKind);
        }

        JsonElement created;
        while (true)
        {
            var message = await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token);
            if (message.GetProperty("kind").GetString() == "datum.created")
            {
                created = message.GetProperty("payload");
                break;
            }
        }

        Assert.Equal((datumId, "Top"), (created.GetProperty("datumId").GetGuid(), created.GetProperty("name").GetString()));

        // ADR-0051 item 9: a new subscriber gets the datum in its snapshot, and the sketch on it.
        using var late = new ClientWebSocket();
        await late.ConnectAsync(new Uri(app.Urls.First().Replace("http://", "ws://") + "/events"), limit.Token);
        await WebSocketTestClient.SendJsonAsync(late, new { }, limit.Token);
        var snapshot = (await WebSocketTestClient.ReceiveJsonAsync(late, limit.Token)).GetProperty("snapshot");
        var entry = Assert.Single(snapshot.GetProperty("datums").EnumerateArray());
        Assert.Equal((datumId, "Top"), (entry.GetProperty("datumId").GetGuid(), entry.GetProperty("name").GetString()));
        var sketch = Assert.Single(snapshot.GetProperty("sketches").EnumerateArray());
        Assert.Equal((sketchId, datumId), (sketch.GetProperty("sketchId").GetGuid(), sketch.GetProperty("datumId").GetGuid()));
    }

    private static async Task<Guid> Output(HttpClient http, string name, object parameters, string output, CancellationToken token)
    {
        using var response = await http.PostAsJsonAsync("/commands", new { name, schemaVersion = 1, parameters }, token);
        var body = await response.Content.ReadAsStringAsync(token);
        Assert.True(response.IsSuccessStatusCode, body);
        var result = JsonDocument.Parse(body).RootElement;
        Assert.True(result.GetProperty("status").GetString() == "Applied", result.ToString());
        return result.GetProperty("outputs").GetProperty(output).GetGuid();
    }
}
