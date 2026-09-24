---
id: 0018
title: The tessellation capability
status: Proposed
topic: Geometry, contracts, rendering
date: 2026-09-23
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Contracts/Geometry/**
  - Engine.Contracts/Schema/**
  - Engine.Core/Queries/**
  - Engine.Geometry.Manifold/**
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
4. ADR-0013 §2 defers the item type of an array, and it names the moment to add it: "When the first
   command needs nested schema ... this ADR amends to add an optional `Items` and `Properties` to
   `FieldSchema`." A result with an array of numbers is that moment. `SchemaQueriesEndpoint` gives
   each `FieldSchema` of a handler as it is, therefore a new field needs no endpoint code.
5. The change adds a type, an interface and a flag to `Engine.Contracts/Geometry/`, and a field to
   `Engine.Contracts/Schema/FieldSchema.cs`. `CLAUDE.md`, section "Stop and ask", gives that decision
   to the owner. This record is the proposal.

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
7. **The item type of an array.** `FieldSchema` gains the optional field `Items`, which names the
   type of each item of an array. It uses the same vocabulary as `Type`. `Properties` stays
   deferred, because no field holds an object. This amends ADR-0013 §2 in the way that §2 planned.
8. **One backend.** `ManifoldGeometryBackend` implements the capability. The managed
   `InProcessMeshBackend` does not, for the reason that ADR-0012 Amendment 1 gives for a transform
   and a boolean: the managed backend holds box sizes only, and a second implementation of the same
   output is a second place for one rule.
9. **Winding, measured.** The tessellation is wound counter-clockwise when seen from outside the body.
   The header of Manifold 3.5.2 does not state this plainly. TASK-0028 must measure it: the signed
   volume of the triangles of a 2 × 3 × 4 box must be +24. If the measurement gives −24, the task
   stops, and a new record decides where the winding changes.

## Consequences

**Good:** Phases R3 to R6 get a mesh. A person, a script, an agent and the desktop host get the same
triangles from the same query. The capability is typed, as anti-objective 9 requires. No native
rebuild is needed.

**Bad:** `Engine.Contracts` grows by one type, one interface, one flag and one schema field. Each
field in `/schema` now also gives `"items": null` when it is not an array, because
`Engine.Api.Http/Json/ApiJson.cs` writes a null value. A JSON
array of doubles is large: a finely carved body can give megabytes, and a later version must change
the form. A host with no native payload shows no geometry, because the managed backend refuses. When
the owned kernel arrives (track K), it needs a tolerance parameter, and that is version 2 of the
query.

**Next:** TASK-0028. On acceptance, this record amends ADR-0012 and ADR-0013: the field `amends` gets
`0012` and `0013`, ADR-0012 gets `amended-by: ['0018']` and the status `Amended`, ADR-0013 adds
`0018` to `amended-by`, and TASK-0028 gets the status `Ready`.
