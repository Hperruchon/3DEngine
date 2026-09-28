---
id: 0018
title: The tessellation capability
status: Accepted
topic: Geometry, contracts, rendering
date: 2026-09-23
supersedes: []
superseded-by: []
amends: ['0012']
amended-by: []
affects:
  - Engine.Contracts/Geometry/**
  - Engine.Core/Queries/**
  - Engine.Geometry.Manifold/**
  - Engine.Cli/Cli.cs
  - Engine.Api.Http/Endpoints/QueriesEndpoint.cs
enforced-by: Engine.Tests/Queries/GetTessellationQueryTests.cs (TASK-0028 creates it)
---

# ADR-0018 — The tessellation capability

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R2 says: "A tessellation capability. A mesh leaves the geometry backend." Roadmap rule
2 says that R2 blocks each later phase of track R. Today no capability gives a mesh. `IMeshOps`
creates a box, `IGeometryQuery` gives a bounding box, and nothing gives a triangle.

ADR-0012 keeps one open challenge for this: "Whether that lives on `IGeometryQuery.Tessellate(handle)`
or `IMeshOps.Tessellate(handle)` is open ... Decided when the first client actually consumes it."
Phase R5 is that client, and R2 must come first.

Five facts limit the choice:

1. ADR-0001 and ADR-0012 reject a general geometry type such as `Mesh` or `Solid` on the wire. Both
   records expect a tessellated preview: ADR-0001 says "clients see commands and tessellated previews
   only", and ADR-0012 adds "via explicit queries". Anti-objective 9 in `docs/CHARTER.md` gives the
   test: "a type that forces unlike geometries into one shape trips this rule. A typed capability
   does not."
2. `CLAUDE.md` rule 6 forbids a tessellation in the Document. A tessellation is presentation.
3. The native payload already exports the double-precision mesh API. On 2026-09-23 a search of the
   raw bytes found each of eleven symbols, among them `manifold_get_meshgl64`,
   `manifold_meshgl64_vert_properties` and `manifold_meshgl64_tri_verts`, in each of the seven native
   libraries for `linux-x64`, `osx-arm64` and `win-x64`. A name that does not exist was absent, which
   proves that the search can fail. R2 therefore needs no new native build.
4. Both hosts fix the result type of a query to `Aabb` (`Engine.Cli/Cli.cs:157`,
   `Engine.Api.Http/Endpoints/QueriesEndpoint.cs:81`). The bus casts the result of the handler to
   that type, therefore a `Tessellation` result fails in each host. On 2026-09-23 a probe called the
   bus with the type `object` and wrote the result with the options of `ApiJson`. The JSON held the
   real fields of each record. The correction is one type argument in each host.
5. The change adds a type, an interface and a flag to `Engine.Contracts/Geometry/`. `CLAUDE.md`,
   section "Stop and ask", gives that decision to the owner.

The first draft of this record, of 2026-09-23, had two errors. It said that the hosts were generic
already, and fact 4 disproves that. It also added the field `Items` to `FieldSchema`. That field adds
`"items": null` to each field in `/schema`, and R2 does not need it. The architecture challenge of
2026-09-23 (`docs/reviews/2026-09-23-architecture-challenge.md`) found both errors. This text
corrects them. The owner accepted the corrected record on 2026-09-25.

## Decision

1. **A separate capability.** `Engine.Contracts/Geometry/ITessellationOps.cs` declares one method:
   `Tessellation Tessellate(BodyHandle handle)`. `BackendCapabilities` gains
   `Tessellation = 1 << 4`. The interface is separate from `IMeshOps` and from `IGeometryQuery`, as
   `ITransformOps` and `IBooleanOps` are (ADR-0012 Amendment 1). No existing backend changes, and a
   backend that cannot tessellate gives `E-GEOM-CAP-MISSING`. This closes the open challenge of
   ADR-0012.
2. **A query-output value type.** `Engine.Contracts/Geometry/Tessellation.cs` is a record, in the
   same way as `Aabb`: `double[] Positions` holds x, y and z of each vertex in sequence, and
   `uint[] Indices` holds three vertex indices for each triangle. It also gives `VertexCount` and
   `TriangleCount`. It has no identity and no operation. It never enters a command, an event, the
   Document or the log.
3. **Double precision on the wire.** The backend reads `MeshGL64`. `Aabb` and `BoxParameters` are
   double, therefore the engine keeps one precision. A host narrows each value to 32 bits for the
   graphics processor, because that is presentation.
4. **JSON arrays, with no binary form.** A query is not logged and not replayed. A later
   `SchemaVersion` 2 can change the wire form, and no log and no saved document needs a migration.
   The cost test in `CLAUDE.md`, section "Anti-patterns", therefore says: do not add it now.
5. **No normal and no tolerance.** A Manifold body is a triangle mesh, therefore the export is exact.
   The chord tolerance in the charter has nothing to control until the owned kernel gives curved
   surfaces (track K). A host computes flat normals.
6. **One query.** `Engine.Core/Queries/GetTessellationQuery.cs` gives `GetTessellation`, version 1.
   Parameter: `bodyId` (guid, required). Result: `vertexCount` (integer), `triangleCount` (integer),
   `positions` (array of number) and `indices` (array of integer). The errors are
   `E-GEOM-CAP-MISSING` and `E-GEOM-BODY-NOT-FOUND`, which exist. No new diagnostic code.
7. **Generic results in the hosts.** `Engine.Cli` and `Engine.Api.Http` call `Query<object>`, and
   they write the result as JSON. A caller in the same process keeps the typed call
   `Query<Tessellation>`. Each later query then needs no change to a host. The schema gives
   `positions` and `indices` the type `array`. The item type of an array waits: ADR-0013 §2 gives the
   moment to add it, and no client needs it now.
8. **One backend.** `ManifoldGeometryBackend` implements the capability. The managed
   `InProcessMeshBackend` does not, for the reason that ADR-0012 Amendment 1 gives for a transform
   and a boolean: the managed backend holds box sizes only, and a second implementation of the same
   output is a second place for one rule.
9. **Winding.** The tessellation is wound counter-clockwise when seen from outside the body. The
   Manifold documentation of `MeshGLP` states this: the triangle indices are "in CCW (from the
   outside) order". glTF 2.0 uses the same rule. That page shows version 3.0 and not 3.5.2, therefore
   TASK-0028 also measures it: the signed volume of the triangles of a 2 × 3 × 4 box must be +24, and
   it must equal `manifold_volume`. If the measurement gives −24, the task stops, and a new record
   decides where the winding changes.

## Consequences

**Good:** Phases R3 to R6 get a mesh. A person, a script, an agent and the desktop host get the same
triangles from the same query. The capability is typed, as anti-objective 9 requires. No native
rebuild is needed.

**Bad:** `Engine.Contracts` grows by one type, one interface and one flag. The schema says `array`
for `positions` and `indices`, and it does not say the type of an item. A JSON array of doubles is
large: a finely carved body can give megabytes, and a later version must change
the form. A host with no native payload shows no geometry, because the managed backend refuses. When
the owned kernel arrives (track K), it needs a tolerance parameter, and that is version 2 of the
query.

**Next:** TASK-0028. It waits for TASK-0034 (queries in series with commands) and TASK-0036 (a native
test that cannot skip in the pipeline). This record closes the open challenge of ADR-0012, therefore
it amends ADR-0012. It does not amend ADR-0013.
