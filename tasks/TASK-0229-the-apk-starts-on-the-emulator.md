---
id: 0229
title: The APK holds its assemblies, and the pipeline starts it on the emulator
status: Done
phase: R14f2
opened: 2026-11-06
depends-on: [0228]
governed-by: [0004, 0007, 0008, 0009, 0014, 0017, 0018, 0019, 0023, 0027, 0029, 0036, 0057, 0061, 0072, 0073]
writes:
  create:
    - tasks/TASK-0229-the-apk-starts-on-the-emulator.md
    - eng/android/smoke-emulator.sh
  modify:
    - eng/android/check-apk.sh
    - .github/workflows/ci.yml
    - 3DEngine.Android/3DEngine.Android.csproj
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.Vulkan/Log.cs
    - docs/adr/0072-the-drawing-code-is-a-library.md
    - 3DEngine.Vulkan/3DEngine.Vulkan.csproj
    - 3DEngine.App/3DEngine.App.csproj
    - 3DEngine.Core/3DEngine.Core.csproj
    - Engine.Core/Engine.Core.csproj
    - Engine.Contracts/Engine.Contracts.csproj
    - Engine.Kernel/Engine.Kernel.csproj
    - Engine.Geometry.Manifold/Engine.Geometry.Manifold.csproj
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - eng/test-floor.txt
    - docs/INDEX.md
    - docs/android-test-sheet.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Api.Http/**
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

- [x] The check refuses the APK of R14e3, which holds no assembly, and passes the new APK.
- [x] The job of the emulator gives the line of the start of the program.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r14f2-emulator) · v0.214

## Method

Mechanical: the steps of the emulator on Ubuntu with KVM, and the read of the log with adb.
Judgement:  the line of the start as the condition, and the first frame as a fact of the report.
Weakest:    the emulator is x86-64 with Vulkan in software, not the Adreno 740 of the phone; the test of the owner
            stays the proof of the device. The smoke test still passes after an exception that follows the start (T149).

## Progress

- 2026-11-06: the APK of R14e3 holds 38 files and no assembly; with `EmbedAssembliesIntoApk` it holds 434 files, among
  them `lib_<assembly>.dll.so` for each assembly and each ABI. The check of the assemblies refuses the APK of R14e3 with
  22 errors (11 assemblies for each of 2 ABIs) and passes the new APK. The new APK builds with zero warnings.
- 2026-11-06: the first run of the job of the emulator (42157609653) reached the limit of 40 minutes inside the smoke
  test and showed no cause, because the waits had no limit. Each wait has a limit now, the start of the emulator is
  checked, and a failure gives the last lines of the emulator as annotations.
- 2026-11-06: the second run (42162085333) showed the cause in its annotations: the emulator found no AVD in
  $HOME/.android/avd. The job sets ANDROID_AVD_HOME for avdmanager and the emulator.
- 2026-11-06: the third run (42162651691) started the program on the emulator: the line of the start came, then the
  session ended with PlatformNotSupportedException before the first frame, and the job passed (T149 of the review of
  2026-11-06 (c)). PhoneHost writes each line of the stack with the prefix, so the next run shows it.
- 2026-11-06: the fourth run (42163303968) gave the stack: GraphicsDevice calls Log.Info, and Log.WriteColored reads
  Console.ForegroundColor, which Android does not have. With SupportedPlatform android in the seven projects of the APK,
  the analyzer CA1416 gave exactly these three sites and no other. Log.cs writes no colour on Android, and the rule
  Each_Project_That_The_Phone_Reaches_Supports_Android holds the item; an injection with no item in Engine.Kernel
  fails it.
- 2026-11-06: the fifth run (42164085778) gave the line of the start and the line of the first frame on Android 14
  for x86-64. The test sheet said that no emulator ran the program, which is false now; it says what the emulator ran.
  Closed at v0.214.
