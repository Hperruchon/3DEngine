---
id: 0184
title: The engine reads the owned kernel, and a query gives the faces of a body with their names
status: Active
phase: K4b
opened: 2026-11-05
depends-on: [0183]
governed-by: [0004, 0013, 0016, 0018, 0019, 0028, 0033, 0035, 0036, 0037, 0039, 0040, 0050, 0054, 0057, 0058]
writes:
  create:
    - tasks/TASK-0184-the-engine-reads-the-owned-kernel.md
    - docs/adr/0058-the-engine-reads-the-owned-kernel.md
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Core/Queries/GetFacesQuery.cs
    - Engine.Core/Queries/GetFacesQueryHandler.cs
    - Engine.Tests/Features/GetFacesTests.cs
  modify:
    - CLAUDE.md
    - Engine.Core/Engine.Core.csproj
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Sketch/SketchProfile.cs
    - Engine.Kernel/Brep.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - eng/test-floor.txt
    - docs/INDEX.md
    - docs/diagnostics.md
    - docs/adr/0057-planar-subtract-in-the-owned-kernel.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0184 — The engine reads the owned kernel, and a query gives the faces of a body with their names

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

K4a (TASK-0183) gives the exact planar subtract of the owned kernel, and no project of the engine reads it. On
2026-11-05 the owner answered the four questions of K4b: the kernel beside Manifold, the B-Rep at the query with a
cache, one entry for each face name, and a split face as one name with its parts. ADR-0058 gives the design.

## Goal

`GetFaces(bodyId)` gives the faces of a live body from the B-Rep of the owned kernel, each with the feature, the element
and the item that made it.

## Scope (in)

1. `Engine.Core` references `Engine.Kernel`, and the dependency gate says so.
2. The kernel solid of a body as a projection of the steps in effect, with the item of each side of a profile.
3. The query `GetFaces`, its two refusal codes, and its cache.
4. A faster topology: a rounded filter before the exact test of a vertex on an edge, and the removal of the straight
   vertices in passes.

## Scope (out)

- K4c: the datums and the references by the identity of a face.
- The desktop does not call `GetFaces`.
- A faster subtract on curves.

## Acceptance criteria

- [x] A box, an extrude, a circle and a cut give the names, the normals, the areas and the parts of ADR-0058 item 4.
- [x] The bounds of the kernel solid of each body agree with the bounds of Manifold.
- [x] A body that is not live and a body with a fault give their codes.
- [x] A query again gives the kept result, and an edit builds a new one.
- [x] An injection into each new step makes a test fail.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the counts and the normals of the names, and the bounds against Manifold.
Judgement:  the fields of an entry, and a body with a fault as a refusal.
Weakest:    no command reaches `E-GEOM-FACES-FAILED`, so the code has no test through the commands.

## Progress

- 2026-11-05: opened after the four answers of the owner on K4b.
- 2026-11-05: the projection, the query and the faster topology; 9 tests, and 15 injections each fail a test.
