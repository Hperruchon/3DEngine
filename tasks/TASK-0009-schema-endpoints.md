# TASK-0009 — Schema endpoints (P6.4)

## Status

Ready

## Context

ADR-0008 §9 specifies six discovery endpoints on the HTTP API:

- `GET /schema/commands` — index of available commands, with versions.
- `GET /schema/commands/{name}@{version}` — full schema (parameters + outputs).
- `GET /schema/queries` — index.
- `GET /schema/queries/{name}@{version}` — full schema (parameters + result).
- `GET /schema/events` — index of event kinds + payload schemas.
- `GET /schema/diagnostics` — code registry.

The ADR is explicit: *"Schemas are generated from the engine's registries, not hand-written. CI fails if a registered command/query/event lacks a schema entry."*

V1.x's surface is minimal: `CommandRegistry` has only `NoOp`; `QueryRegistry` is empty (TASK-0007 §5); events are emitted by `CommandBus` as fixed `Kind` strings (no event registry); diagnostics live in `Engine.Core/DiagnosticCodes.cs` and `docs/diagnostics.md` (kept in sync by the P2 gate, TASK-0003).

The full schema-declaration *mechanism* (how a command declares its parameters / outputs JSON shape) is a Contracts change and out of P6.4 scope. NoOp's per-item schema is hand-authored against the actual `NoOpCommand` fields. When P7 ships the first concrete geometry command, the schema-declaration mechanism arrives alongside; this endpoint then becomes truly registry-driven for per-item shapes.

This task completes P6 by landing the six discovery endpoints with the data we have.

## Goal

Add six `GET /schema/...` endpoints to `Engine.Api.Http`. Index endpoints generate from the corresponding registry (or, for events/diagnostics, the documented source-of-truth). Per-item endpoints serve the one entry that exists today (NoOp) and return 404 for anything else.

Add gate tests in `Engine.Tests/Http/` per the ADR rule: every name in `CommandRegistry` has a schema entry; every `DiagnosticCodes` constant appears in `/schema/diagnostics`.

## Scope (in)

1. **Registry enumeration**
   - `Engine.Core/CommandRegistry.cs` — add `IReadOnlyCollection<(string Name, int SchemaVersion)> Registered { get; }` (or equivalent enumeration). Cheap; backed by the existing dictionary.
   - `Engine.Core/QueryRegistry.cs` — same shape.
   - Both Engine.Core changes only. `Engine.Contracts/**` untouched.

2. **`GET /schema/commands`** — index from `CommandRegistry`. JSON: `[{ "name": "NoOp", "schemaVersion": 1 }]`.

3. **`GET /schema/commands/{name}@{version}`**
   - For `NoOp@1`: hand-authored schema
     ```json
     {
       "name": "NoOp",
       "schemaVersion": 1,
       "parameters": { "echo": { "type": "string", "required": true } },
       "outputs":    { "echo": { "type": "string" } }
     }
     ```
   - For anything else: HTTP 404 with the `E-API-BAD-REQUEST` envelope from TASK-0007 §5.

4. **`GET /schema/queries`** — index from `QueryRegistry`. JSON: `[]` today.

5. **`GET /schema/queries/{name}@{version}`** — always 404 today (registry empty). Same envelope as commands.

6. **`GET /schema/events`** — fixed list, sourced from ADR-0005 §7:
   ```json
   [
     { "kind": "command.applied" }, { "kind": "command.rejected" },
     { "kind": "command.progress" }, { "kind": "command.cancelled" },
     { "kind": "document.loaded" }, { "kind": "document.replayed" },
     { "kind": "document.saved" }, { "kind": "validation.report" },
     { "kind": "heartbeat" }, { "kind": "subscription.resume" },
     { "kind": "subscription.reset" }
   ]
   ```
   - No event registry exists; the list is hand-encoded in `Engine.Api.Http` from the ADR. When an event registry lands (likely with P6.3 WebSocket work), this becomes registry-driven.

