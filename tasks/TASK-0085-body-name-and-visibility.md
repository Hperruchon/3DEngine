---
id: 0085
title: A body has a name and a visibility in the engine
status: Done
phase: R8a
opened: 2026-10-28
depends-on: [0084]
governed-by: [0004, 0005, 0006, 0008, 0010, 0012, 0013, 0016, 0018, 0019, 0020, 0021, 0024, 0028]
writes:
  create:
    - tasks/TASK-0085-body-name-and-visibility.md
    - docs/adr/0028-body-name-and-visibility.md
  modify:
    - Engine.Contracts/BodyRecord.cs
    - Engine.Contracts/Document.cs
    - Engine.Contracts/Handlers/ICommandHandler.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Commands/**
    - Engine.Core/Queries/**
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Api.Http/WebSockets/**
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/Commands/**
    - Engine.Tests/Queries/**
    - Engine.Tests/Cli/**
    - Engine.Tests/Http/**
    - Engine.Tests/ReplayDeterminism/**
    - Engine.Tests/ReplayDeterminism/ReplayDeterminismGateTests.cs
    - Engine.Tests/Hosting/DispatchSurfaceGateTests.cs
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/0021-operand-consumption.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/glossary.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Cli/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0085 — A body has a name and a visibility in the engine

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R8a, the second phase of the path to "CAD on one part" (rule 4 of the order of the roadmap). The
owner decided the defaults R8-Q1 to R8-Q8 on 2026-10-27
(`docs/proposals/2026-10-27-path-to-cad-decision-sheet.md`, part 2), and accepted the contract of ADR-0028 on
2026-10-28, because it changes the public shape of `Engine.Contracts` (`CLAUDE.md`, "Stop and ask").

## Goal

Each body has a name and a visibility that commands change, that a query reads, that the events and the
snapshot give, and that a replay rebuilds.

## Scope (in)

1. ADR-0028 records the contract. It amends ADR-0012 §3 and §6 and ADR-0021 item 5.
2. `BodyRecord` gains `Name` and `Visible`; `CommandHandlerResult` gains `ChangedBodies`; the bus replaces a
   changed record in the commit and gives `body.changed` after each `body.consumed`.
3. The commands `RenameBody` and `SetBodyVisibility`, the query `GetBodyInfo`, the rules of a name and the code
   `E-VAL-BODY-NAME-INVALID`.
4. `Translate` and `Subtract` give their result the name and the visibility of the source and of the minuend.
5. `body.created` and the snapshot give `name` and `visible`; `/schema/events` lists `body.changed`.
6. Tests: the commands and the query, the rules of a name, the order of the events, the snapshot, the schema
   of the events, inheritance, a command chain over the command line and over HTTP, the replay fixture, and an
   injection that shows that `DispatchSurfaceGateTests` still refuses a host that names `RenameBody`.

## Scope (out)

- The desktop: R8b shows the name and the visibility, picks a body and frames the bodies (KD6).
- A filter of hidden bodies in the engine: a hidden body is a body (ADR-0028 item 4).

## Acceptance criteria

- [x] Each new test fails before its correction, or an injection shows that it can fail.
- [x] A replay of a log with a rename and a hide gives the same names, visibilities and events.
- [x] The code `E-VAL-BODY-NAME-INVALID` is in `docs/diagnostics.md`.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The merge
      records the run.

## Outcome

Status: Done · v0.87 · the commit that carries this block.

Each body has a name and a visibility in the engine. `RenameBody` and `SetBodyVisibility` change them,
`GetBodyInfo` reads them, `body.changed` and the snapshot give them, `Translate` and `Subtract` pass them
on, and a replay rebuilds them. A hidden body is a body for each query and command.

## Method

**Mechanical.** 32 new tests, with each theory case counted. Six injections each failed their tests: the
bus with no check of the changed list (5 cases), `body.changed` before `body.consumed` (1), no check of a
name (5), a trimmed name (1), a `Translate` that loses the name (1), and a snapshot that loses it (1). An
injected host file that names "RenameBody" failed `DispatchSurfaceGateTests`. The replay fixture renames and
hides its box, and the replay gate compares the name and the visibility.

**Judgement.** The command record calls its field `NewName`, because `Command.Name` is the name of the
command. The rule of a name comes before the search for the body, so the command line, whose Document is
always empty, still reports a refused name. The query returns `BodyInfo`, a record of `Engine.Core`, and no
new type enters `Engine.Contracts`. A field of an event payload with `Required: false` can be null, and a
test holds that a null in a required field is a problem.

**Weakest.** A command that sets the value that the body has is applied and gives an event (ADR-0028 item
2), so a client that renames on each key press fills the log. The desktop shows nothing yet; R8b does.

## Notes for the implementer

- **The judge.** A commit that changes `Engine.Contracts/**` must also change a file under `docs/adr/`.
- **The size.** About 3 to 4 evenings.

## Progress

- 2026-10-28: opened after TASK-0084 (v0.86). The first commit holds ADR-0028, which the owner accepted, and the
  text changes of ADR-0012 §3 and ADR-0021 item 5.
- 2026-10-28: the contract and the code of ADR-0028. `BodyRecord` gains `Name` and `Visible`,
  `CommandHandlerResult` gains `ChangedBodies`, and the bus checks the list and gives `body.changed` after each
  `body.consumed`. `RenameBody`, `SetBodyVisibility`, `GetBodyInfo` and `E-VAL-BODY-NAME-INVALID` exist;
  `Translate` and `Subtract` pass on the name and the visibility; `body.created`, the snapshot and the two
  schema endpoints give them. A field of an event payload with `Required: false` can be null. 32 new tests;
  six injections each failed their tests (no check of the changed list, the wrong order, no check of a name,
  a trimmed name, a Translate that loses the name, a snapshot that loses it). The replay fixture renames and
  hides the box. An injected host file that names "RenameBody" fails `DispatchSurfaceGateTests`.
  `SchemaDiagnosticsEndpoint.cs` and `ReplayDeterminismGateTests.cs` joined the write set.
- 2026-10-28: closed after a clean build and a full local run of 606 tests (507 in `Engine.Tests`, 75 in
  `3DEngine.Tests`, 24 in `3DEngine.ImageTests`).
