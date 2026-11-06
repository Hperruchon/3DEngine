---
id: 0235
title: The smoke test of the emulator gives the cause of a start that fails
status: Active
phase: P0.93
opened: 2026-11-06
depends-on: [0231]
governed-by: [0074]
writes:
  create:
    - tasks/TASK-0235-the-smoke-test-gives-its-causes.md
  modify:
    - eng/android/smoke-emulator.sh
    - .github/workflows/ci.yml
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0235 — The smoke test of the emulator gives the cause of a start that fails

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The branch run of P0.91 (run 42167561638) passed the job "Start the APK on the emulator", with the whole smoke test in 42
seconds. The run of `main` after its merge (run 42168252840) changed only documents, and the job failed: "the program
did not write its line of the start in 120 seconds". The branch run of P0.92 (run 42168890096) failed in the same way.
The program wrote no line at all, so it did not start; a slow start is not the cause.

The job installs the workload of .NET for Android, the emulator and the system image with no pin. The output of
`am start` goes to `/dev/null`. The logs of a job and its artifacts need a sign-in, and the session has none, so the
annotations are the only output that a reader with no sign-in sees. The failure gives no cause there.

## Goal

A start that fails gives its cause as public annotations, and the next run shows which input changed.

## Scope (in)

1. The build job gives the version of the workload of .NET for Android.
2. The emulator job gives the versions of the emulator, of the platform tools and of the system image.
3. The smoke test gives the output of `am start -W`, with a limit of 60 seconds.
4. With no line of the start, the smoke test gives the package, the process and the lines of Android about the program
   and the runtime.

## Scope (out)

- A pin of the workload, of the emulator or of the image: a later task, when the cause is known.

## Acceptance criteria

- [ ] The probe of T149 (a fake `adb`) gives the same exits as before the change.
- [ ] Continuous integration shows the new annotations; the cause of the failure is in the ledger entry.

## Outcome

Status: Active

## Method

Mechanical: annotations of the versions and of the lines of Android, with `|| true`, so that a report never fails a job.
Judgement:  the lines of the log that the failure gives: the package, its process, the runtime of .NET, SDL, and the
            start of an activity or a process.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: the annotations, and the probe of T149 on the new script.
