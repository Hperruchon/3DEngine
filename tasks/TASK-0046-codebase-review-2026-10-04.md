---
id: 0046
title: The codebase review of 2026-10-04 is a record
status: Done
phase: governance
opened: 2026-10-04
depends-on: [0045]
governed-by: []
writes:
  create:
    - tasks/TASK-0046-codebase-review-2026-10-04.md
    - docs/reviews/2026-10-04-codebase-review.md
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

# TASK-0046 — The codebase review of 2026-10-04 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` permits six milestones after the ledger version
that the last review examined. The review of 2026-09-30 examined v0.34, and v0.40 is the sixth entry
after it. The next ledger entry must therefore be a codebase review, or the build fails. On
2026-10-04 the owner chose the review and not a change of the rule.

Since the last review, TASK-0034 changed the commit path of the engine and the lock order of the
HTTP host. That is the largest change to engine code since 2026-09-23.

## Goal

The repository holds the third codebase review, with the state of each earlier finding.

## Scope (in)

1. `docs/reviews/2026-10-04-codebase-review.md`, in the form of `docs/templates.md`, section 7. It
   examines `main` at `ae5c521`, the merge of v0.39 and v0.40. It gives the twelve measures with the
   method of the earlier reviews, the state of each of the 33 findings of the review of 2026-09-30,
   the new findings, the next planned task with its research, and a recommended approach.
2. Each new finding that no task covers gets an owner: a task, or a register entry within the limit
   of 15 open entries.
3. `docs/roadmap.md`: the section "Shipped" gets the line for P0.13, which TASK-0034 could not add.
4. One ledger entry, v0.41.

## Scope (out)

- No change to code, to a test or to a gate. A review records a defect. A task corrects it.
- No change to an ADR, to `CLAUDE.md` or to `docs/templates.md`.
- No start of TASK-0035 or of another task.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with three reviews in `docs/reviews/`.
- [x] Each finding of the review of 2026-09-30 has one line with its state and the evidence.
- [x] Each statement in the review that names a file and a line was read on disk in this session. A
      statement that a review agent reported and that was not checked again says so.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Notes for the implementer

No file changed in `3DEngine/`, `3DEngine.Vulkan/`, `3DEngine.Core/` or `Engine.Contracts/` after
`14e2c73`, so three review agents read the code: the engine kernel, the two hosts, and the tests with
the pipeline. Check each of their findings on disk before the review uses it with the label
[Observed].

## Outcome

Status: Done · v0.41 · the commit that carries this block.

## Method

**Mechanical.** The twelve measures with the method of the earlier reviews. The state of each of the
33 earlier findings, from a read of each cited line at `ae5c521`. The roadmap line for P0.13 and the
sum of the nine phases that remain.

**Judgement.** Three review agents and not four, because the Vulkan and desktop code did not change.
The severity of each new finding. The grouping: the small findings of one area share one identifier
(E24, T13). Two register entries and not thirteen, so that the register stays at 14 of 15: R-0033 for
the engine and the hosts, and R-0034, with the class `risk`, for the pipeline that can hang. The gate
holes go to R-0031, because they match its title, and its exit names the third review too.

**Weakest.** The runs that a review agent made and that I did not repeat: E18 to E22, the host parts
of E17, T10, and the four forms of T4. I read the cited code of each one. T12 is from the code only; no
run reproduced it. The first draft cited three lines wrongly and claimed too much in T12; a script that
printed each cited line found the three citations before the commit.

## Progress

- 2026-10-04: the task is open. `git diff --stat 14e2c73 ae5c521` shows 26 changed files outside
  `docs/` and `tasks/`, and none in the four projects above.
- 2026-10-04: three review agents reported 33 states and 28 new items. I read each cited line with a
  script that prints it, and I repeated three runs: the bind guard with `Kestrel:Endpoints` (no
  refusal), the project file of the tests under TASK-0035 (the write-set gate passes), and a
  `Directory.Build.props` in `Engine.Core` (16 gate tests of 16 pass). I removed the file.
- 2026-10-04: closed. The review, two new register entries, three progress lines, the roadmap line for
  P0.13 and ledger entry v0.41 are in the repository.
