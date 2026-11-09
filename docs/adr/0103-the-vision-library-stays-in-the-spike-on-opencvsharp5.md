---
id: 0103
title: The vision library stays in the spike, on OpenCvSharp5
status: Accepted
topic: Vision
date: 2026-11-09
supersedes: []
superseded-by: []
amends: ['0042']
amended-by: []
affects:
  - 3DEngine.VisionTests/3DEngine.VisionTests.csproj
  - 3DEngine.VisionTests/VisionSpikeTests.cs
enforced-by: 3DEngine.VisionTests/VisionSpikeTests.cs
---

# ADR-0103 — The vision library stays in the spike, on OpenCvSharp5

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0042 chose OpenCvSharp4 4.13.0.20260627 (OpenCV 4.13) for the spike of phase V0 and kept the library out of each
program that the project distributes; ADR-0047 gave the contour proposal of V3 its own managed code. ADR-0042 item 1 named
OpenCvSharp5 as too new on 2026-10-31. Register entry R-0048 asked the owner whether the desktop may hold the library,
because its native payloads link other libraries with their own licences (FFmpeg under LGPL among them).

On 2026-11-09 the owner answered: "keep opencv separate and set it to v5", and chose the build 5.0.0.20261018 of
OpenCvSharp5, three weeks old, over 5.0.0.20261107, two days old.

## Decision

1. **The place (closes R-0048).** No program that the project distributes holds the vision library. It stays in the test
   project `3DEngine.VisionTests`, which references no project of the repository (ADR-0042 items 3 to 5). A later phase
   that needs vision in a program writes its own record, with the notices of ADR-0042 item 6.
2. **The version (amends ADR-0042 items 1 and 2).** OpenCvSharp5 5.0.0.20261018, over OpenCV 5.0, under the Apache-2.0
   licence, with one native payload for each platform: `OpenCvSharp5.runtime.win`,
   `OpenCvSharp5.official.runtime.linux-x64.slim` and `OpenCvSharp5.runtime.osx.arm64`.
3. **The proof.** The tests of the spike hold the version 5.0 of the native library, and find the contour and the edges
   of a synthetic image on each runner of the pipeline, as before.
4. **The rule of the pins.** `DependencyDirectionGateTests` holds the pin of OpenCvSharp5 in `3DEngine.VisionTests` only, as
   ADR-0042 item 5 holds the pin of OpenCvSharp4; its case of the pin names the new package.

## Consequences

**Good:** the spike follows the current major version, and no licence work reaches a distributed program.

**Bad:** a phase that needs vision in the desktop opens the question of the licences again.

## History

- 2026-11-09 · TASK-0318 · The record, with the answers of the owner.
