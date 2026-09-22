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

## v0.19 — Each governance rule becomes mechanical (P0.4, TASK-0017)

Four rules that existed only as text now fail a build. `docs/working-agreement.md` section 6.2 gives
the reason: a rule in text only is a rule that an agent will break.

**The register gate.** `Engine.Tests/Governance/RegisterGateTests.cs` reads `docs/register.md`. A
passed `due` date fails the build. A second extension fails the build. More than 20 open entries fails
the build. Each entry must declare a valid class and two dates. A `due` date must not be later than
the lifetime of its class; an earlier date is a deliberate tightening and stays permitted.

**The marker gate.** `Engine.Tests/Governance/MarkerGateTests.cs` reads live code and live
configuration. A word such as `TODO`, `interim` or `DRAFT` must cite an `R-nnnn` on its line or just
above it. The gate does not read an ADR, a closed task or the ledger, because each one is immutable or
append-only and the working agreement forbids a change to it. A gate that demanded such a change would
set two rules against each other.

**The ADR gate.** `Engine.Tests/Governance/AdrGateTests.cs` reads the front matter of each record.
Each ADR from 0001 to 0014 received front matter with `id`, `title`, `status`, `topic`, `date`,
`supersedes`, `superseded-by`, `amends`, `amended-by`, `affects` and `enforced-by`. The decision text
of each record is unchanged. A supersession field and an amendment field must be reciprocal. A status
must agree with those fields. The count of records with no enforcement must not grow past two.
`docs/adr/README.md` is regenerated from the front matter and a test holds the two in agreement.

**The dependency direction gate.** `Engine.Tests/Governance/DependencyDirectionGateTests.cs` reads
each project file in the working tree. It verifies each rule in `CLAUDE.md`, section "Dependency
rules": no reference from `Engine.Contracts`, only `Engine.Contracts` from `Engine.Core`, no reference
from `3DEngine.Core`, no reference to the render kernel from an `Engine.*` project, the permitted set
for a client, no reference between two clients, and no cycle. It needs no build, therefore it also
catches a reference that compiles. Register entry R-0004 recorded this gap since 2026-08-25.

**The amendment graph is now reciprocal.** ADR-0011 amends ADR-0004. ADR-0008 amends ADR-0006.
ADR-0016 amends ADR-0013. Each of the three amended records carries the status `Amended`. Register
entry R-0006 recorded the drift.

**Each gate was verified by injection.** A test that cannot fail is worth nothing, therefore each of
the four gates ran against a deliberate violation and each one failed as designed. The register gate
reported an overdue entry. The marker gate reported a `TODO` with no identifier in `CommandBus.cs`.
The ADR gate reported a one-way amendment. The dependency gate reported a reference from
`Engine.Core` to `3DEngine.Core`, named by two tests. Each violation was then removed.

**The first injection found a defect in the gate itself.** The dependency gate passed while the
violation existed. The cause: the repository holds three git worktrees under `.claude/worktrees/`,
and each one is a full copy of every project file pinned to an older commit. The graph is keyed by
project name, therefore a stale copy silently shadowed the real project and the gate reported a broken
rule as satisfied. The gate now excludes that directory, and a further test fails when one project
name appears more than once. This evidence is recorded on register entry R-0014, which asks whether
`.gitignore` must contain the directory.

New tests: 22. `dotnet test` gives 173 passed, zero failed, zero skipped, up from 151. `dotnet build`
on the solution gives zero errors and the same two warnings in the vendored sample framework.

Not done, and why: no tool generates the ADR index. A test that compares the index against the front
matter gives the same protection at a much lower cost. No check verifies the `writes` block of a task
file; that work needs a step in continuous integration and a decision about a local hook, therefore it
became register entry R-0017 with a limit of 2027-03-19.

Closed: R-0004, R-0006. Opened: R-0017. Open entries: 13 of 20.

New diagnostic codes: none. A gate is a test and a test does not emit a code.

## v0.20 — The unmerged branch is salvaged (P0.5, TASK-0018)

