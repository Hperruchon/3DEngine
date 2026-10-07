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

## v0.29 — The twenty objectives are in the charter (governance, TASK-0029)

The owner asked for a check of each rule against the objectives. The repository held no list of
objectives. `CLAUDE.md` cited "Objective 11" and `docs/roadmap.md` cited "Objective 15", and no file
defined either number. The owner confirmed the twenty objectives on 2026-09-20, but the list lived
only in the transcript of that session.

**The list.** `docs/CHARTER.md` now holds the section "The twenty objectives", in five groups, with
the two refinements of the owner for objectives 18 and 19. Objective 1 says "separate from work for
an employer" and does not name a product of the employer, because the repository is public.

**The gate.** `Engine.Tests/Governance/ObjectiveReferenceGateTests.cs` fails when the list is not
numbered from 1 to 20, and when a document cites an objective or an anti-objective that does not
exist. Four violations were injected, and each one failed with the file and the line.

**Two terms.** The glossary defines objective and anti-objective.

New tests: 3. The test list holds 201, up from 198. `dotnet build` gives zero errors and zero
warnings on a clean build.

Open entries: 3 of 15. New diagnostic codes: none.

## v0.30 — The codebase review of 2026-09-23 is in the repository (governance, TASK-0030)

The owner asked for a review of the code and then asked where to store it.
`docs/reviews/2026-09-23-codebase-review.md` holds it: the architecture with four diagrams, the
patterns, the defects in order of importance, the next task, the research with its sources, and a
recommended approach. `docs/INDEX.md` names the folder and states the rule: a review is a dated
record, and `CURRENT-STATE.md` stays the authority for what exists.

The review found two defects that change design truth. A replay can rebuild a different Document
after one rejected command, because `Document.Version` counts rejected events and the log does not
hold them. And queries run with no lock in the HTTP host. The review records each finding. It does
not act on one.

No code changed. Tests: 201, unchanged. Open entries: 3 of 15. New diagnostic codes: none.

## v0.31 — The codebase review is periodic (governance, TASK-0031)

The owner asked for a dated review that repeats, so that the evolution of the code stays visible.

