---
id: 0042
title: The codebase review of 2026-09-30 is a record, and the roadmap gives the true estimate
status: Ready
phase: governance
opened: 2026-09-30
depends-on: [0041]
governed-by: []
writes:
  create:
    - tasks/TASK-0042-codebase-review-2026-09-30.md
    - docs/reviews/2026-09-30-codebase-review.md
  modify:
    - docs/roadmap.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - CLAUDE.md
    - docs/CHARTER.md
    - docs/templates.md
    - docs/adr/**
---

# TASK-0042 — The codebase review of 2026-09-30 is a record, and the roadmap gives the true estimate

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` permits six milestones after the ledger version
that the last review examined. The last review examined v0.28, and v0.34 is the sixth entry after
it. The next ledger entry must therefore be a codebase review, or the build fails.

On 2026-09-30 the owner asked for the plan. `docs/roadmap.md` said that Track 0 and phases R1 to R6
take 25 to 35 evenings. The sum of the column "Evenings" for the ten phases that remain is 32 to
48. The sentence predates the five phases that v0.32 added. The owner said: fix the plan, then do
the codebase review.

## Goal

The repository holds the second codebase review, with the state of each earlier finding, and the
roadmap gives an estimate that agrees with its own table.

## Scope (in)

1. `docs/roadmap.md` gives the sum of the phases that remain, and names the method.
2. `docs/reviews/2026-09-30-codebase-review.md`, in the form of `docs/templates.md`, section 7. It
   examines `main` at the merge of v0.34. It gives the twelve measures with the method of the first
   review, the state of each of the fifteen earlier findings, the new findings, the next planned
   task with its research, and a recommended approach.
3. Each new finding that no task covers gets a register entry, within the limit of 15 open entries.
4. One ledger entry, v0.35.

## Scope (out)

- No change to code, to a test or to a gate. A review records a defect. A task corrects it.
- No change to an ADR, to `CLAUDE.md` or to `docs/templates.md`.
- No start of TASK-0034 or TASK-0040.

## Acceptance criteria

- [ ] `CodebaseReviewGateTests` passes with two reviews in `docs/reviews/`.
- [ ] Each finding of the first review has one line with its state and the evidence.
- [ ] Each statement in the review that names a file and a line was read on disk in this session. A
      statement that a review agent reported and that was not checked again says so.
- [ ] The estimate in `docs/roadmap.md` equals the sum of its column "Evenings" for the phases that
      are not in the list "Shipped".
- [ ] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Notes for the implementer

Four review agents read the code in parallel: the engine kernel, the two hosts, the gates, and the
Vulkan layer with the desktop host. Check each of their findings on disk before the review uses it
with the label [Observed].

## Progress

- 2026-09-30: the task is open.
- 2026-09-30: the roadmap gives 32 to 48 evenings for the ten phases that remain, which is the sum of its column "Evenings".