Branch `claude/happy-booth-1cef3f` held four identifiers that the main branch uses for different
work, and it held a working engine hosting factory that nobody merged. Register entry R-0012 recorded
that condition since 2026-08-25. Each identifier now names one thing.

**The engine hosting factory.** `Engine.Core/Hosting/EngineHosting.cs` gives `EngineKit` and
`EngineHosting.CreateDefault(backend)`. `Engine.Cli/Cli.BuildEngine` and
`Engine.Api.Http/EngineHost` both use it.

The factory is not the branch version. Three rules arrived after the branch and each one changed the
design. First, the factory takes `IGeometryBackend` as a parameter and does not construct one. ADR-0014
section 4 permits a reference to `Engine.Geometry.Manifold` only at the composition root of a host, and
`CLAUDE.md` permits `Engine.Core` to reference only `Engine.Contracts`; a factory that selected the
native backend would break both rules and the dependency direction gate of v0.19 would fail. Each host
therefore keeps its own three lines of backend selection, and that duplication is correct. Second, the
branch methods `RegisterDefaultCommands` and `RegisterDefaultQueries` are not salvaged, because
`HandlerCatalog` is the one list of handlers per ADR-0016 and a second registration surface is the
defect that register entry R-0003 recorded. Third, a verbatim merge would have removed `Translate` and
`Subtract` from each host, because the branch registered two handlers and the catalog holds four.

The kit gives `CreateCommandBus(sink)`, `CreateCommandBus()` and `CreateQueryBus()`. The reason that
the branch gave for not returning a bus still holds: the HTTP host wraps the sink in
`BroadcastingEventSink` before the bus sees it, and the command-line host uses the sink directly.

**ADR-0015 — command-log persistence.** The branch record is re-filed with the identifier 0015 and the
status `Proposed`. The design text is unchanged. The status is not `Accepted`, because no code
implements the design, no task is ready, and the clamp in `CLAUDE.md` still forbids persistence. An
accepted record with no implementation is the drift that the gates of v0.19 exist to prevent.
`docs/roadmap.md` phase P8a already said "ADR-0015" before this task ran.

**TASK-0019 — command-log persistence.** The branch task is re-filed with the number 0019 and the
status `Deferred`. Two conditions unblock it: the owner accepts ADR-0015, and `CLAUDE.md` drops the
clamp.

**The duplicate version label.** The branch also used the label v0.12 for the hosting factory. The
main line gives v0.12 to the charter. The branch label is void. This entry, v0.20, is the first ledger
entry for the hosting factory. The branch note that said "from this point forward, TASK numbers no
longer track CURRENT-STATE version numbers" is correct and stays true on the main line.

**The renderer proposal.** `docs/proposals/render-host-direction.md` existed only as an untracked file
inside `stash@{0}`, and no commit held it. Register entry R-0013 recorded the risk that the analysis
would disappear. A commit now holds it, with a header note that marks two statements as out of date:
the founding line that the v0.17 charter replaced, and the open decisions that roadmap track R settles
in part.

**One gate refinement.** The unenforced budget in `Engine.Tests/Governance/AdrGateTests.cs` now counts
a record in force only. A record with the status `Proposed`, `Withdrawn` or `Rejected` describes work
that nobody built or nobody will build, therefore enforcement cannot exist. The budget still bounds
each accepted decision at two.

New tests: 8. `dotnet test` gives 181 passed, zero failed, zero skipped, up from 173. `dotnet build`
gives zero errors.

Not done, and why: the branch task for the hosting factory is not re-filed as its own file. Its work
lands under TASK-0018, and a second file would give two records of one change. The branch and the
stash both stay, because a commit now holds each useful part and the owner decides when to remove
either one.

Closed: R-0012, R-0013. Open entries: 11 of 20.

New diagnostic codes: none.

## v0.21 — Three governance corrections (P0.8, TASK-0021)

The owner decided three items on 2026-09-21. Each one came out of the review of v0.19 and v0.20.

