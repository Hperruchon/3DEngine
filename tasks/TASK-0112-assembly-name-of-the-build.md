---
id: 0112
title: No MSBuild element gives a client the assembly name of another project
status: Done
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

- [x] The form of T88 fails the gate program, and an injection that restores the old rule fails a test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p044-assembly-name) · v0.114

## Method

Mechanical: the refusal of the item IntermediateAssembly and of each task output to a name of the built file.
Judgement:  a text gate and not a comparison of the built assemblies, because the text gate runs on each commit with
            no build; the task records the comparison as the next step if a probe finds a form that text cannot see.
Weakest:    a target that calls the task Csc with its own OutputAssembly, and a Directory.Build.rsp file (R-0031),
            still name a file that no gate reads.

## Progress

- 2026-10-30: opened by the codebase review of 2026-10-30 (TASK-0111).
- 2026-10-30: started.
- 2026-10-30: the item IntermediateAssembly and the outputs of a task to a name of the built file are offences; 4 new test
  cases; 3 injections caught, one of them the form of the review in `3DEngine/3DEngine.csproj` under the gate program.
- 2026-10-30: closed at v0.114.
