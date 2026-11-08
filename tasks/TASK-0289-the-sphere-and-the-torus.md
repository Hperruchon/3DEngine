---
id: 0289
title: Commands CreateSphere and CreateTorus make a sphere and a torus, and the exact kernel cuts them in about ten seconds
status: Active
phase: K5d
opened: 2026-11-08
depends-on: [0288]
governed-by: [0004, 0012, 0016, 0021, 0028, 0031, 0033, 0035, 0037, 0039, 0051, 0052, 0054, 0055, 0057, 0058, 0059, 0060, 0076, 0083, 0089]
writes:
  create:
    - tasks/TASK-0289-the-sphere-and-the-torus.md
    - Engine.Core/Commands/CreateSphereCommand.cs
    - Engine.Core/Commands/CreateSphereCommandHandler.cs
    - Engine.Tests/Commands/CreateSphereAndTorusCommandTests.cs
    - Engine.Tests/Kernel/SideFilterTests.cs
  modify:
    - Engine.Core/Features/FeatureList.cs
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Core/Features/Regeneration.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Kernel/Bsp.cs
    - Engine.Kernel/Geometry.cs
    - Engine.Kernel/KernelSolid.cs
    - Engine.Kernel/Rational.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/Sketch/SketchReferenceTests.cs
    - docs/adr/0089-the-cone-the-sphere-and-the-torus-are-closed-meshes.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0289 — Commands CreateSphere and CreateTorus make a sphere and a torus, and the exact kernel cuts them in about ten seconds

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0089 item 5 names the sphere and the torus as phase K5d. A first try (v0.273) showed that a cut of a plate by a sphere
of 3968 triangles took about 3 minutes in the exact kernel, and the recursive tree overflowed the stack. Q1 of the codebase
review of 2026-11-08 asked the owner; the standing order gives its recommended answer A: make the exact kernel faster, and
keep the facets of item 5. The answer changes no contract.

## Goal

A person makes a sphere and a torus, and a cut by them keeps the names of the faces in about ten seconds.

## Scope (in)

1. `CreateSphere` (radius) and `CreateTorus` (radius and tube radius): the closed meshes of ADR-0089 item 5 from the table
   of the circle, stored by `ISolidMeshOps`, with the face `surface`. The record holds the inputs only.
2. The exact kernel (ADR-0089 item 6): a tree with no recursion, and a side test with a filter of doubles before the exact
   sign, which sums the fractions with no reduction.
3. Tests: the refusals, the closed meshes, the face of each body, a cut by a sphere within a time limit, and the filter
   against the exact sign.

## Scope (out)

- The entries of the list "New" (K5e). The determinism probe with a sphere.

## Acceptance criteria

- [ ] Each new test passes, and a test fails with each injection of the change.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the meshes from the table of the circle; the time of the cut, measured before and after each change.
Judgement:  the bound of the filter (1e-14 of the sum of the magnitudes, about ten times the bound of the rounding).
Weakest:    to be written at the close.

## Progress

- 2026-11-08: a measure of the cut of a plate by a sphere: 78 s in the subtract, 66 s of it for the tree of the sphere. The
  sign with no reduction gave 16 s, and the filter 2.5 s, with the same 2991 polygons.
- 2026-11-08: the cut by a sphere in the command takes about 10 s (4.1 s for the subtract, 1.9 s for the faces). The
  injections (a filter that trusts each double, and a sign with no last term) fail the tests of the filter.
