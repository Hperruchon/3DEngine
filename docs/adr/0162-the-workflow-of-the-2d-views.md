---
id: 0162
title: A person sees and designs the 2D views of a part in the window and on a sheet page, with the section view, the centre lines and the title block of ISO 7200
status: Accepted
topic: Drawing, window
date: 2026-11-18
supersedes: []
superseded-by: []
amends: ['0118', '0157']
amended-by: []
affects:
  - docs/proposals/2026-11-18-2d-view-workflow.md
enforced-by: Engine.Tests/Governance/TaskGovernanceGateTests.cs
notes: Governance only. Each slice R30a to R30i opens its own ADR, and the ADR of R30g, R30h or R30i states the exact shape of its contract change.
---

# ADR-0162 — A person sees and designs the 2D views of a part in the window and on a sheet page, with the section view, the centre lines and the title block of ISO 7200

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

P10 to P10c gave a sheet in the Document and its export as SVG, DXF and PDF (ADR-0153, ADR-0155, ADR-0158 to ADR-0161).
A person can define the views of a sheet only by a command, and the window does not show a sheet. On 2026-11-17 the owner
compared the sheet of a plate with a hole and the flat views of the window, and said: "we should be able to define the view
for the 2d drawing so the app should be able to show a view similar to the sheet if not the sheet". The flat views of the
window show no hidden lines, draw the outline of the sketch, and have no link to the views of a sheet.

A side session wrote the design file `docs/proposals/2026-11-18-2d-view-workflow.md` (TASK-0402) with a visual prototype,
decisions D1 to D18, the order Q19, and the answer sheet `docs/proposals/2026-11-18-answer-sheet.md`. The owner answered on
2026-11-18, in the chat of that session: "1a 2a 3a 4a 5a/b 6a 7a 8a 9a 10a 11a 12a", then "13a 14a 15a 16a 17a 18a 19a",
and "Main session" for the place of this record. Q16 A replaces Q12 A.

## Decision

1. **The sheet page (D1 A).** A sheet shows on a page that replaces the scene, on the desktop and on the phone. A button
   "Sheet" opens it, and "3D" or "Views" goes back. The paper fills the free part of the window.
2. **The lines of a flat view (D2 A).** A flat view of the window draws the lines of the sheet: a new query of the lines of
   one frame, from `ViewEdges`, the code of GetSheet. The window reads it off the frame thread and keeps the last lines
   until the new lines come. The query needs no contract change.
3. **The look of the lines (D3 A).** A switch "Lines" (key L) in a flat view gives a light background, the visible lines,
   the hidden lines with dashes, no fill, no outline of a sketch and no grid. With the switch off, the flat view is
   shaded, as today.
4. **The add of a view (D4 A).** "Add to sheet" in the net of the views adds the current flat view to a sheet, and
   "Standard views" adds the front, the top and the side view in one tap. The first view opens a pad for the paper and
   the scale. The command line of the window has the same commands.
5. **The place of a new view (D5, A and B).** The app computes the place of a new view from the projection of the sheet,
   first angle or third angle, with a gap of 10 mm of paper. A tap on the paper of the sheet page is a second path that
   gives the place. The owner answered "5a/b"; this record reads it as both options together.
6. **The scale (D6 A).** A new command ScaleSheetView changes the scale of one view, or of each view, in one step of
   undo, and the dimensions stay. The pad offers the scales of ISO 5455 and a free ratio.
7. **A dimension from the window (D7 A).** "Put on sheet" on a value chip of a flat view places that value on the view of
   the sheet with the same frame. R22d keeps the dimension line and the proposal of the agent.
8. **A proposal of the agent (D8 A).** On the sheet page, a proposed view is an amber box at its place, and a proposed
   dimension an amber value. The arc of ADR-0092 keeps Accept and Reject. No preview enters the log.
9. **The export (D9 A).** One button "Export" on the sheet page gives PDF, SVG and DXF; the desktop opens a save dialog,
   and the phone saves or shares the file.
10. **The drag of a view (D13 A).** On the sheet page a drag keeps the alignment of a projected view with Front, and a drag
    of Front takes its projected views with it; Shift, or "Keep aligned" off, moves a view free. One drag is one new
    command, MoveSheetViews, and one step of undo. The command lives in `Engine.Core`; the record holds no parent.
11. **The hidden lines of each view (D14 A).** Each view has a flag for its hidden lines, on by default and off for a
    section. This is a new field of `SheetView` in `Engine.Contracts`.
