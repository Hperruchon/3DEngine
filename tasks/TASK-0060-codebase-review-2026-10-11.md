---
id: 0060
title: The codebase review of 2026-10-11 is a record
status: Done
phase: governance
opened: 2026-10-11
depends-on: [0054]
governed-by: []
writes:
  create:
    - tasks/TASK-0060-codebase-review-2026-10-11.md
    - docs/reviews/2026-10-11-codebase-review.md
    - tasks/TASK-0061-judge-holes-of-the-sixth-review.md
    - tasks/TASK-0062-host-gaps-of-the-sixth-review.md
  modify:
    - docs/register.md
    - docs/roadmap.md
    - tasks/TASK-0055-event-payload-schemas.md
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
    - eng/**
    - CLAUDE.md
    - docs/CHARTER.md
    - docs/templates.md
    - docs/adr/**
---

# TASK-0060 — The codebase review of 2026-10-11 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` permits six milestones after the ledger version
that the last review examined. The review of 2026-10-09 examined v0.52, and v0.58 is the sixth entry
after it. The next ledger entry must therefore be a codebase review, or the build fails. On 2026-10-11
the owner said "merge, then go with the review".

Since the last review, TASK-0057 rewrote the judge of the write set, TASK-0059 corrected the tests of
timing, TASK-0058 closed the gaps of the guards, TASK-0038 made the surface a library, and TASK-0054
mounted it in the desktop host.

## Goal

The repository holds the sixth codebase review, with the state of each earlier finding.

## Scope (in)

1. `docs/reviews/2026-10-11-codebase-review.md`, in the form of `docs/templates.md`, section 7. It
   examines `main` at `de35f51`, the merge of v0.58.
2. Two new tasks own the new findings: TASK-0061 for the judge and the pipeline, and TASK-0062 for the
   hosts. The register stays at 13 of 15.
3. The roadmap gives the line for each phase that shipped, and puts the two tasks before R2.
4. A progress line on R-0031 corrects my statement that a branch protection closes the hole of the
   workflow file.
5. One ledger entry, v0.59.

## Scope (out)

- No change to code, to a test or to a gate.
- No change to an ADR, to `CLAUDE.md` or to `docs/templates.md`, although the review finds false texts
  in them; the owner tasks correct them.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with six reviews in `docs/reviews/`.
- [x] Each finding of the review of 2026-10-09 has one line with its state and the evidence.
- [x] Each statement in the review that names a file and a line was read on disk in this session. A
      statement that a review agent reported and that was not checked again says so.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Outcome

Status: Done · v0.59 · the commit that carries this block.

## Method

**Mechanical.** The measures with the method of the earlier reviews; the script gives 6,093
production lines in 102 files at `f9521de`, the values of the last review, before it counts `de35f51`.
The state of each of the 91 earlier findings; a script confirms that each identifier of the last
review, from its section of earlier findings and from its headings, has one line.

**Judgement.** Two review agents, each one in its own git worktree. The agent of the judge built the
old judge from `f9521de` and ran both judges on the same scratch repositories, which showed that T36 is
a lost rule. Severity as in the last two reviews: a hole that needs a deliberate act is high, not
critical. T31 is not a new identifier, because it is the same class; its state is "open again". Two
tasks and not register entries, so the register stays at 13 of 15.

**Weakest.** I repeated no run of the agents in this review. I read the cited code of each finding,
and I checked the logic of T36 on `Judge.cs:201-215`: in a merge of `main` into a branch, the side of
the second parent is `main`, so the files that `main` changed leave the own change of the merge. The
claims of E50 about Ctrl+C and of T40 about failed calls of git are from reading only. My own texts
were false twice: the state "Fixed" of T31 in the last review, and the sentence about a branch
protection on R-0031.

## Progress

- 2026-10-11: opened and closed in one commit.
