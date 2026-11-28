---
id: 0476
title: The high finding of the codebase review of 2026-11-28, the centre of a placed cut
status: Active
phase: P0.192
opened: 2026-11-28
depends-on: [0475]
governed-by: []
writes:
  create:
    - tasks/TASK-0476-the-high-finding-of-the-review-of-2026-11-28.md
  modify:
    - docs/roadmap.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0476 — The high finding of the codebase review of 2026-11-28, the centre of a placed cut

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-28 (`docs/reviews/2026-11-28-codebase-review.md`, TASK-0475) found one high finding, E246: the
design mass of a cut gives a wrong centre when a body of the chain is a `Subtract` by a tool with no curve and has its own
`PlaceBody`. By `docs/templates.md`, section 7, a high finding comes before the next product phase.

The answers to the questions of the review are answered by the standing order of 2026-11-28 ("same as every previous week":
the recommended option of each question). Q1 A: K25 is the next product phase, before R34 and R35, because R34 and R35 need a
check on the phone and K25 closes R-0080 before its date. Q2 A: TASK-0477 corrects E249 with a scale of each quaternion by an
exact power of two before a product. The owner can change each answer at the final report of 2026-11-29.

## Goal

`GetMassProperties` gives the same centre for a placed cut as for the same cut moved with `Translate` and `Rotate`.

## Scope (in)

1. The correction of section 3 of the review in `Engine.Core/Queries/CutMass.cs`, with the probe of the review as a test in
   `Engine.Tests/Commands/PlaceBodyTests.cs`, and the same scene for a `Union` and an `Intersect` before the cut.
2. The answers to Q1 and Q2 of the review, in the first commit (section 7).
3. The write set grows when the task starts (`docs/templates.md`, section 2).

## Scope (out)

- The medium and low findings: TASK-0477.

## Acceptance criteria

- [ ] The probe of E246 is a test that fails before the correction and passes after it.
- [ ] An injection that moves the cut for each `Subtract` makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · no commit on `main` yet

## Method

Mechanical: the correction that the review names.
Judgement:  none yet.
Weakest:    none yet.

## Progress

- 2026-11-28: opened by TASK-0475.
- 2026-11-28: started under the standing order of 2026-11-28; this commit records the answers Q1 A and Q2 A of the review, in
  this file and in the order of the roadmap.
