# ADR 0013 — Command/query schema declaration

## Status

Accepted — 2026-05-27

## Context

ADR-0008 §9 requires that `/schema/commands/{name}@{version}` (and the query / event / diagnostics siblings) be **registry-driven**, not hand-written: "Schemas are generated from the engine's registries, not hand-written. CI fails if a registered command/query/event lacks a schema entry."

TASK-0009 (v0.9) shipped the endpoints but deferred the *declaration mechanism*. Index endpoints (`/schema/commands`, `/schema/queries`) iterate the registries; per-item endpoints contain a hand-coded switch ("Add a switch as new commands land"). The gate test `SchemaEndpointGateTests.Every_Registered_Command_Has_A_Schema_Entry` only checks that the endpoint returns 200 — it does not verify the returned schema matches the handler's true contract, because there is no handler-side declaration to compare against.

P7 introduces a second command (`CreateBox`) and proves the limit. Either:

- Every new command requires a parallel edit in `SchemaCommandsEndpoint.Item` (drift risk, never enforced).
- The handler itself declares its schema and the endpoint serves whatever the handler says (enforced by construction).

This ADR pins the latter. It is **cross-cutting** — applies to all commands (today: `NoOp`; tomorrow: `CreateBox` + everything after) and to queries (when the first query lands). It is independent of ADR-0012 (which decides handler-to-backend wiring); the two ship together in the V1.x first slice because both are required, not because they depend on each other.

## Decision

### 1. Schema lives on the handler

`ICommandHandler` and `IQueryHandler` grow `Parameters` and `Outputs` (or `Result`) schema properties:

```csharp
public interface ICommandHandler
{
    string CommandName { get; }
    int SchemaVersion { get; }
    IReadOnlyDictionary<string, FieldSchema> Parameters { get; }
    IReadOnlyDictionary<string, FieldSchema> Outputs { get; }
    Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct);
}

public interface IQueryHandler
{
    string QueryName { get; }
    int SchemaVersion { get; }
    IReadOnlyDictionary<string, FieldSchema> Parameters { get; }
    IReadOnlyDictionary<string, FieldSchema> Result { get; }
    // ...Handle signature TBD when first query lands.
}
```

Both schema dictionaries are non-null; an empty dictionary (`{}`) means "no parameters" / "no outputs". The handler is the single source of truth — no separate registration, no attributes, no reflection over the command record type.

### 2. `FieldSchema` lives in `Engine.Contracts`

Today `FieldSchema` is `internal` to `Engine.Api.Http`. It moves to `Engine.Contracts` (new file `Engine.Contracts/Schema/FieldSchema.cs`) so handlers can return it. V1 shape:

```csharp
namespace Engine.Contracts.Schema;

public sealed record FieldSchema(string Type, bool Required = false);
```

`Type` values for V1: `"string"`, `"integer"`, `"number"`, `"boolean"`, `"object"`, `"array"`, `"guid"`, `"datetime"`. Nested object/array shape (item type, properties) is **deferred** to a follow-up ADR — V1 commands and queries do not currently need nested schemas.

When the first command needs nested schema (likely a future boolean-op command with an array of operand handles), this ADR amends to add an optional `Items` and `Properties` to `FieldSchema`. Until then, the flat shape ships.

### 3. Endpoints become pure projections

`SchemaCommandsEndpoint.Item` looks up the handler and projects:

```csharp
return new CommandSchemaItem(
    Name:          handler.CommandName,
    SchemaVersion: handler.SchemaVersion,
    Parameters:    handler.Parameters,
    Outputs:       handler.Outputs);
```

The hand-coded `if (name == "NoOp" && version == 1)` switch is deleted. Same for the queries endpoint when it lands. The endpoints contain *no* per-command knowledge.

### 4. Gate tightens to compare endpoint vs. handler

`SchemaEndpointGateTests.Every_Registered_Command_Has_A_Schema_Entry` is replaced by a stricter assertion:

For every registered handler, the JSON returned by `GET /schema/commands/{name}@{version}` equals the projection of `handler.Parameters` and `handler.Outputs`. Any drift between handler declaration and endpoint output fails the gate.

