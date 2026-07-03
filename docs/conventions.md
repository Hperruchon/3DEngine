# Conventions

File, naming, and grammar conventions **as the codebase already does them** — written so that grep and "find the governing decision" stay cheap. Each entry says whether it is **CI-enforced** or **convention-only**. This page points to authorities ([CLAUDE.md](../CLAUDE.md), the ADRs, [diagnostics.md](diagnostics.md)); it does not restate their rules.

## File-per-command / file-per-query

One command (and one query) per file, with its handler in a sibling file:

```
Engine.Core/Commands/CreateBoxCommand.cs        record CreateBoxCommand : Command
Engine.Core/Commands/CreateBoxCommandHandler.cs  class  CreateBoxCommandHandler : ICommandHandler
Engine.Core/Queries/GetBoundingBoxQuery.cs        record GetBoundingBoxQuery : Query
Engine.Core/Queries/GetBoundingBoxQueryHandler.cs class  GetBoundingBoxQueryHandler : IQueryHandler
```

- The **wire name** is the `Name` property on the record (PascalCase string, e.g. `"CreateBox"`) and matches the type's prefix. Versioning is the `SchemaVersion` int, not the type name.
- Commands live under `Engine.Core/Commands/`, queries under `Engine.Core/Queries/`, backends under `Engine.Core/Geometry/`.
- *Convention-only* (no naming gate). But a registered command with no schema entry **is** CI-enforced — see "Schema declaration" below.

## Type suffixes

Grep-stable suffixes already in use — match them when adding a peer:

| Suffix / shape | Role | Example |
|---|---|---|
| `*Command` / `*Query` | A triad message (`record`, extends `Command`/`Query`). | `CreateBoxCommand` |
| `*CommandHandler` / `*QueryHandler` | The handler (`class`, implements `I*Handler`). | `GetBoundingBoxQueryHandler` |
| `I*Ops` / `IGeometry*` | A negotiated geometry capability interface. | `IMeshOps`, `IGeometryQuery` |
| `*Bus` / `*Registry` / `*Sink` / `*Cache` | `Engine.Core` infrastructure. | `CommandBus`, `IdempotencyCache` |
| `*Endpoint` | An `Engine.Api.Http` route handler. | `SchemaEventsEndpoint` |

Contract/message types are `record` and `sealed` by default; only `Document` is a mutable `class`.

## Diagnostic code grammar

`<E|W|I>-<SUBSYSTEM>-<short-tag>` — kebab, stable, append-only. Subsystem tokens (`CMD`, `QRY`, `API`, `GEOM`, `IO`, `VAL`) and the full active table are owned by **[diagnostics.md](diagnostics.md)** — not repeated here. Each code is mirrored as a PascalCase constant in `Engine.Core/DiagnosticCodes.cs`.

- **CI-enforced:** the diagnostics-registry gate scans `Engine.*` sources for `<E|W|I>-…` tokens and fails `dotnet test` if any is absent from [diagnostics.md](diagnostics.md). Registering a new code in the same change is mandatory per [CLAUDE.md](../CLAUDE.md) "Diagnostic codes".

## Event `Kind` grammar

Domain/lifecycle event kinds are `<noun>.<verb-past>`, lowercase, dot-separated:

```
command.applied   command.rejected   command.cancelled   body.created
document.loaded   document.replayed   document.saved
```

- The authoritative kind list is `GET /schema/events`, hand-encoded in `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` from ADR-0005 §7 (registry-driven schema is a non-goal — see [CHARTER.md](CHARTER.md)). Do not re-enumerate it here.
- **Carve-out:** control/protocol frames are *not* domain events and do not follow the grammar — `heartbeat`, `subscription.resume`, `subscription.reset`. A couple of reserved-but-unemitted kinds in the list (`command.progress`, `validation.report`) also predate strict adherence; treat the grammar as the rule for *new* lifecycle kinds, not a claim that every listed string obeys it.
- Adding an event `Kind` is a contract change → see triggers below. *Convention-only* otherwise.

## Schema declaration

`/schema/commands/{name}@{version}` and `/schema/queries/{name}@{version}` are **pure projections** of the handler's declared `Parameters` / `Outputs` / `Result` (`FieldSchema` maps). The handler is the single source of truth; endpoints carry no per-command branching (ADR-0013).

- **CI-enforced:** the schema gate asserts every registered command/query has a schema entry, compares endpoint JSON structurally against handler declarations, and asserts the endpoint sources contain no name literals.

## Contract-change triggers (needs an ADR)

A change to `Engine.Contracts/**` requires an ADR in the same PR. The authoritative trigger list is [CLAUDE.md](../CLAUDE.md) "When unsure — stop and ask" and the stable-contracts list in [architecture/engine-runtime-boundaries.md](architecture/engine-runtime-boundaries.md); this is the grep-cheap checklist that points back to them:

- Add / remove / rename a public field on a contract record, or change a field's semantic meaning.
- Add a new event `Kind`.
- Add / change a capability interface or a `BackendCapabilities` flag.
- Change the HTTP/WS surface shape (routes, framing, sequence numbering).

**CI-enforced:** `.github/workflows/ci.yml` `contract-gate` fails any PR that touches `Engine.Contracts/**` without a matching `docs/adr/**` change.

## File-header → ADR citation

Files (and decision points inside them) cite their governing ADR inline:

```csharp
// Per ADR-0012 §4: handle is deterministic from CommandId. Replay against
// a fresh backend produces identical state.
```

This is the cheapest path from code to the decision that constrains it — `grep -r "ADR-0012"` finds every site bound by that ADR. The citation is pervasive at decision points; a file-top header comment is *common but not universal* (plain primitive records — e.g. `Command.cs`, `EventRecord.cs` — omit it). When you encode a non-obvious decision, cite the ADR + section the way the surrounding code does.
