---
id: 0172
title: A put of an asset waits for a name that another program holds, and does not replace a name that it cannot read
status: Active
phase: P0.74
opened: 2026-11-03
depends-on: [0160]
governed-by: [0004, 0044]
writes:
  create:
    - tasks/TASK-0172-parallel-puts-wait-for-a-held-name.md
  modify:
    - Engine.Core/Assets/AssetStore.cs
    - Engine.Tests/Assets/AssetStoreTests.cs
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0172 — A put of an asset waits for a name that another program holds, and does not replace a name that it cannot read

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0050: the run 41612851491 on `main`, of the merge of v0.172, failed on windows-latest. In round 10 of
`Parallel_Puts_Of_The_Same_New_Bytes_Each_Give_The_Name`, six of eight puts gave `E-ASSET-WRITE-FAILED` with "Access to
the path is denied". The owner gave the order "fix R-0050, then start P8d" on 2026-11-03.

A probe on Windows 11 gave the error of each state of the name. `File.Move` gives "Access to the path is denied", with
no path, when it replaces a file that a reader holds, also with `FileShare.Delete`. A read of a file that another
program holds with `FileShare.None` fails. The fix of E120 (TASK-0160) took a name that it could not read as a damaged
name, so it replaced it; that replace failed while the other puts read the name, and each failure made the next one.

## Goal

A put gives the name of its bytes when another program, or another put, holds the name for a short time.

## Scope (in)

1. A name has four states: absent, the bytes of the name, other bytes, and a file that cannot be read now. The put
   replaces only a name with other bytes (E115).
2. After a failed move the put reads the name again, and it waits and moves again, up to 12 moves in about 1.3 s.
3. R-0050 closes.

## Scope (out)

- A change of ADR-0044: the decision of item 2, a scratch file and a move, stays.

## Acceptance criteria

- [ ] A put while another program holds the name with no share, and a put of a damaged name that a reader holds, fail
      before the change and pass after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a probe of the errors of `File.Move` for each state of the name on Windows 11; a test that holds the name
            for 100 ms; four injections; 2,400 parallel puts of the probe.
Judgement:  the number of moves and the pauses.
Weakest:    the cause on the runner is inferred from the message, not run there: this computer did not fail in 2,400
            parallel puts before the fix.

## Progress

- 2026-11-03: opened by the order of the owner. The probe, the fix and the test.
