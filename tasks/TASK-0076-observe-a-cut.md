---
id: 0076
title: A person observes the cut of the first demonstration, and an image test with no window holds it
status: Active
phase: R6
opened: 2026-10-14
depends-on: [0073]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023, 0024, 0025]
writes:
  create:
    - tasks/TASK-0076-observe-a-cut.md
    - docs/adr/0025-observe-a-cut.md
    - scripts/first-demonstration.js
  modify:
    - 3DEngine.Vulkan/**
    - 3DEngine.Core/**
    - 3DEngine/**
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - 3DEngine.sln
    - .github/workflows/ci.yml
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - docs/adr/0009-3dengine-core-peer-render-kernel.md
    - docs/adr/0017-first-party-vulkan-layer.md
    - docs/adr/0022-first-render-pipeline.md
    - docs/adr/README.md
    - docs/INDEX.md
    - docs/glossary.md
    - docs/roadmap.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - eng/**
---

# TASK-0076 — A person observes the cut of the first demonstration, and an image test with no window holds it

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R6: "Observe a cut. The live body set of ADR-0021 gives the result. A headless image test."
It completes the first objective (objective 6 of `docs/CHARTER.md`). R5 (TASK-0071) draws each live body
of the session. The codebase review of 2026-10-13, section 5, gives the research. On 2026-10-14 the owner
answered its question Q2 with option A for each part:

1. (D1) The demonstration moves the second box by (5, 5, 5), so the cut shows a notch.
2. (D2) The image test lives in a new project `3DEngine.ImageTests`, in a new class of the dependency gate.
   It may reference what a host that draws references, and the internals of `3DEngine`, so it also tests
   `SessionBodies` (R-0039).
3. (D3) The pipeline installs lavapipe on Ubuntu and requires the test there; on Windows and macOS it skips,
   with the reason in the register.
4. (D4) The test compares the image by structure: the background, the silhouette that the CPU projects from
   the tessellation, and the region of the cut. On a failure the pipeline keeps the picture.
5. (D5) The camera frames the live bodies at each change (also V12 of the review).
6. (D6) A Node.js script in the repository runs the first demonstration over HTTP.

The owner also answered Q3: R6 needs 6 to 8 evenings.

## Goal

A person sees the cut of the first demonstration in the window, and a test with no window holds the same
result on a runner of the pipeline.

## Scope (in)

1. ADR-0025 records the six decisions, and amends ADR-0022 item 3.
2. The Vulkan device can exist with no window: a core of the device apart from the presentation, a
   pipeline that takes a render pass, and an offscreen target with a copy to memory that the host reads.
   The register entries R-0030 V5 and V6 are corrected on that path.
3. The camera frames the live bodies.
4. `scripts/first-demonstration.js` sends the commands of the first demonstration over HTTP.
5. `3DEngine.ImageTests`: the first demonstration through the session, the live body set, the tessellation
   and an offscreen frame, with a structural check of the cut, and synchronization validation on.
6. The pipeline: lavapipe on Ubuntu, and the picture kept on a failure.

## Acceptance criteria

- [ ] A picture of the window after the script shows the notch of the cut. The owner accepts it.
- [ ] The image test passes on the Ubuntu runner with lavapipe, and fails on an injected box with no cut and
      on an injected wrong cull mode.
- [ ] A run of the desktop gives no validation message.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The order.** TASK-0073 comes first (question Q1 of the review).
- **The write set.** `Engine.Tests/Governance/DependencyDirectionGateTests.cs` and `.github/workflows/ci.yml`
  are gate files, named exactly.
- **The size.** About 6 to 8 evenings.

## Progress

- 2026-10-14: the task is active, after TASK-0073 (v0.73). ADR-0025 records the six decisions of the owner
  and the split of the device. It amends ADR-0022 items 2 and 3, as the scope says, and also ADR-0017
  items 1, 3 and 7 and ADR-0009 section 2, which state the same rules; the write set now names those two
  records. The task is governed by ADR-0025 too, whose `affects` intersects the write set.
