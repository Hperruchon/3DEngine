---
id: 0072
title: The codebase review of 2026-10-13 is a record
status: Done
phase: governance
opened: 2026-10-12
depends-on: [0071]
governed-by: []
writes:
  create:
    - tasks/TASK-0072-codebase-review-2026-10-13.md
    - docs/reviews/2026-10-13-codebase-review.md
    - tasks/TASK-0073-gate-holes-of-the-eighth-review.md
    - tasks/TASK-0074-host-and-render-gaps-of-the-eighth-review.md
  modify:
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Tests/**
    - eng/**
    - CLAUDE.md
    - docs/CHARTER.md
    - docs/templates.md
    - docs/adr/**
---

# TASK-0072 — The codebase review of 2026-10-13 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` permits six milestones after the ledger version that
the last review examined. The review of 2026-10-12 examined v0.64, and v0.70 is the sixth entry after it,
so the next ledger entry must be a codebase review. The owner agreed on 2026-10-12 that the review comes
after R5 and before R6.

## Goal

The repository holds the eighth codebase review, with the state of each earlier finding.

## Scope (in)

1. `docs/reviews/2026-10-13-codebase-review.md`, in the form of `docs/templates.md`, section 7. It examines
   `main` at `2cc08b6`, the merge of v0.70.
2. Two new tasks own the new findings: TASK-0073 for the gates and the pipeline, and TASK-0074 for the
   hosts and the render side. The register stays at 14 of 15.
3. The roadmap gives the two tasks as P0.28 and P0.29.
4. A progress line on R-0031.
5. One ledger entry, v0.71.

## Scope (out)

- No change to code, to a test or to a gate.
- No decision for R6: questions Q2 and Q3 of the review ask the owner.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with eight reviews in `docs/reviews/`.
- [x] Each finding of the review of 2026-10-12 has one line with its state and the evidence.
- [x] Each statement in the review that names a file and a line was read on disk in this session. A
      statement that a review agent reported and that was not checked again says so.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Outcome

Status: Done · v0.71 · the commit that carries this block.

## Method

**Mechanical.** The measures with the method of the earlier reviews, with `3DEngine.Tests` counted as
tests; the script gives 7,018 production lines in 110 files at `4a4450d`, the values of the last review. The
state of each of the 116 earlier findings; a script confirms that each identifier of the last review has
one line. I ran again the probe of T48: a `Directory.Solution.targets` with an empty target `VSTest` made
`dotnet test` of the solution run no test, with exit code 0.

**Judgement.** Four review agents in their own worktrees: the gates, the engine with the hosts, the render
side, and the research for R6. Severity as in the last reviews. The six choices of R6 are questions and not
findings. A claim of the agent of the gates, that `CLAUDE.md` and `docs/templates.md` give two different
local commands, was false at `2cc08b6`, and the review does not hold it.

**Weakest.** I did not repeat the runs of the agents except T48, and several of their line numbers did not
match the files; the review cites only lines that I read. The review carries the date 2026-10-13 although I
wrote it on 2026-10-12, because the gate permits one review for each date (T53).

## Progress

- 2026-10-12: opened and closed in one commit.