7. **`GET /schema/diagnostics`** — the code registry as JSON. Source: hand-encoded list in `Engine.Api.Http` mirroring `Engine.Core/DiagnosticCodes.cs`. The P2 gate keeps the constants in sync with `docs/diagnostics.md`; the gate test added here adds a third consumer that fails if the mirror is incomplete.
   ```json
   [
     { "code": "E-CMD-UNKNOWN",        "severity": "Error", "subsystem": "CMD" },
     { "code": "E-CMD-VERSION-STALE",  "severity": "Error", "subsystem": "CMD" },
     { "code": "E-CMD-BUS-BUSY",       "severity": "Error", "subsystem": "CMD" },
     { "code": "E-QRY-UNKNOWN",        "severity": "Error", "subsystem": "QRY" },
     { "code": "E-API-BAD-REQUEST",    "severity": "Error", "subsystem": "API" }
   ]
   ```

8. **Tests** in `Engine.Tests/Http/`:
   - `SchemaEndpointTests.Schema_Commands_Index_Lists_Registered_Commands`
   - `SchemaEndpointTests.Schema_Commands_Item_Returns_NoOp_Schema`
   - `SchemaEndpointTests.Schema_Commands_Item_Unknown_Returns_404_With_Api_Error_Envelope`
   - `SchemaEndpointTests.Schema_Queries_Index_Is_Empty_Today`
   - `SchemaEndpointTests.Schema_Queries_Item_Unknown_Returns_404`
   - `SchemaEndpointTests.Schema_Events_Lists_Documented_Event_Kinds`
   - `SchemaEndpointTests.Schema_Diagnostics_Lists_All_Registered_Codes`
   - `SchemaEndpointGateTests.Every_Registered_Command_Has_A_Schema_Entry` — ADR-0008 §9 rule.
   - `SchemaEndpointGateTests.Every_DiagnosticCodes_Constant_Appears_In_Schema_Diagnostics` — reflection-driven.

9. **Docs**
   - `docs/CURRENT-STATE.md` — v0.9 entry.
   - `docs/roadmap.md` — move P6.4 to Shipped V1.x. P6.3 remains Pending with its blocker.

## Scope (out)

- WebSocket events / reconnect (P6.3). Still blocked on the persistence-snapshot ADR.
- Schema-declaration mechanism for commands/queries (Contracts change). Lands with P7's first concrete command.
- Event registry (a `Kind`-indexed lookup of payload shapes). Currently hand-encoded.
- JSON Schema compliance (`$schema: "http://json-schema.org/..."`). The response shape is API-defined; full JSON Schema is a follow-up.
- `/schema` root index. Out of phase; clients hit the six endpoints directly.
- Caching, ETag, schema-version pinning.
- Auth.
- Any change to `Engine.Contracts/**`.
- Any new diagnostic code.

## Inputs

- ADR-0008 §9 — endpoint list and "schemas from registries" rule.
- ADR-0005 §7 — event kinds.
- `Engine.Core/CommandRegistry.cs`, `Engine.Core/QueryRegistry.cs` — registry surfaces.
- `Engine.Core/DiagnosticCodes.cs` + `docs/diagnostics.md` — diagnostic codes.
- `Engine.Api.Http/Program.cs` — endpoint registration surface.
- `Engine.Api.Http/Errors/ApiErrorEnvelope.cs` — 404 envelope shape (TASK-0007).

## Outputs

- Six `GET /schema/...` endpoints respond per the shapes above.
- `dotnet test` passes with the new tests.
- `curl http://localhost:5099/schema/commands` returns `[{"name":"NoOp","schemaVersion":1}]`.
- `curl http://localhost:5099/schema/diagnostics` returns the registered codes.
- `docs/CURRENT-STATE.md` v0.9 entry.
- `docs/roadmap.md` shows P6.4 Shipped V1.x.

## Files

