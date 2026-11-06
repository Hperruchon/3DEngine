---
id: 0233
title: The pipeline compares the determinism hash of the phone with the hashes of the desktop runners
status: Active
phase: R14f3
opened: 2026-11-06
depends-on: [0230]
governed-by: [0004, 0061, 0073, 0074]
writes:
  create:
    - tasks/TASK-0233-the-hash-of-the-phone.md
    - docs/adr/0074-the-probe-of-determinism-is-in-the-engine.md
    - Engine.Core/Determinism/CrossPlatformProbe.cs
  modify:
    - Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
    - 3DEngine.Android/MainActivity.cs
    - 3DEngine.Android/PhoneHost.cs
    - eng/android/smoke-emulator.sh
    - eng/compare-determinism.js
    - .github/workflows/ci.yml
    - docs/adr/README.md
    - tasks/TASK-0231-gate-and-engine-forms-of-the-twenty-fifth-review.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0233 — The pipeline compares the determinism hash of the phone with the hashes of the desktop runners

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0061 item 8 says that the hash of Android must be equal to the hashes of the desktop runners. The probe of the hash
is in `Engine.Tests`, which the program of the phone cannot reference. Since R14f2 the pipeline starts the APK on the
emulator of Android for x86-64 up to its first frame.

## Goal

Each push compares the determinism hash of the program on the emulator with the hashes of the three desktop runners, and
a difference fails the pipeline.

## Scope (in)

1. ADR-0074, with the options, answered by the standing order of 2026-11-05.
2. `CrossPlatformProbe` in `Engine.Core`, and the test that calls it, with the same bytes of the hash.
3. The extra `probe` of the activity, the lines of the hash in the log, the hash file of the smoke test, and four
   platforms in the comparison.

## Scope (out)

- The hash of arm64, which needs a device or an emulator of arm64.

## Acceptance criteria

- [ ] The hash of the desktop does not change with the move of the probe.
- [ ] The job of the comparison gives one hash for the four platforms.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the move of the probe, and the hash file of the phone in the form of the files of the runners.
Judgement:  the probe in `Engine.Core`, and not a copy in the program of the phone.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: ADR-0074 with options A, B and C; the standing order chose A. The probe moved; the hash file of the test is
  the same bytes before and after the move (59876e9f...). The APK builds with zero warnings.
