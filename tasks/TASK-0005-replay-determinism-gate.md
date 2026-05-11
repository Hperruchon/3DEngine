# TASK-0005 — Replay determinism CI gate (P4)

## Status

Ready

## Context

CLAUDE.md lists what "the gate" runs: `dotnet build`, `dotnet test`, dependency direction check, diagnostic codes registered, **replay determinism fixture**. TASK-0003 shipped the third gate (diagnostics). This task ships the fifth (replay determinism).

The existing `Engine.Tests/ReplayTests.cs` validates replay programmatically: it runs a sequence of commands twice (once through a live `CommandBus`, once through `Replay.ReplayLog`) and asserts the two produce equal observable state. That catches non-determinism within a single test run but not drift: if a future change alters event `Kind` strings, `Seq` numbering, or `Document.Version` semantics, the existing test recomputes both sides at the same time and silently passes.

This task adds the stable baseline: a hand-authored fixture (specific commands with hard-coded `CommandId` GUIDs, paired with hand-authored expected `Document` + event-sequence values) checked into the repo. The gate test replays the fixture and asserts against the snapshot. If a change in `CommandBus`, `Replay`, `Document`, or `InMemoryEventSink` causes the replay to diverge from the snapshot, `dotnet test` fails; the author either accepts the change by updating the fixture in the same PR, or reverts.

Per ADR-0005, replay determinism is asserted **modulo** `EventRecord.Timestamp` and `EventRecord.DocumentId` (and `Document.DocumentId`, `Document.CreatedAt`, `Document.UpdatedAt`). The gate excludes these explicitly.

## Goal

Add a gate test in `Engine.Tests/ReplayDeterminism/` that:

- Constructs a hand-authored fixture: a sequence of `NoOpCommand` instances with hard-coded `CommandId` GUIDs and `Echo` strings.
- Replays the fixture via `Replay.ReplayLog` and asserts the resulting `Document.Version`, `Document.Log` order/contents, and `EventRecord` sequence (`Seq`, `Kind`, `CauseCommandId`) match a hand-authored expected snapshot.
- A second test runs `Replay.ReplayLog` twice against the same fixture and asserts the two replays produce identical observable state (modulo `Timestamp`/`DocumentId`).

No production code changes. No new diagnostic codes.

## Scope (in)

1. **Fixture (test-internal)**
   - `Engine.Tests/ReplayDeterminism/ReplayDeterminismFixture.cs` — static class with:
     - `Commands` — three `NoOpCommand`s with hard-coded `CommandId` GUIDs (`11…`, `22…`, `33…`) and Echo values (`alpha`, `beta`, `gamma`).
     - `ExpectedDocumentVersion = 3`.
     - `ExpectedEvents` — three `ExpectedEvent(Seq, Kind, CauseCommandId)` records, all `Kind = "command.applied"`.

2. **Gate tests**
   - `Engine.Tests/ReplayDeterminism/ReplayDeterminismGateTests.cs`:
     - `Replay_Of_Fixture_Matches_Expected_Document_And_Event_Sequence` — runs `Replay.ReplayLog(fixture)`, asserts every observable in the snapshot. Excludes `Timestamp` and `DocumentId`.
     - `Two_Replays_Of_Fixture_Produce_Identical_Observable_State` — runs replay twice with fresh registries, asserts the two `Document` + event sequences match each other (modulo `Timestamp`/`DocumentId`).

3. **Documentation**
   - `docs/CURRENT-STATE.md` — v0.5 entry referencing this task.
   - `docs/roadmap.md` — move P4 from Pending V1 to Shipped.

## Scope (out)

- Any change to `Engine.Contracts/**`, `Engine.Core/**`, `Engine.Cli/**`. The four replay-relevant types (`Replay`, `CommandBus`, `Document`, `InMemoryEventSink`) are unchanged.
- Any new diagnostic code.
- External fixture files (JSON, YAML, etc.). The fixture is in-code: command serialization is deferred to the wire-format task (ADR-0008 §9), and inventing a one-off format for this gate is unnecessary.
- Snapshotting `EventRecord.Payload`. Payload is `object?` (a dict) — a Kind mismatch already implies an event-structure change. Adding payload assertions is low-value here.
- Rejected/cancelled-command paths in the fixture. The success path is what this gate locks in; reject/cancel are covered by `CommandBusTests`.
- Moving or replacing `Engine.Tests/ReplayTests.cs`. It continues to verify the `Replay` class directly; the new gate is additive.
- GitHub Actions workflow. P5 covers process surfaces; this gate runs under `dotnet test`.

