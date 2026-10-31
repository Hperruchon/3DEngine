---
id: 0138
title: The reason of a native library that does not load separates a missing file from a platform with no payload
status: Done
phase: P0.57
opened: 2026-10-31
depends-on: [0137]
governed-by: [0014, 0018, 0036]
writes:
  create:
    - tasks/TASK-0138-reason-of-a-backend-that-does-not-load.md
  modify:
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Tests/Hosting/BackendSelectionTests.cs
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0138 — The reason of a native library that does not load separates a missing file from a platform with no payload

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

R11b (TASK-0134) waits for the visual check of the owner, so this task takes finding E38 of register entry R-0036. It
changes no file of the desktop. On `win-x64` the sentence of `E-GEOM-BACKEND-INIT` said that the package holds
`win-x64`, which does not tell the person what to correct.

## Goal

A person whose platform has a payload learns that the file is absent or does not load, and a person whose platform has
none learns that.

## Scope (in)

1. `ManifoldGeometryBackend.UnavailableReason(platform)` gives one of two causes. A platform of the folder
   `runtimes` of the package 3.5.2.1 (`win-x64`, `linux-x64`, `osx-arm64`) gives "the file is not in the folder
   of the program, or the system cannot load it". Another platform gives the list of the three platforms.

## Scope (out)

- E34: the version of the library that loaded. No function of `manifoldc` gives it to the code today.
- The other findings of R-0036.

## Acceptance criteria

- [x] A test fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p057-backend-reason) · v0.139

## Method

Mechanical: one overload with the platform as a parameter, and one list of the platforms of the package.
Judgement:  the list is a copy of the folder `runtimes` of the package 3.5.2.1, and a new package must change it;
            no test reads the package folder, because its path depends on the computer.
Weakest:    the list can drift from the package; E34 and E39 stay open.

## Progress

- 2026-10-31: E38 with 1 new test case; the folder `runtimes` of the package 3.5.2.1 holds the three platforms.
- 2026-10-31: 1 injection caught; `governed-by` names ADR-0014, ADR-0018 and ADR-0036; closed at v0.139.
