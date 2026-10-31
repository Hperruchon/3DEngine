---
id: 0136
title: An exception of a handler in a replay is a divergence, and a replay needs a backend
status: Done
phase: P0.55
opened: 2026-10-31
depends-on: [0135]
governed-by: [0004, 0015, 0020]
writes:
  create:
    - tasks/TASK-0136-replay-debt-of-r0036.md
  modify:
    - Engine.Core/Replay.cs
    - Engine.Tests/Commands/EngineDebtTests.cs
    - Engine.Tests/Persistence/BackendRecordTests.cs
    - Engine.Tests/ReplayDeterminism/ReplayVersionTests.cs
    - Engine.Tests/ReplayTests.cs
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0136 — An exception of a handler in a replay is a divergence, and a replay needs a backend

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0036 holds twelve open findings of the codebase review of 2026-10-08. R11b (TASK-0134) waits for the
visual check of the owner, so this task takes the findings of the replay that change no file of the desktop: E27, E28
and E30. The review recommends a correction for each one, and the owner told the session on 2026-10-31 to use its
recommendation for each question.

## Goal

An exception of a handler in a replay names its entry and its command, and each caller of a replay names the backend.

## Scope (in)

1. E28: `Replay.ReplayLog` wraps an exception of an entry that is not a cancellation in
   `ReplayDivergenceException`. Its `Result` is null, and `InnerException` holds the exception. The test of
   finding E7 asserts the exact type.
2. E30: the parameter `backend` of the two methods `Replay.ReplayLog` is required, and null is refused. The
   tests that used the default name `NullGeometryBackend.Instance`.

## Scope (out)

- E27, the refusal of a backend that holds a body. `IGeometryBackend` has no member that tells if it holds a body,
  so the refusal needs a change of `Engine.Contracts` and an ADR. That is a stop condition of `CLAUDE.md`, and the
  finding stays in R-0036 for the owner.
- The other findings of R-0036.

## Acceptance criteria

- [x] A test of each finding fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p055-replay-debt) · v0.137

## Method

Mechanical: one catch around the apply of an entry, a second constructor of the divergence, and a required parameter.
Judgement:  the divergence of a thrown entry has no result, so `Result` is null and `InnerException` holds the
            exception; a cancellation passes the catch, as E29 requires; E27 needs a member of the contract, which is a
            stop condition, so it stays for the owner.
Weakest:    E27 and the other findings of R-0036 stay open.

## Progress

- 2026-10-31: E28 and E30 with 2 new test cases; E27 skipped, see "Scope (out)".
- 2026-10-31: 2 injections caught; `governed-by` names ADR-0004, ADR-0015 and ADR-0020; closed at v0.137.
