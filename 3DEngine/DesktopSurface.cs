using System.Globalization;
using Engine.Api.Http;
using Engine.Core;
using Engine.Geometry.Manifold;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ThreeDEngine.Desktop;

// The document session of the desktop and the HTTP and WebSocket surface on it
// (ADR-0019 items 1, 3 and 5, TASK-0054). An agent sends a command over HTTP to the
// document that the person has open, and the desktop reaches the same session
// with typed calls through Host.Session. No call of the desktop has a privileged
// lane: each change is a command on the same bus (ADR-0019 item 2).
//
// The surface listens on 127.0.0.1 only, at the port of the setting "port"
// (--port, or the variable port), 5180 by default; 0 takes a free port. The checks
// of the surface library refuse each other source of an address before the start
// and each bound address that is not loopback after it (TASK-0044, TASK-0051,
// TASK-0058). The surface stops with the desktop (ADR-0019 item 6).
//
// The desktop is a composition root, so it chooses the native backend here
// (ADR-0014 section 4). As each host does, it stops when the native library does
// not load (TASK-0036).
internal sealed class DesktopSurface : IDisposable
{
    public const int DefaultPort = 5180;

    // The exit codes of the desktop when the surface does not start.
    public const int ExitAddressRefused = 1;
    public const int ExitSettingInvalid = 2;
    public const int ExitBackendUnavailable = 3;

    private readonly WebApplication _app;

    private DesktopSurface(WebApplication app, EngineHost host, string address)
    {
        _app = app;
        Host = host;
        Address = address;
    }

    // The session of the document that the desktop holds.
    public EngineHost Host { get; }

    // The address that an agent uses, for example http://127.0.0.1:5180.
    public string Address { get; }

    // The surface, or null with an exit code and a message.
    public static (DesktopSurface? Surface, int ExitCode, string? Message) Start(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var refusal = EngineSurface.ConfigurationRefusal(builder.Configuration);
        if (refusal is not null)
            return (null, ExitAddressRefused, refusal);

        var setting = builder.Configuration["port"] ?? DefaultPort.ToString(CultureInfo.InvariantCulture);
        if (!int.TryParse(setting, NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port > 65535)
            return (null, ExitSettingInvalid, $"The setting 'port' must be a number from 0 to 65535, and '{setting}' is not.");

        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        if (!ManifoldGeometryBackend.IsNativeAvailable())
            return (null, ExitBackendUnavailable, $"{DiagnosticCodes.GeomBackendInit}: {ManifoldGeometryBackend.UnavailableReason()}");

        var host = new EngineHost(new ManifoldGeometryBackend(), "manifold", ManifoldGeometryBackend.NativeVersion);
        builder.AddEngineSurface(_ => host);

        var app = builder.Build();
        app.MapEngineSurface();
        app.StartAsync().GetAwaiter().GetResult();

        var boundRefusal = app.BoundAddressRefusal();
        if (boundRefusal is not null)
        {
            app.StopAsync().GetAwaiter().GetResult();
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            return (null, ExitAddressRefused, boundRefusal);
        }

        return (new DesktopSurface(app, host, app.Urls.First()), 0, null);
    }

    public void Dispose()
    {
        _app.StopAsync().GetAwaiter().GetResult();
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
