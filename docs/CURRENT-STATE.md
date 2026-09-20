# Current state

What is built today. One short entry per shipped milestone. Reference ADRs by ID; do not restate decisions.

## v0.1 — Engine Runtime spine (P0, TASK-0001)

In-memory, in-process Engine Runtime. No transport, no clients, no persistence, no geometry backend.

**Built:**
- `Engine.Contracts` — `Command`, `CommandResult`, `Outputs`, `Diagnostic`, `ErrorDetail`, `Query`, `QueryResult<T>`, `EventRecord`, `Document`, `BodyHandle`, capability marker interfaces (`IGeometryBackend`, `IMeshOps`, `IGeometryQuery`, `IBRepOps`, `IFeatureIdMap`), handler abstractions.
- `Engine.Core` — `CommandBus` (serial, atomic commit-at-end), `QueryBus` (empty registry), `CommandRegistry`, `QueryRegistry`, `InMemoryEventSink` (bounded ring), `Replay`, `DiagnosticCodes`, `NoOpCommand` + handler.
- `Engine.Tests` — 9 tests covering command application, version-stale rejection, query rejection, monotonic Seq, ring eviction, replay determinism.

**Verified by:** `dotnet build` + `dotnet test` green.

**Decisions in force:** ADRs 0001–0008.

**Deferred:** HTTP/WS transport, CLI, concrete geometry backend, persistence, idempotency cache, schema endpoints, undo/redo. See V1 scope clamps in [CLAUDE.md](../CLAUDE.md).

## v0.2 — Engine.Cli (P1, TASK-0002)

In-process CLI per ADRs 0002, 0008. Verbs `apply` and `query`; JSON output to stdout. Only `NoOp` registered; query registry empty. Exit codes `0` Applied, `1` Rejected/Cancelled, `2` invalid usage. `dotnet build` + `dotnet test` green.

## v0.3 — Diagnostics registry CI gate (P2, TASK-0003)

Test-time gate enforcing CLAUDE.md's diagnostic-codes rule. Scans `Engine.Contracts/`, `Engine.Core/`, `Engine.Cli/` `.cs` sources for tokens matching `<E|W|I>-<SUBSYSTEM>-<tag>` and fails `dotnet test` if any are absent from `docs/diagnostics.md`. Implements the third gate in CLAUDE.md's gate list (after build and test). No production code changes; no new diagnostic codes. `dotnet build` + `dotnet test` green (31 tests).

## v0.4 — `3DEngine.Core` peer render kernel (P3, TASK-0004, ADR-0009)

`3DEngine.Core` is now part of the authority diagram as a peer render kernel to `Engine.Core`. The two kernels are mutually unreferenceable; render-capable hosts reference both and own the projection (events → render state). No code, csproj, or test changes — documentation only. CLAUDE.md, the ADR index, and the roadmap updated accordingly. `dotnet build` + `dotnet test` green (31 tests).

## v0.5 — Replay determinism CI gate (P4, TASK-0005)

Test-time gate completing CLAUDE.md's gate list (build, test, dependency direction, diagnostic codes registered, **replay determinism fixture**). A hand-authored fixture (three `NoOpCommand`s with stable `CommandId` GUIDs) is replayed via `Replay.ReplayLog`; the resulting `Document.Version`, `Document.Log`, and event sequence (`Seq`/`Kind`/`CauseCommandId`) are asserted against a hand-authored snapshot. A second test runs the replay twice and asserts the two runs match each other. Both modulo `Timestamp`/`DocumentId` per ADR-0005. No production code changes; no new diagnostic codes. `dotnet build` + `dotnet test` green (33 tests).

## v0.6 — Workflow gates (P5, TASK-0006)

