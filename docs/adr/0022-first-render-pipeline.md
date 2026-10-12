---
id: 0022
title: The first render pipeline
status: Accepted
topic: Rendering, boundary
date: 2026-10-12
supersedes: []
superseded-by: []
amends: ['0009', '0017']
amended-by: []
affects:
  - 3DEngine.Vulkan/Shaders/**
  - 3DEngine.Tests/**
enforced-by: 3DEngine.Tests/ShaderSourceTests.cs (TASK-0069 creates it)
---

# ADR-0022 — The first render pipeline

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R3 asks for a pipeline that draws a triangle, and then an indexed mesh. The codebase
review of 2026-10-12, section 5, gives four conditions that make each choice below a decision:

1. A pipeline needs shaders in SPIR-V. No runner of the pipeline has a Vulkan SDK or a shader compiler,
   and the computer of the owner has the Vulkan SDK 1.3.290 with `glslc`.
2. The swapchain has a render pass of one colour attachment. Dynamic rendering needs a device feature
   that `GraphicsDevice` does not turn on.
3. No test project may reference the render side. ADR-0009 permitted `Engine.Tests` to reference
   `3DEngine.Core` for a test of the render kernel, and the dependency gate refused it; the decision and
   the gate disagreed.
4. Register entries R-0022 and R-0030 hold defects of the Vulkan layer on Linux, on macOS and on a
   second computer. No runner has a GPU.

The owner answered question Q2 of that review on 2026-10-12 with option A for each part.

## Decision

1. **Shaders.** Each shader has its GLSL source and its compiled SPIR-V in `3DEngine.Vulkan/Shaders/`.
   The file `3DEngine.Vulkan/Shaders/shaders.lock` gives, for each shader, the SHA-256 of the source with
   LF line ends, the SHA-256 of the SPIR-V, and the command and the version of the compiler. A test of
   `3DEngine.Tests` fails when a source or a SPIR-V file differs from its line, or when a SPIR-V file
   has no SPIR-V header. Nothing compiles a shader in the pipeline.
2. **Render pass.** The pipeline draws in subpass 0 of the render pass of the swapchain. It does not use
   dynamic rendering.
3. **Tests of the render side.** A test project `3DEngine.Tests` may reference `3DEngine.Core` and
   `3DEngine.Vulkan`, and nothing else. Its tests create no Vulkan object and need no GPU.
   `Engine.Tests` references no project of the render side. The dependency gate holds both rules.
4. **Platform of R3.** The owner sees the result of R3 in the desktop window on the computer of the
   owner (Windows, x64). The defects of R-0022 and R-0030 wait for a run on Linux or macOS.

## Consequences

**Good:** The pipeline needs no shader tool, and a forgotten compile fails a test. The render pass that
exists stays, so R3 changes the least code. The render side gets tests that a runner can run, and the
gate and ADR-0009 agree.

**Bad:** A SPIR-V file is a binary in the repository, and a reviewer cannot read it; the lock file and
the source are the readable record. A person who changes a shader needs the Vulkan SDK. The test of the
lock file cannot see a SPIR-V file that a person compiled from another source and then recorded; it
sees a forgotten compile only. No test shows a frame, so the owner looks at each result of R3.

**Next:** TASK-0069 applies this decision. R6 adds an image test with no window.

## History

- 2026-10-12 · TASK-0069 · The record is written with the answer of the owner to question Q2 of the
  codebase review of 2026-10-12.
