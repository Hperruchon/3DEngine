using System.Net.Http.Json;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Engine.Tests.Http;

// Finding E66 of the codebase review of 2026-10-17 (TASK-0079): the stop of a host
// ends its wait for an open request after about 3 s, and the disposal of the app then
// freed the backend while a command still ran in it. A review agent saw the command
// commit after the disposal. With Manifold, native memory can be freed under a
// running native call. The session now closes, and the disposal waits for the serial
// section before the backend goes.
[Collection("real host")]
public class EngineHostStopTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task A_Stop_Frees_The_Backend_Only_After_The_Running_Command_Ends()
    {
        var backend = new SlowBackend();
        var host = new EngineHost(backend);
        var app = await StartSurface(host);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var request = http.PostAsJsonAsync("/commands", new
        {
            name = "CreateBox",
            schemaVersion = 1,
            commandId = Guid.NewGuid(),
            parameters = new { sizeX = 1.0, sizeY = 1.0, sizeZ = 1.0 },
        });
        Assert.True(backend.Entered.Wait(Limit), "The command did not reach the backend.");

        // The steps of DesktopSurface.Dispose. The stop gives up the open request after
        // its limit; the disposal must then wait for the command.
        var stopped = false;
        var stop = Task.Run(async () =>
        {
            await app.StopAsync();
            stopped = true;
            await app.DisposeAsync();
        });

        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!Volatile.Read(ref stopped) && clock.Elapsed < Limit)
            await Task.Delay(50);
        Assert.True(Volatile.Read(ref stopped), "The stop did not end.");
        await Task.Delay(500);

        Assert.False(backend.Disposed, "The backend was freed while the command still ran in it.");
        Assert.False(stop.IsCompleted, "The disposal ended while the command still ran.");

        backend.Release.Set();
        await stop.WaitAsync(Limit);

        Assert.Equal(["CreateBox ended", "Disposed"], backend.Calls);
        try
        {
            using var response = await request.WaitAsync(Limit);
        }
        catch (HttpRequestException)
        {
            // The stop may close the connection of the request that it gave up.
        }
    }

    [Fact]
    public async Task A_Closed_Session_Refuses_Each_Later_Call()
    {
        var host = new EngineHost(new InProcessMeshBackend());
        host.Dispose();

        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        var command = await Assert.ThrowsAsync<InvalidOperationException>(() => host.Session.Apply(box));
        Assert.Contains("closed", command.Message, StringComparison.Ordinal);
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.Session.Read((document, _) => document.Version));
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

    // A backend whose CreateBox waits until the test releases it, and that records
    // the end of the call and its own disposal in their order.
    private sealed class SlowBackend : IGeometryBackend, IMeshOps, IDisposable
    {
        private readonly object _lock = new();

        public ManualResetEventSlim Entered { get; } = new();

        public ManualResetEventSlim Release { get; } = new();

        public List<string> Calls { get; } = [];

        public bool Disposed { get; private set; }

        public BackendIdentity Identity { get; } = new("test", "1");

        public BackendCapabilities Capabilities => BackendCapabilities.Mesh;

        public T? TryGet<T>() where T : class => this as T;

        public void CreateBox(BodyHandle handle, BoxParameters parameters)
        {
            Entered.Set();
            Release.Wait(Limit);
            lock (_lock) Calls.Add("CreateBox ended");
        }

        public void Dispose()
        {
            lock (_lock) Calls.Add("Disposed");
            Disposed = true;
        }
    }
}
