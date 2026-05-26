using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Api.Http.Schema;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// GET /schema/commands
// GET /schema/commands/{name}@{version}
//
// Per ADR-0008 §9, TASK-0009 §2–3, and ADR-0013 §3. The endpoint is a
// pure projection: it looks up the handler in CommandRegistry and serves
// its declared Parameters / Outputs. No per-command knowledge here.
internal static class SchemaCommandsEndpoint
{
    public static IResult Index(EngineHost host)
    {
        var entries = host.CommandRegistry.Handlers
            .Select(h => new CommandSchemaIndexEntry(h.CommandName, h.SchemaVersion))
            .OrderBy(e => e.Name, StringComparer.Ordinal)
            .ToArray();
        return Results.Json(entries, ApiJson.Options);
    }

    public static IResult Item(string name, int version, EngineHost host)
    {
        if (!host.CommandRegistry.TryFind(name, version, out var handler))
        {
            return ApiErrorEnvelope.NotFound(
                $"No schema entry for command '{name}'@{version}.");
        }

        return Results.Json(new CommandSchemaItem(
            Name: handler.CommandName,
            SchemaVersion: handler.SchemaVersion,
            Parameters: handler.Parameters,
            Outputs: handler.Outputs), ApiJson.Options);
    }
}
