---
id: 0239
title: The smoke test of the emulator gives the exception of the program as public annotations
status: Active
phase: P0.95
opened: 2026-11-06
depends-on: [0235]
governed-by: [0074]
writes:
  create:
    - tasks/TASK-0239-the-smoke-test-gives-the-exception.md
  modify:
    - eng/android/smoke-emulator.sh
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0239 — The smoke test of the emulator gives the exception of the program as public annotations

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The emulator job failed again on the run of `main` after the merge of v0.222 (run 42270121672), with no change of the
phone. The program wrote the line of the start and the correct determinism hash, and then the runtime of Android wrote
"VM exiting with result code 0". The script failed on the line "ended" of PhoneHost, which holds the exception, but no
annotation showed that line.

GitHub keeps 10 annotations of each level for each step. The output of `am start` took seven notices, and the lines of
the determinism hash came before the line "ended", so the notices ended before it. The logs of a job need a sign-in,
and the session has none.

The review of Codex of v0.220 gave two leads on the same lines: `pm` and `pidof` have no limit of time (finding 1), and
`head -40` of the broad filter can drop the line of the cause (finding 2).

## Goal

A failure of the program on the emulator gives its exception, and the first lines of its stack, as public annotations.

## Scope (in)

1. The output of `am start` is one notice.
2. The notices of the program leave out the lines of the determinism hash and of the stack.
3. The line "ended" is the text of the failure, and the first eight lines of the stack are errors.
4. `pm` and `pidof` have a limit of 30 seconds; the filter of the log gives its last lines.

## Scope (out)

- The cause of the exception: the next failure gives it.
- A second start of the program after a failure: it can hide a defect.

## Acceptance criteria

- [ ] The probe of T149 (a fake `adb`) gives the same exit for each case before and after the change.
- [ ] A case with twelve lines of the hash and twelve lines of the stack gives at most 10 notices, and its failure text
      is the line "ended".

## Outcome

Status: Active

## Method

Mechanical: the counts of the annotations, and two limits of time.
Judgement:  the order of the notices: the start, the hash and the end of the program, before the lines that a file holds.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: the change, and the probe of T149 with two new cases. Before: 28 notices for the case of the stack, and
  the text "see the lines of the program". After: 4 notices, 8 errors of the stack, and the line "ended" as the text.
