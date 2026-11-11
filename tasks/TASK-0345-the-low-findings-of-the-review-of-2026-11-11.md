---
id: 0345
title: The low findings of the codebase review of 2026-11-11
status: Active
phase: P0.143
opened: 2026-11-11
depends-on: [0344]
governed-by: [0007, 0017, 0019, 0023, 0025, 0029, 0031, 0072, 0109, 0121]
writes:
  create:
    - tasks/TASK-0345-the-low-findings-of-the-review-of-2026-11-11.md
    - docs/adr/0121-the-edges-of-a-large-mesh-are-found-on-the-thread-pool.md
  modify:
    - docs/adr/0109-the-edges-of-the-body.md
    - docs/adr/README.md
    - 3DEngine.Vulkan/EdgeLines.cs
    - 3DEngine.App/SessionBodies.cs
    - 3DEngine.ImageTests/BodyEdgesImageTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0345 — The low findings of the codebase review of 2026-11-11

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-11 (`docs/reviews/2026-11-11-codebase-review.md`) gives two low findings in the edge lines of
R18b (ADR-0109). Section 7 of `docs/templates.md` puts a low finding after the next product phase.

## Goal

The edge lines draw no false line, and a large mesh spends no time on lines that it does not draw.

## Scope (in)

1. V166: a triangle of zero area adds no edge.
2. V167: the edges of a mesh above the limit are not found, or are found away from the frame.
3. A test for each, which fails before the fix.

## Scope (out)

- A change of the limit of 30 degrees or of `MostEdges`.

## Acceptance criteria

- [ ] A test proves that a triangle of zero area across a T-vertex adds no edge.
- [ ] A test proves that a mesh above the limit does not run the search of its edges on the upload, or a measure shows the
      new cost.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: the check of a zero normal; the order of the limit and the search.
Judgement:  the bound of V167, and whether the search moves to the thread pool.
Weakest:    to be written at the close.

## Progress

- 2026-11-11: opened by the codebase review of 2026-11-11 (TASK-0344).
- 2026-11-11: the owner ordered "go" after the merge of R20a. V166: `EdgeLines.Feature` skips a triangle whose normal is
  zero. V167: a mesh of more than 100 000 triangles finds its edges on the thread pool and gets its lines at a later frame
  (ADR-0121, amends ADR-0109 items 1 and 5); a limit on the triangles before the search would drop the true lines of a
  fine mesh. Three new tests; three injections each failed a named test, and each file came back byte for byte.
