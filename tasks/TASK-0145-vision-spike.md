---
id: 0145
title: The vision library loads and finds a contour on the three platforms of the pipeline
status: Done
phase: V0
opened: 2026-10-31
depends-on: [0144]
governed-by: [0042]
writes:
  create:
    - tasks/TASK-0145-vision-spike.md
    - docs/adr/0042-vision-library.md
    - 3DEngine.VisionTests/3DEngine.VisionTests.csproj
    - 3DEngine.VisionTests/VisionSpikeTests.cs
  modify:
    - 3DEngine.sln
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - docs/adr/README.md
    - docs/INDEX.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Core/**
    - 3DEngine/**
---

# TASK-0145 — The vision library loads and finds a contour on the three platforms of the pipeline

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-31 (b), question Q3, left the product phase after R11b to the owner. On 2026-10-31 the
owner chose C, the roadmap row V0: "A verification spike. The vision library on three platforms." No earlier record
chose a library.

## Goal

A test project proves on Windows x64, Linux x64 and macOS arm64 that the chosen vision library loads and finds the
contour and the edges of an image.

## Scope (in)

1. ADR-0042: the library, its native payloads, its place outside the projects of the log, and the rule of the gate.
2. `3DEngine.VisionTests`: three tests, the version, the contour of a rectangle and its edges, with exact pixels.
3. `DependencyDirectionGateTests`: the class of the spike, which references no project, and the pins of OpenCvSharp4
   in the spike only.

## Scope (out)

- A library of vision in the product, an image in the Document, and a picture in the viewport (V1 to V3).
- The notices of the library: no distributed program holds it (ADR-0042 item 6).

## Acceptance criteria

- [x] The three tests pass on the three runners of the pipeline, and none skips.
- [x] A reference from the spike to a project, and a pin of OpenCvSharp4 in another project, each fail the gate.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch v0-vision-spike) · v0.147

## Method

Mechanical: the package ids and versions from nuget.org and the SDK, the three tests on the three runners (run
            41180854816), and the count of each test project in the pipeline.
Judgement:  OpenCvSharp4 over OpenCvSharp5, because the line 4 is mature and the line 5 is two weeks old; the slim Linux
            payload, because it needs no GTK3.
Weakest:    the licence facts come from the pages of the packages; a check of the notices of the native payload waits
            for the phase that distributes it (ADR-0042 item 6).

## Progress

- 2026-10-31: the owner chose V0 ("C"). OpenCvSharp4 4.13.0.20260627 over OpenCvSharp5 and Emgu.CV (ADR-0042). The
  analyzer of the package refuses a call of `Rows` or `Cols` in a loop (OCVS002), so the test reads them once. The
  three tests pass on Windows x64 here.
- 2026-10-31: two tests of the gate; 2 injections caught: a reference from the spike to Engine.Core, and a pin of
  OpenCvSharp4 in Engine.Cli.
- 2026-10-31: the three tests passed on the three runners; closed at v0.147.
