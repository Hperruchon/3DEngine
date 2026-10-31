using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Core.Assets;
using Engine.Core.Geometry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Tests.Http;

// The chain of ADR-0044 and ADR-0045 over HTTP and the WebSocket (TASK-0152): an agent puts the
// bytes of an image, places the image on a datum with the name that the store gives, calibrates
// it, watches the image events, and a new subscriber gets the image in its snapshot. The surface
// needs no change for a new command (ADR-0016).
[Collection("real host")]
public sealed class HttpImageScenarioTests : IDisposable
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);
    private readonly string _store = Path.Combine(Path.GetTempPath(), "3de-image-http-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_store))
            Directory.Delete(_store, recursive: true);
    }

    [Fact]
    public async Task An_Agent_Places_And_Calibrates_An_Image_Of_The_Store_Over_Http()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(new AssetStore(_store));
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

        using var put = await http.PutAsync("/assets", new ByteArrayContent("a picture"u8.ToArray()), limit.Token);
        var asset = JsonDocument.Parse(await put.Content.ReadAsStringAsync(limit.Token)).RootElement.GetProperty("asset").GetString()!;
        var datum = "00000000-0000-0000-0000-000000000001";
        var imageId = await Output(http, "PlaceImage", new { asset, datumId = datum, width = 800, height = 600, originU = 10.0 }, "imageId", limit.Token);
        await Output(http, "CalibrateImage", new { imageId, x1 = 0.0, y1 = 0.0, x2 = 800.0, y2 = 0.0, distance = 200.0 }, "imageId", limit.Token);

        var payloads = new List<(string Kind, JsonElement Payload)>();
        while (payloads.Count < 2)
        {
            var message = await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token);
            var kind = message.GetProperty("kind").GetString()!;
            if (kind.StartsWith("image.", StringComparison.Ordinal))
                payloads.Add((kind, message.GetProperty("payload")));
        }

        Assert.Equal(["image.created", "image.changed"], payloads.Select(payload => payload.Kind));
        Assert.Equal(asset, payloads[0].Payload.GetProperty("asset").GetString());
        Assert.Equal(1.0, payloads[0].Payload.GetProperty("scale").GetDouble());
        Assert.Equal(0.25, payloads[1].Payload.GetProperty("scale").GetDouble());

        // ADR-0045 item 10: a new subscriber gets the image in its snapshot.
        using var late = new ClientWebSocket();
        await late.ConnectAsync(new Uri(app.Urls.First().Replace("http://", "ws://") + "/events"), limit.Token);
        await WebSocketTestClient.SendJsonAsync(late, new { }, limit.Token);
        var reset = await WebSocketTestClient.ReceiveJsonAsync(late, limit.Token);
        var entry = Assert.Single(reset.GetProperty("snapshot").GetProperty("images").EnumerateArray());
        Assert.Equal(imageId, entry.GetProperty("imageId").GetGuid());
        Assert.Equal(asset, entry.GetProperty("asset").GetString());
        Assert.Equal(Guid.Parse(datum), entry.GetProperty("datumId").GetGuid());
        Assert.Equal((10.0, 0.0, 800, 600, 0.25), (
            entry.GetProperty("originU").GetDouble(), entry.GetProperty("originV").GetDouble(),
            entry.GetProperty("width").GetInt32(), entry.GetProperty("height").GetInt32(), entry.GetProperty("scale").GetDouble()));
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
