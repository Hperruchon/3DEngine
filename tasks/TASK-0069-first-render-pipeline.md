---
id: 0069
title: The desktop draws a triangle, then an indexed mesh, through the first pipeline
status: Active
phase: R3
opened: 2026-10-12
depends-on: [0066]
governed-by: [0007, 0017, 0019, 0022]
writes:
  create:
    - tasks/TASK-0069-first-render-pipeline.md
    - docs/adr/0022-first-render-pipeline.md
  modify:
    - 3DEngine.Vulkan/**
    - 3DEngine/**
    - 3DEngine.Tests/**
    - 3DEngine.sln
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - docs/adr/0009-3dengine-core-peer-render-kernel.md
    - docs/adr/0017-first-party-vulkan-layer.md
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
    - 3DEngine.Core/**
---

# TASK-0069 — The desktop draws a triangle, then an indexed mesh, through the first pipeline

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R3: "A pipeline. A triangle, then an indexed mesh." The desktop clears its window to
green in a render pass with no draw; `3DEngine.Vulkan` has no pipeline, no shader and no vertex buffer.
ADR-0017 says that R3 writes again the helpers for a shader module, a memory type and a one-time
command buffer.

The codebase review of 2026-10-12, section 5, gives the research, and its question Q2 gives four
decisions. On 2026-10-12 the owner answered "A to all":

1. (a) The GLSL source and the compiled SPIR-V are both in the repository. A test checks the header of
   the SPIR-V and a hash of the source, so that a change of the source with no new compile fails.
2. (b) The pipeline uses the render pass that exists, and not dynamic rendering.
3. (c) A new test project `3DEngine.Tests`, with no GPU, may reference `3DEngine.Core` and
   `3DEngine.Vulkan`, in a new class of the dependency gate. ADR-0009 then names it, and `Engine.Tests`
   stays free of the render side.
4. (d) R3 shows the triangle and the mesh on the computer of the owner, who looks at the window. The
   defects of R-0022 and R-0030 wait for a run on Linux or macOS.

## Goal

The desktop window shows a triangle, and then an indexed mesh, through a pipeline of `3DEngine.Vulkan`.

## Scope (in)

1. ADR-0022 records the four decisions, and amends ADR-0017 and ADR-0009 (its first commit).
2. The helpers for a shader module, a memory type and a one-time command buffer.
3. A pipeline on the render pass that exists, with a vertex shader that holds a triangle and no vertex
   input. The owner looks at it.
4. A vertex format (a position of three 32-bit floats), a vertex buffer and an index buffer of 32-bit
   indices, as ADR-0018 gives them, and a draw of a fixed box. The owner looks at it.
5. `3DEngine.Tests`: the header and the source hash of each SPIR-V file, the stride and the attributes
   of the vertex format, and the description of the pipeline. No test needs a GPU.

## Scope (out)

- No geometry from the engine (R5), no camera and no depth (R4).
- No change to the defects of R-0022 and R-0030, except a line that R3 must change anyway.
- No shader compiler in the pipeline.

## Acceptance criteria

- [ ] The owner sees the triangle in the desktop window, and then the box.
- [ ] A run of the desktop gives no validation message (the owner reads the console).
- [ ] A change of a GLSL file with no new SPIR-V fails a test.
- [ ] `3DEngine.Tests` runs in the pipeline on the three runners.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The owner.** An agent must not open the desktop window. Stop and ask the owner to look at the
  triangle, and again at the box.
- **The compiler.** This computer has `glslc` and `spirv-val` of the Vulkan SDK 1.3.290. Record the
  command and the version that made each SPIR-V file.
- **The write set.** `Engine.Tests/Governance/DependencyDirectionGateTests.cs` is a gate file, named
  exactly. The new project enters a class of that gate in the same commit.
- **The size.** About 4 to 6 evenings.

## Progress

- 2026-10-12: the task is open, in the batch R2 to R6, after TASK-0066 (question Q1 of the review). ADR-0022
  records the four decisions of the owner and amends ADR-0009 and ADR-0017. The dependency gate has the
  class of `3DEngine.Tests`, and an injected reference from it to `Engine.Core` failed the new rule.
- 2026-10-12: the triangle. Two shaders with their SPIR-V and `shaders.lock`, made by
  `3DEngine.Vulkan/Shaders/compile.js` with shaderc v2023.8 of the Vulkan SDK 1.3.290; `ShaderLibrary`,
  `RenderPipeline` and the helper of a shader module; the desktop draws the triangle. A change of
  `triangle.frag` with no new compile failed `ShaderSourceTests`. The owner must look at the window.
