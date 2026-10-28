---
id: 0095
title: The second codebase review of 2026-10-28 is a record
status: Done
phase: governance
opened: 2026-10-28
depends-on: [0094]
governed-by: []
writes:
  create:
    - tasks/TASK-0095-codebase-review-2026-10-28-b.md
    - docs/reviews/2026-10-28-b-codebase-review.md
    - tasks/TASK-0096-open-applies-each-entry.md
    - tasks/TASK-0097-test-sources-that-msbuild-compiles.md
    - tasks/TASK-0098-gate-forms-of-the-eleventh-review.md
    - tasks/TASK-0099-document-file-and-pad-gaps.md
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

# TASK-0095 — The second codebase review of 2026-10-28 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` permits four product milestones and twelve ledger entries
after the ledger version that the last review examined. The review of 2026-10-28 examined v0.88. After it came 4
product milestones (P8a, P8f, P8c, R10) and 8 entries (v0.89 to v0.96). A fifth product milestone makes the gate
fail, so the review comes before K2, the next product phase (KD2). The last review has the same date, so the name
of this review has the suffix `-b` (TASK-0073).

## Goal

The repository holds the eleventh codebase review, with the state of each earlier finding, and the research and
the questions for roadmap phase K2.

## Scope (in)

1. `docs/reviews/2026-10-28-b-codebase-review.md`, in the form of `docs/templates.md`, section 7. It examines
   `main` at `c0fbb64`, the merge of v0.96.
2. The new findings go into tasks or register entries in the order of `docs/templates.md`, section 7.
3. One ledger entry.

## Scope (out)

- No change to code, to a test or to a gate.
- No decision for K2: the questions of the review ask the owner.
- No fix: the owner answers first.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with eleven reviews in `docs/reviews/`.
- [x] Each finding of the review of 2026-10-28 has one line with its state and the evidence.
- [x] Each statement in the review that names a file and a line was read on disk in this session. A statement
      that a review agent reported and that was not checked again says so.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Outcome

Status: Done · v0.97 · the commit that carries this block.

## Method

**Mechanical.** The measures with the method of the earlier reviews; the script gives 11,494 production lines in
147 files and 13,311 test lines at `e0a2c98`, the values of the last review. One line for each identifier of the
last review: a script copies each line "Fixed" of the last review with "No change", and each other line is written
by hand. I ran three findings again in a scratch worktree at `c0fbb64`: E75 and E76 with a probe test, and T68 with
the gate program and a build.

**Judgement.** Four review agents in their own worktrees: the gates, the engine with the hosts, the render side
with the desktop, and the research for K2. Severity as in the last reviews: a wrong Document with no error, or a
failed test that passes with the exact count, is high. Each high finding has a task before K2 (TASK-0096 and
TASK-0097), and the rest has two tasks after K2. T76 closes with the ledger entry. The ten questions are one
register entry, R-0042, because the register holds 13 of 15.

**Weakest.** I did not repeat the runs of the agents except E75, E76 and T68, and I read each line that the review
cites. Five cited lines of the agents were wrong or loose (`DesktopDocument.cs`, `Camera.cs`, the path of the
project file of T69, the line of `DependencyDirectionGateTests` and the count of new forms); I corrected them from
the files. The research for K2 used web sources that I did not open.

## Progress

- 2026-10-28: opened. The clean build gives 0 warnings, and `dotnet test` passes 829 tests at `c0fbb64`.
- 2026-10-28: the review, four tasks for its findings (TASK-0096 to TASK-0099), the roadmap rows P0.37 to P0.40,
  register entry R-0042, a progress line on R-0031, and ledger entry v0.97.