**One status vocabulary.** Three documents gave a different set of values for the ADR `status` field.
`CLAUDE.md`, section "Stop and ask", forbids an agent from correcting either side of a disagreement,
so the v0.20 report gave each side and stopped. The set is now `Proposed`, `Accepted`, `Amended`,
`Superseded`, `Withdrawn` and `Rejected`. `docs/templates.md` gained `Amended` and `Superseded`, and
it lost `Superseded-by-NNNN`, because the number belongs in the field `superseded-by`.

The gate also contradicted itself. `ValidStatuses` did not contain `Superseded`, and one test required
that value. The first superseded record would have failed one test whichever document was right. The
gate now accepts the value. This part was a defect and not a preference.

**The register limit is 15.** Register entry R-0016 asked whether a limit of 20 open entries is too
high to have an effect. The register held 11 open entries. A limit of 12, which the entry proposed,
gives one free slot, therefore the next new problem would force an exit on an old one immediately. A
limit of 15 gives four free slots. Rule 4 in `docs/register.md` and `RegisterGateTests.OpenLimit` both
give 15. R-0016 is closed with the exit `decided`.

**A correction to the v0.20 entry.** Working agreement rule 3.2 forbids a change to a past entry,
therefore this entry gives the correction.

The v0.20 entry, ADR-0015, TASK-0018 and TASK-0019 each stated that the clamp in `CLAUDE.md` forbids
persistence, and each used that statement to support the status `Proposed` on ADR-0015. The statement
was false. Milestone v0.17 removed the persistence clamp, in TASK-0015, four days earlier.
`CLAUDE.md`, section "Scope clamps", says "No clamp is active" and "Persistence arrives with ADR-0015
and its task".

The cause: the agent read the copy of `CLAUDE.md` that arrived in its context at the start of the
session, and not the file. The agent then changed that file itself and kept citing the old copy.

The conclusion does not change. Two true reasons remain for the status `Proposed`: no code implements
the design, and roadmap phase P8a is pending. The owner examined the question on 2026-09-21 and kept
the status. ADR-0015 and TASK-0019 each carry a correction note. TASK-0018 is closed, therefore its
text stays and a correction block follows it.

No gate can catch this class of error, because the wrong statement was about a file and it appeared in
prose. The defence is the habit of reading a file before citing it.

Tests: 181. No new test. `dotnet build` gives zero errors.

Closed: R-0016. Open entries: 10 of 15.

New diagnostic codes: none.

## v0.22 — The write set of a task becomes mechanical (P0.7, TASK-0022)

Register entry R-0017 recorded the last rule in `docs/templates.md` that had no check. Each task file
declares a `writes` block with a create list, a modify list and a forbid list. Nothing read that
block. An agent could change a file that the task forbids, and no check found the error.

**One implementation, two modes.** `Engine.Tests/Governance/WriteSetGateTests.cs` is a test and not a
script. Each static check always runs, therefore `dotnet test` covers it on a local computer and on
each of the three runners. The dynamic check runs when the environment variable `WRITE_SET_FILES`
gives a list of changed paths, one per line. The new job `write-set-gate` sets that variable from
`git diff --name-only`. The logic that continuous integration uses is the logic that the test suite
covers.

**Seven checks.** A changed path must not match a forbid pattern. A changed path must appear in a
create list or a modify list. A change must touch a task file, because `CLAUDE.md`, section
"Anti-patterns", forbids work outside the scope of the active task. A task must not both write and
forbid the same path. A declared path must use the forward slash and must not start with one. A task
with the status `Done` must have created each exact path in its create list. The count of task files
with no front matter must not grow past 13.

**A forbid beats a permit.** A change can touch more than one task file. The gate takes the union of
each create list and each modify list, and it refuses a path that matches any forbid pattern of any of
those tasks. A forbid records a boundary and a permit records an intention, therefore the strict
reading is the safe one. Only a task that the same change touches can govern that change, so a task
from an older change cannot authorise a file today.

