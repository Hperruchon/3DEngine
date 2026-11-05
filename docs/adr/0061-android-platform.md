---
id: 0061
title: Android is a platform of the program, with the person on the phone and no surface
status: Accepted
topic: Platform, clients, rendering
date: 2026-11-05
supersedes: []
superseded-by: []
amends: ['0017', '0027']
amended-by: []
affects:
  - 3DEngine.Android/**
  - 3DEngine.Vulkan/3DEngine.Vulkan.csproj
  - eng/cimgui-native/**
  - .github/workflows/build-manifold-native.yml
enforced-by: Engine.Tests/Governance/DependencyDirectionGateTests.cs
---

# ADR-0061 — Android is a platform of the program, with the person on the phone and no surface

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-09-23 the owner answered "yes" to Android (`docs/reviews/2026-09-23-architecture-challenge.md`, line 530).
Register entry R-0020 recorded the answer as a question: the platform list needed a study before objective 8 changed.
On 2026-11-05 the owner gave the Android order: Android becomes a requirement, with eight decisions and the phases a to g.
A feasibility analysis of 2026-11-05 found no blocker in `Engine.*`. The blockers are in the host and in the native
libraries.

The order requires a check of each library fact on disk before this record. The check of 2026-11-05 read the packages
and built a throwaway app (`docs/reviews/2026-11-05-b-codebase-review.md`, section 5):

- `ppy.SDL3-CS` 2026.1002.1 holds `lib/net8.0-android34.0`, a `libSDL3.so` for `android-arm64` and `android-x64` with
  pages of 16 KB, and the Java class `org.libsdl.app.SDLActivity`. A `net10.0-android` app with the minimum API 33 and
  an activity that subclasses `SDLActivity` built with 0 warnings and 0 errors, in Debug and in Release. Its native
  library is "SDL-3.5.0-ea7a2da". It also ships files for 32-bit x86.
- `Alimer.Bindings.SDL`, the binding of today, has no Android file in any of its 56 versions, and no release after
  2025-04-04.
- `ImGui.NET` 1.91.6.1 has no Android library in any version. NuGet puts its glibc `libcimgui.so` of `linux-x64` into
  the APK for `x86_64`, and `arm64-v8a` gets no cimgui.
- `Vortice.Vulkan` 3.2.3 tries `libvulkan.so.1`, then `libvulkan.so`, on a platform that is not Windows or macOS.
  Android names its loader `libvulkan.so`. No run on a device proved it.
- This computer has the .NET Android workload, the Android SDK and a JDK. It has no NDK, and cmake is not on the path.

No fact is false in a way that blocks the order.

## Decision

1. **The platforms.** Objective 8 becomes "Windows, Linux, macOS and Android each work. A gate verifies each one
   continuously." Anti-objective 16 adds Android on arm64. A build for `android-x64` exists only for the emulator of
   the pipeline. R-0020 closes with this decision.
2. **The reference device.** A Samsung Galaxy S23 Ultra (Snapdragon 8 Gen 2, GPU Adreno 740). The minimum version is
   Android 13 (API level 33), the first version that requires Vulkan 1.3 on a new device.
3. **The Vulkan floor.** Vulkan 1.3 stays the floor. This record does not lower it.
4. **The agent surface.** The first Android version mounts no HTTP surface and no WebSocket surface. The person on the
   phone sends the same commands through the `CommandBus` of the same process. Anti-objective 7 holds: each capability
   on the phone is a command or a query of `Engine.Core`, and the surface of the desktop host gives each one to an
   agent. The phone adds no capability that only a person can operate. An embedded server on the phone, or the phone as
   a client of a computer, needs a new decision of the owner.
5. **The SDL binding.** `ppy.SDL3-CS` replaces `Alimer.Bindings.SDL` on each platform. It amends ADR-0017 item 4: the
   file `3DEngine.Vulkan/3DEngine.Vulkan.csproj` holds the only pin of `ppy.SDL3-CS`. The pin is 2026.1002.1, whose
   native library is SDL 3.5.0 (register entry R-0054). No program ships a file of 32-bit x86 from the package
   (`CLAUDE.md`, determinism rule 5).
6. **Dear ImGui.** The managed `ImGui.NET` 1.91.6.1 stays. This repository builds the native cimgui of the same version,
   with the docking branch that the package holds, for `android-arm64` and `android-x64`, with pages of 16 KB, as it
   builds Manifold (`eng/manifold-native/`). The APK holds no cimgui file of `linux-x64`. It amends ADR-0027 item 9,
   which names this way out.
7. **Manifold.** `.github/workflows/build-manifold-native.yml` adds `android-arm64` and `android-x64`, built with the
   Android NDK (native development kit), serial, with `MANIFOLD_PAR` off (ADR-0014). The NDK is not on the computer of
   the owner and needs no administrator install there, because the build runs on GitHub Actions.
8. **The runtime.** .NET 10 for Android with the Mono runtime, the default. The CoreCLR runtime and NativeAOT are
   experimental on Android, so the program uses neither. The determinism hash of Android must be equal to the hashes of
   the three desktop runners. A hash that differs stops the merge of the phase that shows it.
9. **The phases.** Track R holds the work as slices of R14, so that the review gate counts each one:
   - R14a: this record, the charter change, the closure of R-0020 and the roadmap rows.
   - R14b: the host owns the document session, the surface mounts on that session as an option, and an open of a file
     stays in the same process. The behaviour of the desktop does not change.
   - R14c: `ppy.SDL3-CS` on the desktop, with no change of behaviour.
   - R14d: the native builds of Manifold and cimgui for `android-arm64` and `android-x64`.
   - R14e: the Android host: a `net10.0-android` project, an activity that subclasses `SDLActivity`, the APK, the
     document picker of Android for Open and Save, a new Vulkan surface and swapchain after a loss of the surface, the
     pre-rotation, and a pause in the background. The goal is a box, a subtract of a second box, and the cut on the
     S23 Ultra. A test sheet for the owner comes with it. Its row closes only after the test of the owner on the phone.
   - R14f: the gate: a job builds the APK at each push, a job runs the x86-64 emulator with software Vulkan and a
     smoke test, and the pipeline compares the hash of Android with the hashes of the desktop runners.
   - R14g: the layout of the phone, inside the redesign of the desktop.
   R14a to R14d touch no user interface, and the redesign needs them too, so R14a to R14f come before the redesign.

## Consequences

**Good:** The program reaches the device that the owner carries, with the engine unchanged. One binding of SDL serves
each platform, and it has a maintainer. The person and an agent keep one set of commands.

**Bad:** The swap of the binding touches each file that calls SDL: `Window.cs` and `SdlInput.cs` alone gave 69 distinct
compile errors against `ppy.SDL3-CS`. The SDL of the pin is probably a development version. Two native libraries get a
build of their own for Android, and the computer of the owner cannot run that build. No agent can test on the phone; the
owner is the only tester on a real device.

**Next:** R14b. TASK-0193 writes this record.

## History

- 2026-11-05 · TASK-0193 · Accepted with the Android order and the standing order of the owner of 2026-11-05. The
  order answers the questions of this record in advance with the recommended option.
