---
id: 0184
title: The section view of the window, a cut of each body by a plane with a cap
status: Accepted
topic: Clients, rendering
date: 2026-11-21
supersedes: []
superseded-by: []
amends: ['0174']
amended-by: []
affects:
  - 3DEngine.App/SectionMesh.cs
  - 3DEngine.App/SectionView.cs
  - 3DEngine.App/SceneRenderer.cs
  - 3DEngine.App/SessionBodies.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Section.cs
  - 3DEngine.App/Ui/DesktopUi.Part.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.Orbit.cs
  - 3DEngine.App/Ui/DesktopUi.Pad.cs
  - 3DEngine.ImageTests/SectionViewTests.cs
enforced-by: 3DEngine.ImageTests/SectionViewTests.cs
notes: R26. ADR-0157 item 15 and card N7 give the row; this record gives the choices that the row leaves open, by the standing order of the owner of 2026-11-20.
---

# ADR-0184 — The section view of the window, a cut of each body by a plane with a cap

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R26 of the roadmap: "The section view: a cut plane on an origin plane or on a datum, a pad for its offset, and a cap on the
cut faces; presentation only (kernel boundary: section by a plane). After K10." ADR-0157 item 15 puts it in the order, and card
N7 of `docs/proposals/2026-11-15-assembly-and-agent-design.md` says why: to see a fit, a hole and the inside of a housing, and to
read what a check reports. The card names "a button Section" and no engine change.

Three constraints remove the obvious alternatives:

- **Presentation only.** ADR-0007 keeps a state of the client out of the Document and the log. A query of the engine that gives a
  cut mesh is a new shape of `Engine.Contracts`, which the row does not ask for.
- **The cap is a real face.** A clip plane of the GPU hides the part in front of the plane and leaves each body open: the person
  looks into an empty shell. A cap needs the region of the plane inside each body.
- **The owned kernel cuts a solid by a plane already.** P0.178 (TASK-0445) gave `PlanarBoolean.Cut(solid, point, normal, cap)`
  (ADR-0057 item 10) for the section of a sheet, with convex cap pieces that close the kept part. The window holds the
  tessellation of each body (`SessionBodies`), and `KernelSolid.Mesh` makes a solid of the kernel from triangles.

The window has a ring of the part with no selection (ADR-0174 item 1), with segments that change a state of the client
("Dimensions" of ADR-0122, "Show hidden" of ADR-0178). The row leaves open where the section starts, the planes, the side that
stays, the pad, how the cut is made, the look of the cap, the bodies that the plane misses, the mesh bodies and the end.

## Decision

1. **Where it starts (amends ADR-0174 item 1).** The ring of the part has "Section" after "Bodies" while the part has a live
   body. A tap starts a section. While a section lasts, the ring of the part holds four tools: "Section" in the colour of a tool
   in use, "Plane" with the name of the plane on its segment, "Offset" with the offset and the unit on its segment, and "Flip".
   The section stays while a body is selected; the ring of the body shows then, and the ring of the section comes back with no
   selection.
2. **The planes.** "Plane" goes through the origin planes XY, XZ and YZ, then each datum of the Document with a name and no
   fault, in the order of the Document, and back to XY. A new section is on XZ, the plane of the front view. The direction of an
   origin plane is the side of the eye of the start view (yaw 135 degrees, pitch 30 degrees): +z for XY, +y for XZ, +x for YZ.
   The direction of a datum is its normal. An origin plane starts at the middle of the box of the visible bodies along its
   direction, rounded to a whole unit; a datum starts at its own place, an offset of 0. The host reads the datums off the thread
   of the frame after each change of the Document while a section lasts. TASK-0449 (finding V232 of the third codebase review
   of 2026-11-21): a plane of a datum keeps the identity of the datum. When the host gives the datums again, a section on a
   datum takes the plane of the same datum, so a move, a rename or an undo of the datum moves the section, which keeps its
   offset and its flip. A section whose datum is gone goes to XZ, as a new section, and keeps its flip.
3. **The offset pad.** "Offset" opens a value pad as the others (ADR-0032), titled "Offset", with one field "offset" in the unit
   of the Document. An apply sets the offset and closes the pad. Escape in the pad closes the pad and keeps the offset. The pad
   gives no action to the host.
4. **The side that goes.** With no flip the side of the direction goes, so the caps of a new section face the start eye. "Flip"
   turns the side; it has the colour of a tool in use while the side is flipped.
5. **The cut and the cap.** The renderer cuts each live body with a mesh by the owned kernel: the solid of its tessellation
   (`KernelSolid.Mesh`, triangles of zero area left out), cut by `PlanarBoolean.Cut` with a cap tag of the window. The cut runs on
   the thread pool, off the thread of the frame; a later frame puts the kept part and the cap on the GPU around the centre of the
   body, so the matrix of its entity draws them. A body keeps its last cut on the GPU until its new cut is complete. The cap is
   opaque with the shading of a body, and its hatch is lines at 45 degrees to the two axes of the plane, in the accent colour of
   the theme (the colour of a tool in use), 1/40 of the diagonal of the box of the visible bodies apart. The edge lines of the
   kept part show the outline of the cut. No shader changes: the hatch goes through the pipeline of the edge lines.
   TASK-0449 (findings V229, V230 and V231 of the third codebase review of 2026-11-21):
   - The task of the cut also narrows the positions, finds the edge lines of the kept part and makes the hatch of the cap, so
     the thread of the frame only puts the meshes on the GPU. A cut gives each kept triangle its own three vertices, so the
     search reads more positions than the mesh had; ADR-0121 puts such a search off the thread of the frame.
   - A body runs one cut at a time. A new plane or a new mesh while its cut runs waits until that cut ends; then the newest
     plane and mesh start. A cut that ends for an old plane or an old mesh never reaches the GPU. So quick taps on "Flip" run
     at most two cuts of each body, and not one for each tap.
   - A new mesh of a body takes its last cut off the GPU at once, because that cut is of the old mesh: the body shows whole
     until the cut of its new mesh. A new plane keeps the last cut until the new cut is complete.
   - The section cuts the visible bodies only. A hidden body is not drawn, so its cut costs time and memory of the GPU with no
     effect.
