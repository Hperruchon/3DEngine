---
id: 0178
title: "Show hidden" is on the ring of the part, and the count of the hidden bodies goes on the path line where the status line has no room
status: Accepted
topic: Clients, rendering
date: 2026-11-20
supersedes: []
superseded-by: []
amends: ['0124']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.Orbit.cs
  - 3DEngine.App/Ui/DesktopUi.Part.cs
  - 3DEngine.ImageTests/ShowHiddenTests.cs
enforced-by: 3DEngine.ImageTests/ShowHiddenTests.cs
---

# ADR-0178 — "Show hidden" is on the ring of the part, and the count of the hidden bodies goes on the path line where the status line has no room

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0064 (2026-11-12) held two questions of improvement B of the UX test of 2026-11-10. First, the place of a ring
of the document with "Show hidden", because no ring showed with nothing selected. Second, the place of the part "N hidden ·
Show" of ADR-0124 item 2 on a portrait phone: at 390 pixels the status line has 38 pixels of room right of Home, so the part
does not show, and the way back is the panel.

On 2026-11-20 the owner chose "On the path line" from four options: the path line, the top bar, the centre of the scene, or
the panel only. The question said "a tap on the path line with nothing selected opens the ring of the document; the phone uses
the same place".

The question did not say that R31a (ADR-0174 item 1, v0.401) already gives a ring with nothing selected: the ring of the part,
with "New", "Sketch", "Bodies" and "Images". A second ring for the same state would put two rings on one display. ADR-0174 item
4 also gives a tap on the file name of the path line to the file menu.

## Decision

1. **"Show hidden" on the ring of the part (amends ADR-0124 item 4).** The ring of the part of ADR-0174 item 1 is the ring of
   the document. While at least one live body is hidden, it has one more segment, "Show hidden", which sends the commands of
   the button "Show all" of ADR-0124 item 3: one SetBodyVisibility for each hidden body, so each body is one step of undo (Q1 A
   of the owner, 2026-11-20). With no hidden body the segment does not show.
2. **The count on the path line (amends ADR-0124 item 2).** Where the status line has no room for the part "N hidden · Show"
   (a portrait phone of 390 pixels), the part shows on the path line, right of the file name, with the same tap as in the
   status line (the list of ADR-0124 item 3). Where the status line has room, the part stays there, and the path line does not
   repeat it. The phone and the desktop use the same rule. On the path line the part is "N hidden", with no "· Show", and the
   file name is cut with "..." but keeps its chevron, the target of the file menu (ADR-0174 item 4). A tap on the part opens
   the list of ADR-0124 item 3 under the path line (Q2 A of the owner, 2026-11-20). The part shows on the path line only while
   no body is selected; with a selection the path line gives its room to the segments, and the list closes (finding V226, Q5 A
   of the codebase review of 2026-11-21). Where the room of the path line cannot hold the file name at its floor (ADR-0100 item 1),
   its chevron, the gap and the part, 12 pixels left of "Views" (ADR-0138), the part leaves the path line, and "Show hidden"
   on the ring of the part (item 1) is the way back. An example is a portrait phone of 360 pixels with a sheet (R-0072, Q3 A
   of the owner of 2026-11-25).
3. **No contract change, no engine change.** The ring, the segment and the part are states of the client (ADR-0007).
4. **No code in this record.** Roadmap row R20e (TASK-0424) gives items 1 and 2, with the tests in `ShowHiddenTests` and the
   layout test of ADR-0172.

## Consequences

**Good:** a person finds "Show hidden" where the tools of the part are, on each display, and a portrait phone shows the count
of the hidden bodies.

**Bad:** the answer of the owner named a tap on the path line that opens a ring. This record reads it as the place of the count
of the hidden bodies, and keeps one ring with nothing selected. The owner can correct the reading.

## Alternatives considered

- A second ring that opens from a tap on the path line: two rings for one state, and the file name already opens the file menu.
- The panel only: the owner did not choose it.

## History

- 2026-11-20 · TASK-0427 · The record, from the answer of the owner of 2026-11-20 to R-0064.
- 2026-11-21 · TASK-0426 · Item 1 says that "Show hidden" sends one command for each hidden body, and item 2 says what the path
  line of a phone shortens and where the list opens (findings V218 and V219, Q1 A and Q2 A of the owner); `affects` names
  `DesktopUi.Part.cs`, where the labels of the ring of the part are.
- 2026-11-21 · TASK-0424 · Shipped at v0.412.
- 2026-11-21 · TASK-0431 · Item 2: the part shows on the path line only while no body is selected (finding V226, Q5 A of the
  codebase review of 2026-11-21).
- 2026-11-25 · TASK-0465 · Item 2: the part leaves the path line where the room cannot hold it with the file name at its floor
  (R-0072, Q3 A of the owner of 2026-11-25).
