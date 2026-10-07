---
id: 0050
title: The codebase review of 2026-10-08 is a record
status: Done
phase: governance
opened: 2026-10-08
depends-on: [0049]
governed-by: []
writes:
  create:
    - tasks/TASK-0050-codebase-review-2026-10-08.md
    - docs/reviews/2026-10-08-codebase-review.md
  modify:
    - docs/register.md
    - docs/CURRENT-STATE.md
    - THIRD-PARTY-NOTICES.md
    - tasks/TASK-0036-honest-geometry-backend.md
    - tasks/TASK-0038-hybrid-topology.md
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

# TASK-0050 — The codebase review of 2026-10-08 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` permits six milestones after the ledger version
that the last review examined. The review of 2026-10-04 examined v0.40, and v0.46 is the sixth entry
after it. The next ledger entry must therefore be a codebase review, or the build fails. On 2026-10-08
the owner put the review second in the order of the night, after TASK-0049.

Since the last review, TASK-0035 changed the replay and the version of the Document, TASK-0036 made
each host require the native backend, and TASK-0048 and TASK-0049 rebuilt the native package and its
recipe.

## Goal

The repository holds the fourth codebase review, with the state of each earlier finding.

## Scope (in)

1. `docs/reviews/2026-10-08-codebase-review.md`, in the form of `docs/templates.md`, section 7. It
   examines `main` at the merge of v0.46. It gives the twelve measures with the method of the earlier
   reviews, the state of each of the 46 findings of the review of 2026-10-04, the new findings, the
   next planned task with its research, and a recommended approach.
2. Each new finding that no task covers gets an owner: a task, or a register entry within the limit
   of 15 open entries.
3. The review corrects three false statements about the repository that it finds: the package name in
   `THIRD-PARTY-NOTICES.md`, the sentence about `WebApplicationFactory` in TASK-0036, and the write set
   of TASK-0038, which lost the permit for the project file of the tests (finding T15).
4. One ledger entry, v0.47.

## Scope (out)

- No change to code, to a test or to a gate. A review records a defect. A task corrects it.
- No change to an ADR, to `CLAUDE.md` or to `docs/templates.md`.
- No start of the next task.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with four reviews in `docs/reviews/`.
- [x] Each finding of the review of 2026-10-04 has one line with its state and the evidence.
- [x] Each statement in the review that names a file and a line was read on disk in this session. A
      statement that a review agent reported and that was not checked again says so.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Notes for the implementer

No file changed in `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` after `ae5c521`, so three
review agents read the code: the engine kernel, the two hosts, and the tests with the pipeline and the
native build. Check each of their findings on disk before the review uses it with the label
[Observed].

## Outcome

Status: Done · v0.47 · the commit that carries this block.

## Method

**Mechanical.** The twelve measures with the method of the earlier reviews, and the tests of each
folder from a result file of `dotnet test`. The state of each of the 46 earlier findings, from a read
of each cited line; a script confirms that each identifier has a line in the section of the earlier
findings.

**Judgement.** Three review agents, each one in its own git worktree, so that an injection could not
touch the working tree of the session. The severity of each new finding. One register entry and not
fifteen: R-0036 holds E25 to E39, so that the register stays at 14 of 15. The new pipeline findings go
to R-0031, because they match its title. Five findings of the native build (T18 to T22) were corrected
in TASK-0049 before its merge, so the review records them as fixed.

**Weakest.** The runs that a review agent made and that I did not repeat: E25, E26, E27, E28, E29,
E32, E36, E37, E38, T14, T16, T17 and T25. I read the cited code of each one. One of my own claims was
false and stood in a task file for two days: TASK-0036 said that `WebApplicationFactory` stops
`Program.cs` at its build. An injection after the start check failed 46 of 59 HTTP tests, so the
factory runs that code.

## Progress

- 2026-10-08: the task is open. `git diff --stat ae5c521` shows no change in `3DEngine/`,
  `3DEngine.Vulkan/` or `3DEngine.Core/`.
- 2026-10-08: three review agents reported the states and the new items. I read each cited line, and
  I repeated the check of the sentence of TASK-0036 with an injection, which I removed.
- 2026-10-08: closed. The review, R-0036, a progress line on R-0031 and R-0033, the three corrections
  and ledger entry v0.47 are in the repository.
