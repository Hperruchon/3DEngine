using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Core.Geometry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Engine.Tests.Http;

// ADR-0158 item 4 (phase P10.3, TASK-0395): an agent makes a sheet with a view and a dimension of a sketch over HTTP, and reads
// the JSON names of the view and of the dimension in the event sheet.changed and in the read GetSheet. The surface needs no
// change for a new command (ADR-0016).
[Collection("real host")]
public sealed class HttpSheetScenarioTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task An_Agent_Places_A_Dimension_On_A_Sheet_Over_Http()
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

        var sketchId = await Output(http, "CreateSketch", new { datumId = "00000000-0000-0000-0000-000000000001" }, "sketchId", limit.Token);
        var a = await Output(http, "AddSketchPoint", new { sketchId, x = 0.0, y = 0.0 }, "pointId", limit.Token);
        var b = await Output(http, "AddSketchPoint", new { sketchId, x = 8.0, y = 0.0 }, "pointId", limit.Token);
        var lineId = await Output(http, "AddSketchLine", new { sketchId, startPointId = a, endPointId = b }, "lineId", limit.Token);
        var constraintId = await Output(http, "AddSketchConstraint", new { sketchId, kind = "distance", first = lineId, value = 8.0 }, "constraintId", limit.Token);
        var sheetId = await Output(http, "CreateSheet", new { name = "Sheet 1" }, "sheetId", limit.Token);
        var viewId = await Output(http, "AddSheetView", new
        {
            sheetId, rightX = 1.0, rightY = 0.0, rightZ = 0.0, upX = 0.0, upY = 0.0, upZ = 1.0, eyeX = 0.0, eyeY = -1.0, eyeZ = 0.0,
            scaleNumerator = 1, scaleDenominator = 2, x = 100.0, y = 80.0,
        }, "viewId", limit.Token);
        var dimensionId = await Output(http, "PlaceSheetDimension", new { sheetId, viewId, source = constraintId, sourceKind = "constraint", x = 110.5, y = 90.0 }, "dimensionId", limit.Token);

        JsonElement changed = default;
        var count = 0;
        while (count < 2)
        {
            var message = await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token);
            if (message.GetProperty("kind").GetString() == "sheet.changed")
                (changed, count) = (message.GetProperty("payload"), count + 1);
        }

        var view = Assert.Single(changed.GetProperty("views").EnumerateArray());
        Assert.Equal(viewId, view.GetProperty("id").GetGuid());
        Assert.Equal([1.0, 0, 0, 0, 0, 1, 0, -1, 0], view.GetProperty("direction").EnumerateArray().Select(value => value.GetDouble()));
        Assert.Equal((1, 2, 100.0, 80.0), (view.GetProperty("scaleNumerator").GetInt32(), view.GetProperty("scaleDenominator").GetInt32(), view.GetProperty("x").GetDouble(), view.GetProperty("y").GetDouble()));
        var dimension = Assert.Single(changed.GetProperty("dimensions").EnumerateArray());
        Assert.Equal((dimensionId, viewId, constraintId, "constraint", 110.5, 90.0), (
            dimension.GetProperty("id").GetGuid(), dimension.GetProperty("viewId").GetGuid(), dimension.GetProperty("source").GetGuid(),
            dimension.GetProperty("sourceKind").GetString(), dimension.GetProperty("x").GetDouble(), dimension.GetProperty("y").GetDouble()));

        using var query = await http.PostAsJsonAsync("/queries", new { name = "GetSheet", schemaVersion = 1, parameters = new { sheetId } }, limit.Token);
        var body = await query.Content.ReadAsStringAsync(limit.Token);
        Assert.True(query.IsSuccessStatusCode, body);
        var read = Assert.Single(JsonDocument.Parse(body).RootElement.GetProperty("result").GetProperty("dimensions").EnumerateArray());
        Assert.Equal((dimensionId, viewId, constraintId, "constraint", false, "length", 8.0), (
            read.GetProperty("dimensionId").GetGuid(), read.GetProperty("viewId").GetGuid(), read.GetProperty("source").GetGuid(),
            read.GetProperty("sourceKind").GetString(), read.GetProperty("lost").GetBoolean(), read.GetProperty("label").GetString(),
            read.GetProperty("value").GetDouble()));
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
