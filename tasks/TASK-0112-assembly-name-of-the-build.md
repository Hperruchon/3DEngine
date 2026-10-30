---
id: 0112
title: No MSBuild element gives a client the assembly name of another project
status: Ready
phase: P0.44
opened: 2026-10-30
depends-on: [0111]
governed-by: []
writes:
  create:
    - tasks/TASK-0112-assembly-name-of-the-build.md
  modify:
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0112 — No MSBuild element gives a client the assembly name of another project

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-30 found T88 (high): a target that replaces the item `IntermediateAssembly` gives
the desktop the assembly name `Engine.Tests`, and the desktop reads the internals of `Engine.Core`; the gate program
passes. `docs/templates.md`, section 7, puts a high finding into a task before the next product phase, so this task
comes before R9.

## Goal

The dependency gate refuses each MSBuild element that sets the name or the path of the built assembly, other than
`AssemblyName` in a project file.

## Scope (in)

1. T88: refuse the items `IntermediateAssembly` and the elements `Output` whose `ItemName` or `PropertyName` is
   `IntermediateAssembly`, `TargetName`, `AssemblyName`, `TargetPath` or `TargetFileName`, in each file that MSBuild
   reads for a project.
2. The form of the review and the case forms, as rows of a test.

## Scope (out)

- A comparison of each built assembly name in the pipeline: a later task, if a probe finds a form that the text
  gate cannot see.
- The `Directory.Build.rsp` form of R-0031.

## Acceptance criteria

- [ ] The form of T88 fails the gate program, and an injection that restores the old rule fails a test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-30: opened by the codebase review of 2026-10-30 (TASK-0111).