**Each check was verified by injection.** The dynamic check ran four times. A legitimate change
passed. A change to `Engine.Contracts/Handlers/ICommandHandler.cs` reported the forbid pattern
`Engine.Contracts/**` and named TASK-0021 as its owner. A change to `docs/CHARTER.md` reported that no
list names it. A change with no task file reported that no write set governs it. The three static
checks each failed on a deliberate violation, and each message named the task and the path.

**The hook is refused, not deferred.** `docs/templates.md` described a hook before each commit and
said that the write set is only a recommendation without it. A hook needs an installation step on each
computer, and a person passes it with one flag. The gate runs where nobody can pass it. That row now
describes the gate.

**Task files 0001 to 0013 are exempt.** Each one predates `docs/templates.md` and carries no front
matter. The budget of 13 fails when the count grows, in the same way as the budget for an unenforced
ADR. A new task must declare its write set.

New tests: 6. `dotnet test` gives 187 passed, zero failed, zero skipped, up from 181. `dotnet build`
gives zero errors.

Not done, and why: the gate reads the tasks that a change touches and not a status. A change that
touches a task with the status `Deferred` would use that write set. No such change exists today, and a
stricter rule needs evidence before it earns its cost.

Closed: R-0017. Open entries: 9 of 15. Track 0 now has one pending phase, P0.6, which needs a pull
request before continuous integration can report.

New diagnostic codes: none.

**The gate found a defect on its first live use.** The first run reported
`Engine.Api.Http/Properties/` as a change with no declaration. The file is `launchSettings.json`. The
SDK generates it when a person runs the web host, and it carries random port numbers. It was untracked
and absent from `.gitignore`, therefore it would appear in `git status` after each run and it would
give a false difference on each computer. The stash from TASK-0018 holds an older copy with different
ports, which confirms the behaviour. `.gitignore` now names it.

## v0.23 — The repository is clean and the gate needs no person (P0.9, TASK-0023)

The owner gave three instructions: direct the work and do not do it by hand, clean the branches, and
close each entry that a decision can close.

**The gate no longer needs a person.** `.github/workflows/ci.yml` ran on a push to the main branch
only. The gate therefore gave no report on a branch, and a person had to open a pull request by hand
before register entry R-0002 could close. The trigger is now `push` with no branch filter, plus
`pull_request` for the two jobs that need a base reference.

**Twelve local branches became two.** Ten were fully merged, and `git branch -d` deleted each one,
because that command refuses an unmerged branch and therefore gives the check. Two held unique commits
and each one received a tag first: `archive/happy-booth-1cef3f`, which TASK-0018 salvaged into v0.20,
and `archive/p7b-integrate-package`, whose one commit reached the main branch as `831ebbc`.

**Three worktrees became zero.** Each one lived under `.claude/worktrees/` and held a full copy of
every project file, pinned between 31 and 51 commits behind the main branch. A stale copy of this kind
made the dependency direction gate report a broken rule as satisfied in v0.19. The filter in that gate
stays as a second defence.

**Two stashes became zero.** Neither held a change to a tracked file. Each held untracked files only,
and each of those files is accounted for: the renderer proposal entered a commit in v0.20,
`launchSettings.json` entered `.gitignore` in v0.22, and `.claude/settings.local.json` enters
`.gitignore` here. The tags `archive/stash-0` and `archive/stash-1` preserve both.

**The cleanup found an uncommitted draft ADR.** The worktree `happy-booth-1cef3f` refused removal,
because it held `docs/adr/0015-handler-owned-command-construction.md` with the status `Proposed`. Its
decision is the decision that ADR-0016 carries, which shipped in v0.18 and which a gate enforces, so
no new record is needed. A commit in that worktree captured the draft and
`archive/happy-booth-1cef3f` points at it.

The draft gave one thing that the repository did not hold. It names
`Engine.Core/Persistence/CommandCodec.cs` as a third position that would dispatch on a command name,
and it states that the persistence work must use handler-owned construction from the first day.
TASK-0019 now carries that constraint.

**R-0014 is decided.** `.gitignore` contains `.claude/`. The directory holds settings, a transcript
and a worktree; each one is host-specific and regenerable, and nothing under it was ever tracked.

