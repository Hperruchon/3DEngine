---
id: 0108
title: The engine rebuilds each live extrude of a sketch after an edit, and a broken profile gives a fault on the body
status: Active
phase: K0b
opened: 2026-10-29
depends-on: [0105]
governed-by: [0004, 0005, 0006, 0008, 0010, 0012, 0013, 0015, 0016, 0018, 0019, 0020, 0021, 0028, 0031, 0033, 0035, 0036]
writes:
  create:
    - tasks/TASK-0108-rebuild-of-an-extrude.md
    - docs/adr/0037-rebuild-of-an-extrude.md
  modify:
    - Engine.Contracts/BodyRecord.cs
    - Engine.Contracts/BodyFault.cs
    - Engine.Core/Sketch/**
    - Engine.Core/Commands/**
    - Engine.Core/Queries/**
    - Engine.Core/CommandBus.cs
    - Engine.Core/GeometryKeys.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Api.Http/WebSockets/WireMessage.cs
    - Engine.Tests/**
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/0021-operand-consumption.md
    - docs/adr/0028-body-name-and-visibility.md
    - docs/adr/0031-undo-and-redo.md
    - docs/adr/0036-extrude-of-a-sketch-profile.md
    - docs/adr/0037-rebuild-of-an-extrude.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/glossary.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
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

# TASK-0108 — The engine rebuilds each live extrude of a sketch after an edit, and a broken profile gives a fault on the body

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase K0b is the engine part of the last step of the milestone "CAD on one part": a solid that follows an
edit of its sketch. The codebase review of 2026-10-29 gave six questions for it, and the owner answered A to each on
2026-10-29: one handle and a new geometry key for each rebuild, a fault on the body for a broken profile, a warning
for a consumed extrude, the extrudes found in the log, the key and the fault in `body.changed`, and a split into
K0b (the engine and HTTP) and K0c (the desktop). ADR-0037 gives the decisions, and the owner accepts it before the
code, because `Engine.Contracts` changes.

## Goal

An edit of a sketch rebuilds each live extrude of it under its handle, an undo gives back the old solid with no call
of the backend, and an agent sees the new box, the key and the fault.

## Scope (in)

1. ADR-0037, and the amendments of ADR-0012, ADR-0021, ADR-0028, ADR-0031 and ADR-0036.
2. `BodyRecord.Geometry`, `BodyRecord.Fault`, `BodyFault`, and the geometry key in each read of a solid.
3. `GeometryKeys.Derive`, the steps in effect, and the rebuild in each sketch command.
4. The codes `W-SKETCH-REBUILD-FAILED` and `W-SKETCH-EXTRUDE-CONSUMED`.
5. The fields `geometry` and `fault` of `body.created`, `body.changed` and the snapshot.

## Scope (out)

- The desktop: K0c.
- The chain of a consumed extrude: phase K0.
- The findings of TASK-0106 and TASK-0107.

## Acceptance criteria

- [ ] The owner accepts ADR-0037 before the code.
- [ ] An edit of a sketch changes the box of each live extrude of it, with the same body identifier, and an undo gives
      back the old box.
- [ ] An edit that breaks the profile applies, the body keeps its box and gets the fault, and a later edit that mends
      the profile clears the fault.
- [ ] An edit after a `Translate` of the extrude gives `W-SKETCH-EXTRUDE-CONSUMED` and changes no body; an extrude that
      an undo removed gives no warning.
- [ ] An edit that does not change the profile gives no body event.
- [ ] An open of the file and a replay give the same keys and the same boxes.
- [ ] A read of a solid by the body identifier of an extrude fails, so a missed call site fails loudly.
- [ ] Each new test fails when its injection breaks the code that it covers.
- [ ] `dotnet test` passes, the build gives zero warnings, and CI passes on the branch and on `main`.

## Notes for the implementer

- `Engine.Tests/Sketch/ExtrudeTests.cs:262-263` asserts that the box does not change after `MoveSketchPoint`. This
  task turns it round.
- `Document.Bodies` is a dictionary; the rebuild takes the extrudes in the order of the log.
- The determinism gate reads `Engine.Core`. `GeometryKeys` uses SHA-1 on bytes, which has no floating point.

## Progress

- 2026-10-29: the owner answered Q1 to Q6 of the codebase review of 2026-10-29 with A. The roadmap rows K0b and K0c
  hold the answers, and register entry R-0044 is closed. ADR-0037 is proposed.
