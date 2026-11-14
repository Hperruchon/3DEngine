---
id: 0377
title: An end at a slant to the view is an ellipse, and the outline of a cylinder and a cone comes from its axis
status: Active
phase: K9c
opened: 2026-11-14
depends-on: [0376]
governed-by: [0004, 0142, 0143, 0144]
writes:
  create:
    - tasks/TASK-0377-the-ellipses-and-the-outline-of-a-view.md
    - docs/adr/0144-the-ellipses-and-the-outline-of-a-view.md
    - Engine.Tests/Drawing/ViewEllipsesTests.cs
  modify:
    - Engine.Core/Drawing/ViewEdges.cs
    - docs/adr/0143-the-circles-of-a-view.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - docs/CHARTER.md
---

# TASK-0377 — An end at a slant to the view is an ellipse, and the outline of a cylinder and a cone comes from its axis

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K9c (ADR-0143 item 4): an end at a slant to the view, a turned body and the outline of a cylinder and a cone keep their
facets after K9b. On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Goal

Each end of a curved side is a circle, a line or an ellipse of the design, and the outline of a cylinder and a cone is at the
radius of the design.

## Scope (in)

1. ADR-0144, which amends ADR-0143 item 4.
2. The ellipse of an end, and the outline from the axis, in `ViewEdges`.
3. Tests.

## Scope (out)

- The outline of a torus.
- A client, a query or a change of `Engine.Contracts` (R22, P10).

## Acceptance criteria

- [ ] ADR-0144 exists, and ADR-0143 shows the amendment.
- [ ] Tests prove the ellipses of a cylinder at a slant, its outline at its radius, the outline of a cone tangent to its base
      with exact numbers and in three views of a turn with no symmetry, and the ellipses of a cylinder that a quaternion turns.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the half axes; the lines of the outline; the place of each piece on its line.
Judgement:  doubles with the square root for presentation; the tangent points with no angle.
Weakest:    an interior cut of an outline is at the place of the facets.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after K9b, branch k9c-view-ellipses. TASK-0377 and ADR-0144
  are free. The first version joined the pieces of the outline of the facets; K9a joins the pieces of one edge already, so no
  test could reach that join, and it went. Nine injections each failed a named test; each file came back byte for byte. Two
  injections were first caught by no test, because the first cone was symmetric on the view. A turn with no symmetry caught
  one. The other changed the sign of the tangent. For a right circular cone the apex is on the minor axis of its base, so
  that sign gives the same two points for each input. It is equivalent, and a change of the square root replaced it.
- 2026-11-14: one local run of all tests stopped the host of 3DEngine.ImageTests after 131 of 690 tests, with exit code 1;
  the output was not kept. The next run of 3DEngine.ImageTests alone and the next run of all tests passed. The cause is not known.