**R-0009 was closed in fact and open in the register.** TASK-0017 corrected the false `DRAFT` header
and its Outcome says that the entry is resolved. Nobody moved the entry. The register and the
repository now agree. This was a bookkeeping defect of the agent, and the register is the instrument
that must not carry one.

Tests: 187, unchanged. No code changed. `dotnet build` gives zero errors.

Closed: R-0009, R-0014. Open entries: 7 of 15.

New diagnostic codes: none.

## v0.24 — The gate passes on three operating systems (P0.6, TASK-0020). Track 0 is complete

Run `35786216373` reports a pass on `ubuntu-latest`, on `windows-latest` and on `macos-latest`. Each
job runs `dotnet build`, `dotnet test`, the smoke test for `NoOp` and the smoke test for `CreateBox`.
Register entry R-0002 recorded this gap since 2026-08-25 and it is closed.

The report is automatic. The trigger change of v0.23 means that a push gives it, and a person opens
nothing. This milestone waited four days for one action by a person, which was the condition that
v0.23 removed.

**A correction.** TASK-0020 stated that the native Manifold payload carries the runtime identifier
`win-x64` only, and that the native tests therefore run on the Windows runner and skip on the other
two. The package holds three sets of binaries: `linux-x64`, `osx-arm64` and `win-x64`. Each runner has
a payload, therefore the native backend is expected to run on each runner. The comment in
`.github/workflows/ci.yml` is corrected and the task carries the correction.

The result does not change, because each job passed. The split between a test that ran and a test that
skipped is not observable from outside, because the log endpoint needs a token.

This is the third statement in three days that described the repository without reading it. The other
two were the dependency gate that trusted a project name, and a note that cited a scope clamp which a
previous milestone had removed. No gate catches this class, because each wrong statement sat in prose.

Tests: 187 on this computer. No code changed. Open entries: 6 of 15.

Track 0 is complete: P0.1 to P0.9 are shipped.

New diagnostic codes: none.

## v0.25 — A licence notice and a verified checksum for the native payload (P0.10, TASK-0024)

Register entry R-0005 recorded that the repository holds a binary of 4.4 MB with no licence element
and no checksum, against ADR-0014 section 5. Its limit was 2026-09-24.

**Two components, not one.** `THIRD-PARTY-NOTICES.md` names Manifold under the Apache License 2.0,
and Clipper2 under the Boost Software License 1.0. The first reading found Manifold only. The package
ships no separate Clipper2 binary, therefore the code is compiled into the Manifold library. The
evidence is a search of the raw bytes: the name Clipper appears in `libmanifold.so.3.5.2`, in
`manifold.dll` and in `libmanifold.3.5.2.dylib`. The build sets `MANIFOLD_CROSS_SECTION=ON` and
`cmake/manifoldDeps.cmake` fetches Clipper2 at commit `46f6391`, which agrees.

**The checksums are a rule and not a decoration.**
`Engine.Tests/Governance/NativePackageGateTests.cs` reads each SHA-256 value from the notices and
compares it against the file, in both directions. A change to a binary that does not change the
notices fails the build, and a binary inside the package that no value records fails too. Both
directions were verified by injection.

**The package names the wrong repository.** Its nuspec holds
`<repository type="git" commit="718eab0684178e4fdf7ef419cc4ff26484008705" />`. That commit is not a
Manifold commit. It is a commit in this repository, dated 2026-07-04, with the subject "Merge pull
request #8 from Hperruchon/p7b-finish", while the description in the same nuspec says "Version tracks
the pinned Manifold commit". The package therefore gives incorrect information about the origin of its
own binary. Register entry R-0018 holds the defect, because a correction needs a new build. The
notices give the commit that the tag `v3.5.2` names and state that a reader must not use the nuspec.

**A note on method.** A first attempt to look inside the binaries used `strings`, which this computer
does not have. The command returned nothing, and the absence read as "no Clipper2 inside". A check of
the total count caught the error, and `grep` on the raw bytes gave the true answer. A tool that is
absent returns an empty result, and an empty result is not evidence.

