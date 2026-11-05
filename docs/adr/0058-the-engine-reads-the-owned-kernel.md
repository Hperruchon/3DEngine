---
id: 0058
title: The engine reads the owned kernel for the faces of a body
status: Amended
topic: Kernel, geometry, queries
date: 2026-11-05
supersedes: []
superseded-by: []
amends: ['0057']
amended-by: ['0059']
affects:
  - Engine.Core/Features/KernelProjection.cs
  - Engine.Core/Queries/GetFacesQuery.cs
  - Engine.Core/Queries/GetFacesQueryHandler.cs
  - Engine.Core/Sketch/SketchProfile.cs
  - Engine.Kernel/Brep.cs
enforced-by: Engine.Tests/Features/GetFacesTests.cs (TASK-0184 creates it)
---

# ADR-0058 — The engine reads the owned kernel for the faces of a body

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0057 (phase K4a) gives the exact planar subtract of the owned kernel, with the provenance of each face. No project
of the engine referenced the kernel. Phase K4b is the next slice: the engine reads the kernel. On 2026-11-05 the owner
answered four questions:

- **The role.** The kernel works beside Manifold. Manifold still makes the solid and the mesh of each body. The engine
  builds the B-Rep of the kernel for the faces and their names only.
- **The time.** The engine builds the B-Rep when a query asks for it, and keeps it under the geometry key of the body.
- **The query.** A new query `GetFaces` gives one entry for each face name, with its normal, a point, its area and its
  count of parts. The facets of one curve are one entry.
- **A split face.** A face that a cut splits is one name with a count of its parts. K4c decides how a reference names
  one part.

A probe on 2026-11-05 measured the B-Rep of a cylinder of 64 facets with a hole: about 3 s. That is too slow for a
query, so this record also makes the topology faster.

## Decision

1. **The reference.** `Engine.Core` references `Engine.Kernel`. No other project of the engine does. This amends
   ADR-0057 item 1. The dependency gate holds the rule with a fact.
2. **The projection.** The kernel solid of a body is a projection of the steps in effect, as the regeneration reads
   them (ADR-0055). The walk goes from the body to the command that creates it:
   - `CreateBox` gives a box of the kernel with its sizes.
   - `Extrude` gives the profile of its sketch in effect, on the frame of the datum of the sketch, by the distance in
     effect. The distance keeps its sign, so `start` is on the plane of the sketch (ADR-0051 item 1).
     `SketchProfile.FindWithItems` gives the item of each side: the line, the arc or the circle.
   - `Translate` moves the solid of its body.
   - `Subtract` subtracts the solid of the subtrahend from the solid of the minuend.
   The kernel reads each input double exactly. The projection does not enter the log, and Manifold still makes each
   solid that the Document holds.
3. **The refusals.** `GetFaces` of a body that is not live gives `E-GEOM-BODY-NOT-FOUND`. A body with a fault keeps
   the solid of an earlier state, which the steps in effect no longer give, so it gives `E-GEOM-BODY-FAULTED`. A body
   that the kernel cannot build from the steps in effect gives `E-GEOM-FACES-FAILED` with the reason.
4. **The query.** `GetFaces(bodyId)` gives one entry for each tag of the B-Rep, in the order of its first face:
   - the reference, as `CreateDatum` takes it: the feature, the element and the item;
   - the count of the parts: the faces of the tag, joined by each edge between two of them;
   - the count of the holes;
   - the area, from the exact sum of the cross products of each loop, with one root;
   - a point inside the face: the mean of the vertices of its first convex piece;
   - the unit normal, or null when the faces of the tag have more than one plane (the facets of a curve).
   A face of the subtrahend of a `Subtract` keeps the name of the face of the subtrahend.
5. **The cache and the speed.** The handler keeps the result of the last 64 geometry keys. A rebuild gives a body a new
   key (ADR-0037 item 2), so a key names one solid. In the topology, the test of a vertex on an edge first compares the
   rounded values with the rounded box of the edge. Rounding to the nearest keeps the order, so the filter refuses
   only a vertex that the exact test also refuses. Each pass of the removal of a straight vertex removes each straight
   vertex that it finds: a removal does not change the direction of a neighbour.
6. **Determinism.** The projection and the topology use exact rationals, and the double values of an entry come from
   one rounding and `sqrt`. A query does not enter the log, so a replay does not read it.
7. **Out of this record.** A datum or a reference by the identity of a face, the choice of one part of a split face,
   and a fault for a face that is gone (K4c). The desktop does not call `GetFaces` yet.

## Consequences

- An agent can ask for the faces of a body with their provenance, including the walls of a hole that a cut makes.
- `E-GEOM-FACES-FAILED` has no test through the commands: each command that breaks a profile also gives the body a
  fault, so the commands reach `E-GEOM-BODY-FAULTED` first.
- The subtract of the kernel is still slow on curves: about 0.2 s for one cylinder of 64 facets, and about 2 s for four.
  A filter on the side of a point can follow.

## History

- 2026-11-05 · TASK-0184 · Accepted with the four answers of the owner of 2026-11-05.
- 2026-11-05 · TASK-0185 · ADR-0059 items 1 and 2 amend item 2: the projection also reads the state of an edit, and
  it keeps the solid of each committed key.
