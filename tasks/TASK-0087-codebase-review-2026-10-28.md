---
id: 0087
title: The codebase review of 2026-10-28 is a record
status: Done
phase: governance
opened: 2026-10-28
depends-on: [0086]
governed-by: []
writes:
  create:
    - tasks/TASK-0087-codebase-review-2026-10-28.md
    - docs/reviews/2026-10-28-codebase-review.md
    - tasks/TASK-0088-test-framework-gate-of-the-tenth-review.md
    - tasks/TASK-0089-gate-holes-of-the-tenth-review.md
    - tasks/TASK-0090-host-and-desktop-gaps-of-the-tenth-review.md
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
    - 3DEngine.ImageTests/**
    - eng/**
    - CLAUDE.md
    - docs/CHARTER.md
    - docs/templates.md
    - docs/adr/**
---

# TASK-0087 — The codebase review of 2026-10-28 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` permits four product milestones and twelve ledger entries
after the ledger version that the last review examined. The review of 2026-10-17 examined v0.76. After it came 4
product milestones (R7, R7b, R8a, R8b) and 12 entries (v0.77 to v0.88), so the next ledger entry must be a
codebase review. The order of the owner of 2026-10-27 (KD2) puts the review here, before P8a.

## Goal

The repository holds the tenth codebase review, with the state of each earlier finding, and the research and the
questions for roadmap phase P8a.

## Scope (in)

1. `docs/reviews/2026-10-28-codebase-review.md`, in the form of `docs/templates.md`, section 7. It examines `main`
   at `e0a2c98`, the merge of v0.88.
2. Three new tasks own the new findings, in the order of `docs/templates.md`, section 7: TASK-0088 holds the high
   finding T62 and comes before P8a; TASK-0089 holds T63 to T67; TASK-0090 holds E72 to E74, V18, V26 to V28 and
   V30. The register stays at 14 of 15.
3. The roadmap gives the three tasks as P0.34, P0.35 and P0.36, with their place in the order.
4. A progress line on R-0031.
5. One ledger entry, v0.89.

## Scope (out)

- No change to code, to a test or to a gate.
- No decision for P8a: questions Q1 to Q8 of the review ask the owner.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with ten reviews in `docs/reviews/`.
- [x] Each finding of the review of 2026-10-17 has one line with its state and the evidence.
- [x] Each statement in the review that names a file and a line was read on disk in this session. A statement
      that a review agent reported and that was not checked again says so.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Outcome

Status: Done · v0.89 · the commit that carries this block.

## Method

**Mechanical.** The measures with the method of the earlier reviews; the script gives 8,664 production lines in
127 files and 10,608 test lines at `2b8e928`, the values of the last review. The state of each earlier finding,
with one line for each identifier of the last review. I ran T62 again in a scratch worktree: with a test framework
in a support file of `Engine.Tests`, the dispatch gate passed a file that it fails without it, and the gate program
passed the tree.

**Judgement.** Four review agents in their own worktrees: the gates, the engine with the hosts, the render side
with the desktop, and the research for P8a. Severity as in the last reviews. A finding that is fixed for the forms
that it named counts as fixed, and each later form has a new identifier (T62, T63, T67, V30). The touch path that
no screen has checked is V29, because the owner chose on 2026-10-27 that the next review carries it. The decisions
of P8a are questions and not findings.

**Weakest.** I did not repeat the runs of the agents except T62, and I read each line that the review cites. Two
cited ranges of the agents were wrong (`GpuBuffer.cs` and the append of the log); I corrected them from the files.

## Progress

- 2026-10-28: opened and closed in one commit.
