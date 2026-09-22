---
id: 0017
title: The first-party Vulkan layer
status: Accepted
topic: Boundary, rendering
date: 2026-09-23
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - 3DEngine.Vulkan/**
  - 3DEngine/3DEngine.csproj
enforced-by: Engine.Tests/Governance/DependencyDirectionGateTests.cs
---

# ADR-0017 — The first-party Vulkan layer

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The charter names a Vulkan renderer as the first objective. Roadmap phase R1 asks for two things:
upgrade the Vulkan binding, and move the Vulkan code into a first-party project.

Before this decision the desktop host `3DEngine` referenced `Vortice.Vulkan.SampleFramework`. That
project is sample code from the author of the binding. Three facts made the condition a decision and
not a preference:

- `CLAUDE.md`, section "Dependency rules", permits a client to reference `Engine.Core`,
  `Engine.Contracts` and `3DEngine.Core`. The sample framework is none of them. The dependency
  direction gate did not read the host, therefore it gave no report.
- Three project files pinned `Vortice.Vulkan` 1.9.8. The current release is 3.2.3. It removes each
  global Vulkan function and puts each one in a table for the instance or for the device. The sample
  code does not compile against it.
- Each sample file said "See LICENSE in the repository root". That file is the licence of this
  repository, and not the licence of the sample author. `THIRD-PARTY-NOTICES.md` did not name the
  sample code.

ADR-0009 gives render-side decisions to their own records. This record is the first one. It does not
change the two kernels.

## Decision

1. The Vulkan code lives in `3DEngine.Vulkan`, with the namespace `ThreeDEngine.Vulkan`. It holds the
   device, the swapchain, and the SDL window that owns the Vulkan surface. The host keeps the event
   loop and the projection of events into render state.
2. `3DEngine.Vulkan` references no `Engine.*` project. It may reference `3DEngine.Core` and nothing
   else. It draws render state and it never sees design truth.
3. Only a host that draws with Vulkan references `3DEngine.Vulkan`. Today that host is `3DEngine`. No
   `Engine.*` project, no render kernel and no web client references it.
4. `3DEngine.Vulkan/3DEngine.Vulkan.csproj` holds the only pin of `Vortice.Vulkan` and the only pin of
   `Alimer.Bindings.SDL`. A host receives each binding through its project reference. The version
   of each binding therefore has one answer, in one line.
5. The vendored projects `Vortice.Vulkan.Sample` and `Vortice.Vulkan.SampleFramework` are removed.
   Each type and each method that moved has a caller in the host. A method with no caller did not
   move, because no test and no run can show that it is correct. A later phase adds each method when
   it needs one.
6. Each file that comes from the sample code keeps the copyright line of its author.
   `THIRD-PARTY-NOTICES.md`, section 4, gives the licence text.

## Consequences

**Good:** The host has one first-party dependency for the GPU, with rules that a gate checks. The
binding version has one answer. The two build warnings of the sample code are gone. The licence of the
derived code is correct.

**Bad:** The project owns 1,080 lines, and another author wrote most of them. It must keep them. The
maintained samples of the binding stay upstream. A reader who wants the old sample reads the history
of this repository, or the upstream repository. The removed helpers for a shader module, a memory
type and a one-time command buffer return in phase R3, and that phase writes them again.

**Next:** TASK-0027 applies this decision. Phase R3 adds the pipeline and the first vertex type to
this project.
