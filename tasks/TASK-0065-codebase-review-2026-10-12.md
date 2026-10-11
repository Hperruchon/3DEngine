---
id: 0065
title: The codebase review of 2026-10-12 is a record
status: Done
phase: governance
opened: 2026-10-12
depends-on: [0028]
governed-by: []
writes:
  create:
    - tasks/TASK-0065-codebase-review-2026-10-12.md
    - docs/reviews/2026-10-12-codebase-review.md
    - tasks/TASK-0066-gate-holes-of-the-seventh-review.md
    - tasks/TASK-0067-host-gaps-of-the-seventh-review.md
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
    - eng/**
    - CLAUDE.md
    - docs/CHARTER.md
    - docs/templates.md
    - docs/adr/**
---

# TASK-0065 — The codebase review of 2026-10-12 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` permits six milestones after the ledger version
that the last review examined. The review of 2026-10-11 examined v0.58, and v0.64 is the sixth entry
after it. The next ledger entry must therefore be a codebase review, or the build fails. The owner
ordered the batch R2 to R6 on 2026-10-12, with a stop at each codebase review.

Since the last review, TASK-0061 corrected the judge, TASK-0062 the hosts, TASK-0064 moved the gates
that read only files into a program, and TASK-0028 added the tessellation.

## Goal

The repository holds the seventh codebase review, with the state of each earlier finding.

## Scope (in)

1. `docs/reviews/2026-10-12-codebase-review.md`, in the form of `docs/templates.md`, section 7. It
   examines `main` at `4a4450d`, the merge of v0.64.
2. Two new tasks own the new findings: TASK-0066 for the judge, the pipeline and the gates, and TASK-0067
   for the hosts. The register stays at 14 of 15.
3. The roadmap gives the two tasks as P0.26 and P0.27.
4. A progress line on R-0031.
5. One ledger entry, v0.65.

## Scope (out)

- No change to code, to a test or to a gate.
- No decision for R3: question Q2 of the review asks the owner.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with seven reviews in `docs/reviews/`.
- [x] Each finding of the review of 2026-10-11 has one line with its state and the evidence.
- [x] Each statement in the review that names a file and a line was read on disk in this session. A
      statement that a review agent reported and that was not checked again says so.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Outcome

Status: Done · v0.65 · the commit that carries this block.

## Method

**Mechanical.** The measures with the method of the earlier reviews; the script gives 6,503 production
lines in 105 files at `de35f51`, the values of the last review, before it counts `4a4450d`. The state of
each of the 106 earlier findings; a script confirms that each identifier of the last review, from its
section of earlier findings and from its headings, has one line. I ran again the two merges of T42 and
T43 with the judge built from `4a4450d`, and the first half of T41: a `global.json` with `paths` in
the current folder stopped `dotnet build` of a project in another folder.

**Judgement.** Three review agents, each one in its own git worktree: the gates with the judge and the
pipeline, the engine with the hosts, and the research for R3. Severity as in the last reviews: a hole
that needs a deliberate act is high, not critical; T42 is high although a careless merge also makes it.
The research for R3 gives four decisions, which are questions and not findings. The disagreement of
ADR-0009 line 68 and the dependency gate is part of question Q2 (c), because no test fails today.

**Weakest.** I did not repeat the planted SDK of T41, the planted packages of T44, the forms of T45 to
T47, or the runs of E58 to E60; I read the cited code of each one. Several line numbers in the report of
the engine agent did not match the files, so the review cites only lines that I read. T41 is a defect of
my own step of TASK-0064.

## Progress

- 2026-10-12: opened and closed in one commit.
