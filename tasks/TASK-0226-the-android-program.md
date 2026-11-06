---
id: 0226
title: The Android program builds an APK that draws a document of its own session
status: Done
phase: R14e2
opened: 2026-11-06
depends-on: [0223]
governed-by: [0007, 0014, 0018, 0019, 0025, 0027, 0029, 0036, 0061, 0072]
writes:
  create:
    - tasks/TASK-0226-the-android-program.md
    - 3DEngine.Android/3DEngine.Android.csproj
    - 3DEngine.Android/MainActivity.cs
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.App/LocalSessionHost.cs
    - 3DEngine.ImageTests/LocalSessionHostTests.cs
    - nuget/Engine.Geometry.Manifold.Native.3.5.2.2.nupkg
    - nuget/Engine.Native.DearImGui.1.91.6.1.nupkg
  modify:
    - nuget/Engine.Geometry.Manifold.Native.3.5.2.1.nupkg
    - 3DEngine.App/3DEngine.App.csproj
    - 3DEngine.ImageTests/DesktopSinkContractTests.cs
    - Engine.Geometry.Manifold/Engine.Geometry.Manifold.csproj
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Tests/EventSinkContractTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/NativePackageGateTests.cs
    - Engine.Tests/Hosting/BackendSelectionTests.cs
    - THIRD-PARTY-NOTICES.md
    - docs/diagnostics.md
    - docs/adr/0072-the-drawing-code-is-a-library.md
    - docs/INDEX.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0226 — The Android program builds an APK that draws a document of its own session

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase R14e of ADR-0061 item 9 is the Android host. R14e1 (TASK-0222, ADR-0072) made the library `3DEngine.App`, and a
probe APK that references it builds for `android-arm64`. R14d published the packages of Manifold 3.5.2.2 and of cimgui
1.91.6.1 for Android. The codebase review of 2026-11-06 (b) gives V88 to this task: no program ships a file of 32-bit
x86 (`CLAUDE.md`, determinism rule 5). The first commit of this task gives the write set and the field `governed-by`.

## Goal

A `net10.0-android` program for `android-arm64` and `android-x64` builds an APK that composes a session of its own and
draws its document with `3DEngine.App`.

## Scope (in)

1. The project, an activity that subclasses `SDLActivity`, a session of its own with an `ISessionHost`, and the APK.
2. The packages of R14d in `nuget/`, with the checksums of the notices.
3. The gates: the class of the program, the rule of ADR-0072 for it, the list of its packages, and V88.

## Scope (out)

- The picker, the loss of the surface, the pre-rotation, the pause and the test sheet (later slices of R14e).

## Acceptance criteria

- [x] The APK builds on this computer with zero warnings, and holds the native libraries of SDL, cimgui and Manifold for both ABIs and no file of x86.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r14e2-android-program) · v0.209

## Method

Mechanical: the probe APK of the review 24.
Judgement:  the session of the phone in the library, so that the image tests run it; the program outside the
            solution until R14f.
Weakest:    no device and no emulator ran the APK, and the pipeline does not build it. The loop of the phone has
            no pause, no loss of the surface and no pre-rotation yet.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06 (b).
- 2026-11-06: the packages of R14d in `nuget/` (Manifold 3.5.2.2 and `Engine.Native.DearImGui` 1.91.6.1), the checksum
  of each package and of each file inside it in `THIRD-PARTY-NOTICES.md`, and the gate of the native packages as a
  theory over both. The files of `linux-x64` of 3.5.2.1 and 3.5.2.2 have the same bytes; the files of `win-x64` and the
  files `libmanifoldc` of `osx-arm64` differ, from the same source. `LocalSessionHost` and `FanOutSink` in
  `3DEngine.App`, with three tests. The program `3DEngine.Android`: `MainActivity` and `PhoneHost`. Its APK builds on
  this computer with zero warnings, and holds for `arm64-v8a` and `x86_64` only the libraries `libSDL3.so`,
  `libcimgui.so`, `libmanifold.so` and `libmanifoldc.so`; the checksums of `libcimgui.so` and `libmanifold.so` are
  those of the notices. Five injections each fail a gate: `android-x86` in the runtime identifiers, a reference to
  `Engine.Api.Http`, a framework reference of ASP.NET Core, a wrong checksum of a library of cimgui, and a second binding
  of SDL. No device and no emulator ran the APK.
- 2026-11-06: closed at v0.209. The row R14e stays open.
