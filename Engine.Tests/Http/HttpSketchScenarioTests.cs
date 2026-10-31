using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Core.Geometry;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;
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
        var host = new EngineHost(new InProcessMeshBackend());
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
        // ADR-0035: the solver makes the line horizontal and 40 long, and two degrees stay free.
        var residuals = sketch.GetProperty("constraints").EnumerateArray().Select(c => c.GetProperty("residual").GetDouble()).ToArray();
        Assert.All(residuals, residual => Assert.Equal(0, residual, 9));
        Assert.True(sketch.GetProperty("solved").GetBoolean());
        Assert.Equal(2, sketch.GetProperty("freedom").GetInt32());

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

    // ADR-0037 (TASK-0108): an agent edits the sketch of an extrude over HTTP and sees the
    // rebuild: the new box, a body.changed with a new geometry key, the warning of a broken
    // profile in the result, and the fault in the snapshot of a new subscriber.
    [NativeManifoldFact]
    public async Task An_Agent_Sees_The_Rebuild_Of_An_Extrude_Over_Http()
    {
        var host = new EngineHost(new ManifoldGeometryBackend());
        await using var app = await StartSurface(host);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        using var limit = new CancellationTokenSource(Limit);

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(app.Urls.First().Replace("http://", "ws://") + "/events"), limit.Token);
        await WebSocketTestClient.SendJsonAsync(socket, new { }, limit.Token);
        Assert.Equal("subscription.reset", (await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token)).GetProperty("kind").GetString());

        var sketchId = await Output(http, "CreateSketch", new { datumId = "00000000-0000-0000-0000-000000000001" }, "sketchId", limit.Token);
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(await Output(http, "AddSketchPoint", new { sketchId, x, y }, "pointId", limit.Token));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(await Output(http, "AddSketchLine", new { sketchId, startPointId = points[i], endPointId = points[(i + 1) % points.Count] }, "lineId", limit.Token));
        var bodyId = await Output(http, "Extrude", new { sketchId, distance = 5.0 }, "bodyId", limit.Token);
        await Output(http, "MoveSketchPoint", new { sketchId, pointId = points[2], x = 14.0, y = 9.0 }, "pointId", limit.Token);

        using (var query = await http.PostAsJsonAsync("/queries", new { name = "GetBoundingBox", schemaVersion = 1, parameters = new { bodyId } }, limit.Token))
        {
            var box = JsonDocument.Parse(await query.Content.ReadAsStringAsync(limit.Token)).RootElement.GetProperty("result");
            Assert.Equal((14.0, 9.0, 5.0), (box.GetProperty("maxX").GetDouble(), box.GetProperty("maxY").GetDouble(), box.GetProperty("maxZ").GetDouble()));
        }

        using (var delete = await http.PostAsJsonAsync("/commands", new { name = "DeleteSketchItem", schemaVersion = 1, parameters = new { sketchId, itemId = lines[3] } }, limit.Token))
        {
            var result = JsonDocument.Parse(await delete.Content.ReadAsStringAsync(limit.Token)).RootElement;
            Assert.Equal("Applied", result.GetProperty("status").GetString());
            Assert.Equal("W-SKETCH-REBUILD-FAILED", Assert.Single(result.GetProperty("diagnostics").EnumerateArray()).GetProperty("code").GetString());
        }

        // body.created gives the first key, the move gives a second one, and the delete keeps
        // the second key with a fault.
        var payloads = new List<JsonElement>();
        while (payloads.Count < 3)
        {
            var message = await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token);
            if (message.GetProperty("kind").GetString() is "body.created" or "body.changed")
                payloads.Add(message.GetProperty("payload"));
        }

        var keys = payloads.Select(payload => payload.GetProperty("geometry").GetGuid()).ToList();
        Assert.NotEqual(keys[0], keys[1]);
        Assert.Equal(keys[1], keys[2]);
        Assert.Equal(JsonValueKind.Null, payloads[1].GetProperty("fault").ValueKind);
        Assert.Equal("W-SKETCH-REBUILD-FAILED", payloads[2].GetProperty("fault").GetProperty("code").GetString());

        using var late = new ClientWebSocket();
        await late.ConnectAsync(new Uri(app.Urls.First().Replace("http://", "ws://") + "/events"), limit.Token);
        await WebSocketTestClient.SendJsonAsync(late, new { }, limit.Token);
        var body = Assert.Single((await WebSocketTestClient.ReceiveJsonAsync(late, limit.Token)).GetProperty("snapshot").GetProperty("bodies").EnumerateArray());
        Assert.Equal(keys[2], body.GetProperty("geometry").GetGuid());
        Assert.Equal("W-SKETCH-REBUILD-FAILED", body.GetProperty("fault").GetProperty("code").GetString());
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
