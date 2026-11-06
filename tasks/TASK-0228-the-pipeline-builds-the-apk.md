---
id: 0228
title: The pipeline builds the APK of the phone at each push
status: Done
phase: R14f1
opened: 2026-11-06
depends-on: [0227]
governed-by: []
writes:
  create:
    - tasks/TASK-0228-the-pipeline-builds-the-apk.md
    - eng/android/check-apk.sh
  modify:
    - .github/workflows/ci.yml
    - docs/android-test-sheet.md
    - docs/INDEX.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0228 — The pipeline builds the APK of the phone at each push

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase R14f of ADR-0061 item 9 is the gate of Android: a job builds the APK at each push, a job runs the x86-64 emulator
with software Vulkan and a smoke test, and the pipeline compares the hash of Android with the hashes of the desktop
runners. The solution does not hold `3DEngine.Android`, because the runners of the gate have no workload of .NET for
Android, so no step of the pipeline builds the program (finding T145 of the codebase review of 2026-11-06 (b) named
that). This first slice of R14f builds the APK.

## Goal

Each push builds the APK on Ubuntu, checks its ABIs and its native libraries, and keeps it for the owner.

## Scope (in)

1. The job "Build the APK of the phone" in `.github/workflows/ci.yml`.
2. `eng/android/check-apk.sh`: the ABIs `arm64-v8a` and `x86_64` only, and the native libraries with the checksums of
   the notices.

## Scope (out)

- The emulator, the smoke test and the hash of Android (later slices of R14f).

## Acceptance criteria

- [x] The job passes on the branch, and the check refuses an APK with another ABI or a library with another checksum.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r14f1-apk-job) · v0.213

## Method

Mechanical: the steps of the gate on Ubuntu, and the check of the APK of R14e2 as a script.
Judgement:  the Android dependencies from the target InstallAndroidDependencies, on the runner only.
Weakest:    the job is not a required check of the branch main; the owner decides that in the settings of GitHub.

## Progress

- 2026-11-06: the job and the script. The script passes on the APK of R14e3 built on this computer.
- 2026-11-06: the job passed on the branch in run 42156698951. Closed at v0.213.
