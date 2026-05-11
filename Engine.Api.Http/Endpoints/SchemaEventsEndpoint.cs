using Engine.Api.Http.Json;
using Engine.Api.Http.Schema;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// GET /schema/events
//
// Per ADR-0008 §9 and TASK-0009 §6. No event registry exists today;
// the list is hand-encoded from ADR-0005 §7. When an event registry
// lands (likely with the WebSocket subscription work in P6.3), this
// becomes registry-driven.
internal static class SchemaEventsEndpoint
{
    // Order matches ADR-0005 §7 for diff-friendliness.
    public static readonly IReadOnlyList<EventKindEntry> Kinds = new EventKindEntry[]
    {
        new("command.applied"),
        new("command.rejected"),
        new("command.progress"),
        new("command.cancelled"),
        new("document.loaded"),
        new("document.replayed"),
        new("document.saved"),
        new("validation.report"),
        new("heartbeat"),
        new("subscription.resume"),
        new("subscription.reset"),
    };

    public static IResult Handle() => Results.Json(Kinds, ApiJson.Options);
}
