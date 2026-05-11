using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Api.Http.Schema;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// GET /schema/queries
// GET /schema/queries/{name}@{version}
//
// Per ADR-0008 §9 and TASK-0009 §4–5. Registry is empty in V1.x;
// index returns [] and per-item always returns 404.
internal static class SchemaQueriesEndpoint
{
    public static IResult Index(EngineHost host)
    {
        var entries = host.QueryRegistry.Registered
            .Select(r => new QuerySchemaIndexEntry(r.Name, r.SchemaVersion))
            .OrderBy(e => e.Name, StringComparer.Ordinal)
            .ToArray();
        return Results.Json(entries, ApiJson.Options);
    }

    public static IResult Item(string name, int version, EngineHost host)
    {
        if (!host.QueryRegistry.Registered.Contains((name, version)))
        {
            return ApiErrorEnvelope.NotFound(
                $"No schema entry for query '{name}'@{version}.");
        }

        // No concrete queries today. When the first lands, switch on (name, version).
        return Results.Problem(
            $"Schema entry missing for registered query '{name}'@{version}.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
}
