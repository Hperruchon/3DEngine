---
id: 0089
title: The cone, the sphere and the torus are closed meshes that the engine makes
status: Proposed
topic: Geometry, kernel
date: 2026-11-07
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects: []
enforced-by: none
---

# ADR-0089 — The cone, the sphere and the torus are closed meshes that the engine makes

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This record is **Proposed**. It needs a change of `Engine.Contracts`, so the standing order of 2026-11-05 does not answer
it (`CLAUDE.md`, "Stop and ask"). It holds the research for the decision of the owner. No code follows it before the owner
accepts it.

## Context

The roadmap row K5 gives "the cylinder, the cone, the sphere and the torus". ADR-0076 made the cylinder: the prism of the
64-gon of ADR-0040 item 2, through `IExtrudeOps`, with no change of `Engine.Contracts`. The order of the owner of
2026-11-06 puts K5 after the redesign slices that need no change of the contract; those slices ended at v0.263.

What exists:

- `IExtrudeOps` makes the prism of one plane loop. A cone, a sphere and a torus are not prisms. A cone is a prism with a
  top of zero size, and `ExtrudeParameters` has no scale of the top.
- `IMeshImportOps` (ADR-0083) holds a mesh body, the triangles of an STL file. A mesh body takes no other operation
  (ADR-0083 item 4), so a primitive made that way could not be cut or moved.
- `IBooleanOps.Subtract` and `ITransformOps.Translate` take a solid of the kernel only.
- The C interface of Manifold 3.5.2 makes a solid from a closed mesh (`manifold_of_meshgl64`), and its `manifold_revolve`
  turns a profile about an axis. The built `manifoldc.dll` of the tests exports both names; the backend calls neither
  today. A revolve needs the sine and the cosine of each step; the source of Manifold is not in this repository, so this
  record did not verify how it computes them.
- ADR-0040 item 2: `SketchProfile.Turn` gives the 64 vertices of a circle with `+ - * /` and `sqrt` only: each step halves
  the angle, and each vertex is the one before it turned by the step. So the vertices have the same bits on each platform.
- ADR-0057: each number of the owned kernel is an exact rational. A double is a rational, so the kernel takes the
  vertices that the engine computes, as for the cylinder (`KernelSolid.Cylinder`).
- ADR-0007: no tessellation goes into the Document. A command records its sizes only.

### Q1. The capability

- **A.** A new capability `ISolidMeshOps` in `Engine.Contracts`, with one method that stores a closed, oriented triangle
  mesh as a solid: the positions (three doubles for each vertex) and the triangles (three indices for each). The engine
  computes the mesh from the sizes of the command, with the table of the 64-gon. The Manifold backend makes the solid with
  `manifold_of_meshgl64`; a backend with no such capability gives `E-GEOM-CAP-MISSING`. The solid then takes each
  operation of a solid: subtract, move, tessellate, export.
- **B.** `manifold_revolve` through a new capability of a revolve. If Manifold computes the steps with a sine and a cosine
  of its platform (not verified here), that is against determinism rule 1 of `CLAUDE.md`, and the hash of a document can
  differ between platforms.
- **C.** No change of the contract: a cone from a prism and 64 subtractions of half-spaces. The sphere and the torus cannot
  come this way, and the 64 booleans are slow.

**Recommended: A.** It keeps rule 1, it gives the three primitives at once, and the solid takes each operation of a solid.

### Q2. The facets

- **A.** The table of the 64-gon for each angle: the cone has the 64-gon at its base and one apex; the sphere has the 31
  rings of the latitudes k × 2π/64 for k from −15 to 15, and the two poles; the torus has 64 positions around its axis and
  32 around its tube. Each vertex comes from `SketchProfile.Turn`.
- **B.** A count of facets from a tolerance of the chord, which the Document does not hold yet.

**Recommended: A.** It is the rule of ADR-0076 Q3 for the cylinder.

### Q3. The commands and the names of the faces

- **A.** `CreateCone` (radius, height; the apex at +z), `CreateSphere` (radius) and `CreateTorus` (the radius of the axis
  and the radius of the tube), each with its centre at the origin, as `CreateCylinder`. The owned kernel gives the cone
  the faces `-z` and `side`, and the sphere and the torus one face `surface`, each with no normal, as the side of the
  cylinder (ADR-0076 item 3). The list "New" of the panel gets three entries, each with its pad (ADR-0076 item 7).
- **B.** A name for each facet.

**Recommended: A.** A facet is not a face of the design, and a later true surface keeps the names of A.

### Q4. The volume

The facets of A give a volume less than the true volume: about 0.16 % for the cone (as for the cylinder), about 0.32 % for
the sphere (two polygons of 64), and about 0.8 % for the torus (a tube of 32 and an axis of 64). A later slice with a true surface needs the true surfaces of the kernel (the
question Q1 B of ADR-0076). This record proposes no change of that.

## Decision

None yet. The owner answers Q1 to Q3. With A to each, the next tasks are: the capability and the Manifold call (with a
test of the hash of a sphere on the four platforms of anti-objective 16), then each command with its kernel solid, then
the list "New".

## Consequences

**Good (with A):** the three primitives of K5 with no new number type, and each of them is a solid that the person can cut
and move.

**Bad (with A):** a new capability in `Engine.Contracts`, which each backend must give or refuse. The volume of each
primitive is less than the true volume.

## History

- 2026-11-07 · TASK-0283 · The record, Proposed, with the research for the owner (Q2 of the codebase review of 2026-11-07
  (c)).
