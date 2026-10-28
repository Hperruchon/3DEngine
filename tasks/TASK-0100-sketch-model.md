---
id: 0100
title: The engine holds a sketch of points, lines and five constraints on an origin plane
status: Active
phase: K2a
opened: 2026-10-28
depends-on: [0097]
governed-by: [0004, 0005, 0006, 0007, 0008, 0010, 0012, 0013, 0015, 0016, 0017, 0018, 0019, 0020, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033]
writes:
  create:
    - tasks/TASK-0100-sketch-model.md
    - docs/adr/0033-sketch-model.md
  modify:
    - Engine.Contracts/Document.cs
    - Engine.Contracts/Handlers/ICommandHandler.cs
    - Engine.Contracts/Sketch/**
    - Engine.Core/CommandBus.cs
    - Engine.Core/StepHistory.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Sketch/**
    - Engine.Core/Commands/**
    - Engine.Core/Queries/**
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Api.Http/WebSockets/WireMessage.cs
    - Engine.Tests/Sketch/**
    - Engine.Tests/Commands/**
    - Engine.Tests/Http/**
    - Engine.Tests/Hosting/**
    - Engine.Tests/Persistence/**
    - 3DEngine/**
    - 3DEngine.ImageTests/**
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/0021-operand-consumption.md
    - docs/adr/0031-undo-and-redo.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/glossary.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Vulkan/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0100 — The engine holds a sketch of points, lines and five constraints on an origin plane

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase K2a is the first phase of the sketch on the path to the milestone "CAD on one part". The owner
answered the questions Q1 to Q9 of `docs/reviews/2026-10-28-b-codebase-review.md` on 2026-10-28, A to each, and the
first commit of TASK-0096 put the answers into the roadmap. The sketch changes the public shape of
`Engine.Contracts`, which is a stop-and-ask condition of `CLAUDE.md`: ADR-0033 gives the contract, and the owner
accepts it before the code.

## Goal

An agent builds a sketch of points, lines and the five constraints of K3a on an origin plane through the engine, and
a person sees it in the desktop.

## Scope (in)

1. ADR-0033, items 1 to 13, for the point, the line, the frame of a sketch and the constraint kinds 1 to 5. The
   records of the circle, the arc and the kinds 6 to 14 are named in the ADR and not built.
2. The seven commands, the query `GetSketch` with the residual of each constraint, and the five codes of the
   namespace `SKETCH`, registered in `docs/diagnostics.md`.
3. The three event kinds, the field `sketches` of the snapshot, and the history of undo and redo for sketch records.
4. The document file holds each sketch command, and an open rebuilds each sketch.
5. The desktop draws each live sketch, read only, over the scene.

## Scope (out)

- The tools of the desktop: K2b.
- The solver: K3a. The positions stay the inputs of the commands.
- The circle, the arc and the kinds 6 to 14: phase K2, after K2b.
- A command that deletes a sketch, or makes a datum plane.

## Acceptance criteria

- [ ] The owner accepts ADR-0033 before a change of `Engine.Contracts`.
- [ ] Each command and the query work over HTTP and in a replay; the replay gate holds a sketch.
- [ ] An undo and a redo of each sketch command give the record before and after, and their events.
- [ ] An open of a document file with sketch commands gives the same sketches.
- [ ] A host that names `CreateSketch` fails `DispatchSurfaceGateTests` (an injection test).
- [ ] The residual of each kind 1 to 5 is zero for a satisfied constraint and not zero otherwise.
- [ ] The visual check: the owner accepts a picture of a sketch drawn over the scene.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The order.** The ADR first, then the contract with the bus and the history, then the commands, then the query,
  then the desktop.
- **The governance.** `governed-by` must equal the ADRs in force whose `affects` intersect the write set; the gate
  `TaskGovernance` gives the list. ADR-0033 joins the list when the owner accepts it.
- **The size.** About 4 to 6 evenings.

## Progress

- 2026-10-28: opened with ADR-0033 as Proposed. The work waits for the owner to accept the contract.
- 2026-10-28: the owner accepted ADR-0033 with no change. The record amends ADR-0012, ADR-0021 and ADR-0031, and
  their text changes in this commit.
