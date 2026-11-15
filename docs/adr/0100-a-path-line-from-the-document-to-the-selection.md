---
id: 0100
title: A path line from the document to the selection, and the rows of the document
status: Amended
topic: Clients, selection
date: 2026-11-09
supersedes: []
superseded-by: []
amends: ['0075', '0077', '0093', '0099', '0102']
amended-by: ['0101', '0138']
affects:
  - 3DEngine.App/BodyActions.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.App/Ui/PanelAction.cs
  - 3DEngine.ImageTests/DocumentPathTests.cs
  - 3DEngine.ImageTests/BodyActionsTests.cs
enforced-by: 3DEngine.ImageTests/DocumentPathTests.cs
---

# ADR-0100 — A path line from the document to the selection, and the rows of the document

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-08 the owner said that the window has no way to tell the selection of the whole document from the selection of
one body. The proposal `docs/proposals/2026-11-08-document-and-body-selection.md` gives the state of the code, three tools
(Onshape, Shapr3D and Plasticity) and three options. The owner accepted each recommended answer, Q1 to Q7 A:

- Q1 A: a "component" is a body of a part for now.
- Q2 A: Escape goes up one level each time, to the document.
- Q3 A: with nothing selected, the rows show the document.
- Q6 A: TASK-0313 (the rows of a selected body at the upper left) merges first. It merged at v0.298.

This record is option A. ADR-0101 (TASK-0315) is the focus on one body of option B.

Before this record, the selection had three levels in `BodyActions`: the body, the face and the edge. Escape went from the
edge to the face and from the face to the body, and stopped at the body (ADR-0075 item 2, ADR-0077 item 3). The top bar
gave the program, the file and the unit (ADR-0093 item 1). With nothing selected, the panel gave the file controls, the
list "New", "Sketch on", the toggles, a count, the filter and the list of the bodies (ADR-0099 Q1 A).

### Q1. Where the rows of the document go on a landscape display

The rows of a selected body take the place of the panel (ADR-0099 item 1). ADR-0099 Q1 A keeps the file controls, the list
of the bodies and its filter in the panel when the selection is empty.

- **A.** The rows of the document are the first part of the panel, in the place where the rows of a body start. The list
  of the bodies of the panel becomes the rows of the bodies. The controls of the panel follow, as before.
- **B.** The rows of the document take the place of the panel, as the rows of a body do, and the command line holds each
  control of the panel.

B leaves an empty document with no visible control to make a body, and it changes ADR-0099 Q1 A, which the owner accepted.
**Recommended and chosen: A** (the judgement of the session of 2026-11-09; the owner can change it).

## Decision

1. **The path line (Q1 A).** The path line gives the levels of the selection from the left to the right: the document, the
   selected body, the selected face and the selected edge. The document is the file name, "unsaved", or "Document" for a
   host with no files. The body is its name, or its kind. The face is "Face" and the element of its name, as "Face +z". The
   edge is "Edge" and the elements of its two names, as "Edge +x | +z". A chevron in the quiet colour is between two
   segments. With nothing selected the path is the document only.
2. **A tap on a segment.** Each segment is at least 44 pixels high (ADR-0027 item 6). A tap on a segment that is not the
   last one selects its level: the document clears the body, the face and the edge; the body clears the face and the edge;
   the face clears the edge. The last segment is the selection and takes no tap. A tap on the path line does not reach the
   scene. The selection stays a state of the client (ADR-0007); a tap gives no command.
3. **Escape (Q2 A, amends ADR-0075 item 2 and ADR-0077 item 3).** The key Escape goes up one level each time: from the edge
   to the face, from the face to the body, and from the body to the document. With nothing selected it does nothing. A
   lost pointer still gives no change (finding V108).
4. **The rows of the document (Q3 A and Q1 A, amends ADR-0099 item 1).** On a landscape display with no selected body, the
   first part of the panel is the rows of the document, in the style of the rows of a body (ADR-0084):
   - the first row: the kind on its first line ("Part" or "Assembly", from the session); the unit, the count of the live
     bodies, and "all correct" or the count that failed with the word "failed", in red, on its second line;
   - the filter and its button Clear;
   - one row for each body that the filter keeps: its name at the left; at the right "correct", or "failed" and the code
     of its fault in red, and "hidden". A tap on the row selects the body.

   The controls of the panel follow: the file, the list "New", "Sketch on", the toggles and "Show all". The count line and
   the list of the panel do not show there. A portrait display keeps the panel as before.
5. **A portrait display.** The top bar gives the program and the unit. The path line is the first line under the top bar,
   and the panel is under the path line. A tap on a segment acts as on a landscape display. The panel ends 8 pixels
   above the highest tool of the ring of the selected body, as the sheet of ADR-0111 item 4, when one row of 44 pixels
   still fits there. The path line moves the panel down by its height. Without this rule the panel covered Move at
   390 by 844. The rule also removes the fault of register entry R-0058 at 360 by 780.
6. **The top bar (amends ADR-0093 item 1 and ADR-0102 item 7).** On a landscape display the file of the top bar is the first
   segment of the path line: the program, the path, then the unit. The window of the top bar is as high as a segment, and
   the text stays in its place. A path that is too wide gives "..." for each middle segment, from the second one, until it
   fits; then it cuts the file name, then the last segment, each to at least 48 pixels. The file name is never wider than
   the rows (finding V115). The folder of the file follows the file name only while the path is the document only; with a
   selection, the path takes its room.
7. **No contract change, no engine change.** The host reads the kind of the document from `DocumentSession.Kind`, and the
   names of the face and the edge from the entries of `GetFaces` that it has.

## Consequences

**Good:** the person sees where the selection is in the document, goes up one level with a tap or with Escape, and sees the
kind, the unit, the counts and the faults of the document with nothing selected. The focus of ADR-0101 can show in the same
line.

**Bad:** Escape no longer keeps the body, so a person who presses Escape two times after a face loses the body too. A long
path on a narrow window shows "..." for the middle levels, and a tap on "..." selects the level that it hides. The
document rows put the controls of the panel lower, so on a short display the panel scrolls sooner. No person used the path
line in this window yet.

## History

- 2026-11-09 · TASK-0314 · The record, with the answers of the owner of 2026-11-08 and Q1 A.
- 2026-11-10 · TASK-0314 · Item 5: on a portrait display the panel ends above the ring of the selected body, after the
  merge of P0.139 showed that the path line moved the panel onto Move at 390 by 844.
- 2026-11-10 · TASK-0314 · Shipped at v0.316.
- 2026-11-10 · TASK-0315 · Amended by ADR-0101 items 4 and 6: the path line shows "Focus" and a close mark,
  and with a focus Escape ends the focus before it clears the body.
- 2026-11-14 · TASK-0370 · ADR-0138 item 2 amends item 1: on a portrait display the path line ends 12 pixels left of the
  button "Views" at the upper right.
