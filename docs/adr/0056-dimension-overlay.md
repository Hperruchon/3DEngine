---
id: 0056
title: The dimension overlay of the desktop
status: Accepted
topic: Desktop, renderer
date: 2026-11-04
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - 3DEngine.Core/Measuring/**
  - 3DEngine/DimensionOverlay.cs
enforced-by: 3DEngine.Tests/DimensionsTests.cs (TASK-0178 creates it)
---

# ADR-0056 — The dimension overlay of the desktop

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner used the desktop and an agent to make an L-shaped shelf for an entry hall, a set of boards of boxes. The
owner needs to read the dimensions on the model. The request: a toggle "Show dimensions", dimension lines with units
for the overall width, height and depth, and the dimensions of a selected board, with its thickness. On 2026-11-03 the
owner added the phase R13 to the plan, after P0.75.

Before this record, no roadmap row, ADR, task or register entry named a dimension. The desktop shows the bodies, the
sketches and their labels, and the selection of one body (ADR-0029). The query `GetBodyInfo` gives the distance in
effect of an extrude (ADR-0049), and the desktop holds the box of each mesh that it fetched (`SessionBodies`).

Objective 18 of the charter keeps technical drawing as a direction with no date. A dimension overlay is not a drawing:
it is presentation on the 3D model in the window, and nothing of it enters the document.

On 2026-11-04 the owner answered four questions:

- **Q1, the thickness.** A: for a live extrude, its distance in effect; for each other body, the smallest extent of its
  box.
- **Q2, the bodies of the overall size.** Both as an option: the visible bodies, or each live body.
- **Q3, the display.** A: dimension lines with labels in the scene.
- **Q4, the toggle.** A: a state of the client, off at each start.

## Decision

1. **Presentation only.** The desktop computes each dimension from the boxes of the meshes that it holds and from
   `GetBodyInfo`. No command, no event and no query is new, nothing enters the log or the saved file, and
   `Engine.Contracts` does not change (ADR-0007, ADR-0009).
2. **The toggles (Q4).** The panel has the checkbox "Dimensions" on the line of "Show all", off at each start of the
   desktop, and the checkbox "Hidden too" after it. Each one takes a tap and a click (objective 9). They are a state of
   the client.
3. **The overall size (Q2).** The overall box is the union of the boxes of the visible live bodies. With "Hidden too",
   it is the union of the boxes of each live body that has a mesh. The box that the camera frames does not change: it
   holds the visible bodies only (ADR-0029 item 3).
4. **The lines (Q3).** `Dimensions.Of` in `3DEngine.Core/Measuring/` gives three dimensions of a box, one along each axis
   of the space of the Document. The up axis of the desktop is Y (`Camera.View` looks with `Vector3.UnitY` up, and
   `Camera.Eye` puts the pitch on Y). The dimensions of X and Z lie at the lowest
   Y, on the faces of the box that face the eye; the dimension of Y lies on the vertical edge of the side of the eye in
   X and the far side in Z. Each dimension line is out from its edge by one twentieth of the largest extent, with two
   extension lines from the edge and an arrow at each end. Its label is at its middle.
5. **The labels.** A label gives the axis, the value and the length unit of the document, for example "X 1200 mm". It
   does not say width, height or depth, because a model can use Z or Y as its up axis. A value has at most two
   decimals, in the invariant culture.
6. **The selected body (Q1).** With "Dimensions" on, the selected body also gets its three dimensions, in the colour of
   the selection, and the panel gives its size and its thickness: the absolute value of the distance in effect for a
   live extrude, else the smallest extent of its box. The desktop reads the distance once for each change of the
   selection or of the bodies, not in each frame.
7. **The cost.** The overlay draws six to twelve segments and six labels in each frame, and it reads the boxes that the
   desktop holds. No dimension calls the engine in a frame.
8. **Determinism.** The overlay is outside the log, so the determinism rules of `CLAUDE.md` do not apply to it. It uses
   + - * / and comparisons only.
9. **Out of this record.** A dimension between two picked points, an angle, a dimension of a sketch item, a dimension
   that the document saves, and technical drawing.

## Consequences

- A person reads the overall size and the size and the thickness of a board on the model, by tap and by click.
- An agent gets nothing new: it reads the same values through `GetBoundingBox` and `GetBodyInfo`.
- The box of a body is the box of its mesh, so a curved body gives the box of its tessellation, as the camera frame
  does.

## History

- 2026-11-04 · TASK-0178 · Accepted with the four answers of the owner of 2026-11-04.
