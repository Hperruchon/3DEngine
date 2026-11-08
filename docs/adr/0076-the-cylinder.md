---
id: 0076
title: The cylinder is a primitive of plane facets
status: Accepted
topic: Geometry, kernel
date: 2026-11-06
supersedes: []
superseded-by: []
amends: ['0032']
amended-by: []
affects:
  - Engine.Core/Commands/CreateCylinderCommand.cs
  - Engine.Core/Commands/CreateCylinderCommandHandler.cs
  - Engine.Kernel/KernelSolid.cs
  - Engine.Core/Features/KernelProjection.cs
  - Engine.Core/Features/FeatureList.cs
  - Engine.Core/Features/Regeneration.cs
  - Engine.Core/Sketch/DatumResolver.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.App/BodyActions.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/PanelAction.cs
enforced-by: Engine.Tests/Commands/CreateCylinderCommandTests.cs
---

# ADR-0076 — The cylinder is a primitive of plane facets

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row K5 gives "the cylinder, the cone, the sphere and the torus", 55 to 85 evenings. The fallback queue of
the owner (2026-11-05) gives its first slice, the cylinder, with the recommended answers.

What exists:

- ADR-0040 item 2: an extrude of a circle gives a prism of 64 plane facets. The engine computes each vertex with
  `+ - * / sqrt` only, so the polygon has the same bits on each platform (item 3).
- ADR-0057 item 2: each number of the owned kernel is an exact rational, and each predicate is exact. A point on a true
  circle is not rational in general, so a curved surface in the kernel needs a type of algebraic number.
- ADR-0058: the query `GetFaces` gives one entry for each face name. The 64 facets of a circle have one name, so the
  side of an extrude of a circle is one face with no normal.
- `CreateBox` is the only primitive. The cone, the sphere and the torus cannot come from an extrude.
- ADR-0007: no tessellation goes into the Document.

The owner ordered the fallback queue under the standing order of 2026-11-05, which answers each question with the
recommended option. This record is Accepted under that order; the owner checks it on 2026-11-08.

### Q1. The surface in the kernel

- **A.** The kernel keeps plane facets: the side of a cylinder is 64 facets with one name. The command records the
  radius and the height exactly, so a later slice can give the true surface from the log.
- **B.** A true curved face in the kernel, with a type of algebraic number for the intersections of a plane and a
  cylinder.
- **C.** Doubles for a curved face.

A keeps each exact predicate of ADR-0057 and needs no new number type. B is the main cost of K5, many evenings, and
no client needs it yet. C breaks the exact predicates and anti-objective 16. **Recommended: A.**

### Q2. The command

- **A.** A new command `CreateCylinder` with a radius and a height, as `CreateBox`.
- **B.** No command: the extrude of a circle on a datum is the cylinder.

A gives the first of the four primitives, and the cone, the sphere and the torus need commands anyway. B needs a datum, a
sketch, a circle, a constraint and an extrude for one cylinder. **Recommended: A.**

### Q3. The count of facets

- **A.** 64, the circle of ADR-0040 item 2.
- **B.** A count from a chord tolerance.

A gives one polygon for a circle of a sketch and for a cylinder, and its bits are already tested on each runner. B
needs a tolerance setting that the Document does not hold yet. **Recommended: A.**

### Q4. The STEP export

- **A.** The facets as plane faces, as for each solid of the kernel today (ADR-0070).
- **B.** A `CYLINDRICAL_SURFACE` for the side.

B needs the true surface of Q1 B. **Recommended: A.**

### Q5. The button of the panel (K5b, TASK-0244)

The first line of the panel holds Undo, Redo and "New box" and has no room for a second button of 136 pixels. The
panel has no room for a new line at 1280 by 720: with a fault line and the controls of a body, Show all goes 16 pixels
below the panel (finding V50).

- **A.** A list "New" takes the place of the button "New box". Its entries are "Box" and "Cylinder", each at least 44
  pixels high, and each opens its pad. A box needs two taps.
- **B.** A button "Cylinder" on a new line, and a panel that scrolls.
- **C.** A shorter label of each button on the first line.

A keeps the height of the panel, and the cone, the sphere and the torus become more entries of the same list. B moves
Show all out of view on a small screen. C does not give enough room: the label "Cylinder" alone needs 136 pixels.
**Recommended: A.**