New tests: 4. `dotnet test` gives 191 passed, up from 187. `dotnet build` gives zero errors.

Closed: R-0005. Opened: R-0018. Open entries: 6 of 15.

New diagnostic codes: none.

## v0.26 — Each open question is decided (P0.11, TASK-0025)

Four entries held a question and not a piece of work. The register now holds two open entries, from
sixteen at the start of this branch.

**R-0008 — the command-line parser is accepted permanently.** The convention `--param k=v` is normal.
ADR-0016 gave type coercion and validation to `ParameterBinder`, therefore `ArgParser` splits text and
makes no decision about a value. JSON input on the command line would copy the HTTP surface. The
comment that promised a replacement is replaced by the decision. The entry is the first in the section
"Accepted compromises".

**R-0010 — the boundary document is archived.** It is at
`docs/archive/engine-runtime-boundaries.md` with a header that forbids its use, and `docs/INDEX.md`
points at the section "Authority diagram" in `CLAUDE.md`. A rewrite was refused: each part that is
still correct lives in that section and in the ADRs, therefore a rewrite would give a second place for
one decision, which is the condition that made the document wrong.

**R-0011 — a limit, and not only an answer.** The entry names quantity as the risk, so an answer about
two codes would return as the same question at the third. `docs/diagnostics.md` gains a section that
names each reserved code with its reason, as an addition, because `CLAUDE.md` permits an addition to
that file and nothing else. `Engine.Tests/Governance/DiagnosticsReserveGateTests.cs` bounds the
quantity at two and was verified by injection.

**R-0015 — the command line uses the relaxed encoder.** `Engine.Cli/JsonRenderer.cs` uses
`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`, therefore the output shows `it's here <&>` and not a
line of Unicode escapes. `Engine.Api.Http` keeps the default encoder, because a browser client can put
a response into a page. No test asserted the escaped form.

New tests: 3. `dotnet test` gives 194 passed, up from 191. `dotnet build` gives zero errors.

Closed: R-0010, R-0011, R-0015. Accepted: R-0008. Open entries: 2 of 15, which are R-0007 and R-0018.

New diagnostic codes: none. The two reserved codes are unchanged and now carry a permanent reason.

## v0.27 — The write-set gate runs in the pipeline and its rule is corrected (P0.12, TASK-0026)

Two jobs reported `skipped` on each run, because each one had the condition
`github.event_name == 'pull_request'` and v0.23 made a push the normal trigger. The write-set gate of
v0.22 therefore never ran in the pipeline. It ran on a local computer only, by hand, one change at a
time.

A measurement of what a pull request would report found a defect in the gate itself.

**The forbid rule was wrong.** TASK-0022 wrote that a forbid beats a permit, and beats a permit from
another task, and it called the strict reading the safe one. A forbid binds the task that declares it.
`Engine.Cli/**` in the forbid list of the persistence task says that the persistence work must stay
out of the command line. It does not say that nobody may touch the command line. The rule only looked
correct because each change that tested it carried one task.

The measurement: the whole-branch difference reported 19 files and each report was false, because the
forbid list of TASK-0014, which pinned the SDK, blocked each file that TASK-0016 created. One commit
failed for the same reason: TASK-0018 modified `Engine.Cli/Cli.cs` and declared it, and the forbid
list of the deferred TASK-0019 blocked that declaration.

A permit now beats a forbid. A forbid blocks a file only when no task in the change permits it, and it
then gives the better message, because it names the boundary that an author wrote. On this branch 12
of 13 commits pass, from 11 of 13 before.

**The gate reads one commit at a time.** A task governs the commit that carries it. A difference
across many tasks cannot say which task made which change, therefore it can only compare against the
union of each write set, and the union is not the rule.

**The job runs on a push.** It needs no base reference now. The contract gate keeps its condition,
because it compares two trees and a pull request is the correct condition for that.

**History is grandfathered by one named commit.** `eng/write-set-cutoff.txt` holds the tip at the
moment the gate was turned on. The gate skips each commit behind it. One commit needs this:
`ecb1f9a`, "gitignore: ignore graphify-out", which opened the branch and touches no task file. The
exemption names one commit and covers nothing after it.

