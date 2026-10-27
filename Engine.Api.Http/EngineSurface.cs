using System.Net;
using System.Net.Sockets;
using Engine.Api.Http.Endpoints;
using Engine.Api.Http.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Api.Http;

// The HTTP and WebSocket surface as a library that a host mounts on a document
// session that it owns (ADR-0019 item 3, TASK-0038). engine-api-http mounts it in
// its program, and the desktop host mounts it in its own process (TASK-0054).
// Each host uses the same checks, so the rules of TASK-0044, TASK-0051 and
// TASK-0058 hold for each host.
//
// A host calls the parts in this order:
//
//   1. ConfigurationRefusal before the build; a refusal stops the host.
//   2. AddEngineSurface before the build, with the session that the host owns.
//   3. MapEngineSurface after the build.
//   4. BoundAddressRefusal after the start; a refusal stops the host.
public static class EngineSurface
{
    // The limit that the framework gives to the wait of a stop for an open request. The
    // default of the framework is 30 s, and a local client that sent part of a request
    // held the stop of a host for that time (finding E58 of the codebase review of
    // 2026-10-12, TASK-0067). The stop takes longer than this limit: a review agent
    // measured 3.01 s for a request that a slow command held (finding E70 of the review
    // of 2026-10-17), three runs of EngineHostStopTests gave 3.01 s to 3.02 s on
    // 2026-10-27, and DesktopSurfaceTests permits 5 s. The disposal that follows the
    // stop also waits for a command that still runs (DocumentSession.Close, TASK-0079).
    public static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(2);

    // Before the build. The host composes its session in the given function, and
    // the surface serves it. The function runs one time, when the first service
    // asks for the session.
    public static WebApplicationBuilder AddEngineSurface(this WebApplicationBuilder builder, Func<IServiceProvider, EngineHost> session)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(session);

        // The default of the framework reloads Kestrel:Endpoints when a
        // configuration file changes, and starts each new endpoint with no check
        // (finding E41 of the review of 2026-10-09). This loader does not reload.
        builder.WebHost.ConfigureKestrel((context, options) =>
            options.Configure(context.Configuration.GetSection("Kestrel"), reloadOnChange: false));

        builder.Services.Configure<Microsoft.Extensions.Hosting.HostOptions>(options => options.ShutdownTimeout = ShutdownTimeout);

        builder.Services.AddSingleton(session);
        builder.Services.AddSingleton(services => services.GetRequiredService<EngineHost>().Broadcaster);
        builder.Services.AddSingleton(SubscriberOptions.Default);

        // The Host header must name a loopback host. Without this filter a page
        // that resolves a name to 127.0.0.1 can post a command from a browser. The
        // host filtering middleware of the framework runs first in the pipeline
        // and answers 400 (TASK-0044).
        builder.Services.Configure<HostFilteringOptions>(options =>
        {
            options.AllowedHosts = [.. SubscriberOptions.Default.PermittedOriginHosts];
        });