12. **Centre lines and centre marks (D15 A).** GetSheet gives a new kind of line, "centre": a cross on each whole circle
    that faces the view and the axis of each cylinder seen from the side, 3 mm of paper past the outline. The three files
    draw them. The flag of item 11 has a part for them.
13. **The section view (D16 A, which replaces Q12 A).** A section of a sheet comes now, as R30h, before R26: a cut line on
    a view, a cut plane along the right or the up of its parent, a letter, the hatch of the cut faces and the label "A–A".
    This is a new field of `SheetView` in `Engine.Contracts`. "Section by a plane" is one of the six operations of the
    charter; the section of R26 in the window later uses the same cut.
14. **The diameter of a whole circle (D17 A).** A dimension of a radius whose edge is a whole circle prints ⌀ and the
    diameter, for example "⌀16"; an arc keeps "R".
15. **The title block (D18 A).** The sheet record holds the fields of ISO 7200, set by one command: title, identification
    number, revision, date of issue, sheet number, legal owner, material, creator and a note of the general tolerances.
    The title block shows the symbol of the projection of ISO 5456-2, and the frame gets the centring marks. This is a new
    field of `SheetRecord` in `Engine.Contracts`.
16. **A pictorial view (D11 A).** A pictorial view is an exact frame of whole numbers of one length, for example right
    (2, 2, −1), up (−1, 2, 2) and toward the eye (2, −1, 2). It needs the fix of E211 (TASK-0400). This is the exit of
    register entry R-0068, which closes when a slice proves such a view with its circles.
17. **The contract changes.** Items 11, 13 and 15 change the public shape of `Engine.Contracts`. The owner answered the
    design questions; the slice that changes the contract (R30g, R30h or R30i) writes the ADR that states the exact shape,
    in the same commit as the code (`CLAUDE.md`, "Stop and ask").
18. **The slices (Q19 A).** R30a: the lines of the sheet in a flat view and the switch "Lines" (items 2, 3). R30b: "Add to
    sheet", "Standard views", the pad of the paper and the scale, ScaleSheetView (items 4 to 6). R30c: the sheet page, its
    views and values, "Go to", "Scale", "Remove", the drag and MoveSheetViews, and the phone (items 1, 5, 10). R30d: the
    button "Export" (item 9). R30e: "Put on sheet" (item 7). R30f: the proposal of the agent on the sheet page (item 8).
    R30g: the flag of each view, the centre lines and ⌀ (items 11, 12, 14). R30h: the section view (item 13). R30i: the
    title block of ISO 7200 (item 15).
19. **The order (Q19 A).** This amends ADR-0157 item 11. After P10c: P0.162 (TASK-0400); R30g beside R30a, R30b and R30c;
    then R30h; then R30d, R30e, R30f and R30i; then K10 and the rest of the order of ADR-0157 item 11.
20. **The scope (amends ADR-0118 item 7).** The hatch of a section and a note of the general tolerances as a field of the
    title block are now in the scope of the drawing of a part. A tolerance on a dimension, a symbol of a surface finish,
    a drawing of an assembly and electrical schematics stay out.
21. **No code in this record.** This record changes ADR-0118, ADR-0157, the roadmap and the index of the ADRs. Each slice
    opens its task and its ADR at its start.

## Consequences

**Good:** the window shows what the sheet prints, a person makes and changes a sheet without the command line, and a
drawing gets the conventions that a workshop reads: hidden lines where they help, centre lines, ⌀, a section with its
hatch, and a title block with a number and a date.

**Bad:** nine slices of about 13 to 17 evenings come before K10, so the assembly track starts later. Three contract changes
(items 11, 13 and 15) wait for the ADR of their slice. The rules of the standards in the design file come from search
summaries and memory, not from the paid standards. R-0068 stays open until a slice proves a pictorial view.

## Alternatives considered

- **K10 first, then R30 (Q10 B, Q19 B).** It keeps the order of 2026-11-15. The owner chose the drawing work first, while
  the work of P10 is fresh.
- **The section after R26, from the cut of the window (Q12 A).** One cut model for the window and the sheet. The owner chose
  Q16 A: a part with a hole is clear only in a section, and the section of a sheet comes first.
- **A parent field in the record of a view (Q13 B).** The engine would keep the alignment for the agent too. The owner chose
  the alignment in the client and one command.

## History

- 2026-11-18 · TASK-0403 · The record, from the answers of the owner of 2026-11-18 to Q1 to Q19 of the design file.
- 2026-11-18 · TASK-0403 · Shipped at v0.386.