**Created:**
- `Engine.Api.Http/Endpoints/SchemaCommandsEndpoint.cs`
- `Engine.Api.Http/Endpoints/SchemaQueriesEndpoint.cs`
- `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs`
- `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs`
- `Engine.Api.Http/Schema/SchemaTypes.cs` — small records for the JSON shapes.
- `Engine.Tests/Http/SchemaEndpointTests.cs`
- `Engine.Tests/Http/SchemaEndpointGateTests.cs`
- `tasks/TASK-0009-schema-endpoints.md` (this file)

**Modified:**
- `Engine.Core/CommandRegistry.cs` — expose enumeration of registered names+versions.
- `Engine.Core/QueryRegistry.cs` — same.
- `Engine.Api.Http/Program.cs` — map the six new routes.
- `docs/CURRENT-STATE.md` — add v0.9 entry.
- `docs/roadmap.md` — move P6.4 to Shipped.
- `tasks/TASK-0009-schema-endpoints.md` — Status flip in close commit.

**Do not touch:**
- `Engine.Contracts/**`. Schema-declaration mechanism is out of phase.
- `Engine.Cli/**`. No CLI changes.
- `docs/diagnostics.md`. No new codes.
- ADRs.
- `3DEngine/`, `BlazorApp/`, `3DEngine.Core/`, `Vortice.Vulkan.*`.

## Tests

- `SchemaEndpointTests.Schema_Commands_Index_Lists_Registered_Commands`
- `SchemaEndpointTests.Schema_Commands_Item_Returns_NoOp_Schema`
- `SchemaEndpointTests.Schema_Commands_Item_Unknown_Returns_404_With_Api_Error_Envelope`
- `SchemaEndpointTests.Schema_Queries_Index_Is_Empty_Today`
- `SchemaEndpointTests.Schema_Queries_Item_Unknown_Returns_404`
- `SchemaEndpointTests.Schema_Events_Lists_Documented_Event_Kinds`
- `SchemaEndpointTests.Schema_Diagnostics_Lists_All_Registered_Codes`
- `SchemaEndpointGateTests.Every_Registered_Command_Has_A_Schema_Entry`
- `SchemaEndpointGateTests.Every_DiagnosticCodes_Constant_Appears_In_Schema_Diagnostics`

## Acceptance criteria

1. `dotnet build` succeeds.
2. `dotnet test` passes — existing 52 plus the 9 new tests (61 total).
3. All six `/schema/...` endpoints return per the shapes documented above.
4. No `Engine.Contracts/**` change.
5. No new diagnostic code.
6. `docs/CURRENT-STATE.md` v0.9 entry exists.
7. `docs/roadmap.md` lists P6.4 under Shipped V1.x; P6.3 remains Pending with its blocker.

## Notes for the implementer

- **Hand-known where necessary, registry-driven where possible.** Index endpoints are registry-driven today. Per-item command schemas (and the events list) are hand-known because no declaration mechanism exists yet. The gate tests catch drift between registries and the hand-known data.
- **`Registered` accessor on the registries** is the minimal additive change. The new property returns a snapshot of `(Name, SchemaVersion)` tuples; existing `TryFind`/`Register`/`Count` are unchanged.
- **Event list is hand-encoded from ADR-0005 §7.** When an event registry is introduced (likely with WebSocket work in P6.3), the hand-list becomes registry-driven; the gate test adapts to assert all registered Kinds appear.
- **Diagnostic codes list mirrors `DiagnosticCodes.cs` constants.** The P2 gate (TASK-0003) keeps that file in sync with `docs/diagnostics.md`. The gate test added here uses reflection over `DiagnosticCodes` to assert the schema endpoint exposes every constant.
- **404 envelope reuse.** Per-item endpoints reuse `ApiErrorEnvelope.BadRequest(...)` to format their 404 response. The body shape `{"error": {"code": "E-API-BAD-REQUEST", "message": ...}}` matches what TASK-0007 §5 already specified.
- **No new routes on POST.** All six are `GET`. The existing `/commands` and `/queries` POST routes are untouched.
- **Route templates.** ASP.NET Core minimal API supports `{name}` and constraints; the `@version` suffix is in the route path (e.g. `/schema/commands/{name}@{version:int}`). Verify both NoOp@1 and unknown@99 cases.
