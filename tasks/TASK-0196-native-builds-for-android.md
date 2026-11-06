---
id: 0196
title: The native builds of Manifold and cimgui for android-arm64 and android-x64 run on GitHub Actions
status: Active
phase: R14d
opened: 2026-11-06
depends-on: [0195]
governed-by: [0061]
writes:
  create:
    - tasks/TASK-0196-native-builds-for-android.md
    - eng/cimgui-native/cimgui-ref.txt
    - eng/cimgui-native/build-android.sh
    - eng/cimgui-native/Engine.Native.DearImGui.csproj
    - .github/workflows/build-cimgui-native.yml
  modify:
    - eng/manifold-native/build.sh
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - eng/manifold-native/manifold-ref.txt
    - .github/workflows/build-manifold-native.yml
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - 3DEngine/**
---

# TASK-0196 — The native builds of Manifold and cimgui for android-arm64 and android-x64 run on GitHub Actions

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase d of the Android order of 2026-11-05 (ADR-0061 items 6 and 7). The native Manifold package holds win-x64,
linux-x64 and osx-arm64. `ImGui.NET` 1.91.6.1 has no Android library, and NuGet puts its glibc library of linux-x64 into
the APK for x86_64. The computer of the owner has no NDK, so the builds run on GitHub Actions, where the Ubuntu image
holds an NDK.

## Goal

A push builds and checks Manifold and cimgui for android-arm64 and android-x64, serial for Manifold, with pages of
16 KB, and a run on main pushes each package to its branch.

## Scope (in)

1. `eng/manifold-native/build.sh` takes an Android ABI, and the workflow builds two more RIDs; the package is 3.5.2.2.
2. `eng/cimgui-native/`: the pin of cimgui 1.91.6dock at `970c614`, the build script and the pack project, and the
   workflow `build-cimgui-native.yml`.
3. The check of each library: the machine, the alignment of each LOAD segment, and its dependency or its exports.

## Scope (out)

- The copy of the packages into `nuget/`, the checksums of the notices, and the Android host (R14e).

## Acceptance criteria

- [ ] The two workflows pass on the branch, each with the Android RIDs.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the pattern of the Manifold workflow for cimgui.
Judgement:  the commit of cimgui, read from the nativebuild of ImGui.NET, tag v1.91.6.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: opened with the Android order; the Android branch of build.sh, two rows of the Manifold matrix, and the
  workflow, the script and the pin of cimgui. The commit of cimgui: the tag 1.91.6dock names 970c614, "pull imgui
  docking 1.91.6 and generate", which ImGui.NET-nativebuild v1.91.6 pins.
- 2026-11-06: the first run built Manifold for both Android ABIs, and the check of the build machine failed: clang
  writes the source path into the text of a lambda. Both Android scripts map the source folder with -ffile-prefix-map.
- 2026-11-06: the second run built and checked Manifold for both Android ABIs; cimgui stopped with the code 127,
  because the edit of the flags wrote the characters  in place of two line continuations.
