using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Api.Http.Schema;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// GET /schema/queries
// GET /schema/queries/{name}@{version}
//
// Per ADR-0008 §9, TASK-0009 §4–5, and ADR-0013 §3. Pure projection from
// QueryRegistry handlers; no per-query knowledge here.
internal static class SchemaQueriesEndpoint
{
    public static IResult Index(EngineHost host)
    {
        var entries = host.QueryRegistry.Handlers
            .Select(h => new QuerySchemaIndexEntry(h.QueryName, h.SchemaVersion))
            .OrderBy(e => e.Name, StringComparer.Ordinal)
            .ToArray();
        return Results.Json(entries, ApiJson.Options);
    }

    public static IResult Item(string name, int version, EngineHost host)
    {
        if (!host.QueryRegistry.TryFind(name, version, out var handler))
        {
            return ApiErrorEnvelope.NotFound(
                $"No schema entry for query '{name}'@{version}.");
        }

        return Results.Json(new QuerySchemaItem(
            Name: handler.QueryName,
            SchemaVersion: handler.SchemaVersion,
            Parameters: handler.Parameters,
            Result: handler.Result), ApiJson.Options);
    }
}
