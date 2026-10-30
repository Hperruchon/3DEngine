---
id: 0117
title: The engine holds the circle and the arc of a sketch, and solves tangent, radius, pointOn and equal on them
status: Done
phase: K2d
opened: 2026-10-30
depends-on: [0116]
governed-by: [0004, 0012, 0013, 0016, 0019, 0021, 0028, 0031, 0033, 0035, 0037, 0039]
writes:
  create:
    - tasks/TASK-0117-circle-and-arc.md
    - Engine.Core/Commands/AddSketchCircleCommand.cs
    - Engine.Core/Commands/AddSketchCircleCommandHandler.cs
    - Engine.Core/Commands/AddSketchArcCommand.cs
    - Engine.Core/Commands/AddSketchArcCommandHandler.cs
    - Engine.Tests/Sketch/CircleArcTests.cs
  modify:
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Commands/DeleteSketchItemCommandHandler.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Sketch/SketchModel.cs
    - Engine.Core/Sketch/SketchConstraints.cs
    - Engine.Core/Sketch/SketchSolver.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - docs/diagnostics.md
    - docs/adr/0033-sketch-model.md
    - docs/adr/0035-sketch-solver.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/CommandBus.cs
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0117 — The engine holds the circle and the arc of a sketch, and solves tangent, radius, pointOn and equal on them

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0033 item 2 names five entities, and K2a gave the point, the line and the frame. `SketchRecord` already holds
the lists `Circles` and `Arcs`, and `SketchCircle` and `SketchArc` exist in `Engine.Contracts`, so the public shape
does not change. The plan of 2026-10-30 puts K2d after K0d. Under its answer Q2 A, the variables and the equations of
the solver are a History line of ADR-0035, and the commands and the kinds a History line of ADR-0033.

## Goal

`AddSketchCircle` and `AddSketchArc` add a circle and an arc, the solver keeps the two radii of an arc equal and
treats the radius of a circle as a variable, and `AddSketchConstraint` accepts `tangent`, `radius`, and `pointOn` and
`equal` on a circle or an arc.

## Scope (in)

1. The two commands, with the new code `E-SKETCH-CURVE-INVALID`, and the delete of a circle, an arc and the curves of
   a deleted point.
2. The solver: the radius of each circle as a variable after the points, the equation of each arc, and the equations
   of the four kinds on a curve with their derivatives by formula.
3. Tests: each kind solves, the derivatives agree with a central difference, a fixed solve gives constant bits, each
   refusal, a delete, and an extrude that refuses a curve.

## Scope (out)

- The desktop draws no circle and no arc yet (K2e).
- An extrude of a profile with an arc needs a sine and a cosine of the polygon of the arc, or a tessellation in the
  backend; it waits (ADR-0036, determinism rule 1).

## Acceptance criteria

- [x] Each kind on a curve solves a rough sketch to a residual less than 1e-9, with its free degrees.
- [x] The analytic derivatives agree with a central difference, also by a radius.
- [x] A fixed solve with curves gives constant bits on the three runners.
- [x] An injection that breaks each new equation or each new refusal fails a test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch k2d-circle-arc) · v0.117

## Method

Mechanical: the two commands, the code, the delete, the residuals, and the History lines.
Judgement:  the radius of a circle is a variable of the solver, so a constraint changes it as it moves a point; the
            radius of an arc is the distance to its start, so an arc adds no variable and one equation. A term of a
            radius names its circle, so one function writes each column of the Jacobian.
Weakest:    the side of a tangent comes from the positions at the start of the solve, so a tangent cannot move a
            circle to the other side of its line. A point on an arc lies on the circle of the arc, with no check of
            its angle.

## Progress

- 2026-10-30: opened by the plan of 2026-10-30, after K0d.
- 2026-10-30: the commands, the solver, the kinds, 25 new test cases and 11 injections caught.
- 2026-10-30: closed at v0.117.
