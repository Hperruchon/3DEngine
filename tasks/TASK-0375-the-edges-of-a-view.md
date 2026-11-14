---
id: 0375
title: The visible and the hidden edges of a view, exact, from the faces and the edges of the owned kernel
status: Active
phase: K9a
opened: 2026-11-14
depends-on: [0372, 0374]
governed-by: [0004, 0142]
writes:
  create:
    - tasks/TASK-0375-the-edges-of-a-view.md
    - docs/adr/0142-the-edges-of-a-view-outside-the-kernel.md
    - Engine.Core/Drawing/ViewEdges.cs
    - Engine.Tests/Drawing/ViewEdgesTests.cs
  modify:
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - docs/CHARTER.md
---

# TASK-0375 — The visible and the hidden edges of a view, exact, from the faces and the edges of the owned kernel

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K9 of the roadmap (ADR-0118 items 2 and 6) comes after R21. ADR-0118 item 6 asks for an ADR on the feature boundary of
the charter. On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Goal

The engine gives the visible and the hidden lines of the live bodies along a view, exactly, for R22 and P10.

## Scope (in)

1. ADR-0142: the lines are computed outside the kernel; the view; the lines; the exact visibility; one line one time.
2. `ViewEdges` in `Engine.Core`, from `Brep`, and from the live bodies of a Document.
3. The split of row K9 into K9a and K9b.
4. Tests with exact expected lines.

## Scope (out)

- The circle or the arc of a curved side, and the outline from the axis (K9b).
- A client, a query or a change of `Engine.Contracts` (R22, P10).

## Acceptance criteria

- [ ] ADR-0142 exists, and the charter does not change.
- [ ] Tests prove a box from three views, the hidden walls of a hole and a body seen through it, a body behind another one
      with the cut at the outline, an edge behind a seam of a curved side, the tags of each line, the outline of a curved
      side with no seam, the frames, and the lines of a Document.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the projection; the cuts; the test of a point in a polygon.
Judgement:  outside the kernel; an outline of the facets in K9a; one line under a visible line.
Weakest:    no client reads the lines yet; a sphere takes about 2 s.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after P0.150, branch k9a-view-edges. TASK-0375 and ADR-0142
  are free. ADR-0118 item 6 is answered with the recommended option: outside the kernel. A first test of the Document
  expected two hidden lines and got four: the small box was in the hole, behind the front face, so the test was wrong and
  now names the four lines. The measure on this computer: a cylinder 0.12 to 0.16 s, a cone 0.02 to 0.08 s, a sphere 1.0 to
  1.9 s, a torus 1.1 to 1.7 s.
- 2026-11-14: the first rule of a cover was "strictly inside the face". An edge behind a seam between two facets of a nearer
  curved side is on the border of each facet, so it showed as visible; the rule is now "inside or on the border, and strictly
  nearer", with a test. Nine injections each failed a named test; one was first caught by no test (no body was seen
  through a hole), so the test of the hole has a body under the plate. Each file came back byte for byte.
