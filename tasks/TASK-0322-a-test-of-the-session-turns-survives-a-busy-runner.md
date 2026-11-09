---
id: 0322
title: The test of the window hold of the session turns survives a busy runner
status: Active
phase: P0.135
opened: 2026-11-09
depends-on: [0319]
governed-by: [0025]
writes:
  create:
    - tasks/TASK-0322-a-test-of-the-session-turns-survives-a-busy-runner.md
  modify:
    - 3DEngine.ImageTests/SessionTurnsTests.cs
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0322 — The test of the window hold of the session turns survives a busy runner

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The run on `main` of v0.301 (42543751923) failed on windows-latest in one test:
`SessionTurnsTests.A_Later_Session_Waits_For_The_Window_Of_The_Earlier_One`, at `Assert.False(later.IsCompleted)`. The run of
the branch on the same tree passed. The later session waited for the window with a limit of 10 s, and the test asserted 100
ms later that it still waited. When the continuation of that delay runs more than 10 s late, the waiter gives up first. No
earlier failure of this test is in the ledger.

## Goal

The test asserts the wait of the later session with a limit that a late continuation does not reach.

## Scope (in)

1. The limit of the later session in the test is 5 minutes.

## Scope (out)

- A change of `SessionTurns`.

## Acceptance criteria

- [ ] With a stall of 11 s before the assertion, the test fails with the old limit and passes with the new one.
- [ ] Continuous integration passes on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the line of the assertion and the limit of the wait.
Judgement:  the cause is a late continuation on a busy runner, inferred from the code and from the green run of the
            branch; the log of the runner does not show the delay.
Weakest:    to be written at the close.

## Progress

- 2026-11-09: the limit of the later session is 5 minutes. A stall of 11 s before the assertion failed the test with the
  limit of 10 s and passed it with the new limit.
