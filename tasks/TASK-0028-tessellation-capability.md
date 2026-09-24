---
id: 0028
title: A mesh leaves the geometry backend through the query GetTessellation
status: Deferred
phase: R2
opened: 2026-09-23
depends-on: [0027]
governed-by: [0001, 0004, 0008, 0012, 0013, 0014, 0016, 0018]
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
    - Engine.Contracts/Schema/FieldSchema.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Geometry.Manifold/Native/ManifoldNative.cs
    - Engine.Tests/Geometry/ManifoldGeometryBackendTests.cs
    - Engine.Tests/Http/SchemaEndpointGateTests.cs
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/0013-command-query-schema-declaration.md
    - docs/adr/README.md
    - docs/glossary.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/Handlers/**
    - Engine.Core/Commands/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - 3DEngine/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Core/**
    - BlazorApp/**
    - nuget/**
---

# TASK-0028 — A mesh leaves the geometry backend through the query GetTessellation

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Status

`Deferred`. ADR-0018 has the status `Proposed`. The task changes the public shape of
`Engine.Contracts`, and `CLAUDE.md`, section "Stop and ask", gives that decision to the owner. When
the owner accepts ADR-0018, this task gets the status `Ready`, and the first commit of the work makes
the changes that ADR-0018, section "Next", lists.

## Context

Roadmap phase R2: "A tessellation capability. A mesh leaves the geometry backend." Roadmap rule 2
says that R2 blocks each later phase of track R. ADR-0018 gives the decision and each reason.

Three facts were verified on 2026-09-23:

1. The native payload exports `manifold_get_meshgl64` and each accessor that this task needs, in
   each of the seven libraries for `linux-x64`, `osx-arm64` and `win-x64`. A search for a name that
   does not exist found nothing, therefore the search can fail. No native rebuild is needed.
2. `SchemaQueriesEndpoint` gives the `FieldSchema` of each handler as it is, and
   `Engine.Api.Http/Json/ApiJson.cs` writes a null value. A new field on `FieldSchema` therefore
   needs no endpoint code, and each existing field gains `"items": null`.
3. `NativeManifoldFactAttribute` skips a test when the native library does not load. A runner that
   loses the library therefore reports green, and the core test of this task would prove nothing.
   The log of a run needs a token, so nobody outside the runner sees a skip.

## Goal

A client reads the triangles of a body from the query `GetTessellation`.

## Scope (in)

1. `ITessellationOps`, the value type `Tessellation`, and the flag `BackendCapabilities.Tessellation`.
2. The field `Items` on `FieldSchema`.
3. `GetTessellationQuery` and its handler, registered in `HandlerCatalog`.
4. `ManifoldGeometryBackend` implements `ITessellationOps` through `manifold_get_meshgl64`.
5. In continuous integration, a native test fails instead of skipping when the library does not load.
   GitHub Actions sets the variable `CI` to `true` on each runner. The GitHub reference for variables
   says "Always set to true" (verified 2026-09-23).
6. The front matter of ADR-0012 and ADR-0013 records the amendment. Their text does not change.

## Scope (out)

- No host code. The desktop host reads the mesh in phase R5.
- No normal, no binary wire form and no tolerance parameter. ADR-0018, items 4 and 5, gives the reason.
- No tessellation in the managed `InProcessMeshBackend`. ADR-0018, item 8, gives the reason.
- No `Properties` on `FieldSchema`. No field holds an object.
- No change to `Engine.Cli` or `Engine.Api.Http`. Each surface is generic already.
- No native rebuild and no change to `nuget/`.

## Acceptance criteria

- [ ] On the native backend, a 2 × 3 × 4 box gives 12 triangles, and the signed volume of the
      triangles is +24. This measures the winding that ADR-0018, item 9, states.
- [ ] A 2 × 2 × 2 box minus a 2 × 2 × 2 box translated by (1, 0, 0) gives a closed mesh with a signed
      volume of +4.
- [ ] Each mesh is closed and consistent: each edge occurs in exactly two triangles, one time in each
      direction.
- [ ] Each position lies inside the box that `GetBoundingBox` gives for the same body.
- [ ] On the managed backend the query gives `E-GEOM-CAP-MISSING`. For an unknown body it gives
      `E-GEOM-BODY-NOT-FOUND`.
- [ ] The query adds no event, and the Document version does not change.
- [ ] `/schema/queries/GetTessellation@1` gives the four result fields, with `items` set to `number`
      for `positions` and to `integer` for `indices`.
- [ ] Through `Engine.Api.Http`, a `CreateBox` and then a `GetTessellation` give `triangleCount` 12.
- [ ] With `CI=true`, a native test fails when the library does not load. Verified by injection:
      hide the native library from the test output, and watch the test fail.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **Ownership.** TASK-0012 §2 verified that `manifold_delete_manifold` and `manifold_delete_box`
  free the buffer of the caller. Nobody verified this for `manifold_delete_meshgl64`. Verify it in the
  same way before you write the release code. A wrong guess frees the buffer two times and stops the
  process (TASK-0012 saw exit code 127).
- **Layout.** `manifold_meshgl64_num_prop` gives the count of properties for each vertex. Measure that
  the first three are x, y and z. Do not assume it.
- **Indices.** `manifold_meshgl64_tri_verts` gives 64-bit values. Narrow each one to 32 bits, and
  throw if a value does not fit.
- **Commits.** One topic in each commit: the contract, the backend, the query, the pipeline rule for
  native tests. Each commit carries this task file, because the write-set gate needs it.
- **ADR-0015.** It affects `Engine.Core/**`, and it has the status `Proposed`. It is not in force,
  therefore it is not in `governed-by`.
