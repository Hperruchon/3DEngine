---
id: 0339
title: The medium and low findings of the codebase review of 2026-11-10
status: Ready
phase: P0.141
opened: 2026-11-10
depends-on: [0338]
governed-by: [0001, 0007, 0008, 0012, 0018, 0019, 0025, 0029, 0036, 0072, 0092, 0107, 0108, 0110, 0111, 0112, 0113]
writes:
  create:
    - tasks/TASK-0339-medium-and-low-findings-of-the-review-of-2026-11-10.md
  modify:
    - 3DEngine.App/ProposedValues.cs
    - 3DEngine.App/AcceptHold.cs
    - Engine.Contracts/Geometry/Rotation.cs
    - Engine.Tests/Commands/RotateCommandTests.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Kernel/**
---

# TASK-0339 — The medium and low findings of the codebase review of 2026-11-10

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-10 (`docs/reviews/2026-11-10-codebase-review.md`) found one medium finding and three low
findings. Section 7 of `docs/templates.md` puts them after the next product phase (R18a or R18b).

## Goal

Each of V164, E201, V165 and T181 is fixed, or has a register entry with its reason.

## Scope (in)

1. V164: the line of a proposal shows an angle in degrees, or does not name an angle constraint as a length.
2. E201: a quaternion whose entries are not a turn is refused or scaled first; a new code goes into `docs/diagnostics.md`.
   A change of the refusals of `Rotate` changes a decision of ADR-0112: an ADR or an amendment records it.
3. V165: a tap on the path line is free while an accept runs.
4. T181: a test of the hold through the host, or a register entry; the layout test checks each pair of panels.
5. A test for each fix that fails without the fix.

## Scope (out)

- E200, which TASK-0338 holds.

## Acceptance criteria

- [ ] Each fix has a test that fails on an injection of the old code.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

- E201 changes `Engine.Contracts/Geometry/Rotation.cs`. CLAUDE.md asks the owner before a change of the public shape of
  `Engine.Contracts`; a new refusal inside `FromQuaternion` keeps the shape, but say so in the ADR.
- `governed-by` must hold each ADR in force whose `affects` meets the write set. `TaskGovernanceGateTests` gives the list.
  Add a file to the write set when the work needs it, and say why in the progress.

## Method

Mechanical: the probes of the review as the tests.
Judgement:  the limit of a quaternion; the form of an angle in the line of a proposal.
Weakest:    T181 needs the window, or a seam that the code does not have yet.

## Progress

- 2026-11-10: opened from the codebase review of 2026-11-10.
- 2026-11-10: `governed-by` adds 0108 (TASK-0332): ADR-0108 affects `3DEngine.ImageTests/ProposalArcTests.cs`.
