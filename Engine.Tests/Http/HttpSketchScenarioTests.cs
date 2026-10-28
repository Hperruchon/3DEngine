using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Core.Geometry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Engine.Tests.Http;

// The chain of ADR-0033 on one Document over HTTP and the WebSocket (TASK-0100): an agent
// builds a sketch, reads it with GetSketch, watches the sketch events, and a new subscriber
// gets the sketch in its snapshot. The surface needs no change for a new command (ADR-0016).
[Collection("real host")]
public class HttpSketchScenarioTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task An_Agent_Builds_A_Sketch_Over_Http_And_Reads_It_With_Its_Residuals()
    {
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        await using var app = await StartSurface(host);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        using var limit = new CancellationTokenSource(Limit);

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(app.Urls.First().Replace("http://", "ws://") + "/events"), limit.Token);
        await WebSocketTestClient.SendJsonAsync(socket, new { }, limit.Token);
        Assert.Equal("subscription.reset", (await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token)).GetProperty("kind").GetString());

        var sketchId = await Output(http, "CreateSketch", new { datumId = "00000000-0000-0000-0000-000000000001", name = "Profile" }, "sketchId", limit.Token);
        var a = await Output(http, "AddSketchPoint", new { sketchId, x = 0.0, y = 0.0 }, "pointId", limit.Token);
        var b = await Output(http, "AddSketchPoint", new { sketchId, x = 40.0, y = 3.0 }, "pointId", limit.Token);
        var line = await Output(http, "AddSketchLine", new { sketchId, startPointId = a, endPointId = b }, "lineId", limit.Token);
        await Output(http, "AddSketchConstraint", new { sketchId, kind = "horizontal", first = line }, "constraintId", limit.Token);
        await Output(http, "AddSketchConstraint", new { sketchId, kind = "distance", first = line, value = 40.0 }, "constraintId", limit.Token);

        using var query = await http.PostAsJsonAsync("/queries", new
        {
            name = "GetSketch",
            schemaVersion = 1,
            parameters = new { sketchId = sketchId.ToString() },
        }, limit.Token);
        query.EnsureSuccessStatusCode();
        var sketch = JsonDocument.Parse(await query.Content.ReadAsStringAsync(limit.Token)).RootElement.GetProperty("result");
        Assert.Equal("Profile", sketch.GetProperty("name").GetString());
        Assert.Equal(3, sketch.GetProperty("points").GetArrayLength());
        var residuals = sketch.GetProperty("constraints").EnumerateArray().Select(c => c.GetProperty("residual").GetDouble()).ToArray();
        Assert.Equal(3.0, residuals[0]);
        Assert.Equal(Math.Sqrt(1609) - 40, residuals[1]);

        var kinds = new List<string>();
        while (kinds.Count(kind => kind.StartsWith("sketch.", StringComparison.Ordinal)) < 6)
        {
            var kind = (await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token)).GetProperty("kind").GetString()!;
            if (kind != "heartbeat")
                kinds.Add(kind);
        }

        Assert.Equal(["sketch.created", "sketch.changed", "sketch.changed", "sketch.changed", "sketch.changed", "sketch.changed"],
            kinds.Where(kind => kind != "command.applied"));

        // ADR-0033 item 9: a new subscriber gets the sketch in its snapshot.
        using var late = new ClientWebSocket();
        await late.ConnectAsync(new Uri(app.Urls.First().Replace("http://", "ws://") + "/events"), limit.Token);
        await WebSocketTestClient.SendJsonAsync(late, new { }, limit.Token);
        var reset = await WebSocketTestClient.ReceiveJsonAsync(late, limit.Token);
        var entry = Assert.Single(reset.GetProperty("snapshot").GetProperty("sketches").EnumerateArray());
        Assert.Equal(sketchId, entry.GetProperty("sketchId").GetGuid());
        Assert.Equal("Profile", entry.GetProperty("name").GetString());
    }

    private static async Task<Guid> Output(HttpClient http, string name, object parameters, string output, CancellationToken token)
    {
        using var response = await http.PostAsJsonAsync("/commands", new { name, schemaVersion = 1, parameters }, token);
        response.EnsureSuccessStatusCode();
        var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token)).RootElement;
        Assert.True(result.GetProperty("status").GetString() == "Applied", result.ToString());
        return result.GetProperty("outputs").GetProperty(output).GetGuid();
    }

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
}
