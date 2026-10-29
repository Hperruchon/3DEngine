---
id: 0103
title: The engine extrudes a closed profile of a sketch over Manifold, and the desktop extrudes the active sketch
status: Done
phase: K0a
opened: 2026-10-29
depends-on: [0098]
governed-by: [0001, 0004, 0007, 0008, 0012, 0013, 0014, 0015, 0016, 0017, 0018, 0019, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0035, 0036]
writes:
  create:
    - tasks/TASK-0103-extrude-of-a-sketch-profile.md
    - docs/adr/0036-extrude-of-a-sketch-profile.md
  modify:
    - Engine.Contracts/Geometry/**
    - Engine.Core/Commands/ExtrudeCommand.cs
    - Engine.Core/Commands/ExtrudeCommandHandler.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Sketch/SketchProfile.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Geometry.Manifold/**
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/Sketch/**
    - Engine.Tests/Geometry/**
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - 3DEngine/**
    - 3DEngine.ImageTests/**
    - docs/adr/0036-extrude-of-a-sketch-profile.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/glossary.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Core/CommandBus.cs
    - Engine.Cli/**
    - Engine.Api.Http.Server/**
    - 3DEngine.Vulkan/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0103 — The engine extrudes a closed profile of a sketch over Manifold, and the desktop extrudes the active sketch

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase K0a is the second part of the milestone "CAD on one part". K3a (TASK-0102, v0.103) solves the five
constraints of a sketch, and P0.39 (TASK-0098, v0.104) came after it by the order of the owner of 2026-10-29. The
owner chose on 2026-10-28 that the extrude records the identifier of its sketch and a distance (Q7 of the second
codebase review of that date). KD5 asks first for a check that the native package exports the extrude function.
ADR-0036 gives the decisions, and the owner accepts it before the code.

## Goal

A person or an agent extrudes the closed profile of a solved sketch into a solid, and the log holds the identifier of
the sketch and the distance only.

## Scope (in)

1. ADR-0036: the command, the profile, the distance, the refusals, the capability, the backend and the desktop.
2. A test that loads each native function of the extrude on each runner.
3. The command `Extrude`, `SketchProfile`, `IExtrudeOps` and its Manifold implementation, and the code
   `E-SKETCH-PROFILE-INVALID`.
4. The button "Extrude" of the window "Sketch" with a pad of one field.

## Scope (out)

- A solid that follows an edit of its sketch: K0b.
- A profile with a hole or several loops, an arc, and a profile of selected lines.
- A datum other than the three origin planes.
- An extrude in the managed stub: it gives `E-GEOM-CAP-MISSING` (ADR-0012 item 7).

## Acceptance criteria

- [x] The owner accepts ADR-0036 before the code.
- [x] A test loads `manifold_extrude`, `manifold_simple_polygon`, `manifold_polygons` and `manifold_transform` on each
      runner.
- [x] A rectangle of 10 by 6 mm on each origin plane, extruded by 5 and by −5, gives a solid with the volume 300 and the
      bounding box that the frame of its datum gives.
- [x] A profile that is open, that has a branch, that crosses itself, or that has the area zero, and a sketch that is
      not solved, each give `E-SKETCH-PROFILE-INVALID`, and the document does not change.
- [x] A replay of the log gives the same body, and an undo removes it.
- [x] Each new test fails when its injection breaks the code that it covers.
- [x] The visual check: a person draws a rectangle, constrains it, and extrudes it in the desktop.
- [x] `dotnet test` passes, and the build gives zero warnings.

## Notes for the implementer

- The determinism gate reads `Engine.Core`. `SketchProfile` uses `+ - * /` and comparisons only.
- A new command changes only its own files and `HandlerCatalog.cs` (`DispatchSurfaceGateTests`).

## Progress

- 2026-10-29: the owner said yes to K0a after P0.39. A search of the native binaries of
  `Engine.Geometry.Manifold.Native` 3.5.2.1 found `manifold_extrude`, `manifold_simple_polygon`,
  `manifold_polygons` and their sizes in `win-x64`, `linux-x64` and `osx-arm64`. ADR-0036 is proposed with the
  questions Q1 to Q4.
- 2026-10-29: the owner accepted ADR-0036 with Q1 to Q4 A: one closed loop, a signed distance, a refusal of a
  sketch that is not solved, and the button "Extrude" with a pad.
- 2026-10-29: the engine extrudes. `IExtrudeOps`, `ExtrudeParameters` and the flag `Extrude` in the contract,
  the Manifold implementation, `SketchProfile`, the command `Extrude` and the code `E-SKETCH-PROFILE-INVALID`. A
  test loads each of the 8 native functions. 19 tests in `ExtrudeTests`; each of 17 injections fails a test. The
  write set gains `Engine.Tests/Persistence/DocumentFileTests.cs`, whose catalog test needs a sample of each command.
- 2026-10-29: the desktop extrudes. The window "Sketch" has the button "Extrude" with a pad of one field, `distance`
  (text 10). A success ends the sketch mode and selects the new body. 3 new tests; each of 5 injections fails one.
  The visual check: a rough rectangle from the pad, 4 free after the horizontal and vertical constraints, 2 free after
  the sizes 10 and 6, then Extrude 5 gives the body "Extrude" with the box (2.25, 1.1, 0) to (12.25, 7.1, 5). An undo
  removes it, and the sketch stays. The log has no warning.
- 2026-10-29: CI runs 40865119918 and 40866036929 passed on the three runners, with the load of the native
  functions on each. The owner accepted the pictures of the visual check.

## Outcome

Status: Done · commit (the merge of the branch k0a-extrude) · v0.105

## Method

Mechanical: the contract, the native imports, the command with its encoder, the code, the button and its pad.
Judgement:  the handler reads the solved record of the Document and checks it with `SketchSolver.Status`, so the
            log holds the identifier and the distance only. The profile walks the lines from the start of the
            first line, so its order depends on the record only. The backend gets a positive height and a frame,
            and the frame of an origin plane holds 0 and 1 only, so the move is exact. A line that folds back
            on the line before it is refused by the area (3 lines) or by a touch (4 lines or more).
Weakest:    the tests of a crossing and of a touch use exact comparisons with zero. A point that is very near a
            line, and not on it, passes the check, and Manifold then decides. The visual check added the
            constraints over HTTP and drew the rectangle through the pad, as the check of K3a did.
