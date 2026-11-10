---
id: 0118
title: The 2D drawing of a part has a date, and its work starts with a flat view
status: Accepted
topic: Charter, drawing
date: 2026-11-10
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - docs/CHARTER.md
enforced-by: Engine.Tests/Governance/ObjectiveReferenceGateTests.cs
---

# ADR-0118 — The 2D drawing of a part has a date, and its work starts with a flat view

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Objective 18 of the charter kept 2D drawing, technical drawing and electrical schematics as directions with no date, and
the list of the non-goals named them. On 2026-11-10 the owner asked if the app has a 2D drawing export for persons who
need a drawing or documentation. It has none: the exports are STL and STEP (`Engine.Core/Export/`).

The owner then asked for renders of a drawing of the Bracket and of the right dimensions on it, and for a comparison with
the app (`https://claude.ai/artifact/Vt9NaHWhekzbHk6nV2HaAn`). The comparison found:

- The log holds the values of the design: the constraints of a sketch and the distance of an extrude.
- The camera has a perspective projection only (`3DEngine.Core/Models/Camera.cs`).
- R18b draws the feature edges and the hidden edges of the selected body, on the GPU, as presentation (ADR-0109).
- The owned kernel holds each face as a polygon of exact rationals, and a curved side as facets that know their arc or
  circle (ADR-0057 item 4).
- The sketch solver gives a residual for each constraint and does not find a part that no constraint fixes.
- The overlay of ADR-0056 draws three dimensions of a box, as presentation only.
- No sheet, no view of a sheet and no export of a drawing exist.

On 2026-11-10 the owner answered the five questions of the page with the recommended options: Q1 A, Q2 A, Q3 A, Q4 in the
recommended order, and Q5 A.

## Decision

1. **The date (Q1 A).** Objective 18 changes: 2D drawing and technical drawing of a part have a place in the roadmap,
   after R18. Electrical schematics stay a direction with no date. The list of the non-goals keeps electrical schematics
   only.
2. **The order (Q5 A, Q4).** The work starts with a flat view in the 3D window, and each later phase uses the one before
   it:
   - R21: the flat views in the window;
   - K9: the edges of a view, from the owned kernel;
   - R22: the right dimensions on a view;
   - P10: the sheet in the Document;
   - P10a, P10b and P10c: the export of a sheet as SVG, then DXF, then PDF.
3. **The sheet is in the Document (Q2 A).** A sheet, its views, the scale of each view and the place of each dimension
   are decisions of a person, so they are commands in the log, and a replay gives the same sheet. The lines of a view are
   computed from the bodies and are not stored, as a tessellation is not (determinism rule 6). The direction of a view is
   a matrix and not an angle (determinism rule 2). P10 changes the public shape of `Engine.Contracts`, so it needs its own
   ADR and a question to the owner before its code.
4. **The projection (Q3 A).** A sheet uses ISO first angle by default: the top view goes under the front view, and the view
   from the left goes on the right. Third angle is a choice for each sheet.
5. **The right dimension.** A dimension on a view is a value of the design, not a measure of the picture:
   - each value shows one time, in a view where its length is true;
   - a chain of n parts gets n − 1 values of the design and one reference in parentheses;
   - a size or a place that no constraint fixes shows in a separate form, so the drawing does not hide a gap in the
     design;
   - a value whose constraint left the design shows as lost;
   - the agent proposes the views and the place of each dimension, and the person accepts or rejects them (ADR-0092).
6. **The kernel boundary.** A view of the edges with the hidden edges is not one of the six operations of the closed
   feature boundary of the charter. K9 needs an ADR that adds it, or that computes it outside the kernel from the faces
   that the kernel gives.
7. **Out of this record.** Electrical schematics, a drawing of an assembly, a hatch of a section, a tolerance, and a
   symbol of a surface finish.

## Consequences

- The scope test of the charter now passes a request for a flat view or for a drawing of a part: the roadmap holds a
  track row for each phase.
- No code changes in this record. Each phase opens its task, and its ADR when it needs one, at its start.
- The UX test of 2026-11-10 found that the niche of a unit does not come in front of the eye in one step (problem 6 of
  `docs/proposals/2026-11-10-ux-test-of-a-multi-part-project.md`). R21 answers it.

## Alternatives considered

- **Keep objective 18 (Q1 B).** A person who needs a drawing then exports STEP to another program. The owner chose A.
- **A sheet as presentation that the client saves apart (Q2 B).** It needs no contract change, but an agent cannot
  propose a sheet with commands, and a paper copy cannot point to one entry of the log. The owner chose A.

## History

- 2026-11-10 · TASK-0343 · The record, from the answers of the owner of 2026-11-10 to Q1 to Q5 of the renders of a drawing.
- 2026-11-10 · TASK-0343 · Shipped at v0.326.
