---
id: 0126
title: An extrude takes a profile with arcs and circles
status: Active
phase: K0e
opened: 2026-10-31
depends-on: [0117, 0122, 0128]
governed-by: [0004, 0033, 0035, 0036, 0037, 0039, 0040]
writes:
  create:
    - tasks/TASK-0126-extrude-of-curves.md
    - docs/adr/0040-extrude-of-curves.md
    - Engine.Tests/Sketch/CurveProfileTests.cs
  modify:
    - Engine.Core/Sketch/SketchProfile.cs
    - Engine.Core/Sketch/ExtrudeProfile.cs
    - Engine.Tests/Sketch/CircleArcTests.cs
    - docs/adr/0036-extrude-of-a-sketch-profile.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
---

# TASK-0126 — An extrude takes a profile with arcs and circles

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

K2d (TASK-0117) gave the sketch the circle and the arc, and an extrude refused a sketch with a curve, because the
polygon of an arc needs a sine and a cosine in code of the log (determinism rule 1). On 2026-10-31 the owner chose this
item ("1 3 4", item 1) and asked for the ADR before the code. The owner then told the session to use its recommended
answer for each question until the morning, so ADR-0040 is Accepted with Q1 A, Q2 A and Q3 A.

## Goal

An extrude of a sketch with arcs, or of one circle, gives a solid whose polygon has the same bits on each platform.

## Scope (in)

1. ADR-0040: the profile of lines and arcs, or of one circle, and the polygon of a curve with `+ - * / sqrt`.
2. `SketchProfile.Find` walks the edges, adds the polygon of each arc in the direction of the walk, and checks the
   polygon. `SketchProfile.Turn` gives the polygon of a curve.
3. A bit test of the polygon on the three runners, the refusals, and an extrude and a rebuild of a circle.
4. The row of `E-SKETCH-PROFILE-INVALID` names the rules of a curve (Q3 A).

## Scope (out)

- Holes: a loop inside the outer loop (Q1 A). It needs a change of `ExtrudeParameters`.
- A number of segments from a tolerance (Q2 A).
- A change of the desktop: the button Extrude already sends the command.

## Acceptance criteria

- [ ] The bit test gives one hash on Windows, Linux and macOS.
- [ ] Each rule of ADR-0040 item 1 has a refusal test, and an injection that removes a rule fails a test.
- [ ] An extrude and a rebuild of a circle on the native backend give the box of the circle.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: ADR-0040 Accepted under the standing answer of the owner. The draft compared the cosine of the step with
  cos(2π / 64); item 2 uses 0.9951 as a margin, so the count of a full turn does not depend on the last bit of its
  halvings. An injection of cos(2π / 64) also gives 64 segments on this computer, so no test can hold the margin.