Each case was verified by injection: a file that one task forbids and another permits passes; a file
that a task forbids and no task permits fails and names the task; a file in no list fails; a change
with no task file fails.

Tests: 194, unchanged. `dotnet build` gives zero errors. Open entries: 2 of 15.

New diagnostic codes: none.

## v0.28 — The Vulkan code is first-party and uses Vortice.Vulkan 3.2.3 (R1, TASK-0027, ADR-0017)

Roadmap phase R1 is the first of the six phases that end at the first objective of the owner: create
a box, subtract a second box, and observe the cut.

**Two facts came first.** Three project files pinned `Vortice.Vulkan` 1.9.8, and nuget.org lists
3.2.3 as the highest version. The desktop host built and ran on this computer before any change: a
green window, the layer `VK_LAYER_KHRONOS_validation` active, and no validation message.

**One first-party project.** `3DEngine.Vulkan` holds the device, the swapchain and the SDL window
that owns the surface. The host referenced the vendored sample framework before, which `CLAUDE.md`
does not permit, and the dependency direction gate did not read the host. ADR-0017 gives the rules.
`Vortice.Vulkan.Sample` and `Vortice.Vulkan.SampleFramework` are removed. Each type and each method
that moved has a caller in the host. The two build warnings of the sample code are gone with it.

**The binding is 3.2.3, with one pin.** `3DEngine.Vulkan/3DEngine.Vulkan.csproj` holds the only pin of
`Vortice.Vulkan` and of `Alimer.Bindings.SDL`. Version 3.x removes each global Vulkan function: an
instance function lives on `VkInstanceApi` and a device function on `VkDeviceApi`. Commit `885ba75`
holds the port and nothing else.

**The host releases each Vulkan object at exit.** It released nothing before. The window closes with
the exit code 0 and no validation message.

**The validation layer was made to speak.** Its silence is evidence only if it can report. A render
pass that does not end gave `VUID-vkEndCommandBuffer-commandBuffer-00060`. A device that is not
destroyed gave `VUID-vkDestroyInstance-instance-00629` at the close. Each defect was removed.

**The gates.** `Engine.Tests/Governance/DependencyDirectionGateTests.cs` reads the host now, and it
adds three checks: the Vulkan layer references at most the render kernel, only a host that draws
references it, and each binding has one pin. A headless client can no longer reference the render
kernel, which ADR-0009 section 2 already forbade. `MarkerGateTests.cs` reads the render side. Seven
violations were injected, and each one failed with a message that names the project and the rule.

**The licence.** Each sample file said "See LICENSE in the repository root", which is the licence of
this repository. `THIRD-PARTY-NOTICES.md`, section 4, now gives the MIT licence of the sample author.

**Three statements of this milestone were false when first written, and each one is corrected.**

- The notice said that the first commit of the repository added the sample code. The root commit is
  `f758d1e`. The commit that added the files is `939e23f`, and both carry the subject "Initial
  commit".
- ADR-0017 said that the project owns about 600 lines. The count is 1,080. The record was corrected
  before the first push, when no person had read it. The decision did not change.
- Commit `885ba75` ticked "zero warnings". A clean build of it gives CS9191. The build that compiled
  the port ran behind a filter that failed, and the next build compiled nothing. Commit `f602022`
  removes the warning. A clean build is now the method for a warning count.

**Found in passing.** Six statements in five documents describe a repository state that no longer
exists. Register entry R-0019 holds them, with a limit of 30 days.

Not done, and why: no runner has a GPU, so the runners build the Vulkan code and never run it. macOS
needs MoltenVK, and nothing tests it. The swapchain is not created again after
`ErrorOutOfDateKHR`; the window is not resizable, and a minimized window was not tested.

New tests: 4. The test list holds 198, up from 194. `dotnet build` gives zero errors and zero
warnings on a clean build.

Opened: R-0019. Open entries: 3 of 15.

New diagnostic codes: none.