A second assertion: for every registered handler, `handler.Parameters` is non-null. Empty is fine; null is not.

### 5. Queries get the same treatment when they land

Registry `QueryRegistry` is empty in V1. When the first query handler ships, it implements `IQueryHandler` with the symmetric `Parameters` / `Result` schemas, `SchemaQueriesEndpoint` projects the same way, and the queries gate test mirrors the commands one. This ADR does NOT require shipping a query in V1; it requires that *when* one ships, it uses this mechanism.

### 6. Events: out of scope

Event payload schemas (`/schema/events`) remain hand-encoded for V1.x. Per TASK-0009 §Notes, "will become registry-driven when an event registry lands." That event registry is its own concern (likely tied to whoever ships persistence). This ADR addresses commands and queries only.

## Consequences

- **`Engine.Contracts` evolves.** `FieldSchema` moves in; `ICommandHandler` grows two members; `IQueryHandler` grows two members. Gated by this ADR per CLAUDE.md.
- **`NoOpCommandHandler` updates.** Adds `Parameters = { "echo": new(Type: "string", Required: true) }` and `Outputs = { "echo": new(Type: "string") }` — matching the hand-coded schema being deleted from the endpoint.
- **`Engine.Api.Http`'s `SchemaTypes.FieldSchema` deletes.** Replaced by the contracts type. Existing `SchemaTypes.CommandSchemaItem` keeps its shape (it stays internal; its `Parameters` / `Outputs` field types switch from the now-deleted local `FieldSchema` to `Engine.Contracts.Schema.FieldSchema`).
- **No drift between handler and wire.** A handler that changes its parameter shape without changing its schema declaration fails its own contract; the gate catches it.
- **New commands no longer edit `SchemaCommandsEndpoint`.** They edit their own handler and ship.
- **No schema-declaration breakage when a new field type is added.** `FieldSchema.Type` is `string`; new vocabulary is additive. Clients reading `/schema/commands/...` continue to see whatever the latest server emits.

## Non-goals

- Nested schema (array item types, object property shapes) for V1. Deferred until a command needs it.
- Schema description / human-readable docs / examples. Add when the AI-agent surface needs them; not yet.
- Versioned schema evolution rules (deprecation, breaking-change policy across `SchemaVersion`). Today commands are `@1`; revisit when the first `@2` arrives.
- Auto-generation from C# records or attributes. Explicit declaration is clearer for V1 and avoids reflection-vs-trimming friction.
- A pluggable `ISchemaProvider` registered alongside handlers. Premature; handler-as-self-describing keeps the surface lean.
- Event payload schemas (separate from this ADR; tied to an event-registry that does not yet exist).

## Validation rules

1. `ICommandHandler.Parameters` and `ICommandHandler.Outputs` are non-null for every registered handler. CI: gate test asserts.
2. `GET /schema/commands/{name}@{version}` JSON output equals the projection of the matching handler's declared `Parameters` and `Outputs`. CI: gate test compares structurally.
3. `SchemaCommandsEndpoint.Item` contains no per-command branching. CI: the file's source contains no string-literal matches for any registered command name. (Negative-source-content test.)
4. When `IQueryHandler` ships, the same three rules apply to it and `SchemaQueriesEndpoint`. Until then, only the commands rules are exercised.
5. `FieldSchema.Type` values used by any handler are within the V1 vocabulary listed in §2. CI: gate test asserts.

## Open challenges

- **Nested schema.** First boolean-op or multi-operand command forces it. Pin when the use case arrives; the additive change is small (`Items`, `Properties`).
- **Schema versioning across `SchemaVersion` bumps.** When `CreateBox@2` ships alongside `CreateBox@1`, do both schemas live in their handlers (one handler per version) or in one handler with both shapes? Likely one handler per version (already how the registry keys are shaped). Confirm when the first `@2` arrives.
- **Cross-language client codegen.** Once the schema is reliably handler-declared, codegen (TypeScript clients, Python SDK, AI-agent tool definitions) becomes mechanical. Not in scope but unblocked by this ADR.
- **Default values.** `FieldSchema(Type, Required)` has no `Default`. The first command with an optional-with-default parameter forces an additive amendment.
