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
// use (TASK-0062); 5 when the start fails for another reason, for example a port
// that the system refuses (TASK-0067), or a configuration that the framework cannot
// read (TASK-0079); 6 when the document file does not open (TASK-0091); 0 at a normal stop.
//
// The argument --file names the document file (ADR-0015 item 10). The host opens it,
// or makes it when it does not exist, before it serves a request, and each applied
// command goes into it before its result.

// Until TASK-0079 a configuration file that the framework cannot read, for example
// an appsettings.json with an error, ended the host with an unhandled exception and
// the exit code of a crash (finding E68 of the codebase review of 2026-10-17). The
// desktop gave 5 for the same file.
WebApplicationBuilder builder;
string? refusal;
try
{
    builder = WebApplication.CreateBuilder(args);
    refusal = EngineSurface.ConfigurationRefusal(builder.Configuration);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"The host did not start: {ex.Message}");
    return 5;
}

if (refusal is not null)
{
    Console.Error.WriteLine(refusal);
    return 1;
}

builder.Services.AddSingleton(HostBackendOptions.Native);
builder.Services.AddSingleton(new DocumentFileOptions(builder.Configuration["file"]));
builder.AddEngineSurface(services => ServerEngine.Create(
    services.GetRequiredService<HostBackendOptions>(),
    services.GetRequiredService<DocumentFileOptions>()));

WebApplication app;
try
{
    app = builder.Build();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"The host did not start: {ex.Message}");
    return 5;
}

// The engine is built at the start and not at the first request, so that a host
// with no native backend stops before it serves anything, with exit code 3, the
// same code as the command line (TASK-0036).
try
{
    foreach (var warning in app.Services.GetRequiredService<EngineHost>().OpenWarnings)
        Console.Error.WriteLine($"{warning.Code}: {warning.Message}");
}
catch (BackendUnavailableException ex)
{
    Console.Error.WriteLine(ex.Message);
    await app.DisposeAsync();
    return 3;
}
catch (IOException ex)
{
    // A DocumentFileException gives its code in its message (ADR-0015 item 8).
    Console.Error.WriteLine($"The document file did not open: {ex.Message}");
    await app.DisposeAsync();
    return 6;
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
// Until TASK-0067 each other failure of the start ended the host with an unhandled
// exception and the exit code of a crash (finding E59 of the review of 2026-10-12).
catch (Exception ex)
{
    Console.Error.WriteLine($"The host did not start: {ex.Message}");
    await app.DisposeAsync();
    return 5;
}

var boundRefusal = app.BoundAddressRefusal();
if (boundRefusal is not null)
{
    Console.Error.WriteLine(boundRefusal);
    await app.StopAsync();
    // Until TASK-0079 this path returned with the app not disposed (finding E71).
    await app.DisposeAsync();
    return 1;
}

await app.WaitForShutdownAsync();
return 0;

// Marker so Engine.Tests can use WebApplicationFactory<Program>.
public partial class Program;