6. **The bodies that the plane misses.** A body with no vertex on the side that goes shows whole, with no cut on the GPU. A body
   with no vertex on the side that stays does not show. The side of each vertex is exact.
7. **Mesh bodies.** A mesh body of an STL import (ADR-0083) can be open, so a cap has no sure boundary. Its triangles are clipped
   to the side that stays, in doubles, and it gets no cap.
8. **The end.** Escape ends the section at the level of the document: with no selected body and no active sketch. With a
   selection, Escape goes up to the document first, so the next Escape ends the section. A second tap on "Section" ends it. The
   section ends with no live body and on the home screen.
9. **What the section changes.** The drawing of the scene only. A pick, the dimensions, the box that the camera frames, the
   lines of a flat view and the sheet use the whole bodies.
10. **No contract change.** No command, query, event, field of the Document or diagnostic code. A tool of the section gives no
    action, so nothing goes to the engine. The client may use `Math.Sqrt` for the hatch; the cut of a solid is exact.
11. **The test.** `SectionViewTests` holds items 1 to 8: the state of the window, the cut of a box, a plate with a hole and the
    five panels of the bookcase of K10a, a mesh body, the hatch, and the pictures of the bookcase. The layout test of ADR-0172
    item 5 gets the state Section. TASK-0449 (finding T189): `SectionViewTests` holds the cuts with the hook `RunCut` for item
    5, and the layout test gets the state `SixSegments`, the ring of the part with New, Sketch, Bodies, Section, Images and
    Show hidden. At 640 by 360 pixels the first segment of that ring goes under the windows of the top left (register entry
    R-0076).

## Consequences

**Good:** a person sees the inside of a part, a hole and a fit with two taps, on the desktop and on the phone, with no change of
the engine. The cap is the face that the section of a sheet has, from one cut of the kernel.

**Bad:** a new offset or plane cuts each body again. The cut of a curved body of some thousands of triangles takes a part of a
second (the solid of a sphere of 3,968 triangles took 0.13 s to build at v0.430), so its cut shows some frames after the tap;
the old cut stays until then. A pick goes through the part that the section hides. The hatch has one spacing for the part, so a
thin panel of a large model shows few lines. A mesh body shows open. "Flip" and the order of "Plane" are new places for a person
to learn. The ring of the part has one segment more, so a portrait ring with "Images" and "Show hidden" has six narrow segments.

**Next:** none. A drag of the plane, a pick that stops at the plane and a second plane are not in row R26.

## Alternatives considered

- **Where it starts.** A cell "Section" in the net of the views: the net closes after a choice of a view, and the section needs
  three more tools while it lasts. A command of the command line only: the card names a button. The ring of the part holds the
  tools of the part and the tools that change a state of the window (ADR-0174, ADR-0178), so it is the smallest place.
- **One segment for each plane.** Three origin planes and each named datum on the ring: the count has no bound, and a portrait
  ring has room for six segments. "Plane" goes through them with one segment.
- **A clip plane of the GPU and a cap mesh from the kernel.** The clip needs a change of `mesh.vert` and of each pipeline, the
  feature `shaderClipDistance` on each device, and a new compile of the locked shaders on a computer with the Vulkan SDK. The cap
  still needs the cut of the kernel, which gives the kept part too. One path is smaller.
- **The cut in the engine, by a query.** A change of `Engine.Contracts` and of the HTTP surface for a state of the client, against
  ADR-0007.
- **The cut on the thread of the frame.** The bodies of plane faces cut in milliseconds, but a sphere takes a part of a second,
  and the frame stops for that time.
- **The side away from the eye of each frame.** The side would change while the view turns. A fixed side and "Flip" are clear.
- **The start plane XY.** A horizontal cut of a part that stands on z = 0 shows its plan, but the front view comes first in a
  drawing, and XZ cuts each body of the bookcase.
- **A cap in its own colour.** A new fragment shader or a colour of each draw changes the locked shaders. The hatch in the accent
  marks the cap with the pipelines that exist.
- **A spacing of the hatch in millimetres.** ISO 128-50 gives a spacing on paper; the 3D view has no scale, so a part of 3 m and a
  part of 30 mm need the spacing of their size. The first form, 1/120 of the diagonal, filled the caps of the bookcase.
- **Bodies that the plane misses.** Hide each body that the plane does not cut: the person loses the bodies behind the plane.
- **Mesh bodies.** Refuse the section while a mesh body exists, or show a mesh body whole: the person sees a part cut and a part
  whole with no reason. A cap of a mesh body needs a check that it is closed, which the STL import does not make.
- **The end.** A tool "Back" on the ring of the section: "Section" in use already ends it, as "Dimensions" does.

## History

- 2026-11-21 · TASK-0447 · The record, from row R26 and card N7, under the standing order of the owner of 2026-11-20.
- 2026-11-21 · TASK-0449 · Items 2, 5 and 11: a section follows its datum by its identity (V232), the search of the edge lines
  and the hatch in the task of the cut (V229), one cut at a time for each body (V230), the visible bodies only (V231), and the
  tests of item 5 and of the ring of six segments (T189).