## Decision

1. **The command (Q2 A).** `CreateCylinder` has the fields `radius` and `height`, two lengths greater than zero, and
   the output `bodyId`. The body handle comes from the command identifier (ADR-0012 item 4). The cylinder has its centre
   at the origin and its axis along +z, as `CreateBox` has its centre at the origin; a `Translate` moves it. The record
   is the input only: no vertex and no facet goes into the log (ADR-0007).
2. **The solid (Q1 A, Q3 A).** The circle is the circle of ADR-0040 item 2, with the centre (0, 0) and the first vertex
   (radius, 0): 64 vertices, `+ - * / sqrt` only. The handler gives that loop to `IExtrudeOps`, from z = −height / 2 to
   z = +height / 2. `IExtrudeOps` and `ExtrudeParameters` do not change, so `Engine.Contracts` does not change. A
   backend with no `IExtrudeOps` gives `E-GEOM-CAP-MISSING`; a radius or a height that is not greater than zero, or not
   finite, gives `E-GEOM-INVALID-PARAM`. No new diagnostic code.
3. **The names (ADR-0058).** The owned kernel builds the same prism with three face names: `+z`, the top; `-z`, the
   bottom; and `side`, the 64 facets, with no item. `GetFaces` gives three entries; the entry `side` has no normal.
4. **The references (ADR-0051).** A datum on `+z` or `-z` has the frame of the face of a box of the same height. The
   element `side` has no plane, so a datum on it gives the reason "a curved face has no plane". A cylinder gives no edge
   and no vertex to `AddSketchReference` in this slice.
5. **The export (Q4 A).** The STEP export writes the facets as plane faces, and the STL export writes the triangles of
   Manifold, as for an extrude of a circle.
6. **The exception to the rule of a new command.** `CLAUDE.md` says that a new command changes no file outside
   `Engine.Core/Commands/` and `Engine.Core/Hosting/HandlerCatalog.cs` (ADR-0016). A new primitive is a new creator of
   a body, so it also changes the creators of `FeatureList`, `Regeneration`, `KernelProjection` and `DatumResolver`, and
   adds one solid to `KernelSolid`. ADR-0028 item 9 gives the same kind of exception. No host names the command, and
   `DispatchSurfaceGateTests` does not change.
7. **The panel (Q5 A, K5b).** A list "New" takes the place of the button "New box" of ADR-0032 item 4, with the box of
   the list "Cut with" (ADR-0073 item 1). Its entry "Box" opens the pad of ADR-0032 item 4 with the title "New box",
   and its entry "Cylinder" opens a pad with the title "New cylinder" and the fields `radius` (5) and `height` (10), in
   the length unit of the document; its apply sends `CreateCylinder`. The desktop and the phone draw the same panel
   (ADR-0072). This item amends ADR-0032 item 4. The entries Cone (radius 5, height 10), Sphere (radius 5) and Torus
   (radius 10, tube radius 3) follow, with pads of the same form (ADR-0089 item 5, phase K5e, TASK-0298).

## Consequences

**Good:** the first primitive after the box, with no change of `Engine.Contracts` and no new number type. The log holds
the radius and the height exactly, so the true surface of a later slice needs no migration of a saved document.

**Bad:** the side is 64 facets. A cut of a cylinder and a box gives facets, and the volume differs from π r² h by about
0.16 %. Each later primitive changes the same four creator files.

**Next:** the button of the desktop and of the phone, with a pad of two values; the cone, the sphere and the torus;
the true surface and `CYLINDRICAL_SURFACE` after Q1 B.

## History

- 2026-11-06 · TASK-0238 · The record, Accepted under the standing order, with the command, the names and the
  references.
- 2026-11-06 · TASK-0238 · Shipped at v0.223.
- 2026-11-06 · TASK-0244 · Q5 and item 7: the list "New" of the panel of the desktop and of the phone, with a pad of
  the radius and the height (section "Next"), Accepted under the standing order of 2026-11-05; it amends ADR-0032
  item 4.
- 2026-11-06 · TASK-0251 · The affects field names the four files of the list "New" that TASK-0244 changed for item 7
  (finding T164 of the codebase review of 2026-11-06 (e)).
- 2026-11-08 · TASK-0298 · Item 7: the entries Cone, Sphere and Torus (ADR-0089 item 5, phase K5e).
