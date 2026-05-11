using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Api.Http.Schema;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// GET /schema/commands
// GET /schema/commands/{name}@{version}
//
// Per ADR-0008 §9 and TASK-0009 §2–3. The index is registry-driven
// (CommandRegistry.Registered); the per-item shape is hand-known for
// NoOp because no schema-declaration mechanism exists yet (deferred
// to P7's first concrete command).
internal static class SchemaCommandsEndpoint
{
    public static IResult Index(EngineHost host)
    {
        var entries = host.CommandRegistry.Registered
            .Select(r => new CommandSchemaIndexEntry(r.Name, r.SchemaVersion))
            .OrderBy(e => e.Name, StringComparer.Ordinal)
            .ToArray();
        return Results.Json(entries, ApiJson.Options);
    }

    public static IResult Item(string name, int version, EngineHost host)
    {
        if (!host.CommandRegistry.Registered.Contains((name, version)))
        {
            return ApiErrorEnvelope.NotFound(
                $"No schema entry for command '{name}'@{version}.");
        }

        // Hand-known for NoOp@1. Add a switch as new commands land.
        if (name == "NoOp" && version == 1)
        {
            var item = new CommandSchemaItem(
                Name: "NoOp",
                SchemaVersion: 1,
                Parameters: new Dictionary<string, FieldSchema>
                {
                    ["echo"] = new FieldSchema("string", Required: true),
                },
                Outputs: new Dictionary<string, FieldSchema>
                {
                    ["echo"] = new FieldSchema("string"),
                });
            return Results.Json(item, ApiJson.Options);
        }

        // A command was registered but no hand-known schema exists.
        // The gate test in Engine.Tests catches this — fail loudly here too.
        return Results.Problem(
            $"Schema entry missing for registered command '{name}'@{version}.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
}