`.github/` introduced: PR template, CODEOWNERS, and `workflows/ci.yml`. The workflow runs three jobs on push to `main` and on pull_request: **build-and-test** (`dotnet build` + `dotnet test`), **headless-smoke** (spawns the CLI binary and asserts NoOp's JSON output at process scope), and **contract-gate** (PR-only; fails when `Engine.Contracts/**` changes without a corresponding `docs/adr/**` change). No `Engine.*` code, test, or contract changes. V1 Pending is empty; the engine advances to V1.x.

## v0.7 — Engine.Api.Http scaffold (P6.1, TASK-0007)

First V1.x phase. New project `Engine.Api.Http` (ASP.NET Core minimal API) hosts `POST /commands` and `POST /queries` against an in-process engine, mirroring the CLI's dispatch and JSON shape. One command registered (`NoOp`); query registry empty. Transport errors (malformed body, missing required fields, wrong content-type, wrong method, unknown route) return `400`/`404`/`405`/`415` with an `E-API-BAD-REQUEST` envelope; engine verdicts always return `200` with the `CommandResult`/`QueryResult` JSON. New diagnostic code `E-API-BAD-REQUEST` registered in `docs/diagnostics.md`; new `API` subsystem token. References only `Engine.Core` and `Engine.Contracts` (authority diagram holds). No WebSocket, no idempotency cache, no schema endpoints — those are P6.2/P6.3/P6.4. `dotnet build` + `dotnet test` green (44 tests).

## v0.8 — Idempotency cache (P6.2, TASK-0008)

`Engine.Core/IdempotencyCache.cs` lands a FIFO-evicting `Dictionary<Guid, CommandResult>` (default capacity 1024 per ADR-0006 §7). `CommandBus.Apply` checks the cache inside its serial section: on hit, the cached `CommandResult` is returned with no new event, log entry, or Seq increment. On miss, the bus runs as before and stores the result on the way out. All three terminal states (Applied, Rejected, Cancelled) are cached — a retried `CommandId` always gets the same answer, which is the point of the cache. `CommandBus`'s constructor gains an optional `IdempotencyCache` parameter; existing callers (CLI, HTTP host, tests) compile unchanged. CLAUDE.md V1 clamps tightened: idempotency-cache clamp dropped; HTTP-transport clamp narrowed to WebSocket-only. No `Engine.Contracts/**` change. `dotnet build` + `dotnet test` green (52 tests).

## v0.9 — Schema endpoints (P6.4, TASK-0009)

Six `GET /schema/...` discovery endpoints on `Engine.Api.Http`, per ADR-0008 §9. Index endpoints (`/schema/commands`, `/schema/queries`) generate from `CommandRegistry`/`QueryRegistry` via a new `Registered` accessor. `/schema/events` lists the documented event kinds from ADR-0005 §7 (hand-encoded; will become registry-driven when an event registry lands). `/schema/diagnostics` mirrors `Engine.Core/DiagnosticCodes.cs`. Per-item command schemas are hand-known for `NoOp@1`; the full schema-declaration mechanism is deferred to P7's first concrete command. A new gate test (`SchemaEndpointGateTests`) enforces ADR-0008 §9's "every registered command has a schema entry" rule and the diagnostics-mirror invariant via reflection. No `Engine.Contracts/**` change; no new diagnostic code. `dotnet build` + `dotnet test` green (61 tests).

## v0.10 — WebSocket event stream + reconnect (P6.3, TASK-0010, ADRs 0010 & 0011)

`GET /events` on `Engine.Api.Http` upgrades to WebSocket and delivers Document events with the cursor-based reconnect protocol of ADR-0005 §§4–5. The client sends one `subscribe { documentId?, lastSeenSeq? }` frame; the engine replies with `subscription.resume { fromSeq }` (cursor inside ring) followed by buffered + live events, or `subscription.reset { documentId, snapshot }` (cursor missing or stale) with the V1.x snapshot shape from ADR-0010 — `Document` metadata only, no `Log`, no render state. Per-subscriber bounded outbound channel (default 1024 per ADR-0005 §6); on overflow the engine closes the WebSocket with status `1008` + reason `subscriber.lagged` and other subscribers are unaffected. 30 s idle heartbeat (`{ "kind": "heartbeat" }`). New `BroadcastingEventSink` decorator wraps the existing `InMemoryEventSink`; `Engine.Core` is untouched. New diagnostic codes `W-API-WS-LAGGED` and `E-API-WS-INVALID-SUBSCRIBE` registered in `docs/diagnostics.md`, `DiagnosticCodes.cs`, and `/schema/diagnostics`. ADR-0011 frames the WebSocket as part of the canonical server-default deployment topology; ADR-0010 fixes the snapshot wire shape. `dotnet build` + `dotnet test` green (70 tests). P6 is complete.

## v0.11 — First geometry slice: `CreateBox` end-to-end (P7a, TASK-0011, ADRs 0012 & 0013)

First concrete geometry command and the wiring posture for all future geometry. ADR-0012 settles the V1 capability surface (`IGeometryBackend.TryGet<T>()`, `IMeshOps.CreateBox`, `IGeometryQuery.GetBoundingBox`, `BackendCapabilities` flags), the handler-to-backend access mechanism (`Handle` gains a non-null `IGeometryBackend` parameter; bus and `Replay.ReplayLog` accept the active backend; cache-recovery via replay-against-fresh-backend), the Document `Bodies` projection (handles + minimum metadata; mutated only inside `CommandBus.Apply`'s commit section), and deterministic body identity (`BodyHandle = new BodyHandle(command.CommandId)` for single-body create commands). ADR-0013 makes `/schema/commands/{name}@{version}` and `/schema/queries/{name}@{version}` pure projections from handler-declared `Parameters` / `Outputs` / `Result` schemas; `FieldSchema` moves to `Engine.Contracts/Schema/`; `SchemaCommandsEndpoint` and `SchemaQueriesEndpoint` lose all per-command branching; the schema gate test now compares endpoint JSON against handler declarations structurally and asserts the endpoint sources contain no name literals. V1.x first backend is the in-process managed `InProcessMeshBackend` per ADR-0012 §7 — no native interop; Manifold is deferred to P7b. New event kind `body.created` (consecutive Seq after `command.applied`); `subscription.reset` snapshot grows a `bodies` array per ADR-0012 §6 (closes ADR-0010's "Geometry snapshot shape" open challenge). New diagnostic codes `E-GEOM-CAP-MISSING`, `E-GEOM-INVALID-PARAM`, `E-GEOM-BODY-NOT-FOUND` registered in the three required places. Replay-determinism fixture extends to cover `CreateBox` (one `body.created` event between two NoOps; `Document.Bodies` projection determinism asserted). `engine apply CreateBox … && engine query GetBoundingBox …` work in CLI; `POST /commands` + `POST /queries` work over HTTP with parity. CLAUDE.md drops the "No concrete geometry backend" V1 clamp.

## v0.12 — Charter (governance, docs-only)

`docs/CHARTER.md` adds the missing "why" layer above the ADRs: mission (anchored on "we do not build a 3D app…"), target consumers (AI agents first-class), V1 definition-of-success stated as realized properties (points here for the ledger, does not re-enumerate), V1.x/V2 direction (points to roadmap), the Non-goals (deferred) vs Anti-objectives (refused-forever) split, and an agent-first scope test (refuse → defer → proceed → stop-and-ask). Promoted to CLAUDE.md navigation entry #1 with the read-order rule "charter says whether to act · CLAUDE.md says where · ADR says how · CURRENT-STATE says what exists." No phase, no TASK, no code/test/contract change; no new diagnostic codes. Authored via a 4-phase multi-agent workflow (gather → draft → 3 adversarial critique lenses → reconcile); the critique caught and removed a false "founding manifesto" provenance claim. Build + test unaffected.

## v0.13 — Navigation infrastructure (governance, docs-only)

Finishes the navigation layer the P1 experiment deferred. Three new docs, each scoped to **point, not duplicate** — they hold navigation and naming only, deferring *why* to the ADRs, *what exists* to this file, and *boundaries* to CLAUDE.md:

- `docs/INDEX.md` — one-page repo map (paths + one-line purpose for every area); links to `architecture/engine-runtime-boundaries.md` as the canonical boundary diagram rather than redrawing it.
- `docs/glossary.md` — canonical vocabulary, one line per term + a pointer to its defining file/ADR. Terms verified against `Engine.Contracts/` source (Command, Query, EventRecord, CommandResult, Outputs, Diagnostic, ErrorDetail, Document, Version, Seq, BodyHandle, Body, IGeometryBackend, capability/`TryGet<T>`, Replay, projection, design truth, the two kernels).
- `docs/conventions.md` — file/naming/grammar conventions derived from what the code already does (file-per-command, `*Command`/`*Handler`/`*Query` suffixes, diagnostic-code grammar, event-`Kind` grammar with control-frame carve-outs, the contract-change trigger checklist, and the inline file-header→ADR citation), each annotated CI-enforced vs convention-only.

Promoted to CLAUDE.md navigation: `INDEX.md` as #2 (the *where* map), `glossary.md`/`conventions.md` adjacent to `diagnostics.md` as the reference cluster (#6–8). No phase, no TASK, no code/test/contract change; no new diagnostic codes. Applied the CHARTER scope test: additive governance work, touches no `Engine.Contracts` shape. Build + test unaffected (docs-only).

## v0.14 — Manifold native geometry backend (P7b, TASK-0012, ADR-0014)

Swaps a real, native, double-precision Manifold backend in behind the v0.11 capability surface — no `Engine.Contracts` change. A new project `Engine.Geometry.Manifold` (references only `Engine.Contracts`, keeping native deps out of `Engine.Core`) hosts `ManifoldGeometryBackend : IGeometryBackend, IMeshOps, IGeometryQuery, IDisposable`, bound to Manifold **v3.5.2**'s `manifoldc` C API via source-generated `[LibraryImport]` P/Invoke — the repo's first hand-authored native binding (ADR-0014 §1) — with a `SafeHandle` encoding the verified *delete-owns-the-buffer* ownership. The CLI and HTTP hosts select Manifold when its native library is loadable and fall back to the managed `InProcessMeshBackend` otherwise, so the engine runs on any platform; this pragmatically resolves ADR-0014/TASK-0012's deferred RID-aware-fallback open question. The canonical replay-determinism gate stays on the managed stub, so core determinism is untouched.

The native payload ships as a multi-RID NuGet (`Engine.Geometry.Manifold.Native`, `runtimes/<rid>/native`) built **from source, serial (`MANIFOLD_PAR=OFF`, no TBB)** by a manual CI matrix workflow (`.github/workflows/build-manifold-native.yml`, win-x64 + linux-x64 + osx-arm64) and consumed via a local-folder feed (interim bootstrap; GitHub Packages is the eventual home). New diagnostic codes `E-GEOM-NATIVE-OP` (raised by `CreateBoxCommandHandler` on a backend op failure) and `E-GEOM-BACKEND-INIT` (reserved — the fallback subsumes it) registered in the three required places. New tests: `ManifoldGeometryBackendTests` (behavioural parity with the stub) and `ManifoldReplayRoundTripTests` (native replay reconstructibility), both skipped when native manifoldc is unavailable so Linux CI stays green on the stub. Verified on win-x64: `engine apply CreateBox` applies via native Manifold and the full solution builds + tests green. Also fixed a latent CI bug — `ci.yml` now installs the pinned SDK via `global-json-file` (the `10.x` + `include-prerelease` combo never satisfied `global.json`). V1.x is complete.

## v0.15 — First real Manifold operations: Translate + Subtract (P7c, TASK-0013, ADR-0012 Amendment 1)

Adds the first geometry operations that require a real kernel — a boolean **Subtract** (box A minus box B → a carved solid) and a **Translate** (move a body off the origin) — behind two new capability interfaces `ITransformOps` and `IBooleanOps` (new `BackendCapabilities.Transform`/`.Booleans`). Both are implemented **only** by the native `ManifoldGeometryBackend` (via `manifold_translate` / `manifold_difference`, extending the P7b `manifoldc` binding); the managed `InProcessMeshBackend` is untouched and honestly reports `E-GEOM-CAP-MISSING`, since it stores box dimensions only and cannot represent a moved or carved solid. Translate exists to make the cut observable: with origin-centered boxes a non-empty `A − B` keeps A's bounding box, so moving B off-center first lets `GetBoundingBox` witness the trim — no new query.

Each op produces a **new** body (handle = `CommandId`, ADR-0012 §4), leaves its operands intact, and reuses the existing `body.created` event (no new event kind); the result's `Kind` is `"Solid"`. Handlers validate operand existence in `Document.Bodies` → capability → backend op, so a bad reference reports `E-GEOM-BODY-NOT-FOUND` on any backend and the one-shot CLI stays deterministic; a fully-consumed difference (subtrahend ⊇ minuend) rejects as a degenerate `E-GEOM-NATIVE-OP`. **No new diagnostic codes** — the existing `GEOM` codes cover every path. Wired through the CLI (`apply Translate` / `apply Subtract`) and HTTP (`POST /commands`), with `/schema/commands` auto-projecting both. The canonical replay-determinism gate stays stub-backed and unchanged; a separate native-gated round-trip replays create → create → translate → subtract twice and asserts identical state. `Engine.Contracts` change is confined to the two interfaces + two flags (gated by the ADR amendment). Verified on win-x64: the full solution builds + tests green with the native path exercised (a 10-cube minus an off-center 10-cube trims maxX from 5 to 0).

## v0.16 — A released SDK pin (P0.1, TASK-0014)

`global.json` pinned the SDK version `10.0.300-preview.0.26177.108` and set `rollForward` to
`disable`. No public feed supplies that preview SDK. Therefore each computer without that exact
build failed at SDK resolution, and the solution did not build. TASK-0006 recorded the risk at its
line 102 and predicted this outcome.

`global.json` now pins the released version `10.0.300`, sets `rollForward` to `latestFeature`, and
sets `allowPrerelease` to `false`. The policy accepts each SDK with the major version 10 and the
minor version 0, and it selects the highest one. A computer with 10.0.300 or with 10.0.400 builds
the solution.

Two documentation files gave the SDK version `10.0.200-preview.0.26103.119`, which no file pinned.
Each file now gives "10.0.300 or higher" and points to `global.json`. Rule 10 in `CLAUDE.md` permits
this change to `3DEngine/` and to `BlazorApp/`, because this task gave that scope.

`.github/workflows/ci.yml` needed no change. It installs the SDK from `global.json` already.

Verified on win-x64: the resolved SDK is 10.0.400. `dotnet build` passes with zero warnings and zero
errors. `dotnet test` passes 134 tests and skips none. No code changed, no contract changed, and no
diagnostic code was added.

Register entry R-0001 is closed.

## v0.17 — The charter and each operational rule (P0.2, TASK-0015)

A review of five steps examined the twenty objectives, the anti-objectives, the deferred non-goals
and the operational rules. The owner approved each result. This entry records the change to the
documents. No code changed.

**`docs/CHARTER.md` is replaced.** The mission is now "We do not build one application. We build the
parts that many applications use." The previous sentence, "We do not build a 3D app", had a correct
intent and incorrect words, and it blocked the renderer objective.

Two anti-objectives are amended, because each one blocked approved work. "A command lands completely
or it does not land" now states that it does not apply to a rebuild, therefore a feature tree can
hold a failed feature. "No needless future-proofing" now uses the cost test: add a property now if a
later change must rewrite the log or migrate each document.

Two anti-objectives gain a rule. Anti-objective 2 states that an interactive tool sends provisional
commands to the log and never uses a separate buffer. Anti-objective 10 states that a command records
the backend that performed the operation, therefore a replay never selects a backend.

Six anti-objectives are new. Four give the refused rungs of kernel ambition, by name and with a
measurement: no fillet on an edge chain, no general NURBS surface, no shape healing, and no general
surface intersector. One refuses a solver for fluid dynamics, a mesh generator and an engine for
molecular dynamics. One states that a replay gives the same result on each supported platform.

The charter gains a section for the kernel boundary. The owned kernel gives identity, and Manifold
gives geometry. The feature boundary is closed: five surfaces, three curves, seven topology types,
six operations, and one global tolerance.

The deferred list has one item on each line. Six items are promoted and are absent from the list:
persistence, undo and redo, B-Rep operations, fillet as a later rung, feature identifiers, and
tessellated meshes for a client. One item was obsolete and is removed, because v0.14 shipped the
native Manifold backend.

**`CLAUDE.md`** loses the persistence clamp. It gains nine operational rules: six for determinism and
three for the kernel. The rules forbid a transcendental function in the command path, a rotation
stored as an angle, `Math.FusedMultiplyAdd`, four APIs whose guarantee covers one process, a 32-bit
x86 runtime identifier, a tessellation in the Document, a tolerance on an entity, a general surface
intersector, and a solver result in the log. One rule is marked inactive until milestone P0.3,
because a person cannot obey it today. One reevaluation condition is added: report to the owner when
evidence shows that an objective needs a change.

**`docs/INDEX.md`** now agrees with `CLAUDE.md` about the projects outside the engine spine. The form
is conditional: do not change them unless a task gives that scope. The reference to a section named
"Do not touch" is repaired, because that section was renamed. The three new documents are in the map.
The entry for `engine-runtime-boundaries.md` records that the file is not current.

**`docs/roadmap.md`** is replaced. It now holds four tracks: foundations, the platform, the renderer
and the kernel, plus machine vision. Each approved objective has a track. This change repairs the
scope test in the charter, which treats the absence of a track as a stop signal.

`docs/register.md`, `docs/working-agreement.md` and `docs/templates.md` change from `Proposed` to
`Accepted`.

Verified: `dotnet build` passes with zero warnings and zero errors. `dotnet test` passes 134 tests
and skips none. No contract changed, and no diagnostic code was added.

## v0.18 — Handler-declared construction (P0.3, TASK-0016, ADR-0016)

Each host held a switch statement on the command name, and each case held parameter parsing written
by hand. A new command changed about ten files, and two of them were central. Therefore two agents
that added two commands collided each time. Registration was duplicated across seven positions, and
register entry R-0003 recorded the consequence: the canonical replay determinism gate registered two
handlers while each host registered five.

ADR-0013 already made each handler the source of truth for its schema. Nothing consumed that
declaration to build a command. ADR-0016 adds the step that reads it.

**`Engine.Contracts` change, gated by ADR-0016.** `ICommandHandler` gains `Command Create(CommandInput)`
and `IQueryHandler` gains `Query Create(QueryInput)`. Each input type is a record that carries the
bound parameters, the identifier and the optional expected version. A record avoids a second contract
change when a field arrives later.

**`Engine.Core/Hosting/ParameterBinder.cs`** converts raw values into typed values against the
declared `FieldSchema`. It reports a missing required field, an incorrect type and an unknown field,
each with the field name. Each parse uses the invariant culture, therefore a machine with a comma
decimal separator produces the same number. The types `object` and `array` stay deferred and fail
with a message that names the declared type.

**`Engine.Core/Hosting/HandlerCatalog.cs`** holds one explicit list of each handler. The list is not
a scan of the assembly, because replay determinism needs a fixed set and a fixed order. Each host and
each replay gate call `HandlerCatalog.RegisterAll`. Register entry R-0003 is closed at its source.

Both switch statements are removed. `Engine.Cli/Cli.cs`, `Engine.Api.Http/Endpoints/CommandsEndpoint.cs`
and `Engine.Api.Http/Endpoints/QueriesEndpoint.cs` now follow the same three steps: find the handler,
bind, then call `Create`. `Engine.Cli/Usage.cs` generates the command list and each example from the
handler declarations, therefore no file in `Engine.Cli` names a command. The HTTP surface converts a
`JsonElement` to a neutral value and keeps a number as a double, so no number passes through a
string; `Engine.Core` therefore needs no reference to `System.Text.Json`.

**The rule becomes active.** `CLAUDE.md` marked one anti-pattern inactive in v0.17, because a person
could not obey it. `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` now enforces it: a quoted
command name in a host file fails the gate and the failure names the file and the line. A deliberate
violation was injected and the gate failed as designed, then the violation was removed.

New tests: 17. `ParameterBinderTests` covers each declared type, an unknown field, a value that is
already typed, and the invariant culture in both directions under the `fr-FR` culture.
`DispatchSurfaceGateTests` covers the host surface, the catalog, the stable order, and a round trip
from each declared schema through `Create`.

Three CLI tests changed their assertion. Each one asserted the exact wording of a message that a
hand-written parser produced. The binder produces a generic message, therefore each test now asserts
that the message names the field and the rejected value. The behaviour contract did not change: the
exit code is 2 and the usage text appears.

Verified: `dotnet build` on the solution gives zero errors. Two warnings remain in the vendored
sample framework and predate this task. `dotnet test` gives 151 passed, zero failed, zero skipped, up
from 134. The command-line host applies `NoOp` and `CreateBox` through the new path.

New diagnostic codes: none. The existing codes cover each path.
