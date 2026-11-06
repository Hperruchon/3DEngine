---
id: 0229
title: The APK holds its assemblies, and the pipeline starts it on the emulator
status: Active
phase: R14f2
opened: 2026-11-06
depends-on: [0228]
governed-by: [0061, 0073]
writes:
  create:
    - tasks/TASK-0229-the-apk-starts-on-the-emulator.md
    - eng/android/smoke-emulator.sh
  modify:
    - eng/android/check-apk.sh
    - .github/workflows/ci.yml
    - 3DEngine.Android/3DEngine.Android.csproj
    - 3DEngine.Android/PhoneHost.cs
    - docs/INDEX.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0229 — The APK holds its assemblies, and the pipeline starts it on the emulator

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase R14f of ADR-0061 item 9 includes a job that runs the x86-64 emulator with software Vulkan and a smoke test. No
emulator is on this computer, and no device ran the program. While this task made the smoke test, a read of the APK of
R14e3 found 38 files and no assembly: a Debug build of .NET for Android leaves the assemblies out for the fast deployment
of an IDE. So the APK of R14e2, R14e3 and R14f1, installed with adb or from a file, could not start on a phone.

## Goal

The APK holds its assemblies, the check of R14f1 requires them, and each push starts the APK on the emulator and reads
its log.

## Scope (in)

1. `EmbedAssembliesIntoApk` in the project of the program, and the check of the assemblies in `eng/android/check-apk.sh`.
2. Two lines of `PhoneHost` in the log: the start of the session and the first frame.
3. The job "Start the APK on the emulator" and `eng/android/smoke-emulator.sh`.

## Scope (out)

- The hash of Android (a later slice of R14f), and a test of the gestures on the emulator.

## Acceptance criteria

- [ ] The check refuses the APK of R14e3, which holds no assembly, and passes the new APK.
- [ ] The job of the emulator gives the line of the start of the program.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the steps of the emulator on Ubuntu with KVM, and the read of the log with adb.
Judgement:  the line of the start as the condition, and the first frame as a fact of the report.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: the APK of R14e3 holds 38 files and no assembly; with `EmbedAssembliesIntoApk` it holds 434 files, among
  them `lib_<assembly>.dll.so` for each assembly and each ABI. The check of the assemblies refuses the APK of R14e3 with
  22 errors (11 assemblies for each of 2 ABIs) and passes the new APK. The new APK builds with zero warnings.
- 2026-11-06: the first run of the job of the emulator (42157609653) reached the limit of 40 minutes inside the smoke
  test and showed no cause, because the waits had no limit. Each wait has a limit now, the start of the emulator is
  checked, and a failure gives the last lines of the emulator as annotations.
