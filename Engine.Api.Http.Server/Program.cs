using Engine.Api.Http;
using Microsoft.AspNetCore.Connections;

// The program engine-api-http (ADR-0019 item 4, TASK-0038). It serves headless
// work: continuous integration, scripts, and a shared server later. It composes
// one document session and mounts the surface library on it; the desktop host
// mounts the same library on its own session (TASK-0054).
//
// The host binds to a loopback address only. docs/CHARTER.md, target consumers,
// says so for engine-api-http, and ADR-0019 item 5 says so for each host of the
// surface. The framework default, with no value, is localhost. A refusal is an
// exit code and a message, not an exception (TASK-0044). The checks live in the
// library, so each host uses the same checks (EngineSurface).
//
// Exit codes: 1 for an address that is not loopback, before or after the start;
// 3 when the native backend does not load (TASK-0036); 4 when the address is in
// use (TASK-0062); 0 at a normal stop.

var builder = WebApplication.CreateBuilder(args);

var refusal = EngineSurface.ConfigurationRefusal(builder.Configuration);
if (refusal is not null)
{
    Console.Error.WriteLine(refusal);
    return 1;
}

builder.Services.AddSingleton(HostBackendOptions.Native);
builder.AddEngineSurface(services => ServerEngine.Create(services.GetRequiredService<HostBackendOptions>()));

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

app.MapEngineSurface();

// Before TASK-0062 a port in use ended the host with an exception and the exit
// code of a crash (finding E51 of the review of 2026-10-11).
try
{
    await app.StartAsync();
}
catch (IOException ex) when (ex.InnerException is AddressInUseException)
{
    Console.Error.WriteLine($"The address is in use: {ex.Message}");
    await app.DisposeAsync();
    return 4;
}

var boundRefusal = app.BoundAddressRefusal();
if (boundRefusal is not null)
{
    Console.Error.WriteLine(boundRefusal);
    await app.StopAsync();
    return 1;
}

await app.WaitForShutdownAsync();
return 0;

// Marker so Engine.Tests can use WebApplicationFactory<Program>.
public partial class Program;
