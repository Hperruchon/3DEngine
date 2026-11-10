---
id: 0338
title: A pocket from a datum on a moved or turned face opens the face in the owned kernel
status: Ready
phase: P0.140
opened: 2026-11-10
depends-on: [0337]
governed-by: [0004, 0033, 0035, 0037, 0039, 0051, 0052, 0054, 0055, 0058, 0059, 0076, 0083, 0089, 0112]
writes:
  create:
    - tasks/TASK-0338-the-high-finding-of-the-review-of-2026-11-10.md
  modify:
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Tests/Commands/RotateCommandTests.cs
    - eng/test-floor.txt
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine.App/**
---

# TASK-0338 — A pocket from a datum on a moved or turned face opens the face in the owned kernel

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-10 (`docs/reviews/2026-11-10-codebase-review.md`) found E200: the owned kernel moves and
turns a solid exactly, in rationals, and the frame of a datum on a face moves and turns in doubles. After a Translate by
(0, 0, 0.3) of a box 7 high, or a Rotate by (5, 1, 2, 3), a pocket from a datum on the moved `+z` face leaves `+z` whole
(area 1200, no hole) and a closed cavity under it. A high finding goes into a task before the next product phase, so this
task comes before the closes of R18a and R18b.

## Goal

After a move or a turn of a body, a pocket from a datum on a moved face opens that face in the owned kernel.

## Scope (in)

1. The fix, form (a) of the review if the work confirms it: when the datum of a sketch names a face, the kernel takes the
   exact plane of that face for the sketch, so the start face of the tool and the face are one plane. An ADR records the
   rule if it changes a decision (ADR-0051, ADR-0058 or ADR-0112).
2. Tests: the cases of the probe of the review, a Translate along the normal by a decimal and a general Rotate, each with
   `+z` with one hole and an area of 1100. Each test fails on the code of v0.318.

## Scope (out)

- V164, E201, V165 and T181, which TASK-0339 holds.
- Any change of the log or of the command payloads.

## Acceptance criteria

- [ ] The tests fail at v0.318 and pass after the fix.
- [ ] The cross-platform hash of the determinism probe does not change, or the change has its reason in the ledger entry.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

- `KernelProjection` makes the solid of an extrude from the frame that `DatumResolver` gives. The kernel solid of the body
  holds the face with its exact plane (`PlaneR`).
- The probe of the review: a box 40 by 30, a move or a turn, a datum on `+z`, a square of 10 by 10 at the middle of the
  datum, an extrude of -2, and a Subtract from the moved body; then `GetFacesQuery` on the cut.
- `governed-by` must hold each ADR in force whose `affects` meets the write set. `TaskGovernanceGateTests` gives the list.
  Add a file to the write set when the work needs it, and say why in the progress.

## Method

Mechanical: the probe of the review as the tests.
Judgement:  the source of the plane of a sketch on a face.
Weakest:    the backend has its own tolerance; this task checks the kernel only.

## Progress

- 2026-11-10: opened from the codebase review of 2026-11-10.
