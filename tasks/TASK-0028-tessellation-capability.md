---
id: 0028
title: A mesh leaves the geometry backend through the query GetTessellation
status: Ready
phase: R2
opened: 2026-09-23
depends-on: [0034, 0036]
governed-by: [0001, 0002, 0004, 0008, 0011, 0012, 0014, 0016, 0018, 0019]
writes:
  create:
    - tasks/TASK-0028-tessellation-capability.md
    - docs/adr/0018-tessellation-capability.md
    - Engine.Contracts/Geometry/ITessellationOps.cs
    - Engine.Contracts/Geometry/Tessellation.cs
    - Engine.Core/Queries/GetTessellationQuery.cs
    - Engine.Core/Queries/GetTessellationQueryHandler.cs
    - Engine.Tests/Queries/GetTessellationQueryTests.cs
    - Engine.Tests/Geometry/ManifoldTessellationTests.cs
    - Engine.Tests/Http/HttpTessellationScenarioTests.cs
  modify:
    - Engine.Contracts/Geometry/BackendCapabilities.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Geometry.Manifold/Native/ManifoldNative.cs
    - Engine.Cli/Cli.cs
    - Engine.Api.Http/Endpoints/QueriesEndpoint.cs
    - Engine.Tests/Geometry/ManifoldGeometryBackendTests.cs
    - Engine.Tests/Http/SchemaEndpointGateTests.cs
    - Engine.Tests/Cli/**
    - docs/adr/README.md
    - docs/glossary.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/Handlers/**
    - Engine.Contracts/Schema/**
    - Engine.Core/Commands/**
    - Engine.Api.Http/WebSockets/**
    - 3DEngine/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Core/**
    - BlazorApp/**
    - nuget/**
---

# TASK-0028 — A mesh leaves the geometry backend through the query GetTessellation

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Status

`Ready`. The owner accepted ADR-0018 on 2026-09-25, after two corrections. The task waits for two
tasks:

- TASK-0034 puts queries in series with commands. A tessellation is a long read, and today a query
  reads shared state while a command writes it.
- TASK-0036 makes a native test fail in the pipeline when the library does not load. Most tests of
  this task need the library, and today they skip.

## Context

Roadmap phase R2: "A tessellation capability. A mesh leaves the geometry backend." Roadmap rule 2
says that R2 blocks each later phase of track R. ADR-0018 gives the decision and each reason.

Three facts were verified on 2026-09-23:

1. The native payload exports `manifold_get_meshgl64` and each accessor that this task needs, in
   each of the seven libraries for `linux-x64`, `osx-arm64` and `win-x64`. A search for a name that
   does not exist found nothing, therefore the search can fail. No native rebuild is needed.
2. Both hosts fix the result type of a query to `Aabb` (`Engine.Cli/Cli.cs:157`,
   `Engine.Api.Http/Endpoints/QueriesEndpoint.cs:81`). A probe called the bus with the type `object`,
   and the JSON held the real fields of each record. ADR-0018, item 7, therefore changes one type
   argument in each host.
3. The Manifold documentation of `MeshGLP` says that the triangle indices are "in CCW (from the
   outside) order", that `numProp` is at least 3, and that the first three properties are x, y and
   z. The page shows version 3.0, not 3.5.2. The task measures each of these facts.

The first version of this task, of 2026-09-23, also added `FieldSchema.Items` and the pipeline rule
for native tests. ADR-0018 no longer has the first, and TASK-0036 has the second.

## Goal

A client reads the triangles of a body from the query `GetTessellation`.

## Scope (in)

1. `ITessellationOps`, the value type `Tessellation`, and the flag `BackendCapabilities.Tessellation`.
2. `GetTessellationQuery` and its handler, registered in `HandlerCatalog`.
3. `ManifoldGeometryBackend` implements `ITessellationOps` through `manifold_get_meshgl64`.
4. `Engine.Cli` and `Engine.Api.Http` call `Query<object>` (ADR-0018, item 7).
5. The field `enforced-by` of ADR-0018 loses the text "(TASK-0028 creates it)" when the test exists.

## Scope (out)

- No host code that draws. The desktop host reads the mesh in phase R5.
- No normal, no binary wire form and no tolerance parameter. ADR-0018, items 4 and 5, gives the reason.
- No tessellation in the managed `InProcessMeshBackend`. ADR-0018, item 8, gives the reason.
- No change to `FieldSchema`. The item type of an array waits (ADR-0018, item 7).
- No change to a host other than the type argument of the query call.
- No native rebuild and no change to `nuget/`.

## Acceptance criteria

- [ ] On the native backend, a 2 × 3 × 4 box gives 12 triangles. The signed volume of the triangles
      is +24, and it equals `manifold_volume`. This measures the winding that ADR-0018, item 9,
      states.
- [ ] A 2 × 2 × 2 box minus a 2 × 2 × 2 box translated by (1, 0, 0) gives a closed mesh with a signed
      volume of +4.
- [ ] Each mesh is closed and consistent: each edge occurs in exactly two triangles, one time in each
      direction. The test pairs the edges by position, because `MeshGL` can split a vertex.
- [ ] Each position lies inside the box that `GetBoundingBox` gives for the same body.
- [ ] On the managed backend the query gives `E-GEOM-CAP-MISSING`. For an unknown body it gives
      `E-GEOM-BODY-NOT-FOUND`.
- [ ] The query adds no event, and the Document version does not change.
- [ ] `/schema/queries/GetTessellation@1` gives the four result fields, with the type `array` for
      `positions` and `indices`.
- [ ] Through `Engine.Api.Http`, a `CreateBox` and then a `GetTessellation` give `triangleCount` 12.
- [ ] A query with a record result, other than `Aabb`, returns its fields through `Engine.Cli` and
      through `Engine.Api.Http`.
- [ ] The Outcome block records the time and the JSON size of the tessellation of a body of about
      100,000 triangles, and the output of `GetTessellation` for the cut body of the first
      demonstration.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`, and the
      native tests ran on each runner.

## Notes for the implementer

- **Ownership.** TASK-0012 §2 verified that `manifold_delete_manifold` and `manifold_delete_box`
  free the buffer of the caller. Nobody verified this for `manifold_delete_meshgl64`. Verify it in the
  same way before you write the release code. A wrong guess frees the buffer two times and stops the
  process (TASK-0012 saw exit code 127).
- **Layout.** `manifold_meshgl64_num_prop` gives the count of properties for each vertex. Read it,
  and take the first three values of each stride. Do not assume a stride of 3.
- **Merge vectors.** Measure `manifold_meshgl64_merge_length` for a box, and record the value.
- **Indices.** `manifold_meshgl64_tri_verts` gives 64-bit values. Narrow each one to 32 bits, and
  throw if a value does not fit.
- **Commits.** One topic in each commit: the contract, the backend, the query and the hosts. Each
  commit carries this task file, because the write-set gate needs it. The commit that changes
  `Engine.Contracts` also changes ADR-0018 (its field `enforced-by`), because the contract gate
  wants an ADR change in the same pull request.
- **ADR-0015.** It affects `Engine.Core/**`, and it has the status `Proposed`. It is not in force,
  therefore it is not in `governed-by`.
