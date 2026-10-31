---
id: 0135
title: The log of the Document is read-only, a cancelled replay is a cancellation, and a query of a lost body gives an error
status: Done
phase: P0.54
opened: 2026-10-31
depends-on: [0133]
governed-by: [0004, 0008, 0015, 0016, 0018, 0020, 0021, 0028, 0033, 0037]
writes:
  create:
    - tasks/TASK-0135-engine-debt-of-r0036.md
    - Engine.Tests/Commands/EngineDebtTests.cs
  modify:
    - Engine.Contracts/Document.cs
    - Engine.Core/Replay.cs
    - Engine.Core/Queries/GetBoundingBoxQueryHandler.cs
    - Engine.Core/Queries/GetTessellationQueryHandler.cs
    - docs/adr/0020-document-version-meaning.md
    - docs/diagnostics.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0135 — The log of the Document is read-only, a cancelled replay is a cancellation, and a query of a lost body gives an error

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0036 holds fifteen findings of the codebase review of 2026-10-08 with no task. R11b (TASK-0134) waits
for the visual check of the owner, so this task takes three of them that change no file of the desktop: E26, E29 and
E32. Each has a fix that its review gives or that its text makes clear.

## Goal

A client cannot change the log through a cast, a replay that its caller cancels ends with a cancellation, and a query
of a body that the backend does not hold gives an error and not an exception.

## Scope (in)

1. E26: `Document.Log` is a read-only view of the list. The public shape does not change.
2. E29: `Replay.ReplayLog` throws `OperationCanceledException` when a command gives Cancelled and the token of the
   caller is cancelled.
3. E32: `GetBoundingBox` and `GetTessellation` give `E-GEOM-NATIVE-OP` when the backend throws.

## Scope (out)

- The other twelve findings of R-0036.

## Acceptance criteria

- [x] A test of each finding fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p054-engine-debt) · v0.136

## Method

Mechanical: one read-only view, one check of the token, and two catches of the backend.
Judgement:  a change of Engine.Contracts with no change of the public shape still needs an ADR file in the commit, so
            ADR-0020, whose rule E26 broke, records it; a cancellation that the caller did not ask for stays a
            divergence.
Weakest:    the other twelve findings of R-0036 stay open.

## Progress

- 2026-10-31: E26, E29 and E32 with 3 new test cases.
- 2026-10-31: 4 injections caught; the write set gains ADR-0020; closed at v0.136.
