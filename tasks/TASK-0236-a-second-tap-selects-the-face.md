---
id: 0236
title: A second tap on the selected body selects the face under the point
status: Active
phase: R12a
opened: 2026-11-06
depends-on: [0234]
governed-by: [0004, 0016, 0018, 0028, 0033, 0037, 0058, 0060]
writes:
  create:
    - tasks/TASK-0236-a-second-tap-selects-the-face.md
    - docs/adr/0075-a-second-tap-selects-the-face.md
  modify:
    - Engine.Core/Queries/GetFacesQuery.cs
    - Engine.Core/Queries/GetFacesQueryHandler.cs
    - Engine.Tests/Features/GetFacesTests.cs
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
---

# TASK-0236 — A second tap on the selected body selects the face under the point

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row R12 gives the escalated selection of faces and edges, after K4. K4b gives a name to each face, and
`GetFaceAt` gives the face under a hit point. No name of an edge exists. The fallback queue of the owner (2026-11-05)
gives R12 after the split of the user interface.

## Goal

A person selects a face of the selected body with a second tap, sees its outline in the scene, and reads it in the
panel, with a finger, a pen or a mouse.

## Scope (in)

1. ADR-0075 with the options of the gesture, of the mark and of the panel, answered by the standing order of 2026-11-05.
2. The outline of each face name in `GetFaces` (ADR-0075 item 1).
3. The gesture of the second tap, the key Escape, and the row of the body (ADR-0075 item 2).
4. The mark of the outline in the scene, and the face in the panel with its area, its normal and a button Sketch
   (ADR-0075 item 3).

## Scope (out)

- The edges (R12b), which need a name of an edge in track K.
- The layout of the redesign.

## Acceptance criteria

- [ ] Each face of a box has an outline of four edges, a hole is in the outline, and no edge inside a curve is.
- [ ] A test of the gesture with a finger and with a mouse gives the same face.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the outline from the edges of the B-Rep, and a test of each form before the change.
Judgement:  the three answers of ADR-0075, which the standing order gave.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: ADR-0075 is Proposed, with the options. Item 1: GetFaces gives the outline of each face name; two tests,
  and an injection that counts the edges inside a name fails the test of the curve. The gesture and the panel come next.
