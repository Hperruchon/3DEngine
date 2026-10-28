using Engine.Api.Http.Json;
using Engine.Api.Http.Schema;
using Engine.Contracts.Schema;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// GET /schema/events
//
// Per ADR-0008 §9 and TASK-0009 §6. No event registry exists today;
// the list is hand-encoded from ADR-0005 §7 plus subsequent additions.
// When an event registry lands, this becomes registry-driven.
//
// Each entry also gives the fields that a message of its kind carries, in the form
// of the fields of /schema/commands (TASK-0055): for an event record, the fields of
// its payload, which Engine.Core/CommandBus.cs writes; for a message of the
// transport (heartbeat, subscription.*), its own fields beside the kind. A kind that
// no source emits yet has no field. A test compares each payload that the bus emits
// with its entry, so a new field with no entry fails (ADR-0008 §9, ADR-0005,
// validation rule 5).
internal static class SchemaEventsEndpoint
{
    private static IReadOnlyDictionary<string, FieldSchema> Fields(params (string Name, string Type)[] fields)
        => fields.ToDictionary(field => field.Name, field => new FieldSchema(field.Type, Required: true), StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, FieldSchema> None = Fields();

    // The fields of a body: Required false means that the value can be null, as the name
    // of a body with no name (ADR-0028 items 1 and 7).
    private static IReadOnlyDictionary<string, FieldSchema> BodyFields(params (string Name, string Type)[] fields)
    {
        var result = new Dictionary<string, FieldSchema>(Fields(fields), StringComparer.Ordinal)
        {
            ["name"] = new FieldSchema("string", Required: false),
            ["visible"] = new FieldSchema("boolean", Required: true),
        };
        return result;
    }

    // The payload of a command event: the name and the version of the command (ADR-0006).
    private static readonly IReadOnlyDictionary<string, FieldSchema> CommandFields = Fields(("name", "string"), ("schemaVersion", "integer"));

    // Order matches ADR-0005 §7 first, then additions in registration order.
    public static readonly IReadOnlyList<EventKindEntry> Kinds = new EventKindEntry[]
    {
        new("command.applied", CommandFields),
        new("command.rejected", Fields(("name", "string"), ("schemaVersion", "integer"), ("errorCode", "string"))),
        new("command.progress", None),
        new("command.cancelled", CommandFields),
        new("document.loaded", None),
        new("document.replayed", None),
        new("document.saved", None),
        new("validation.report", None),
        new("heartbeat", None),
        new("subscription.resume", Fields(("fromSeq", "integer"))),
        new("subscription.reset", Fields(("documentId", "guid"), ("snapshot", "object"))),
        new("body.created", BodyFields(("bodyId", "guid"), ("kind", "string"))), // ADR-0012 §5, ADR-0028 item 7
        new("body.consumed", Fields(("bodyId", "guid"))), // ADR-0021 item 6, TASK-0037
        new("body.removed", Fields(("bodyId", "guid"))), // ADR-0031 item 6, TASK-0093
        new("body.changed", BodyFields(("bodyId", "guid"))), // ADR-0028 item 7, TASK-0085
    };

    public static IResult Handle() => Results.Json(Kinds, ApiJson.Options);
}
