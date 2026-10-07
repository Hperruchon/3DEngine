using System.Net;
using Engine.Api.Http;
using Engine.Api.Http.Endpoints;
using Engine.Api.Http.WebSockets;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

var builder = WebApplication.CreateBuilder(args);

// The host binds to a loopback address only. docs/CHARTER.md, target consumers,
// says so for Engine.Api.Http, and ADR-0019 §5 says so for the desktop host that
// mounts the same surface later. The framework default, with no value, is
// localhost. A refusal is an exit code and a message, not an exception
// (TASK-0044).
//
// Kestrel reads addresses from four keys of the configuration, and TASK-0044
// checked one of them (finding E23 of the review of 2026-10-04). Each key is
// checked before the start, so the host never binds a foreign address from
// them. A second check after the start reads the addresses that the server
// bound, for a source that this list does not know (TASK-0051).
var refusal = Program.ConfigurationRefusal(builder.Configuration);
if (refusal is not null)
{
    Console.Error.WriteLine(refusal);
    return 1;
}

// One engine per host process. Restart resets state until phase P8a.
// EventBroadcaster registered first; EngineHost depends on it for its
// BroadcastingEventSink wiring (TASK-0010).
builder.Services.AddSingleton<EventBroadcaster>();
builder.Services.AddSingleton(HostBackendOptions.Native);
builder.Services.AddSingleton<EngineHost>();
builder.Services.AddSingleton(SubscriberOptions.Default);

// The Host header must name a loopback host. Without this filter a page that
// resolves a name to 127.0.0.1 can post a command from a browser. The host
// filtering middleware of the framework runs first in the pipeline and answers
// 400 (TASK-0044).
builder.Services.Configure<HostFilteringOptions>(options =>
{
    options.AllowedHosts = [.. SubscriberOptions.Default.PermittedOriginHosts];
});

var app = builder.Build();

// The engine is built at the start and not at the first request, so that a host
// with no native backend stops before it serves anything, with exit code 3, the
// same code as the command line (TASK-0036).
try
{
    app.Services.GetRequiredService<EngineHost>();
}
catch (BackendUnavailableException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 3;
}

app.UseHostFiltering();
app.UseWebSockets();

// A WebSocket upgrade with an Origin header that names a foreign host gets 403.
// A browser sends the header; a script or an agent with no header passes. The
// list of permitted hosts lives in SubscriberOptions, so a test and the desktop
// host can change it (TASK-0044).
//
// This middleware runs after UseWebSockets on purpose. Before it, Kestrel does
// not yet report the request as a WebSocket request, and the check is skipped;
// the test server reports it either way, so an in-process test cannot see the
// difference. A run on the real host did.
app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var origin = context.Request.Headers.Origin.ToString();
        var permitted = context.RequestServices.GetRequiredService<SubscriberOptions>().PermittedOriginHosts;

        if (origin.Length > 0 && !Program.OriginIsPermitted(origin, permitted))
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

await app.StartAsync();

var bound = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses;
var boundRefusal = Program.LoopbackRefusal(bound is null ? null : string.Join(';', bound));
if (boundRefusal is not null)
{
    Console.Error.WriteLine(boundRefusal);
    await app.StopAsync();
    return 1;
}

await app.WaitForShutdownAsync();
return 0;

// Marker so Engine.Tests can use WebApplicationFactory<Program>. The two
// functions are the rules of TASK-0044 in a form that a test can call.
public partial class Program
{
    // Null when each address in the list is a loopback address, or when the
    // list is absent and the framework default applies. Otherwise the message
    // that the host prints before it exits with the code 1.
    internal static string? LoopbackRefusal(string? urls)
    {
        if (string.IsNullOrWhiteSpace(urls))
            return null;

        foreach (var url in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && IsLoopbackHost(uri.Host))
                continue;

            return $"engine-api-http binds to a loopback address only, and '{url}' is not one. "
                + "Use http://127.0.0.1:<port> or http://localhost:<port>. See docs/CHARTER.md, "
                + "section \"Target consumers\", and ADR-0019 section 5.";
        }

        return null;
    }

    // Null when each address that the configuration gives to Kestrel is a
    // loopback address. The keys are "urls" (from --urls, ASPNETCORE_URLS or
    // DOTNET_URLS), each Kestrel:Endpoints:<name>:Url, and "http_ports" and
    // "https_ports" (from ASPNETCORE_HTTP_PORTS and ASPNETCORE_HTTPS_PORTS). A
    // port key binds each interface, so a value in it is always refused.
    internal static string? ConfigurationRefusal(IConfiguration configuration)
    {
        foreach (var key in new[] { "http_ports", "https_ports" })
        {
            if (!string.IsNullOrWhiteSpace(configuration[key]))
                return $"engine-api-http binds to a loopback address only, and the setting '{key}' "
                    + $"(ASPNETCORE_{key.ToUpperInvariant()}) binds each interface. Remove it, and use "
                    + "--urls http://127.0.0.1:<port>. See ADR-0019 section 5.";
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

    private static bool IsLoopbackHost(string host)
        => string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || (IPAddress.TryParse(host.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address));
}
