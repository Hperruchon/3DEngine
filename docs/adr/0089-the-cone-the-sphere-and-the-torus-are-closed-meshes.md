---
id: 0089
title: The cone, the sphere and the torus are closed meshes that the engine makes
status: Accepted
topic: Geometry, kernel
date: 2026-11-07
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Contracts/Geometry/ISolidMeshOps.cs
  - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
  - Engine.Geometry.Manifold/Native/ManifoldNative.cs
  - Engine.Core/Commands/CreateConeCommand.cs
  - Engine.Core/Commands/CreateConeCommandHandler.cs
  - Engine.Core/Commands/CreateSphereCommand.cs
  - Engine.Core/Commands/CreateSphereCommandHandler.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - Engine.Core/Features/FeatureList.cs
  - Engine.Core/Features/KernelProjection.cs
  - Engine.Core/Features/Regeneration.cs
  - Engine.Core/Sketch/DatumResolver.cs
  - Engine.Kernel/KernelSolid.cs
  - Engine.Kernel/Bsp.cs
  - 3DEngine.App/BodyActions.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.App/Ui/PanelAction.cs
  - Engine.Core/Determinism/CrossPlatformProbe.cs
enforced-by: Engine.Tests/Commands/CreateConeCommandTests.cs
---

# ADR-0089 — The cone, the sphere and the torus are closed meshes that the engine makes

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

The owner accepted this record on 2026-11-08 with A to Q1, Q2 and Q3 (the form of decisions in the chat). It changes
`Engine.Contracts`: one new capability, `ISolidMeshOps`.

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

The facets of A give a volume less than the true volume:

- about 0.16 % for the cone, as for the cylinder;
- about 0.40 % for the sphere: the ratio is sin(π/32)/(π/32) × (1 + cos(π/32))/2 for 64 longitudes and 32 bands (the
  review of Codex of v0.268, TASK-0285; the first form of this record gave 0.32 %);
- about 0.8 % for the torus, with a tube of 32 and an axis of 64.

A later slice with a true surface needs the true surfaces of the kernel (the question Q1 B of ADR-0076). This record
proposes no change of that.

## Decision

The owner answered A to Q1, Q2 and Q3 (2026-11-08).

1. **The capability (Q1 A).** `ISolidMeshOps.CreateSolid` stores a closed, oriented triangle mesh as a solid: three
   doubles for each vertex and three indices for each triangle, counter-clockwise from outside. The Manifold backend makes
   the solid with `manifold_meshgl64` and `manifold_of_meshgl64`, and a status that is not zero or an empty solid is a
   fault. A backend with no such capability gives `E-GEOM-CAP-MISSING`. No new diagnostic code, and no new flag of
   `BackendCapabilities` (as `IMeshImportOps`).
2. **The cone (Q2 A, Q3 A; phase K5c).** `CreateCone` has the fields `radius` and `height` and the output `bodyId`. The
   base is the 64-gon of ADR-0040 item 2 at z = -height / 2, the apex is at z = +height / 2, and the base has a centre
   vertex. The record holds the inputs only (ADR-0007).
3. **The names (Q3 A).** The owned kernel gives the cone the faces `-z` and `side`, with no item; the side has no
   normal and no plane, so a datum takes `-z` only, with the frame of the bottom of a cylinder of the same height. A cone
   gives no edge and no vertex to a sketch in this slice.
4. **The exception to the rule of a new command.** As ADR-0076 item 6, a new primitive is a new creator of a body, so it
   also changes `FeatureList`, `Regeneration`, `KernelProjection`, `DatumResolver` and `KernelSolid`.
5. **Next (phases K5d and K5e).** The sphere (31 rings of the latitudes k × 2π/64 for k from -15 to 15, and two poles) and
   the torus (64 positions around the axis and 32 around the tube), each with the face `surface`; then the three entries
   of the list "New" (ADR-0076 item 7). The determinism probe of ADR-0074 takes a primitive of this record in a later slice,
   with the new hash in the test sheet of the phone.
6. **The exact kernel for a mesh of many triangles (phase K5d; Q1 of the codebase review of 2026-11-08, A under the
   standing order).** A cut by a sphere of 3968 triangles took about 3 minutes, and the recursive tree overflowed the
   stack. The tree walks with stacks of its own. The side of a vertex comes from a filter of doubles, and from the exact
   sign of `Rational` only when the doubles are too near zero; that sign sums the fractions with no reduction. Each side,
   each tree and each result stay the same, so the facets of item 5 stay.

## Consequences

**Good:** the three primitives of K5 with no new number type, and each of them is a solid that the person can cut
and move.

**Bad:** a new capability in `Engine.Contracts`, which each backend must give or refuse. The volume of each
primitive is less than the true volume.

## History

- 2026-11-07 · TASK-0283 · The record, Proposed, with the research for the owner (Q2 of the codebase review of 2026-11-07
  (c)).
- 2026-11-07 · TASK-0285 · Q4: the volume of the sphere is about 0.40 % less than the true volume, not 0.32 % (the review
  of Codex of v0.268).
- 2026-11-08 · TASK-0288 · Accepted by the owner (A to Q1, Q2 and Q3), with items 1 to 5 and the cone (phase K5c).
- 2026-11-08 · TASK-0288 · The cone shipped at v0.273.
- 2026-11-08 · TASK-0295 · Item 1: the backend refuses a mesh whose volume is not positive, the volume of a mesh with
  triangles that are clockwise from outside (finding E186 of the codebase review of 2026-11-08).
- 2026-11-08 · TASK-0289 · Item 6: the exact kernel for a mesh of many triangles (Q1 of the codebase review of
  2026-11-08, A under the standing order). The sphere and the torus shipped at v0.282.
- 2026-11-08 · TASK-0298 · Item 5: the entries of the list "New" (phase K5e); the affects field names the four files of
  the panel that cite this record.
- 2026-11-08 · TASK-0298 · The entries of the list "New" shipped at v0.283.
- 2026-11-08 · TASK-0299 · Item 5: the determinism probe of ADR-0074 holds the three primitives (phase K5f); the affects
  field names the probe.
- 2026-11-08 · TASK-0302 · Item 1: the backend refuses a volume below zero only; a volume of zero or NaN comes from a size
  that underflows or overflows the doubles (finding E194 of the codebase review of 2026-11-08 (b)).
