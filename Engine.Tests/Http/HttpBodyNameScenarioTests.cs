using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Core.Geometry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Engine.Tests.Http;

// The chain of ADR-0028 on one Document over HTTP and the WebSocket (TASK-0085): create a
// box, rename it, hide it, read it with GetBodyInfo, and watch body.changed arrive at a
// subscriber of /events. The surface needs no change for a new command (ADR-0016).
[Collection("real host")]
public class HttpBodyNameScenarioTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task A_Rename_And_A_Hide_Over_Http_Reach_The_Query_And_The_Event_Stream()
    {
        var host = new EngineHost(new InProcessMeshBackend());
        await using var app = await StartSurface(host);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        using var limit = new CancellationTokenSource(Limit);

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(app.Urls.First().Replace("http://", "ws://") + "/events"), limit.Token);
        await WebSocketTestClient.SendJsonAsync(socket, new { }, limit.Token);
        Assert.Equal("subscription.reset", (await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token)).GetProperty("kind").GetString());

        var bodyId = (await Command(http, "CreateBox", new { sizeX = 1.0, sizeY = 2.0, sizeZ = 3.0 }, limit.Token))
            .GetProperty("outputs").GetProperty("bodyId").GetGuid();
        Assert.Equal("Applied", (await Command(http, "RenameBody", new { bodyId, name = "Plate" }, limit.Token)).GetProperty("status").GetString());
        Assert.Equal("Applied", (await Command(http, "SetBodyVisibility", new { bodyId, visible = false }, limit.Token)).GetProperty("status").GetString());

        using var query = await http.PostAsJsonAsync("/queries", new
        {
            name = "GetBodyInfo",
            schemaVersion = 1,
            parameters = new { bodyId = bodyId.ToString() },
        }, limit.Token);
        query.EnsureSuccessStatusCode();
        var info = JsonDocument.Parse(await query.Content.ReadAsStringAsync(limit.Token)).RootElement.GetProperty("result");
        Assert.Equal(bodyId, info.GetProperty("bodyId").GetGuid());
        Assert.Equal("Box", info.GetProperty("kind").GetString());
        Assert.Equal("Plate", info.GetProperty("name").GetString());
        Assert.False(info.GetProperty("visible").GetBoolean());

        // The stream gives the events of the three commits in order; the last body.changed
        // gives the whole new state.
        var kinds = new List<string>();
        JsonElement last = default;
        while (kinds.Count(kind => kind == "body.changed") < 2)
        {
            var frame = await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token);
            var kind = frame.GetProperty("kind").GetString()!;
            if (kind == "heartbeat")
                continue;
            kinds.Add(kind);
            if (kind == "body.changed")
                last = frame.GetProperty("payload");
        }

        Assert.Equal(["command.applied", "body.created", "command.applied", "body.changed", "command.applied", "body.changed"], kinds);
        Assert.Equal("Plate", last.GetProperty("name").GetString());
        Assert.False(last.GetProperty("visible").GetBoolean());
    }

    [Fact]
    public async Task The_Schema_Endpoints_List_The_New_Commands_The_Query_And_The_Event_Kind()
    {
        var host = new EngineHost(new InProcessMeshBackend());
        await using var app = await StartSurface(host);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var commands = await http.GetStringAsync("/schema/commands");
        var queries = await http.GetStringAsync("/schema/queries");
        var events = JsonDocument.Parse(await http.GetStringAsync("/schema/events")).RootElement;

        Assert.Contains("\"RenameBody\"", commands, StringComparison.Ordinal);
        Assert.Contains("\"SetBodyVisibility\"", commands, StringComparison.Ordinal);
        Assert.Contains("\"GetBodyInfo\"", queries, StringComparison.Ordinal);
        var changed = events.EnumerateArray().Single(entry => entry.GetProperty("kind").GetString() == "body.changed");
        Assert.Equal(
            ["bodyId", "fault", "geometry", "name", "visible"],
            changed.GetProperty("payload").EnumerateObject().Select(field => field.Name).Order(StringComparer.Ordinal));
    }

    private static async Task<JsonElement> Command(HttpClient http, string name, object parameters, CancellationToken token)
    {
        using var response = await http.PostAsJsonAsync("/commands", new { name, schemaVersion = 1, parameters }, token);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(token)).RootElement;
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