Each review in `docs/reviews/` now has four fields: `date`, `commit`, `ledger` and `previous`. It
also has twelve measures with a method for each, and findings with stable identifiers. Each later
review must give the state of each earlier finding. `docs/templates.md`, section 7, gives the form.

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` fails the build when a field or a measure is
absent, when a finding of the previous review has no state, or when more than six milestones follow
the last review. The period counts milestones and not days, because objective 12 says that time is
irregular. The next review must come before ledger entry v0.35. Four violations were injected, and
each one failed. A complete second review passed as the positive control.

New tests: 5. The test list holds 206, up from 201. Open entries: 3 of 15. New diagnostic codes: none.

## v0.32 — The decisions of 2026-09-25 (governance, TASK-0032)

The owner answered the nine questions of the architecture challenge. This milestone records each
answer, and it changes no code.

- **Three new ADRs, accepted on the decision of the owner.** ADR-0019: the desktop host owns the
  session and serves the HTTP and WebSocket surface as a library, and `engine-api-http` becomes a
  small program around the same library. ADR-0020: the version counts applied commands, and the reset
  snapshot gains `seq` for the cursor. ADR-0021: an operation consumes its operands, and the Document
  holds the live bodies only.
- **ADR-0018 is accepted** after two corrections: both hosts call `Query<object>`, and
  `FieldSchema.Items` waits. It amends ADR-0012 only.
- **Five ADRs are now `Amended`:** ADR-0006, ADR-0008 and ADR-0010 by ADR-0020, ADR-0011 by ADR-0019,
  and ADR-0012 by ADR-0018 and ADR-0021. ADR-0005 does not change, because it does not define the
  version.
- **Anti-objective 2** keeps the drag rule of ADR-0007: the preview stays in the client, and one
  command goes to the log on release.
- **Six ready tasks.** TASK-0034 (one serial boundary), TASK-0035 (the version), TASK-0036 (a host
  refuses to start without the native backend), TASK-0028 (R2, now `Ready`), TASK-0037 (operand
  consumption) and TASK-0038 (the hybrid topology). The roadmap gives their order.
- **A gap in the write-set gate.** A commit that touches a task file gets all its permits. Register
  entry R-0026 records the risk, which an injection found.
- **The architecture challenge** is in `docs/reviews/2026-09-23-architecture-challenge.md`, with the
  answers of the owner in its last section.

Nothing new exists in the code. `CLAUDE.md` gives the hybrid topology as the target, not as the
state.

New tests: 0. The test list holds 206. Open entries: 10 of 15. New diagnostic codes: none. The next
codebase review must come before ledger entry v0.35.

## v0.33 — The web shell is removed (governance, TASK-0033)

The owner said on 2026-09-25 that a browser client is not the main idea. `BlazorApp` and
`BlazorApp.Client` were a template that made no call to the engine, so this milestone deletes them.

- The solution holds no Blazor project. The two entries in `3DEngine.sln` are gone.
- ADR-0003 has the status `Withdrawn`, with the reason in its field `notes`. The legend and
  `docs/templates.md` now permit the owner to withdraw an accepted record that no later record
  replaces.
- The budget of unenforced ADRs falls from 2 to 1, because ADR-0003 was one of the two.
- `CLAUDE.md`, `docs/INDEX.md`, the pull request template and the notes of `3DEngine.Core` name no
  Blazor project.

Register entry R-0021 holds the open question of how to show the application.

New tests: 0. The test list holds 206. Open entries: 10 of 15. New diagnostic codes: none. The next
codebase review must come before ledger entry v0.35.

## v0.34 — The rules are fewer, each one has one home, and ten more have a gate (governance, TASK-0039)

The rules review of 2026-09-25, `docs/reviews/2026-09-25-rules-review.md`, examined each rule and
the organization of the repository. It found 124 rules in thirteen files, seventy of them in text
only, fourteen statements about a repository that no longer existed, five rules that the practice
contradicted, and eighteen files that a session read to find its position. The owner agreed with
the proposal on 2026-09-26 and gave four directions: show the organization, change the rules that
contradict the practice, clean each statement that does not need to exist, and keep each ADR in
agreement with the code. This milestone does the first three. TASK-0040 holds the fourth.

**Three files are gone.** `docs/working-agreement.md`, `docs/conventions.md` and
`docs/open-questions.md`. Each live rule moved to `CLAUDE.md` or to `docs/templates.md`, and each
rule that repeated a gate or another file went. The rationale of the working agreement, with its
reasons from the Blender, FreeCAD and Ondsel projects, is in `docs/archive/`. `CLAUDE.md` is a
position file of 150 lines. The six determinism rules are unchanged. Section 4.0 of the review
gives one home for each topic that lived in two or more places.

**Five rules changed, and each one keeps its purpose.** A session runs the tests before each commit,
and the gate on three operating systems is the proof. A false statement about the repository is an
error that the task corrects; a decision that the code contradicts is a question for the owner. A
diagnostic code is never removed and never changes meaning, and a row may move. An accepted ADR
agrees with the code, with a section "History" and the acceptance of the owner for each change to a
Decision (`docs/templates.md`, section 1). Each push is green, and the owner merges when convenient.

**Four new gates.** `AdrEnforcementExistsGateTests` fails when an ADR in force names an `enforced-by`
file that does not exist, unless a Ready task creates it. `TaskGovernanceGateTests` fails when
`governed-by` of a Ready task differs from the ADRs whose `affects` intersect its write set, or when
`depends-on` names no task. `DeterminismCallGateTests` fails when a source in the log path calls a
transcendental function, `FusedMultiplyAdd` or a function whose guarantee covers one process, or when
a project file names a 32-bit runtime identifier. `DocumentPathGateTests` fails when a path in
`docs/INDEX.md` does not exist or a gate class has no row in the gate table.

**Two gates changed.** The write-set gate reads the task that the commit trailer names, a line of the
form `TASK-nnnn`, and only that task governs the commit; a commit with no trailer that touches several
task files fails. This closes register entry R-0026. The contract gate runs on each push, against the
previous tip, and not on a pull request only; it had never checked a merge into `main`.

**Five gates read a scan instead of a fixed list.** The dispatch gate reads each host source. The
diagnostics scanner reads each `Engine.*` project, and `Engine.Api.Http` raises three codes that it
did not read before. The schema gate reads `HandlerCatalog`. The dependency gate fails on a project
that is in no class. The marker gate reads each project that has a project file.

**Sixteen injections, and each one failed.** An ADR with a missing file and an ADR with a closed task;
a Ready task with an extra ADR, a missing ADR and an absent dependency; `Math.Pow` in `Engine.Core`
and `win-x86` in a project file; a host file with a quoted command name, a code in `Engine.Api.Http`,
a project in no class, a `TODO` in that project, and a quoted query name in the schema endpoint; the
change list of v0.32 plus `Engine.Core/CommandBus.cs`, with TASK-0032 named and with no task named;
a path that does not exist in the map, and the gate table without one class. The old fixed lists
passed four of the five scan injections. Each commit after the cut-off passes the trailer rule; a
prose line of the v0.27 message that starts with an identifier made the first form of the rule fail,
and the trailer form corrected it.

**Fourteen stale statements are corrected or removed.** Register entry R-0019 is closed. Two pointer
lines of `docs/CHARTER.md` named the deleted working agreement and now point at `CLAUDE.md`; no
objective, anti-objective or non-goal changed. The task moved the charter from `forbid` to `modify`
for those two lines after the write-set gate refused the commit.

**One status vocabulary.** A task is `Ready`, `Active`, `Done` or `Deferred`. The roadmap loses its
Status column and keeps the Shipped list. The glossary defines `V1` and `V1.x`, which fifteen ADRs
and the charter use.

**The measures**, with the method of the review, section 1, so that the next review can repeat them:

| Measure | Before | After | Method |
|---|---|---|---|
| Rule files | 13 | 10 | The files with rules, without the ledger and the three CI files. |
| Lines in the rule files and the CI files | 2,971 | 2,688 | `wc -l` on the same sixteen files, before this entry. |
| Sentences with "must" | 52 | 40 | `grep -o -w -i must` on the rule files. |
| Sentences with "do not" | 80 | 46 | `grep -o -i -E '\bdo not\b'` on the rule files. |
| Rules in the inventory | 124 | 93 | Section 2 of the review, less the rows marked deleted. |
| Rules that a gate enforces in full | 30 | 30 | Ten deleted rows had a gate; ten rows gained one. |
| Rules in text only | 70 | 48 | The rows with `none` that stay. Twelve are the charter's. |
| Files that a session reads to find its position | 18 | 17 | The method of the review, section 1, for TASK-0034. |
| Gate classes | 13 | 17 | Test classes whose name ends in `GateTests`. |
| Open register entries | 10 of 15 | 8 of 15 | Section "Open" of `docs/register.md`. |

**Not done, and why.** The folds of the seven amended ADRs are TASK-0040, because each one changes a
Decision that the owner accepts. `CLAUDE.md` has 150 lines and not the 120 of the estimate, because
the diagram and the six determinism rules take forty lines that stay. A gate for a contraction in a
document was offered and not asked for.

New tests: 8. The test list holds 214, up from 206. `dotnet build 3DEngine.sln --no-incremental`
gives zero errors and zero warnings. Closed: R-0019, R-0026. Open entries: 8 of 15. New diagnostic
codes: none.

**The next ledger entry, v0.35, must be a codebase review.** Six milestones follow v0.28, which the
last review examined, and the limit of `CodebaseReviewGateTests` is six.

**Added on 2026-09-26, after the push and before the merge, in the same milestone (TASK-0041).** The
rules that earlier sessions broke, and that the owner then put in the prompt of each session, were
not in `CLAUDE.md`: read a file before you cite it, prove a gate by injection, `set -o pipefail`, an
absent tool is not evidence, Node.js and not Python, a clean build for a warning count, and the
commit trailer. One section "Method" now holds them, with the write-set command, and it absorbs the
sections "Tests" and "Rules with no other home". `CLAUDE.md` has 165 lines. This paragraph is in the
entry v0.34 and not in a new entry, because the branch is unmerged and the next entry must be the
codebase review.

## v0.35 — The codebase review of 2026-09-30 (governance, TASK-0042)

`docs/reviews/2026-09-30-codebase-review.md` is the second codebase review. It examines `main` at
`14e2c73`, the merge of v0.32 to v0.34. The review changes no code.

**No production source changed after the first review.** The difference between the two reviewed
commits in the eight production projects is one documentation file. Each engine finding and each
Vulkan finding of the first review is open. One earlier finding is fixed: the contract gate ran on a
push to `main`, in run 36443245386, where each of the five jobs gave a success.

**Thirty-three findings are open: two critical, twelve high, fourteen medium and five low.** Nineteen
are new. Four review agents read the code in parallel, and a run or a reading on disk checked each
finding that the review gives as observed.

- The query defect is a measured failure. With commands on one task and reads on four tasks, about
  4,500 queries and about 4,000 snapshot copies threw an exception in each of three runs.
- An idle subscriber receives 1,025 heartbeat frames at second 30, and not one frame.
- A replay that rejects a command returns an empty Document and reports nothing.
- A subscriber that connects during a commit can lose an event or receive it two times.
- The host accepts a WebSocket upgrade with a foreign Origin, and no source checks the bind address.
- Eight injected violations pass their gate. One of them is a reference from `Engine.Core` to
  `3DEngine.Core`. TASK-0039 wrote or changed nine of the gates two days before.

**Five register entries are new.** R-0027 holds six findings of the first review that had no owner.
R-0028 holds the new engine and host findings. R-0029 holds the heartbeat loop, with a limit of 30
days. R-0030 holds the Vulkan findings. R-0031 holds the holes in the gates.

**The roadmap is corrected.** It said that the path to the first objective takes 25 to 35 evenings.
The sum of its column "Evenings" for the ten phases that remain is 32 to 48. The sentence predates
the five phases that v0.32 added.

**The next task is TASK-0034.** The review gives the research and an approach in seven steps. It
recommends one change beyond the task text: the commit changes the Document first and publishes the
events second, so that a sink that fails cannot leave a partial commit. The owner decides that in
question Q1 of the review.

Not done, and why: no finding is corrected, because a review records and a task corrects. Two host
findings and six small findings are given as reported by a review agent and not checked. The
heartbeat and the handshake findings were not run on a second computer.

New tests: 0. The test list holds 214. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Opened: R-0027, R-0028, R-0029, R-0030, R-0031. Open entries: 13 of 15. New
diagnostic codes: none. The next codebase review must come before ledger entry v0.41.

## v0.36 — Each injected violation of the second review fails its gate (governance, TASK-0043)

The codebase review of 2026-09-30 injected eight violations that passed their gate, findings T3 to
T6. The owner answered yes to its five questions. This milestone closes the gate holes and records
the three decisions that need a file. No engine code changed.

**The gates.** The dependency gate reads each project file as XML and takes each path of each
`Include`; a reference from `Engine.Core` to `3DEngine.Core` inside an `Include` with two paths now
fails. The write-set gate checks a merge commit on the changes that the merge made itself, requires
that a commit changes the file of the task that it names, requires that a gate file is named exactly
and not by a pattern, and holds the cut-off commit in a test. The determinism gate reads each source
with its comments removed, so a call with the parenthesis on the next line is seen and a comment
that names a function is not; it also refuses the same functions on `double`, `float` and `Half`,
and a static import of `System.Math`. The marker gate refuses an identifier that is not in the
register. The task gate refuses a status outside the set and an identifier that differs from the
file name. The register gate compares the entries that it parsed with the headings of the section,
so an open code fence cannot hide an entry. The two small jobs of the workflow use `bash` with
`pipefail`, and each git command is tested.

**Each injection was repeated.** Two paths in one `Include`; `double.Pow`, a static import and a
call with the parenthesis on the next line; a marker with an identifier that no register holds; a
status `ready`; a register with an open fence; a gate file changed under a task that permits
`Engine.Tests/**`; a commit that names a task and does not change it; a moved cut-off. Each one
passed before the change and fails after it. A comment that names a function passes, as the
control. A merge with a change of its own, made in a throwaway clone, lists that change under
`git diff-tree --cc` and nothing under the plain form. Each commit after the cut-off still passes
the write-set rule.

**The decisions.** TASK-0034 gains the commit order, change the Document first and publish second,
and three tests for the findings E9 and E10 (question Q1). `docs/diagnostics.md` gives the close
status 1007 for an invalid subscribe frame; the code sent it from the start, and TASK-0010 said 1003
(question Q3). TASK-0044 opens for the heartbeat loop and the three host checks, before TASK-0038
mounts the surface in the desktop host (question Q5). Register entry R-0027 stays as it is (question
Q2), and the gate work comes before TASK-0034 (question Q4).

Not done, and why: the parts of finding T5 that need a new parser, an `affects` value with quotes,
the reserve table heading and the codebase review gate, and the low findings of T7. Register entry
R-0031 stays open for them, with its limit of 2026-12-24.

New tests: 3. The test list holds 217, up from 214. `dotnet build 3DEngine.sln --no-incremental`
gives zero errors and zero warnings. Open entries: 13 of 15. New diagnostic codes: none. The next
codebase review must come before ledger entry v0.41.

## v0.37 — The host binds to a loopback address only, checks the Origin and the Host, and sends one heartbeat in each interval (TASK-0044)

Findings E8 and E15 of the codebase review of 2026-09-30, and question Q5, which the owner answered
yes. This is the first change to host code since v0.28.

**One heartbeat in each interval.** The loop wrote a frame and started again with no wait, so one
idle subscriber received 1,025 frames at second 30. The loop now moves the time of the last send
when it writes a frame (`Engine.Api.Http/WebSockets/Subscriber.cs`). The run of the review on the
real host, one idle subscriber for 36 seconds, gives 1 heartbeat frame at second 30.
Register entry R-0029 is closed.

**A loopback address only.** `engine-api-http` refuses to start on an address whose host is not a
loopback address, with exit code 1 and a message, before it builds the application. The framework
default, with no address given, is localhost and passes. `docs/CHARTER.md` said this for months,
and no source checked it.

**A foreign Origin gets 403.** A WebSocket upgrade with an Origin header that names a host outside
the list in `SubscriberOptions` is refused before the upgrade. The list holds the three loopback
names. A request with no Origin header passes, because a script and an agent send none. The value
"null" is refused.

**A foreign Host gets 400.** The host filtering middleware of the framework accepts the same three
names. A page that resolves its own name to the loopback address cannot post a command.

**Each test failed first.** `Engine.Tests/Http/HeartbeatTests.cs` counted a burst.
`Engine.Tests/Http/HostGuardTests.cs` saw 101 for the foreign Origin, 200 for the foreign Host, and a
process that ran on 0.0.0.0 until the test stopped it. After the change each one passes, and a run
on the real host gives 403, 400, and a refusal with exit code 1.

Not done, and why: the close frame after a cancelled token (finding E17) stays in R-0028, and the
Host filter is not tested with the IPv6 literal.

New tests: 16. The test list holds 233, up from 217. `dotnet build 3DEngine.sln --no-incremental`
gives zero errors and zero warnings. Closed: R-0029. Open entries: 12 of 15. New diagnostic codes:
none. The next codebase review must come before ledger entry v0.41.

## v0.38 — The heartbeat test measures the gap between frames (TASK-0044, correction)

The merge of v0.37 into `main` gave a red run: the Ubuntu job failed in its test step, while the
same content passed on the three runners of the branch minutes before, and the suite passed six
times in a row on this computer. The log of the job needs a sign-in, so the failed test has no name
here. The test step ran its normal eight seconds, so one test failed and the suite did not stop.

The first form of the heartbeat test counted the frames of one second and expected three to eleven.
A loaded runner can make that count low. The test now takes the moment of three consecutive
heartbeat frames and requires that each gap is at least the interval: a slow runner makes each gap
longer and never shorter, and the defect of finding E8 makes each gap close to zero. With the wait
line removed for one run, the test fails with the gaps that it saw. The two tests that spawn the real
host share one collection with the heartbeat test, so that a process start does not run beside a
measurement of time.

Not done, and why: the name of the failed test on Ubuntu stays unknown, because the log needs a
sign-in that this computer does not have. If the next run on Ubuntu fails again, the log names it.

New tests: 0. The test list holds 233. `dotnet build 3DEngine.sln --no-incremental` gives zero
errors and zero warnings. Open entries: 12 of 15. New diagnostic codes: none. The next codebase
review must come before ledger entry v0.41.
## v0.39 — Commands, queries and reads of the Document pass through one serial boundary (TASK-0034)

Finding E2 of the codebase review of 2026-09-23, findings E4, E9 and E10 of the review of 2026-09-30,
and question Q1, which the owner answered yes. This is the first code task since 2026-09-23.

**One document session.** `Engine.Core/DocumentSession.cs` owns the Document, its backend, its
command bus, its query bus and its event sink. A command, a query and a read of the Document enter one
serial section. The probe of the review is the first test, with 5,000 commands and four readers. On
the code before the session, three runs gave 1,021, 1,152 and 950 queries that threw
`InvalidOperationException`, and 839, 920 and 849 snapshot copies that threw `ArgumentException`.
With the session, the test passes 100 runs of 100.

**Both hosts use it.** The command line and the HTTP host send each command, each query and each
read of the Document through the session. The registries do not change after start, and a host reads
them with no lock.
The WebSocket handshake enters the session first and the broadcaster second, the same order as a
commit. On the code before the change, three runs gave 190, 201 and 209 answers with HTTP 500, and
in one run of three the first live `seq` after a reset was the snapshot version plus two. With the
opposite lock order injected into the handshake, the deadlock test stops at its limit of 11 s.

**Change, then publish.** The commit computes each sequence number, appends the command, adds each
body, advances the version and stores the result in the cache. Then it appends the events with
`CancellationToken.None`. A rejection and a cancellation use the same order. A cancellation after
the log append and a sink that throws now leave the log, the bodies and the version in agreement. The
sink loses an event, which a subscriber recovers with a reset, and a retry gets the cached result.

**One bus for each Document.** The bus refuses a second bus on the same Document, and it refuses to
move the version back.

Not done, and why: scope item 8 asks that `Document.AdvanceVersion` refuses a lower value. That
method is in `Engine.Contracts/Document.cs`, which the task forbids, and TASK-0035 modifies that file.
The bus, the one caller, refuses in its place. `docs/INDEX.md` lists the parts of `Engine.Core` and
does not name the session yet; the file was outside the write set. `docs/register.md` joined the
write set of the task, because the owner asked for the progress line in R-0028.

The first run of the pipeline on the code commits, 4545f75, failed in the test step on Windows, and
passed on Ubuntu and macOS. The log needs a sign-in, so the failed test has no name here. Here the
suite passed 30 runs, 10 runs at full load on each core, 12 runs with a smaller thread pool and 10
runs on two cores by affinity, with no failure. Register entry R-0032 asks for the name of a failed
test in a public annotation.

New tests: 9. The test list holds 242, up from 233. `dotnet build 3DEngine.sln --no-incremental`
gives zero errors and zero warnings. Open entries: 13 of 15, with the new entry R-0032. New diagnostic
codes: none. The next codebase review must come before ledger entry v0.41.
## v0.40 — A failed test in the pipeline has a public name, and two tests of v0.39 tolerate a slow runner (TASK-0045, TASK-0034 correction)

Register entry R-0032. The log of a job needs admin rights, and the annotations of a check run are
public. Two red runs, on Ubuntu for v0.38 and on Windows for v0.39, had no test name that a reader
without a sign-in could see.

**The name of each failed test.** The step "Test" of the pipeline writes a `.trx` file. When
`dotnet test` fails, `eng/report-failed-tests.js` writes the name, the message and six lines of the
stack of each failed test as an annotation, with a maximum of ten. On the branch
`r0032-injection-2026-10-03`, which is never merged, an injected failure gave its name on each of
the three runners through the check-run API with no sign-in. The first form of the script found no
failed test, because its pattern read past the empty element of a passed test; the injection on this
computer found that.

**The failure of v0.39 has a name.** The same run on Windows named a real failure:
`HttpConcurrencyTests.Queries_And_Commands_In_Parallel_Get_No_Http_500`, with
`TaskCanceledException`. It did not repeat here. Its four readers turned in a loop with no await
until the first body existed, which can starve the server on a runner with few cores. The readers now
start after the first command; this cause is inferred and not observed. On one core here, the E9 test
of v0.39 failed 3 times: the client stopped reading after its measurement, its queue filled, and the
server disconnected it as a slow subscriber, as ADR-0005 §6 requires. Its close then found a closed
socket. The close after a measurement now accepts that. The full suite on one core failed 1 time in
25 runs before the change and 0 times in 40 runs after it. The HTTP tests alone on one core
passed 80 runs of 80 after it.

Not done, and why: the cause of the Ubuntu failure of v0.38 stays unknown, because its log needs a
sign-in. The branch `r0032-injection-2026-10-03` stays on the server until the owner deletes it.

New tests: 0. The test list holds 242. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Closed: R-0032. Open entries: 12 of 15. New diagnostic codes: none. The next
codebase review must come before ledger entry v0.41.
## v0.41 — The codebase review of 2026-10-04 (governance, TASK-0046)

The third codebase review, `docs/reviews/2026-10-04-codebase-review.md`. It examines `main` at
`ae5c521`, the merge of v0.39 and v0.40. The gate permitted six milestones after v0.34, and v0.40
was the sixth, so this entry had to be the review. On 2026-10-04 the owner chose the review and not a
change of the rule.

**What it found.** Forty findings are open: one critical, eleven high, twenty-one medium and seven
low. Six earlier findings are fixed: E2, E4, E8, E9, E15 and T6. Thirteen findings are new. The most
important new ones:

- E23: the bind guard reads the key `urls` only. An address given through `Kestrel:Endpoints`
  passes with no refusal. A run on this computer shows it.
- T8: the pipeline has no time limit, so a test that hangs holds a runner for six hours and gives no
  name.
- T9: the project file of the tests is not a gate file, so a task that permits `Engine.Tests/**` can
  remove each gate, and a `Directory.Build.props` file gets past the dependency gate and the x86
  gate. Runs on this computer show both.
- E18 to E21: the session of TASK-0034 has three latent defects, a reference that can leave it, a
  call back that blocks it, and a sink that leaves a gap, and a handler that fails after it changed
  the backend leaves an orphan solid.

**Method.** No file changed in `3DEngine/`, `3DEngine.Vulkan/`, `3DEngine.Core/` or
`Engine.Contracts/` after the last review, so three review agents read the code, and not four. I read
each cited line on disk, and I repeated the runs of E23 and T9. The first draft cited three lines
wrongly and said that two red runs had the shape of T12; the review corrects both before this entry.

**Owners.** R-0033 holds E18 to E24, and R-0034 holds T8, T12 and two parts of T13. R-0031 now also
holds T9, T10, T11 and the other parts of T13. R-0027 and R-0028 have a progress line. The roadmap
lists P0.13 as shipped, and the estimate is 30 to 45 evenings for the nine phases that remain.

**Questions for the owner.** Four, each with a recommendation: add the replay findings E7 and E17 to
TASK-0035; correct T8 and T9 before TASK-0035; refuse a commit that names a task that is `Done`; and
the rule for a sink that throws.

New tests: 0. The test list holds 242. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. New register entries: R-0033, R-0034. Open entries: 14 of 15. New diagnostic codes:
none. The next codebase review must come before ledger entry v0.47.
## v0.42 — A hang stops with a name, no file outside the exact write set can turn a gate off, and a closed task governs no commit (governance, TASK-0047)

The owner answered "yes to all" to the four questions of the codebase review of 2026-10-04. This
task does the governance part: findings T8 and T9 and question Q3. TASK-0035 records Q1, register
entry R-0033 records Q4, and TASK-0035 now depends on this task, as Q2 asked.

**A hang stops with a name (T8).** Each job has `timeout-minutes`, and the step "Test" stops a test
after two minutes with `--blame-hang-timeout`. A test that hangs gives no result in the `.trx`
file, so `eng/report-failed-tests.js` now reads the sequence file of the blame collector, where the
test has `Completed="False"`. On a branch that is never merged, an injected hang stopped after about
130 seconds on each runner, and a public annotation named it.

**No file outside the exact write set can turn a gate off (T9).** The project file of the tests, each
`Directory.Build` and `Directory.Packages` file, the cut-off and each workflow are gate files. The
dependency gate reads each `Directory.Build` file above a project and each file that a project
imports, and the x86 gate reads each `.csproj`, `.props` and `.targets` file. Three injections
passed before the change and fail after it.

**A closed task governs no commit (Q3).** The gate reads the governing task at the commit, which the
workflow now gives in `WRITE_SET_COMMIT`, and refuses a task that is `Done` before the commit and
after it. A correction reopens the task with the status `Active`. Each commit up to `67c564f` keeps
the earlier rule, because three earlier commits changed a task that was `Done`; a replay of the 51
commits after the cut-off passes. `docs/templates.md`, section 3, gives the rule.

Not done, and why: the other gate findings of the review stay in R-0031 and R-0034. Two injection
scripts of this task were wrong first: one wrote a control character into a props file, and one lost
a backslash in a heredoc. Each was found before its result entered a record.

New tests: 0. The test list holds 242. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next codebase review must
come before ledger entry v0.47.
## v0.43 — The document version counts applied commands, and a replay rebuilds it (TASK-0035)

ADR-0020, finding E1 of the codebase review of 2026-09-23, and findings E7, E10 and the replay part of
E17 of the review of 2026-10-04, which the owner added to the task (question Q1).

**The version is the count of the log.** `Document.Version` equals the count of entries in
`Document.Log`, by construction. `Document.AdvanceVersion` is gone, so no code can set the version,
higher or lower, which closes finding E10. A rejection and a cancellation do not change it, and their
event still takes the next `Seq`. The bus keeps `Seq` alone, and no code computes one counter from
the other.

**The reset snapshot gives the cursor.** The snapshot of a `subscription.reset` gains the field
`seq`, the highest `Seq` at the reset, and a client continues from `seq + 1`. The E9 test of v0.39
reads it, and the reset test holds a rejection, so its snapshot gives version 5 and seq 6.

**A replay reports a divergence.** `Replay.ReplayLog` stops at the first result that is not
`Applied` with a `ReplayDivergenceException`, which names the entry, the command and the error. It
applies each entry with no idempotency cache.

**Each test failed first.** On the code before the change: the replay fixture, where `Beta` now
states the version 2, gave version 5 where 4 is expected; a live session with a rejection gave
version 4, and its replay gave 2 with the box lost; a rejected command reported version 2 where 1 is
expected; a replay of a rejection threw nothing; and a log with one `CommandId` two times replayed to
one entry.

Not done, and why: the backend argument of `ReplayLog` stays optional; finding E7 recommended to make
it necessary, and question Q1 did not include that part. The other findings of R-0028 stay.

New tests: 4. The test list holds 246, up from 242. `dotnet build 3DEngine.sln --no-incremental`
gives zero errors and zero warnings. Open entries: 14 of 15. New diagnostic codes: none. The next
codebase review must come before ledger entry v0.47.
## v0.44 — A host refuses to start without the native backend, and a native test cannot skip in the pipeline (TASK-0036)

Findings E3 and T1 of the codebase review of 2026-09-23, and anti-objective 9, which refuses a silent
fallback when a capability is absent.

**Each host requires the native backend.** When the library `manifoldc` does not load, `Engine.Cli`
writes `E-GEOM-BACKEND-INIT` with a sentence that names the library, Manifold 3.5.2 and the platform,
and stops with exit code 3 before the command. `engine-api-http` builds its engine at the start, writes
the same, and stops with exit code 3 before it listens. Until now each host took the managed backend
with no message, and that backend holds boxes only. A test selects the managed backend with an option
record, `BackendOptions` for the command line and `HostBackendOptions` in the service container of
the HTTP host. No argument and no setting does.

**The active backend in /schema.** `GET /schema/backend` gives the name and the version of the
backend that the host composed, `manifold` and `3.5.2`. A test holds the version equal to the
package that the project file pins.

**A native test cannot skip in the pipeline.** GitHub Actions sets `CI` to `true` on each runner,
and with that value the attribute of the native tests no longer skips. A smoke test applies two
boxes, `Translate` and `Subtract` on the native backend through a session.

**Each result was seen before and after.** With the native library hidden from the output folders,
before the change: the command line applied a box with exit code 0, the HTTP host listened, and with
`CI=true` the 13 native tests skipped and the run passed. After it: exit code 3 with the message for
each host, and with `CI=true` 7 native tests failed and none skipped.

**The native backend had never loaded on Linux and macOS.** The first pipeline run of this task was red
on both: the hosts stopped with `E-GEOM-BACKEND-INIT`. The package stores the build folder of a
runner, such as `/home/runner/work/3DEngine/3DEngine/build/src`, as the search path of `libmanifold`
in `libmanifoldc`, and not `$ORIGIN` or `@loader_path`, so the system loader did not find the
dependency. Until now the native tests skipped there, the hosts took the managed backend, and each run
was green. The backend now loads the dependency first by its full path, and the run after it was green
on the three runners with no native test skipped. Register entry R-0035 holds the rebuild of the
package, which is the correct end state.

**The registry.** `E-GEOM-BACKEND-INIT` moved from the reserved codes to the raised codes of
`docs/diagnostics.md`, with the same name and meaning. The reserve holds one code, and the budget of
`DiagnosticsReserveGateTests` is one; with the reserved row put back, the gate failed. The three places
that read ADR-0014 as a rule for a fallback agree with the code, and the comment of `EngineHost.cs`
that cited the clamp removed in v0.17 is corrected (a part of finding C1).

Not done, and why: a platform with no payload, such as `win-arm64`, `linux-arm64` or `osx-x64`, now
cannot start a host. That is the honest result until register entry R-0020 decides the platforms.

New tests: 12. The test list holds 258, up from 246. `dotnet build 3DEngine.sln --no-incremental`
gives zero errors and zero warnings. New register entry: R-0035. Open entries: 15 of 15, the limit.
New diagnostic codes: none; one code moved from reserved to raised. The next codebase review must come before ledger entry v0.47.
## v0.45 — The native package finds its own dependency on each platform, names its source, and a commit rebuilds it (governance, TASK-0048)

Register entries R-0035 and R-0018, and the decision of the owner of 2026-10-06 that the pack job may
push the package to a branch (question Q1 of TASK-0048).

**The package 3.5.2.1.** It replaces 3.5.2, from the same Manifold source, `v3.5.2`. On Linux and
macOS each library now finds its dependency in its own folder: the build gives `$ORIGIN` and
`@loader_path`, and on macOS a step removes the absolute search path and signs the library again.
The nuspec records the Manifold address and commit `11235e6b…`, and no commit or branch of this
repository. The workaround of TASK-0036 is removed, and the pipeline passed on the three runners with
no native test skipped.

**A rebuild needs no manual step.** A push that changes `eng/manifold-native/manifold-ref.txt` or the
workflow starts the build, and the pack job pushes the package to `native-package/<version>`, where git
fetches it with no sign-in. Only the pack job can write; the jobs that compile the third-party source
read only. A check in the build job fails on a path of a build machine, and it writes each failure as
a public annotation.

**Each step failed first, or was found.** The first form, with no flag, failed its check on Linux and
macOS as planned. The install flags then had no effect, because Manifold's own `CMakeLists.txt` sets
those variables; the build flags of the build tree worked on Linux, and macOS needed the step after
the staging. The first package also named a branch of this repository; a local pack of one minute found
the flag that stops it. Two gate tests failed on 3.5.2 and pass on 3.5.2.1.

**On the way.** The polls of this evening used up the API limit of 60 requests an hour once. The rule
is now: the first check at the normal end of a run, then every 3 minutes. The owner decided that a
local build of the native libraries is for development and test, and the official package comes from
GitHub; a later task makes the shared build script and the local build.

New tests: 2. The test list holds 260, up from 258. `dotnet build 3DEngine.sln --no-incremental` gives
zero errors and zero warnings. Closed: R-0018, R-0035. Open entries: 13 of 15. New diagnostic codes:
none. The next codebase review must come before ledger entry v0.47.
## v0.46 — One build script makes the native libraries in the pipeline and on this computer (governance, TASK-0049)

The owner decided on 2026-10-06 that a local build of the native libraries is for development and test,
and that the official package comes from GitHub. On 2026-10-08 the owner permitted the merge of each
task of the night after a green run on the three runners.

**One recipe.** `eng/manifold-native/build.sh` builds, stages and checks Linux and macOS, and
`build.ps1` builds Windows. The workflow calls the two scripts and keeps no copy of their steps, and
`eng/manifold-native/README.md` gives the local commands. Run 37696665835 built and checked with them
on the three runners.

**A version is immutable, and only main publishes.** The pack job pushes nothing when the branch of a
version exists, and nothing from a branch other than `main`. A tag push starts no run. The token of the
workflow stays off the disk, and the pin values pass strict forms before they reach a script.

**Found on the way.** A local run on this computer stopped at the link: the Windows SDK is absent, so
`kernel32.lib` cannot be found. The review agents of the night found five defects in the new code
before its merge, and this task corrected them: a second build into one staging folder, a stop of
PowerShell 5.1 at a CMake warning, a branch that could claim a version, the token on disk, and unchecked
pin values.

Not done, and why: the local Windows build waits for the Windows SDK, and the local Linux build for an
Ubuntu distribution in WSL. Both are installs of the owner.

New tests: 0. The test list holds 260. `dotnet build 3DEngine.sln --no-incremental` gives zero errors
and zero warnings. Open entries: 13 of 15. New diagnostic codes: none. The next codebase review must come
before ledger entry v0.47.
