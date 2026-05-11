using Engine.Api.Http;
using Engine.Api.Http.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// One engine per host process. Restart resets state (V1 clamp: no persistence).
builder.Services.AddSingleton<EngineHost>();

var app = builder.Build();

app.MapPost("/commands", CommandsEndpoint.Handle);
app.MapPost("/queries", QueriesEndpoint.Handle);

app.MapGet("/schema/commands", SchemaCommandsEndpoint.Index);
app.MapGet("/schema/commands/{name}@{version:int}", SchemaCommandsEndpoint.Item);
app.MapGet("/schema/queries", SchemaQueriesEndpoint.Index);
app.MapGet("/schema/queries/{name}@{version:int}", SchemaQueriesEndpoint.Item);
app.MapGet("/schema/events", SchemaEventsEndpoint.Handle);
app.MapGet("/schema/diagnostics", SchemaDiagnosticsEndpoint.Handle);

app.Run();

// Marker so Engine.Tests can use WebApplicationFactory<Program>.
public partial class Program;
