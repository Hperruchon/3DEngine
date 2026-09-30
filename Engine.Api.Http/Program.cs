using System.Net;
using Engine.Api.Http;
using Engine.Api.Http.Endpoints;
using Engine.Api.Http.WebSockets;
using Microsoft.AspNetCore.HostFiltering;

var builder = WebApplication.CreateBuilder(args);

// The host binds to a loopback address only. docs/CHARTER.md, target consumers,
// says so for Engine.Api.Http, and ADR-0019 §5 says so for the desktop host that
// mounts the same surface later. The address comes from `--urls` or from the
// variable ASPNETCORE_URLS, which the configuration exposes as "urls"; the
// framework default, with no value, is localhost. A refusal is an exit code and
// a message, not an exception (TASK-0044).
var refusal = Program.LoopbackRefusal(builder.Configuration["urls"]);
if (refusal is not null)
{
    Console.Error.WriteLine(refusal);
    return 1;
}

// One engine per host process. Restart resets state until phase P8a.
// EventBroadcaster registered first; EngineHost depends on it for its
// BroadcastingEventSink wiring (TASK-0010).
builder.Services.AddSingleton<EventBroadcaster>();
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

app.MapGet("/events", EventsEndpoint.Handle);

app.Run();
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

    // An Origin header names a permitted host, compared without case. The value
    // "null", which a sandboxed page sends, is not an address and is refused.
    internal static bool OriginIsPermitted(string origin, IReadOnlyList<string> permittedHosts)
        => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && permittedHosts.Any(host => string.Equals(host, uri.Host, StringComparison.OrdinalIgnoreCase));

    private static bool IsLoopbackHost(string host)
        => string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || (IPAddress.TryParse(host.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address));
}