        return builder;
    }

    // After the build: the middleware and each endpoint of the surface.
    public static WebApplication MapEngineSurface(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseHostFiltering();
        app.UseWebSockets();

        // A WebSocket upgrade with an Origin header that names a foreign host gets
        // 403. A browser sends the header; a script or an agent with no header
        // passes. The list of permitted hosts lives in SubscriberOptions, so a test
        // and the desktop host can change it (TASK-0044).
        //
        // This middleware runs after UseWebSockets on purpose. Before it, Kestrel
        // does not yet report the request as a WebSocket request, and the check is
        // skipped; the test server reports it either way, so an in-process test
        // cannot see the difference. A run on the real host did.
        app.Use(async (context, next) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var origin = context.Request.Headers.Origin.ToString();
                var permitted = context.RequestServices.GetRequiredService<SubscriberOptions>().PermittedOriginHosts;

                if (origin.Length > 0 && !OriginIsPermitted(origin, permitted))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }

            await next(context);
        });

        app.MapPost("/commands", CommandsEndpoint.Handle);
        app.MapPost("/queries", QueriesEndpoint.Handle);

        app.MapGet("/schema/commands", SchemaCommandsEndpoint.Index);
        app.MapGet("/schema/commands/{name}@{version:int}", SchemaCommandsEndpoint.Item);
        app.MapGet("/schema/queries", SchemaQueriesEndpoint.Index);
        app.MapGet("/schema/queries/{name}@{version:int}", SchemaQueriesEndpoint.Item);
        app.MapGet("/schema/events", SchemaEventsEndpoint.Handle);
        app.MapGet("/schema/diagnostics", SchemaDiagnosticsEndpoint.Handle);
        app.MapGet("/schema/backend", SchemaBackendEndpoint.Handle);

        app.MapGet("/events", EventsEndpoint.Handle);

        return app;
    }

    // After the start: null when each address that the server bound is a
    // loopback address, or the message of the refusal. This check covers a
    // source of an address that ConfigurationRefusal does not know; a foreign
    // address from such a source is bound until the host stops (TASK-0051).
    public static string? BoundAddressRefusal(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var bound = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses;
        return LoopbackRefusal(bound is null ? null : string.Join(';', bound));
    }

    // Null when each address in the list is a loopback address, or when the list
    // is absent and the framework default applies. Otherwise the message that the
    // host prints before it stops.
    public static string? LoopbackRefusal(string? urls)
    {
        if (string.IsNullOrWhiteSpace(urls))
            return null;

        foreach (var url in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // The parser of Kestrel itself, so that this check and the bind read
            // the same host. System.Uri read "http://evil@localhost:5000" as
            // localhost, and Kestrel read the host "evil@localhost" and bound each
            // interface (finding E40 of the review of 2026-10-09, TASK-0058).
            if (TryParseBindingAddress(url, out var address) && !address.IsUnixPipe && IsLoopbackHost(address.Host))
                continue;

            return $"The surface binds to a loopback address only, and '{url}' is not one. "
                + "Use http://127.0.0.1:<port> or http://localhost:<port>. See docs/CHARTER.md, "
                + "section \"Target consumers\", and ADR-0019 section 5.";
        }

        return null;
    }

    // Before the build: null when each address that the configuration gives to
    // Kestrel is a loopback address. The keys are "urls" (from --urls,
    // ASPNETCORE_URLS or DOTNET_URLS), each Kestrel:Endpoints:<name>:Url, and
    // "http_ports" and "https_ports" (from ASPNETCORE_HTTP_PORTS and
    // ASPNETCORE_HTTPS_PORTS). A port key binds each interface, so a value in it
    // is always refused. TASK-0044 checked "urls" only (finding E23 of the review
    // of 2026-10-04, TASK-0051).
    public static string? ConfigurationRefusal(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        foreach (var key in new[] { "http_ports", "https_ports" })
        {
            if (!string.IsNullOrWhiteSpace(configuration[key]))
                return $"The surface binds to a loopback address only, and the setting '{key}' "
                    + $"(from ASPNETCORE_{key.ToUpperInvariant()}, DOTNET_{key.ToUpperInvariant()} or --{key}) binds each interface. Remove it, and "
                    + "give a loopback address such as http://127.0.0.1:<port>. See ADR-0019 section 5.";
        }

        var endpoints = configuration.GetSection("Kestrel:Endpoints").GetChildren().Select(endpoint => endpoint["Url"]);
        var urls = new[] { configuration["urls"] }.Concat(endpoints).Where(url => !string.IsNullOrWhiteSpace(url));
        return LoopbackRefusal(string.Join(';', urls));
    }

    // An Origin header names a permitted host, compared without case. The value
    // "null", which a sandboxed page sends, is not an address and is refused.
    internal static bool OriginIsPermitted(string origin, IReadOnlyList<string> permittedHosts)
        => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && permittedHosts.Any(host => string.Equals(host, uri.Host, StringComparison.OrdinalIgnoreCase));

    private static bool TryParseBindingAddress(string url, out BindingAddress address)
    {
        try
        {
            address = BindingAddress.Parse(url);
            return true;
        }
        catch (FormatException)
        {
            address = null!;
            return false;
        }
    }

    // A host in brackets is one pair around an IPv6 address. A host with no
    // brackets is localhost or an IPv4 address. Kestrel reads each other form as a
    // host name and binds each interface. The check removed each bracket at the
    // two ends before it read the address, so it passed "[127.0.0.1]" and
    // "[[::1]]" (finding E48 of the review of 2026-10-11, TASK-0062).
    private static bool IsLoopbackHost(string host)
    {
        if (host.Length > 2 && host[0] == '[' && host[^1] == ']')
        {
            var inside = host[1..^1];
            return inside.IndexOfAny(['[', ']']) < 0
                && IPAddress.TryParse(inside, out var v6)
                && v6.AddressFamily == AddressFamily.InterNetworkV6
                && IPAddress.IsLoopback(v6);
        }

        if (host.IndexOfAny(['[', ']', ':']) >= 0)
            return false;

        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || (IPAddress.TryParse(host, out var v4) && v4.AddressFamily == AddressFamily.InterNetwork && IPAddress.IsLoopback(v4));
    }
}
