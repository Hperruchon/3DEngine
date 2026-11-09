using System.Globalization;
using Engine.Api.Http;
using Engine.Core;
using Engine.Core.Persistence;
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
// Phase R14b (ADR-0061 item 9, TASK-0194): the host owns the session (DesktopSession), and the surface mounts on it.
// The surface opens the session at the point of its checks where it made it before, so the order of the checks and the
// exit codes stay. It disposes the session only when its own start fails; else the host disposes the session after
// the surface. DesktopSession holds the backend, the document file and the store of the assets.
//
// The setting "Desktop:SynchronizationValidation" (true or false, false by default)
// also turns on the checks of synchronization of the validation layer, for a check of
// the present path by a person (finding V16 of the codebase review of 2026-10-17,
// TASK-0079). Give it as --Desktop:SynchronizationValidation=true.
//
// The document file (ADR-0030, TASK-0092): DesktopSession gives the rules.
internal sealed class DesktopSurface : IDisposable
{
    public const int DefaultPort = 5180;

    // The exit codes of the desktop when the surface does not start.
    public const int ExitAddressRefused = 1;
    public const int ExitSettingInvalid = 2;
    public const int ExitBackendUnavailable = 3;
    public const int ExitAddressInUse = 4;
    public const int ExitStartFailed = 5;
    public const int ExitFileNotOpened = 6;

    // Finding V92 of the codebase review of 2026-11-06 (b) (TASK-0225): the window did not open, or its loop failed.
    public const int ExitWindowFailed = 7;

    public const string FileSetting = "file";

    public const string UnsavedFolderSetting = "Desktop:UnsavedFolder";

    public const string DocumentFolderSetting = "Desktop:DocumentFolder";

    // ADR-0102 item 6 (TASK-0316): the file of the recent projects; by default beside the folder of unsaved documents.
    public const string RecentFileSetting = "Desktop:RecentFile";

    public const string PortSetting = "Desktop:Port";

    public const string SynchronizationValidationSetting = "Desktop:SynchronizationValidation";

    private readonly WebApplication _app;

    private DesktopSurface(WebApplication app, DesktopSession session, string address, bool synchronizationValidation)
    {
        _app = app;
        Session = session;
        Address = address;
        SynchronizationValidation = synchronizationValidation;
    }

    // The session that the surface serves. The host owns it and disposes it after the surface (R14b).
    public DesktopSession Session { get; }

    // True when the validation layer must also check the synchronization (V16).
    public bool SynchronizationValidation { get; }

    public DesktopDocument Document => Session.Document;

    // The session of the document that the desktop holds.
    public EngineHost Host => Session.Host;

    // The address that an agent uses, for example http://127.0.0.1:5180.
    public string Address { get; }

    // ADR-0046 item 2: the store of the assets that the surface serves, so the window and an agent
    // read the same bytes.
    public Engine.Core.Assets.AssetStore Assets => Session.Assets;

    // Cancelled when the surface stops, for example at Ctrl+C in the console. The
    // window then closes too (finding E50 of the review of 2026-10-11).
    public CancellationToken Stopping => _app.Lifetime.ApplicationStopping;

    // The surface, or null with an exit code and a message. A file that is not null replaces the setting "file": the
    // host gives the file of an open (ADR-0030 item 4, R14b).
    public static (DesktopSurface? Surface, int ExitCode, string? Message) Start(string[] args, string? file = null)
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

        var synchronizationSetting = builder.Configuration[SynchronizationValidationSetting] ?? "false";
        if (!bool.TryParse(synchronizationSetting, out var synchronizationValidation))
            return (null, ExitSettingInvalid, $"The setting '{SynchronizationValidationSetting}' must be true or false, and '{synchronizationSetting}' is not.");

        var (session, code, message) = DesktopSession.Open(builder.Configuration, file);
        if (session is null)
            return (null, code, message);

        // ADR-0030 item 2 and ADR-0046 item 2: the folder and the store of the session, so the window and an agent read the
        // same files. Before R14b the container made the session here and disposed it with the app (TASK-0062).
        builder.Services.AddSingleton(session.Document.Folder);
        builder.Services.AddSingleton(session.Assets);
        builder.AddEngineSurface(session.Host);

        WebApplication app;
        try
        {
            app = builder.Build();
        }
        catch (Exception ex)
        {
            session.Dispose();
            return (null, ExitStartFailed, $"The surface did not start: {ex.Message}");
        }

        try
        {
            // Finding E197 of the codebase review of 2026-11-09 (TASK-0324), ADR-0102 item 6: a request of the surface that
            // gives the session another named file, as POST /document/save, puts that file in the recent projects, as a save
            // from the window does. Until TASK-0324 a save of an agent went into the list only at the next start on the file.
            app.Use(async (context, next) =>
            {
                var before = session.Host.Session.FilePath;
                await next(context);
                if (session.Host.FileName is not null && session.Host.Session.FilePath is { } after && !string.Equals(before, after, StringComparison.Ordinal))
                    session.Document.Remember(after);
            });
            app.MapEngineSurface();
            app.StartAsync().GetAwaiter().GetResult();
        }
        catch (IOException ex) when (ex.InnerException is AddressInUseException)
        {
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            session.Dispose();
            return (null, ExitAddressInUse, $"The address is in use. Give another port with --port. {ex.Message}");
        }
        // Until TASK-0067 each other failure of the start, for example a port that the
        // system refuses, ended the desktop with an unhandled exception (finding E59
        // of the codebase review of 2026-10-12).
        catch (Exception ex)
        {
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            session.Dispose();
            return (null, ExitStartFailed, $"The surface did not start: {ex.Message}");
        }

        var boundRefusal = app.BoundAddressRefusal();
        if (boundRefusal is not null)
        {
            app.StopAsync().GetAwaiter().GetResult();
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            session.Dispose();
            return (null, ExitAddressRefused, boundRefusal);
        }

        return (new DesktopSurface(app, session, app.Urls.First(), synchronizationValidation), 0, null);
    }

    // Stops and frees the surface and its port. The session stays for the host, which disposes it (R14b).
    public void Dispose()
    {
        _app.StopAsync().GetAwaiter().GetResult();
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