## Inputs

- CLAUDE.md — gate list, test discipline, dependency rules.
- ADR-0005 — event stream and replay protocol; what is asserted modulo what.
- ADR-0006 / ADR-0008 — command execution model, `CommandResult`/event shape.
- TASK-0001 — `Replay`/`ReplayResult` surface; `Engine.Tests/ReplayTests.cs` for style.

## Outputs

- `dotnet test` runs the new tests as part of the existing suite.
- Two new gate tests pass on the current tree.
- If a future change alters event `Seq` numbering, `Kind` string, `CauseCommandId` routing, or `Document.Version` semantics in a way the fixture sees, `dotnet test` fails with a clear assertion message — author either updates the fixture in the same PR or reverts.
- `docs/CURRENT-STATE.md` v0.5 entry.
- `docs/roadmap.md` updated: P4 moved Pending → Shipped.

## Files

**Created:**
- `Engine.Tests/ReplayDeterminism/ReplayDeterminismFixture.cs`
- `Engine.Tests/ReplayDeterminism/ReplayDeterminismGateTests.cs`

**Modified:**
- `docs/CURRENT-STATE.md` — add v0.5 entry.
- `docs/roadmap.md` — move P4 to Shipped.
- `tasks/TASK-0005-replay-determinism-gate.md` — flip Status to Done in close commit.

**Do not touch:**
- `Engine.Contracts/**`, `Engine.Core/**`, `Engine.Cli/**`.
- `docs/diagnostics.md` (no new codes).
- `Engine.Tests/ReplayTests.cs` (additive task — existing tests stay).
- `3DEngine/`, `BlazorApp/`, `Vortice.Vulkan.*`. (`3DEngine.Core/` is now a peer kernel per ADR-0009, but is unrelated to replay and untouched here.)
- ADRs.

## Tests

- `ReplayDeterminismGateTests.Replay_Of_Fixture_Matches_Expected_Document_And_Event_Sequence`
- `ReplayDeterminismGateTests.Two_Replays_Of_Fixture_Produce_Identical_Observable_State`

## Acceptance criteria

1. `dotnet build` succeeds.
2. `dotnet test` passes — 33 tests (31 existing + 2 new).
3. The fixture's command count, expected `Document.Version`, and expected event sequence are hand-authored constants (no programmatic derivation in the test body).
4. No file under `Engine.Contracts/**`, `Engine.Core/**`, `Engine.Cli/**`, or `docs/diagnostics.md` is modified.
5. No new diagnostic code is introduced.
6. `docs/CURRENT-STATE.md` lists v0.5 with this task id.
7. `docs/roadmap.md` lists P4 under Shipped, removed from V1 Pending.

## Notes for the implementer

- **`CommandId` is `init`-settable.** The fixture sets explicit GUIDs (`11111111-…`, `22222222-…`, `33333333-…`) rather than letting `Guid.NewGuid()` run. This is what makes the fixture stable across runs.
- **Why not assert `EventRecord.Payload`?** Payload is a `Dictionary<string, object?>` with `{ "name": "NoOp", "schemaVersion": 1 }`. A regression that changed the payload would almost certainly also change `Kind` or event count, so the gate catches it without structural payload comparison. Revisit if a payload-only regression slips past.
- **Modulo what?** Per ADR-0005: `EventRecord.Timestamp`, `EventRecord.DocumentId`. The gate excludes them. `Document.CreatedAt`, `Document.UpdatedAt`, `Document.DocumentId` follow the same exclusion rationale (their values come from `DateTime.UtcNow` / `Guid.NewGuid` in the `Document` constructor, by design).
- **The existing `ReplayTests.cs` is unchanged.** Its tests verify the `Replay` class directly. The new tests are gates over the whole bus + sink + Document pipeline against a stable baseline. Distinct purposes, distinct files.
- **Namespace collision.** Folder/namespace is `ReplayDeterminism`, not `Replay`, because `Engine.Tests.Replay` would shadow `Engine.Core.Replay` for any test file in or under `Engine.Tests` that calls `Replay.ReplayLog(...)` unqualified. Naming the folder more specifically avoids the collision and reads as the gate's purpose.
