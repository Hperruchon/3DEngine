using Engine.Api.Http;
using Engine.Api.Http.Endpoints;
using Engine.Api.Http.WebSockets;

var builder = WebApplication.CreateBuilder(args);

// One engine per host process. Restart resets state (V1 clamp: no persistence).
// EventBroadcaster registered first; EngineHost depends on it for its
// BroadcastingEventSink wiring (TASK-0010).
builder.Services.AddSingleton<EventBroadcaster>();
builder.Services.AddSingleton<EngineHost>();
builder.Services.AddSingleton(SubscriberOptions.Default);

var app = builder.Build();

app.UseWebSockets();

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

// Marker so Engine.Tests can use WebApplicationFactory<Program>.
public partial class Program;
