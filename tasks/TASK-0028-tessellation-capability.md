---
id: 0028
title: A mesh leaves the geometry backend through the query GetTessellation
status: Done
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

`Done`, v0.64 (the field `status` above is the authority). The owner accepted ADR-0018 on 2026-09-25,
after two corrections. The task waited for two tasks, which are both `Done`:

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

- [x] On the native backend, a 2 × 3 × 4 box gives 12 triangles. The signed volume of the triangles
      is +24, and it equals `manifold_volume`. This measures the winding that ADR-0018, item 9,
      states.
- [x] A 2 × 2 × 2 box minus a 2 × 2 × 2 box translated by (1, 0, 0) gives a closed mesh with a signed
      volume of +4.
- [x] Each mesh is closed and consistent: each edge occurs in exactly two triangles, one time in each
      direction. The test pairs the edges by position, because `MeshGL` can split a vertex.
- [x] Each position lies inside the box that `GetBoundingBox` gives for the same body.
- [x] On the managed backend the query gives `E-GEOM-CAP-MISSING`. For an unknown body it gives
      `E-GEOM-BODY-NOT-FOUND`.
- [x] The query adds no event, and the Document version does not change.
- [x] `/schema/queries/GetTessellation@1` gives the four result fields, with the type `array` for
      `positions` and `indices`.
- [x] Through `Engine.Api.Http`, a `CreateBox` and then a `GetTessellation` give `triangleCount` 12.
- [x] A query with a record result, other than `Aabb`, returns its fields through `Engine.Cli` and
      through `Engine.Api.Http`. Through `Engine.Cli` in part only: see "Weakest".
- [x] The Outcome block records the time and the JSON size of the tessellation of a body of about
      100,000 triangles, and the output of `GetTessellation` for the cut body of the first
      demonstration.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`, and the
      native tests ran on each runner. The merge records the run.

## Outcome

Status: Done · v0.64 · the commit that carries this block.

A client reads the triangles of a body from the query `GetTessellation`. `ITessellationOps`, the value
type `Tessellation` and the flag `BackendCapabilities.Tessellation` are in `Engine.Contracts`. The
Manifold backend implements the capability through `manifold_get_meshgl64`, and the managed backend
does not. Both hosts hold each query result as an object, so a new query needs no change to a host.

Measures on the computer of the owner (Windows, x64), with the native library 3.5.2:

- A 2 × 3 × 4 box: 8 vertices, 12 triangles, signed volume +24, merge length 0. The cut of the
  2 × 2 × 2 boxes: 8 vertices, 12 triangles, signed volume +4, merge length 0.
- A box with 75 × 75 pits, made with 5,625 subtractions in 157.8 s: 90,012 triangles and 45,008
  vertices. The tessellation took 26.1 ms. Its JSON with the options of the surface is 4,378,820
  bytes, written in 53.6 ms. A body of this size therefore costs about 4.4 MB on the wire, which
  ADR-0018 accepts for version 1.
- The first demonstration (objective 6): two 10 × 10 × 10 boxes, the second moved by (5, 0, 0) and
  subtracted. `POST /queries` with `GetTessellation` on the cut body gave no error and this result:

  ```json
  {"positions":[-5,-5,-5,-5,-5,5,-5,5,-5,-5,5,5,0,-5,-5,0,-5,5,0,5,-5,0,5,5],
   "indices":[0,2,4,0,1,3,0,5,1,0,3,2,2,3,6,1,7,3,0,4,5,1,5,7,2,6,4,3,7,6,5,4,7,6,7,4],
   "vertexCount":8,"triangleCount":12}
  ```

## Method

**Mechanical.** The winding measured +24 for the 2 × 3 × 4 box, as ADR-0018 item 9 states, so the
task did not stop. A reversed winding injected into the backend failed both volume tests with −24 and
−4. The HTTP scenario failed on the hosts of v0.63, because the surface read the result as an `Aabb`,
and passes after the change of one type argument. Each needed symbol is in the three native libraries
of the package 3.5.2.1, and a false name is absent, so the search can fail. The full suite of 353
tests passes, and the program of the gates passes its 70 runs.

**Judgement.** The release of the mesh. The source of the C binding of 3.5.2 shows that
`manifold_get_meshgl64` builds the mesh in the buffer of the caller with placement new, and that
`manifold_delete_meshgl64` calls the C++ `delete` on it, as `manifold_delete_manifold` and
`manifold_delete_box` do. The backend calls `manifold_destruct_meshgl64` and then frees the buffer with
`Marshal.FreeHGlobal`, which gave it, so one allocator owns one buffer. A test calls the tessellation
2,000 times on one body. The accessors copy with `memcpy` into the memory of the caller, so the
backend gives them pinned managed arrays. The contract and the query share one commit, because
ADR-0018 names the test of the query in `enforced-by` and the contract rule needs an ADR change in the
commit that changes `Engine.Contracts`.

**Weakest.** Each invocation of the command line builds an empty Document (ADR-0011 §5), so a query
there never finds a body, and no test sees a record result through the command line end to end. The
tests show that the query reaches its handler there, and that the renderer of the command line writes
the fields of a `Tessellation` held as an object. The large body was measured on one computer only.
The pipeline must still show that the native tests ran on the three runners.

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

## Progress

- 2026-10-12: the task is open, in the batch R2 to R6 that the owner ordered on 2026-10-12. The
  contract and the query share one commit: ADR-0018 names `Engine.Tests/Queries/GetTessellationQueryTests.cs`
  in `enforced-by`, and the contract rule needs an ADR change in the commit that changes
  `Engine.Contracts`, so the test must exist in that commit.
- 2026-10-12: the backend. The winding measures +24 for the 2 × 3 × 4 box, as ADR-0018 item 9 states.
  A reversed winding injected into the backend failed both volume tests with −24 and −4.
- 2026-10-12: the hosts and the documents. Closed, after a full local run of 353 tests.
