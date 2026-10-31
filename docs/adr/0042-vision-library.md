---
id: 0042
title: The vision library and its place
status: Amended
topic: Vision, boundary
date: 2026-10-31
supersedes: []
superseded-by: []
amends: []
amended-by: ['0047']
affects:
  - 3DEngine.VisionTests/**
enforced-by: Engine.Tests/Governance/DependencyDirectionGateTests.cs
---

# ADR-0042 — The vision library and its place

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Objective 15 of `docs/CHARTER.md` puts machine vision before materials, chemistry and DNA. The roadmap row V0 is "A
verification spike. The vision library on three platforms." V3 turns a contour of a photo into an editable sketch,
so the library must find contours and edges on Windows x64, Linux x64 and macOS arm64, the platforms of the native
geometry package and of the pipeline.

After the codebase review of 2026-10-31 (b), question Q3, the owner chose V0 on 2026-10-31. The owner told the session
on the same day to use its recommendation for each question, so this record is Accepted.

The candidates on 2026-10-31, from nuget.org and the pages of each project:

| Candidate | Licence | Windows x64 | Linux x64 | macOS arm64 |
|---|---|---|---|---|
| OpenCvSharp4 4.13.0.20260627 (OpenCV 4.13) | Apache-2.0 | `OpenCvSharp4.runtime.win` | `OpenCvSharp4.official.runtime.linux-x64` and `.slim` | `OpenCvSharp4.runtime.osx.arm64` |
| OpenCvSharp5 5.0.0.20261018 (OpenCV 5.0) | Apache-2.0 | yes | yes | yes |
| Emgu.CV 5.0.0.6584 | GPL v3, or a commercial licence | yes | Ubuntu 24.04 and 26.04 | no; the macOS package has `osx-x64` only |
| AForge.Imaging, Accord.Imaging | — | last change 2013 and 2017, and no contours | | |

## Decision

1. **The library.** OpenCvSharp4 4.13.0.20260627, over OpenCV 4.13, under the Apache-2.0 licence. OpenCvSharp5 is two
   weeks old and its OpenCV 5.0 is new; a later change to it is a change of the pins. Emgu.CV has no macOS arm64
   payload, and its GPL v3 licence binds the whole work.
2. **The native payloads.** One runtime package for each platform: `OpenCvSharp4.runtime.win`,
   `OpenCvSharp4.official.runtime.linux-x64.slim` and `OpenCvSharp4.runtime.osx.arm64`. A condition on the operating
   system of the build selects one, so each runner restores one payload. The slim Linux package needs no GTK3, and the
   module `imgproc`, which finds contours and edges, is in it.
3. **The place.** Vision is input, not design truth. A vision result enters the Document only through the commands
   that exist, which record numbers: the client proposes a contour, the person accepts it, and the commands record its
   points (V3). No `Engine.*` project and no `3DEngine.Core` reference the library, so a replay never calls it, and
   anti-objective 16 and the determinism rules of `CLAUDE.md` do not reach it.
4. **The spike (V0).** The test project `3DEngine.VisionTests` references the library and no project of the
   repository. It proves on each runner that the native library loads, gives its version, and finds the contour and
   the edges of a synthetic image with exact pixel results.
5. **The rule.** `DependencyDirectionGateTests` puts `3DEngine.VisionTests` in a class of its own that references no
   project, and the packages of OpenCvSharp4 have their pins in that project only. A later phase that adds a library
   of vision changes this record and the gate.
6. **The notices.** No program that the project distributes holds the library in V0. The phase that first puts it into
   a distributed program adds its notices to `THIRD-PARTY-NOTICES.md`, with the notices of each library that its
   runtime package holds.

## Consequences

- Each runner of the pipeline restores one payload of 40 to 57 megabytes once more for each run.
- A contour of a photo can differ by one pixel between two versions of OpenCV. That is acceptable, because the log
  records the accepted points and not the photo (item 3).

## History

- 2026-10-31 · TASK-0145 · The record, Accepted under the standing answer of the owner of 2026-10-31.
- 2026-10-31 · TASK-0155 · ADR-0047 amends item 3: V3 proposes a contour with its own managed code, and the library
  stays in the spike until the owner answers register entry R-0048 on its licences.
