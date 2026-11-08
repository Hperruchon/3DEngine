---
id: 0288
title: A command CreateCone makes a cone, a closed mesh that the engine stores as a solid
status: Done
phase: K5c
opened: 2026-11-08
depends-on: [0287]
governed-by: [0001, 0004, 0008, 0012, 0014, 0016, 0018, 0021, 0028, 0031, 0033, 0035, 0036, 0037, 0039, 0051, 0052, 0054, 0055, 0057, 0058, 0059, 0060, 0076, 0083, 0089]
writes:
  create:
    - tasks/TASK-0288-the-cone.md
    - Engine.Contracts/Geometry/ISolidMeshOps.cs
    - Engine.Core/Commands/CreateConeCommand.cs
    - Engine.Core/Commands/CreateConeCommandHandler.cs
    - Engine.Tests/Commands/CreateConeCommandTests.cs
  modify:
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Geometry.Manifold/Native/ManifoldNative.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Features/FeatureList.cs
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Core/Features/Regeneration.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Kernel/KernelSolid.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - docs/adr/0089-the-cone-the-sphere-and-the-torus-are-closed-meshes.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid: []
---

# TASK-0288 — A command CreateCone makes a cone, a closed mesh that the engine stores as a solid

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-08 the owner answered the form of the open decisions in the chat:

- ADR-0089 (K5c): A to Q1 (a closed mesh as a solid, through a new capability), and A to Q2 and Q3 (the 64-gon table, and
  the faces `-z`, `side` and `surface`). This task holds the change of `Engine.Contracts` and the ADR in one commit, as
  `CLAUDE.md` asks.
- The redesign: start the agent proposal arc; the author of a command and the rollback wait.
- The fonts Instrument Sans and JetBrains Mono: download them.
- E164: add the field of the warning to the HTTP answer of a save, with an ADR.
- A start with `--file` skips the home screen.
- The affects rule stays from ADR-0082. A sketch edit while a body is selected stays as it is (ADR-0084 item 6). The phone
  host treats CA1416 only as an error, as now.
- The owner gave no answer to Q1 of the review of 2026-11-07 (c) (E182), so its answer A stays open for the check of the
  owner, and named no register entry to close.

## Goal

The person can make a cone with one command; the cone is a solid that takes each operation of a solid.

## Scope (in)

1. `ISolidMeshOps` in `Engine.Contracts`, and its Manifold implementation.
2. `CreateCone`, its mesh from the 64-gon, its kernel solid with the faces `-z` and `side`, and the creators of ADR-0089
   item 4.
3. ADR-0089 Accepted.

## Scope (out)

- The sphere and the torus (K5d). The entries of the list "New" (K5e). The other answers of the owner, each in its own
  task.

## Acceptance criteria

- [x] A cone has the faces `-z` and `side`, a datum on `-z`, a refusal on the side, and it cuts a plate.
- [x] The mesh is closed and oriented, and its base is the circle of a sketch.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch k5c-cone) · v0.273

## Method

Mechanical: the tests of the cone; injections of a base with the wrong winding and of a catalog with no cone.
Judgement:  the mesh with a centre vertex of the base, so each triangle of the base has a fixed shape.
Weakest:    the cone is not in the determinism probe yet, so no run compares its hash on the four platforms; its mesh
            uses only + - * / and sqrt.

## Progress

- 2026-11-08: ADR-0089 Accepted; `ISolidMeshOps`, its Manifold call, `CreateCone` and its kernel solid. The signatures of
  `manifold_meshgl64` and `manifold_of_meshgl64` come from `manifoldc.h` of the tag v3.5.2 on GitHub. An injection of a base
  with the wrong winding fails four tests; an injection of a catalog with no cone fails eleven.
- 2026-11-08: run 42430465747 passed. Closed at v0.273.
