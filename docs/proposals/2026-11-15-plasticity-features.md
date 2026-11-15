# The features of Plasticity and 3DEngine

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-15. Prepared by a side session.
- Answered on 2026-11-15, in the chat: the owner accepts the order of section 6.
- Source for Plasticity: the page `https://www.plasticity.xyz/product`, read on 2026-11-15 by a fetch agent. The page lists
  10 main features, 12 short features, the file formats of each licence, and the platforms. The agent did not read the
  detail pages of `doc.plasticity.xyz`. The page does not describe the hotkeys, the command palette or the radial menus.
- Source for 3DEngine: `origin/main` at v0.375 (f7786c1): the list of `Engine.Core/Commands/`, searches in `3DEngine.App`,
  `3DEngine.Core` and `Engine.Core`, and the roadmap.

## 1 The two products are different

Plasticity is a direct modeller for artists on the Parasolid kernel: free surfaces, fillets, no history, no constraints.
3DEngine is a parametric engine: a log of commands, sketches with a constraint solver, a rebuild, an exact kernel for planes,
cylinders, cones, spheres and tori, and one surface for a person and for an agent. So a feature of Plasticity makes sense only
when it helps a printable mechanical part, an assembly, or a drawing.

## 2 The main features

| # | Plasticity | 3DEngine today | Does it make sense? |
|---|---|---|---|
| 1 | Robust filleting | No. A search for "fillet" and "chamfer" in the code gives no file. | **No fillet: the charter refuses it.** Anti-objective 11 (`docs/CHARTER.md:239`): "No fillet and no chamfer on an edge chain." **A flat chamfer of one edge: yes.** It is one of the six operations of the kernel boundary (`docs/CHARTER.md:161`), and no command gives it yet. |
| 2 | Direct face manipulation (push, pull, offset) | Part. A value on an edge opens a pad and sends `SetExtrudeDistance` or `SetSketchConstraintValue`; a gizmo moves a body with one `Translate`. No drag of any face. | **Yes, as a parametric edit.** A drag of a face changes the value that owns the face (the distance of an extrude, the size of a box). A free edit of the B-Rep is against the log as design truth. Needs "the size of a primitive", a gap that row K10 already names. |
| 3 | xNURBS surface blending | No. | No. Free surfaces are outside the kernel boundary. |
| 4 | PolySplines (mesh to NURBS) | No. A mesh body is context only (ADR-0083). | No. |
| 5 | Export hidden line (SVG drawings) | Half. The engine computes the visible and the hidden lines of a view, exact, with circles, arcs and ellipses (K9a to K9c, `ViewEdges`). The sheet is in the Document (P10.1). The SVG export is row P10a. | **Yes. It is on the roadmap** (P10.2, P10.3, P10a). 3DEngine also adds the dimensions of the design, which Plasticity does not have. |
| 6 | Surface continuity analysis | No. | No. It is for free surfaces. |
| 7 | Section analysis view | No. I found no cut plane in the window. | **Yes, soon.** A cut plane with a cap shows the inside of a housing, a hole and a fit. "Section by a plane" is one of the six operations of the kernel boundary (`docs/CHARTER.md:161-162`). The view is presentation: no contract change. It helps each assembly check. |
| 8 | Pattern and array tools | No. | **Yes.** A row of holes, a grid of feet, the shelves of a furniture, the screws of a flange. A linear and a grid pattern use `+ - *` only. A radial pattern records a quaternion for each copy, as `Rotate` does. |
| 9 | CV editing | No. | No. |
| 10 | Booleans (unite, subtract, intersect) | Part. `Subtract` only. | **Yes.** "Boolean" is in the kernel boundary. A union is a gap that row K10 names (a knob with a collar). An intersection of two bodies is the overlap volume, which the interference query K11 needs: one work, two uses. A revolve is also in the boundary and has no command. |

## 3 The short features

| Plasticity | 3DEngine today | Does it make sense? |
|---|---|---|
| Angle dimensions | Part. A sketch has an `angle` constraint (a cosine and a sine); a proposal shows an angle in degrees (ADR-0114). No measure of an angle between two faces. | Yes, as a measure query (see the assembly design file). |
| Dimensions | Yes. Driving values in a sketch, values on the edges of a body (ADR-0082, ADR-0115), the box dimensions (ADR-0056, ADR-0125), references of a chain and free sizes in a flat view (ADR-0148, ADR-0149). | Have. |
| Construction planes | Yes. A datum on an origin plane or on a face, which follows its body (`CreateDatum`, ADR-0051, ADR-0112). | Have. |
| Precise snapping | Part. In a sketch a tap snaps to a point, an intersection, a curve, then the grid (ADR-0041, `SketchTool.Snap`). No snap of a body to a body in 3D. | Yes for 3D: it is the base of a precise placement in an assembly. |
| Snapping intelligence (guides) | Not verified. | Later; small. |
| Tangent snaps | Part. A `tangent` constraint exists; no snap. | Low. |
| Polygons | No tool. | Low. A client can add the lines and the constraints. |
| Offset curves | No. | Later. Useful for a wall of constant thickness. |
| Bridge curves, curvature combs, sculpt surfaces, surface extension | No. | No. They are for free surfaces. |

## 4 Formats and platforms

| Subject | Plasticity | 3DEngine today | Does it make sense? |
|---|---|---|---|
| Import | OBJ, STL, 3MF, FBX, STEP, images, SVG; more with a higher licence | STL (`ImportStl`), images (`PlaceImage`) | STEP import: yes, but large; it is the door to standard parts (screws). 3MF, OBJ, FBX: no. |
| Export | OBJ, STL; STEP, 3MF, SVG and Parasolid with a paid licence | STL and STEP (`/export/stl`, `/export/step`); SVG, DXF and PDF are rows P10a to P10c | 3MF: yes, after P14 (ADR-0126 item 7). OBJ: no need. |
| Blender bridge | Yes (paid) | No | No. The equivalent for 3DEngine is the HTTP and WebSocket surface for an agent. |
| Platforms | Windows, macOS, Linux | Windows, Linux, macOS, and Android on arm64 (`CLAUDE.md`, determinism rules) | Have, and more. |

## 5 What 3DEngine has that the page of Plasticity does not show

- Sketches with constraints and a solver, and a rebuild after a change of a value.
- A log of commands with undo, redo and a replay that gives the same result on each platform.
- One surface for a person and an agent, and the proposal of an agent that a person accepts or rejects (ADR-0092).
- A drawing sheet in the Document, with dimensions that read the design.

## 6 The order that I recommend

1. Union and intersection (with K10 and K11).
2. The section view (before K11, so a person can see what a check reports).
3. The size of a primitive and the drag of a face as a parametric edit (with P12).
4. Linear and grid patterns, then radial.
5. The flat chamfer of one edge. No fillet (anti-objective 11).
6. The snap of a body to a body in 3D (with the placement work of the assembly design file).
7. STEP import and 3MF export, later.

Section "The next items" of `docs/proposals/2026-11-15-assembly-and-agent-design.md` gives each item in a form to code.

## 7 Not verified

- The detail pages of Plasticity. Each row of sections 2 to 4 uses the one-line text of the product page.
- "No cut plane in the window" and "no snap of a body in 3D": I searched file names and words, and I did not read each file
  of the window.
- The alignment guides of the window.
