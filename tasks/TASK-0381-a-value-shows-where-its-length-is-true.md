---
id: 0381
title: In a flat view a value shows only where its length is true
status: Done
phase: R22a
opened: 2026-11-14
depends-on: [0379]
governed-by: [0007, 0019, 0025, 0029, 0072, 0082, 0084, 0115, 0117, 0146]
writes:
  create:
    - tasks/TASK-0381-a-value-shows-where-its-length-is-true.md
    - docs/adr/0146-a-value-shows-where-its-length-is-true.md
    - 3DEngine.ImageTests/FlatViewValuesTests.cs
  modify:
    - 3DEngine.App/EdgeValues.cs
    - docs/adr/0082-the-values-on-the-edges-of-the-redesign.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0381 — In a flat view a value shows only where its length is true

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R22 (ADR-0118 item 5) comes after K9 and after the high findings of the review of 2026-11-14 (e) (TASK-0379). On
2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Goal

A flat view shows only the values whose length it shows true.

## Scope (in)

1. ADR-0146, which amends ADR-0082 item 1 and splits row R22 into R22a to R22d.
2. The rule of the true length in `EdgeValues.Of`.
3. Tests.

## Scope (out)

- R22b to R22d.

## Acceptance criteria

- [x] ADR-0146 exists, and ADR-0082 shows the amendment.
- [x] Tests prove the values of a plate in the six flat views and with perspective, the radius of a disc and of a half arc,
      and the rule of the circle of a radius against its chord.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r22a-true-values) · v0.365

## Method

Mechanical: the test of the direction of an edge.
Judgement:  the slices of R22; the limit of 0.001.
Weakest:    no person used it in the window.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after P0.151, branch r22a-true-values. TASK-0381 and ADR-0146
  are free. A first test expected the distance of a disc from the front; a disc has no edge between two side faces, so its
  distance shows in no view, and the test now says so. Four injections each failed a named test; each file came back byte
  for byte. A fifth injection passed the rule of a length for a radius at its call; on the edges of GetFaces the segments of
  an arc are not in the order of a chain, so the chord leans and the rule of a length hides that radius too. A direct test
  holds the rule of the circle.
- 2026-11-14: run 43575434859 passed on the work commit. Closed at v0.365.
