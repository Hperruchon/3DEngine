---
id: 0069
title: The desktop draws a triangle, then an indexed mesh, through the first pipeline
status: Done
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

- [x] The owner sees the triangle in the desktop window, and then the box. The owner told me to run
      the desktop and show a picture of the window; the owner accepted both pictures on 2026-10-12.
- [x] A run of the desktop gives no validation message. The console of each run had no `[WARN]` and
      no `[ERROR]` line, also after the close message.
- [x] A change of a GLSL file with no new SPIR-V fails a test.
- [x] `3DEngine.Tests` runs in the pipeline on the three runners (runs 38210858637 and 38213376016).
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.68 · the commit that carries this block.

The desktop draws the cut body of the first demonstration as an indexed mesh through the first
pipeline of `3DEngine.Vulkan`: the 8 vertices and 12 triangles that `GetTessellation` gave in TASK-0028,
narrowed to float around the centre of the box, with flat shading and back-face culling. ADR-0022
records the four decisions of the owner and amends ADR-0009 and ADR-0017. The shaders have their GLSL
source, their SPIR-V and `shaders.lock`, made by `3DEngine.Vulkan/Shaders/compile.js`. The new test
project `3DEngine.Tests` (15 tests, no GPU) holds the lock, the fixed state of the pipeline, the vertex
format, the narrowing, the choice of a memory type and the winding.

## Method

**Mechanical.** Each step was seen in a picture of the window on the computer of the owner (Windows,
x64, validation on): first the triangle with no vertex input, then the mesh. Four injections: a
reference from `3DEngine.Tests` to `Engine.Core` failed the new rule of the dependency gate; a change
of `triangle.frag` with no new compile failed `ShaderSourceTests`; a reversed triangle failed the test
of the winding; the narrowing test shows that a float holds no fraction at 1e9. The full suite passes:
370 tests in `Engine.Tests` and 15 in `3DEngine.Tests`, and the gate program passes 72 runs.

**Judgement.** The viewport has a negative height, so y points up, and the front face is
counter-clockwise; with the winding of ADR-0018 item 9, culling keeps each face that points to the
viewer, and a convex body needs no depth buffer before R4. The positions are narrowed after the
subtraction of the centre in double precision, as R5 asks. The buffers use memory that the host sees,
so no staging buffer and no one-time command buffer exist; ADR-0017 named that helper for R3, and it
would have no caller. The triangle shaders left the repository with the mesh, because no code calls
them; the first commit of the mesh holds them.

**Weakest.** No test draws a frame: the pictures are the evidence of the draw, and R6 adds an image
test. The fixed view of `mesh.vert` and the shading are presentation and have no test. The run is on
one computer; R-0022 and R-0030 hold the other platforms.

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
- 2026-10-12: on the order of the owner ("Screenshot and show in conv"), I ran the desktop, captured
  its window and sent the picture: the triangle, with no validation message. The note above that an
  agent must not open the window no longer applies to the checks of this task.
- 2026-10-12: the indexed mesh. The mesh shaders replace the triangle shaders, which no code calls now.
  `MeshVertex` narrows each position to float after the subtraction of the origin, `GpuBuffer` holds
  the vertices and the indices, `MemoryTypes` chooses the memory, and the desktop draws the cut body
  of the first demonstration. No helper of a one-time command buffer: memory visible to the host needs
  none, and no code would call it. A reversed triangle failed the new test of the winding.
- 2026-10-12: the owner accepted both pictures. Closed, after a full local run of 385 tests.
