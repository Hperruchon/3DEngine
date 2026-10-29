---
id: 0106
title: The sketch refuses an identifier that it holds, and the pads of the sketch follow the state that they act on
status: Active
phase: P0.42
opened: 2026-10-29
depends-on: [0105]
governed-by: [0004, 0007, 0009, 0012, 0013, 0016, 0017, 0018, 0019, 0021, 0022, 0023, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0035, 0036, 0037]
writes:
  create:
    - tasks/TASK-0106-sketch-gaps-of-the-twelfth-review.md
  modify:
    - Engine.Core/Commands/**
    - Engine.Core/Sketch/**
    - Engine.Core/Queries/GetSketchQuery.cs
    - Engine.Core/Queries/GetSketchQueryHandler.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/DocumentFolder.cs
    - Engine.Api.Http/Json/ApiJson.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - 3DEngine/**
    - 3DEngine.Core/Sketching/**
    - Engine.Tests/Sketch/**
    - Engine.Tests/Http/**
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - docs/adr/0035-sketch-solver.md
    - docs/adr/0036-extrude-of-a-sketch-profile.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/CommandBus.cs
    - Engine.Cli/**
    - Engine.Api.Http.Server/**
    - 3DEngine.Vulkan/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0106 — The sketch refuses an identifier that it holds, and the pads of the sketch follow the state that they act on

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-29 found medium and low findings in the sketch and its tools (findings E86 to E93
and V40 to V45 of `docs/reviews/2026-10-29-codebase-review.md`). `docs/templates.md`, section 7, puts a medium or
low finding into a task after the next product phase, so this task comes after K0b. V41 touches the pad of the
extrude, which K0b changes; K0b can take it.

## Goal

A sketch never holds two items with one identifier, a held point goes where the person put it when a solution
exists, and each pad of the sketch closes when the state that it acts on changes.

## Scope (in)

1. E86: each sketch handler refuses an identifier that its sketch holds, with a code.
2. E87: the held solve converges where a solution exists, or the result gives a warning when the solver drops the
   hold; the comment of `SketchResults.cs` agrees with the code.
3. E88 to E92: a residual that is not finite over HTTP, the cost of the solve and its limit in ADR-0035, a relative
   tolerance or its limit, read-only lists of a sketch record, and a reason of its own for points that coincide,
   with a decision about a smallest height in ADR-0036.
4. E93: the reserved names of Windows that the review found.
5. V40 and V41: the Next point pad and the Extrude pad have a scope, and close when it changes.
6. V42 to V45: the pad on a portrait display, the buttons of the origin, one read of each changed sketch, and a
   pixel that is not finite.

## Scope (out)

- The cache that forgets an identifier (E17, register entry R-0028).
- A change of the public shape of `Engine.Contracts`.

## Acceptance criteria

- [ ] Each finding of the scope has a test that fails before the change.
- [ ] An injection that restores each old rule fails a test.
- [ ] A new code is in `docs/diagnostics.md`.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-29: opened by the codebase review of 2026-10-29 (TASK-0104).
- 2026-10-29: the owner said yes to TASK-0106 after K0c. The engine part: `E-SKETCH-ID-IN-USE` in the four handlers
  that add an item (E86), `W-SKETCH-HOLD-DROPPED` and `SketchSolver.SolveWithHold` (E87), a residual that is not
  finite is null (E88), read-only lists of a record (E91), the reason of a line of length zero (E92), and more
  reserved names (E93). The limits of E89 and E90, measured in a Release build, and the rule of E92 are History lines
  of ADR-0035 and ADR-0036. The write set gains `Engine.Core/Queries/GetSketchQuery.cs`, which holds the type of the
  residual.
