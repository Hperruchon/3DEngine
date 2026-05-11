using Engine.Api.Http;
using Engine.Api.Http.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// One engine per host process. Restart resets state (V1 clamp: no persistence).
builder.Services.AddSingleton<EngineHost>();

var app = builder.Build();

app.MapPost("/commands", CommandsEndpoint.Handle);
app.MapPost("/queries", QueriesEndpoint.Handle);

app.Run();

// Marker so Engine.Tests can use WebApplicationFactory<Program>.
public partial class Program;
