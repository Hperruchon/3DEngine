---
id: 0104
title: The codebase review of 2026-10-29 is a record
status: Done
phase: governance
opened: 2026-10-29
depends-on: [0103]
governed-by: []
writes:
  create:
    - tasks/TASK-0104-codebase-review-2026-10-29.md
    - docs/reviews/2026-10-29-codebase-review.md
    - tasks/TASK-0105-test-sources-of-the-output-path.md
    - tasks/TASK-0106-sketch-gaps-of-the-twelfth-review.md
    - tasks/TASK-0107-gate-forms-of-the-twelfth-review.md
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

# TASK-0104 — The codebase review of 2026-10-29 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` permits four product milestones and twelve ledger entries
after the ledger version that the last review examined. The second review of 2026-10-28 examined v0.96. After it
came 4 product milestones (K2a, K2b, K3a, K0a) and 9 entries (v0.97 to v0.105). K0b, the next product phase, makes a
fifth milestone, so the gate fails at its ledger entry. The owner chose on 2026-10-29 to do the review before K0b.

## Goal

The repository holds the twelfth codebase review, with the state of each earlier finding, and the research and
the questions for roadmap phase K0b.

## Scope (in)

1. `docs/reviews/2026-10-29-codebase-review.md`, in the form of `docs/templates.md`, section 7. It examines
   `main` at `730e508`, the merge of v0.105.
2. The new findings go into tasks or register entries in the order of `docs/templates.md`, section 7.
3. One ledger entry.

## Scope (out)

- No change to code, to a test or to a gate.
- No decision for K0b: the questions of the review ask the owner.
- No fix: the owner answers first.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with twelve reviews in `docs/reviews/`.
- [x] Each finding of the second review of 2026-10-28 has one line with its state and the evidence.
- [x] Each statement in the review that names a file and a line was read on disk in this session. A statement
      that a review agent reported and that was not checked again says so.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Notes for the implementer

- The review gate requires the three rule measures for a review of the ledger v0.104 or later (TASK-0098).

## Outcome

Status: Done · v0.106 · the commit that carries this block.

## Method

**Mechanical.** The measures with the method of the earlier reviews; the script gives 13,749 production lines in
165 files and 15,972 test lines at `c0fbb64`, the values of the last review. The rule measures with the method of
the rules review of 2026-09-25: the counts of "must" and "do not" at `c0fbb64` equal the last review, and the count
of lines does not (2,269, not 2,291). One line for each identifier of the last review: a script copies each line
"Fixed" with "No change", and the other 79 lines are by hand. A script printed each cited line of the code at
`730e508`, and I read each one; one path was wrong, and I corrected it.

**Judgement.** Four review agents in their own worktrees: the gates, the engine with the hosts, the render side
with the desktop, and the research for K0b. Severity as in the last reviews: a failed test that passes with the
exact count is high (T79). E86 gives a wrong sketch with no error, but only after a client sends one identifier
again after 1,024 other results, so it is medium. T79 has a task before K0b, and the rest has two tasks after it.
The six questions are one register entry, R-0044, which makes 15 of 15.

**Weakest.** I repeated the runs of E86 and T79 only; for T79 I did not repeat the framework that makes a failed test
pass. The times of E89 and V44 come from one computer, and only V44 has a Release build. The research for K0b reads
code and records and runs nothing. I missed the red run of T87 at the merge of v0.104, and the agent of the gates
found it.

## Progress

- 2026-10-29: opened. The owner chose the review before K0b. Four review agents run in their own worktrees at
  `730e508`: the gates, the engine with the hosts, the render side with the desktop, and the research for K0b.
- 2026-10-29: the review, three tasks for its findings (TASK-0105 to TASK-0107), the roadmap rows P0.41 to P0.43,
  register entry R-0044, and ledger entry v0.106.
