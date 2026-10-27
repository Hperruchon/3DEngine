using System.Globalization;
using Engine.Api.Http;
using Engine.Core;
using Engine.Geometry.Manifold;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ThreeDEngine.Desktop;

// The document session of the desktop and the HTTP and WebSocket surface on it
// (ADR-0019 items 1, 3 and 5, TASK-0054). An agent sends a command over HTTP to the
// document that the person has open, and the desktop reaches the same session
// with typed calls through Host.Session. No call of the desktop has a privileged
// lane: each change is a command on the same bus (ADR-0019 item 2).
//
// The surface listens on 127.0.0.1 only, at the port of the setting "Desktop:Port"
// (--port, or the variable Desktop__Port), 5180 by default; 0 takes a free port.
// The key has a prefix, because tools often set a variable PORT, which the
// framework reads as the key "port" (finding E54 of the review of 2026-10-11).
// The checks of the surface library refuse each other source of an address before
// the start and each bound address that is not loopback after it (TASK-0044,
// TASK-0051, TASK-0058). The surface stops with the desktop (ADR-0019 item 6).
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
    public const int ExitAddressInUse = 4;
    public const int ExitStartFailed = 5;

    public const string PortSetting = "Desktop:Port";

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

    // Cancelled when the surface stops, for example at Ctrl+C in the console. The
    // window then closes too (finding E50 of the review of 2026-10-11).
    public CancellationToken Stopping => _app.Lifetime.ApplicationStopping;

    // The surface, or null with an exit code and a message.
    public static (DesktopSurface? Surface, int ExitCode, string? Message) Start(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddCommandLine(args, new Dictionary<string, string> { ["--port"] = PortSetting });

        var refusal = EngineSurface.ConfigurationRefusal(builder.Configuration);
        if (refusal is not null)
            return (null, ExitAddressRefused, refusal);

        var setting = builder.Configuration[PortSetting] ?? DefaultPort.ToString(CultureInfo.InvariantCulture);
        if (!int.TryParse(setting, NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port > 65535)
            return (null, ExitSettingInvalid, $"The setting '{PortSetting}' (--port) must be a number from 0 to 65535, and '{setting}' is not.");

        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        if (!ManifoldGeometryBackend.IsNativeAvailable())
            return (null, ExitBackendUnavailable, $"{DiagnosticCodes.GeomBackendInit}: {ManifoldGeometryBackend.UnavailableReason()}");

        builder.AddEngineSurface(_ => new EngineHost(new ManifoldGeometryBackend(), "manifold", ManifoldGeometryBackend.NativeVersion));

        var app = builder.Build();

        // The container makes the session here, so it owns it, and the disposal of
        // the app disposes the backend. Before TASK-0062 the desktop made the
        // session itself, and nothing disposed it (finding E52 of the review of
        // 2026-10-11). The session resolves inside the try, so that a throw there also
        // disposes the app; until TASK-0079 it resolved before the try, and the outer
        // catch of the desktop gave code 5 with the app not disposed (finding E71 of the
        // codebase review of 2026-10-17).
        EngineHost host;
        try
        {
            host = app.Services.GetRequiredService<EngineHost>();
            app.MapEngineSurface();
            app.StartAsync().GetAwaiter().GetResult();
        }
        catch (IOException ex) when (ex.InnerException is AddressInUseException)
        {
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            return (null, ExitAddressInUse, $"The address is in use. Give another port with --port. {ex.Message}");
        }
        // Until TASK-0067 each other failure of the start, for example a port that the
        // system refuses, ended the desktop with an unhandled exception (finding E59
        // of the codebase review of 2026-10-12).
        catch (Exception ex)
        {
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            return (null, ExitStartFailed, $"The surface did not start: {ex.Message}");
        }

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
