---
id: 0057
title: The exact planar subtract of the owned kernel, with the provenance of each face
status: Accepted
topic: Kernel, geometry, determinism
date: 2026-11-05
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Kernel/**
enforced-by: Engine.Tests/Kernel/PlanarSubtractTests.cs (TASK-0183 creates it)
---

# ADR-0057 — The exact planar subtract of the owned kernel, with the provenance of each face

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`docs/CHARTER.md`, section "The kernel boundary", says: the project owns a geometry kernel whose purpose is identity.
Manifold computes the solids; the owned kernel holds the topology (vertex, edge, coedge, loop, face, shell, solid) and
gives each face, each edge and each vertex a stable identity with its provenance. "A pair of surfaces with a closed
formula computes exactly. Each other pair refuses, and the operation falls back to Manifold."

Roadmap phase K4 is "Planar faces only, with a boolean. Provenance inside the kernel." It needs K0, K1 and K2, which are
done. ADR-0050 left the 3D predicate `Orient3D` to K4, its first consumer. K4 is 35 to 55 evenings, so it comes in
slices. On 2026-11-05 the owner answered four questions:

- **The first slice.** The boolean first: the planar subtract of the owned kernel, with the exact 3D predicate. The
  identity of each face comes from the result.
- **The project.** A new project `Engine.Kernel`, which no backend and no client reaches.
- **The curved sides.** The flat facets of one arc or one circle of a profile (ADR-0040) are one named face.
- **The references.** The datums of ADR-0051 and the references of ADR-0052 do not change in the first slice.

## Decision

1. **The project.** `Engine.Kernel` is a library of managed code that references no project and no package. In K4a no
   project of the engine references it; `Engine.Tests` does. The dependency gate holds it in the class of the kernel,
   and the determinism gate reads it as a project of the path of the log.
2. **The numbers.** Each coordinate, each plane and each value of the kernel is an exact rational of two `BigInteger`
   values. A double enters exactly (its bits), and the kernel uses + - * / and comparisons only, so each sign of each
   predicate is the sign of the true value, on each platform. A result leaves as a double only for a comparison or a
   display, with one rounding to the nearest.
3. **The predicate.** `Orient3D(a, b, c, d)` gives the sign of the determinant of `b − a`, `c − a`, `d − a`. A plane
   has an exact normal and offset from three of its points, and the side of a point is the sign of one rational value.
   No filter is needed while each value is exact; a filter can come later for speed.
4. **The solids.** A solid of the kernel is a closed set of planar polygons, each with its plane and its tag. The kernel
   builds a box (centred at the origin, as `CreateBox` is), an extrude of a closed profile of points on a frame by a
   distance, and a translate. The tag of a face is its provenance: the feature, the element and the item, as a
   reference of ADR-0051 names them: `+x` of a box, `start`, `end` and `side` of an extrude with the line or the curve
   of the profile. The facets of one arc or circle carry one tag (the answer of the curved sides).
5. **The subtract.** `Subtract(a, b)` clips the polygons of each solid by a binary space partition of the other, in the
   form of the algorithm of csg.js (Evan Wallace): the polygons of `a` outside `b`, and the polygons of `b` inside `a`,
   turned. With exact planes, a split point is exact and each coplanar polygon goes to one side by the sign of the dot
   product of the two normals. Each piece keeps the tag of the face that it came from, so a face of the result names
   the face of `a` or of `b` that made it. An empty result is a refusal, as an empty subtract of Manifold is.
6. **The topology.** After a subtract, the kernel splits each edge at each vertex that lies on it exactly, joins the
   pieces of one plane and one tag into one face, and gives its loops: the outer loop and each hole, by the sign of the
   area. Each edge of a valid result has two coedges in opposite directions. The B-Rep gives the vertices, the edges,
   the faces with their loops and tags, the shells, and the exact volume.
7. **The proof.** The kernel does not make the solid of the engine in K4a: Manifold still makes each solid. A test
   builds random models of boxes, moves and extrudes in both, and compares the exact volume and the bounds of the
   kernel with those of Manifold, and the topology with the Euler–Poincaré formula.
8. **Determinism.** The kernel has no transcendental call, no hash order and no clock; each list keeps the order of its
   input. The same input gives the same B-Rep on each platform.
9. **Out of this record.** The engine reads the kernel (K4b): a B-Rep of each body as a projection of the feature list,
   a query of the faces and their names. The datums and the references by the identity of a face, and a fault for a
   face that is gone (K4c). A union and an intersection. Curved surfaces (K5).

## Consequences

- The subtract of planar solids has an exact result with the provenance of each face, which Manifold cannot give.
- A rational number grows with each operation, so the kernel is slow for a large model; K4a proves correctness first,
  and a filter or a snap to a grid can follow.
- A binary space partition can cut one face into more pieces than needed; the topology joins them again by their plane
  and their tag.

## History

- 2026-11-05 · TASK-0183 · Accepted with the four answers of the owner of 2026-11-05.
