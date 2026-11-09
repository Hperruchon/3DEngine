---
id: 0318
title: The vision spike moves to OpenCvSharp5 and the library stays out of each distributed program
status: Done
phase: V0b
opened: 2026-11-09
depends-on: [0317]
governed-by: [0042, 0103]
writes:
  create:
    - tasks/TASK-0318-the-vision-spike-on-opencvsharp5.md
    - docs/adr/0103-the-vision-library-stays-in-the-spike-on-opencvsharp5.md
  modify:
    - 3DEngine.VisionTests/3DEngine.VisionTests.csproj
    - 3DEngine.VisionTests/VisionSpikeTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - docs/adr/0042-vision-library.md
    - docs/adr/README.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0318 — The vision spike moves to OpenCvSharp5 and the library stays out of each distributed program

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-09 the owner answered register entry R-0048: "keep opencv separate and set it to v5", and chose OpenCvSharp5
5.0.0.20261018. On that day nuget.org gave OpenCvSharp5 5.0.0.20261012, 5.0.0.20261018 and 5.0.0.20261107, with the
runtime packages `OpenCvSharp5.runtime.win`, `OpenCvSharp5.official.runtime.linux-x64.slim` and
`OpenCvSharp5.runtime.osx.arm64` at each version.

## Goal

The spike of phase V0 runs on OpenCvSharp5 on the three runners, and ADR-0103 records that no distributed program holds
the library.

## Scope (in)

1. ADR-0103, which amends ADR-0042 items 1 and 2 and closes R-0048 by a decision.
2. The pins of `3DEngine.VisionTests`, and the test of the version.

## Scope (out)

- Vision in a distributed program. A change of the contour of V3.

## Acceptance criteria

- [x] The three tests of the spike pass on Windows, Linux and macOS in the pipeline.
- [x] R-0048 is closed with ADR-0103.
- [x] Continuous integration passes on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch v0b-opencv5) · v0.306

## Method

Mechanical: the answer of the owner and the names of the packages on nuget.org.
Judgement:  none.
Weakest:    the spike tests three operations only; the rest of OpenCV 5 is not used by the project.

## Progress

- 2026-11-09: the pins and the test of the version. On Windows the contour and the edges pass with OpenCV 5.0, and the
  test of the version failed with the old text "4.13." before its change.
- 2026-11-09: the case of the pin of the dependency gate names OpenCvSharp5; with the old name the gate failed, because no
  file names OpenCvSharp4 any more.
- 2026-11-09: run 42554669161 passed on Windows, Linux and macOS. The close waited for the codebase review of 2026-11-09,
  because V0b was the fifth product milestone after the review of v0.292. Closed at v0.306.
