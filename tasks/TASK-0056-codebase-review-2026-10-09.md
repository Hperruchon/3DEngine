---
id: 0056
title: The codebase review of 2026-10-09 is a record
status: Done
phase: governance
opened: 2026-10-09
depends-on: [0053]
governed-by: []
writes:
  create:
    - tasks/TASK-0056-codebase-review-2026-10-09.md
    - docs/reviews/2026-10-09-codebase-review.md
    - tasks/TASK-0057-judge-holes-of-the-fifth-review.md
    - tasks/TASK-0058-guard-gaps-of-the-fifth-review.md
  modify:
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - tasks/TASK-0038-hybrid-topology.md
    - tasks/TASK-0054-desktop-serves-the-surface.md
    - tasks/TASK-0055-event-payload-schemas.md
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
    - eng/**
    - CLAUDE.md
    - docs/CHARTER.md
    - docs/templates.md
    - docs/adr/**
---

# TASK-0056 — The codebase review of 2026-10-09 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` permits six milestones after the ledger version
that the last review examined. The review of 2026-10-08 examined v0.46, and v0.52 is the sixth entry
after it. The next ledger entry must therefore be a codebase review, or the build fails. On 2026-10-09
the owner said "go with the review".

Since the last review, TASK-0051 changed the session and the address guard of the HTTP host, TASK-0037
made each operation consume its operands, and TASK-0053 moved the judge of the write set out of the
test assembly.

## Goal

The repository holds the fifth codebase review, with the state of each earlier finding.

## Scope (in)

1. `docs/reviews/2026-10-09-codebase-review.md`, in the form of `docs/templates.md`, section 7. It
   examines `main` at `f9521de`, the merge of v0.52.
2. Each new finding that no task covers gets an owner: a task, or a register entry within the limit of
   15 open entries. Two new tasks own the eighteen new findings, except E46, which TASK-0055 owns:
   TASK-0057 for the judge and the pipeline, and TASK-0058 for the guards of TASK-0051 and TASK-0037.
3. The roadmap gives the line for P0.17, which TASK-0053 did not add, and puts the two new tasks
   before TASK-0038 as P0.18 and P0.19. TASK-0038, TASK-0054 and TASK-0055 move to P0.20 to P0.22, and
   TASK-0038 depends on TASK-0058.
4. One ledger entry, v0.53.

## Scope (out)

- No change to code, to a test or to a gate. A review records a defect. A task corrects it.
- No change to an ADR, to `CLAUDE.md` or to `docs/templates.md`.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with five reviews in `docs/reviews/`.
- [x] Each finding of the review of 2026-10-08 has one line with its state and the evidence.
- [x] Each statement in the review that names a file and a line was read on disk in this session. A
      statement that a review agent reported and that was not checked again says so.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Outcome

Status: Done · v0.53 · the commit that carries this block.

## Method

**Mechanical.** The measures with the method of the earlier reviews; a script gives 5,361 production
lines in 99 files at `9ad025b`, which are the values of the previous review, before it counts
`f9521de`. The state of each of the 73 earlier findings, from a read of each cited line; a script
confirms that each identifier has a line.

**Judgement.** Two review agents, each one in its own git worktree, and not three, because only the
engine, the HTTP host, the judge and the pipeline changed. The severity of each new finding, and its
owner.

**Judgement, continued.** The agent of the judge rated T29 and T30 critical. This review rates them
high, as the last review rated T14: each one needs a deliberate act by a holder of commit access,
except an accidental merge of an old branch, which the judge then passes. Two tasks own the findings
and not a register entry, so the register stays at 14 of 15.

**Weakest.** I repeated two runs: the root commit of this repository lists no file for
`git diff-tree` without `--root`, and `System.Uri` gives the host `localhost` for
`http://evil@localhost:5000`. I did not repeat the other runs of the agents: E40 (the parser of
Kestrel), E41, E42, E43, E44, T29, T30 (the merges), T31, T32, T33 and T34. I read the cited code of
each one. T29 on Linux and the force push of T28 are from reading only. My own progress line of
2026-10-09 on R-0031 was false (T26), and the review corrects it.

## Progress

- 2026-10-09: opened and closed in one commit.
