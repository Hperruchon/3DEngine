---
id: 0376
title: A view shows the circle of the design for a curved side, and an end seen from the side as one line
status: Done
phase: K9b
opened: 2026-11-14
depends-on: [0375]
governed-by: [0004, 0054, 0058, 0059, 0076, 0083, 0089, 0112, 0142, 0143]
writes:
  create:
    - tasks/TASK-0376-the-circles-of-a-view.md
    - docs/adr/0143-the-circles-of-a-view.md
    - Engine.Tests/Drawing/ViewCurvesTests.cs
  modify:
    - Engine.Core/Drawing/ViewEdges.cs
    - Engine.Core/Features/KernelProjection.cs
    - docs/adr/0142-the-edges-of-a-view-outside-the-kernel.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - docs/CHARTER.md
---

# TASK-0376 — A view shows the circle of the design for a curved side, and an end seen from the side as one line

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K9b (ADR-0142 item 7): in K9a a curved side gives the outline of its facets, and an end of a cylinder is 64 short lines.
On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Goal

A view shows a hole or a cylinder as one circle with the radius of the design, and its end from the side as one line.

## Scope (in)

1. ADR-0143, which amends ADR-0142 items 5 and 7.
2. The curves of the design in `KernelProjection.Curves`.
3. The circles, the arcs and the lines of an end in `ViewEdges`.
4. Tests.

## Scope (out)

- An ellipse, a turned body and the outline of a cylinder from its axis (ADR-0143 item 4).
- A client, a query or a change of `Engine.Contracts` (R22, P10).

## Acceptance criteria

- [x] ADR-0143 exists, and ADR-0142 shows the amendment.
- [x] Tests prove a cylinder from the top as one circle, from the front as four lines, a visible and a hidden arc under a box,
      the outline of a sphere as one circle, a frame of another length with its facets, and the circles of the design
      through an extrude, a cylinder, a translate and a subtract.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch k9b-view-curves) · v0.361

## Method

Mechanical: the walk of the curves; the chains of arcs; the runs of a line.
Judgement:  the curves from the commands, not from the facets; the exact tests of the axis.
Weakest:    an ellipse and a turned body keep their facets; the ends of an arc are on the facets.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after K9a, branch k9b-view-curves. TASK-0376 and ADR-0143
  are free. Ten injections each failed a named test; each file came back byte for byte. The test of a frame of another
  length came after the first run, because no test held the guard of the length.
- 2026-11-14: run 43570185151 passed on the work commit. Closed at v0.361.
