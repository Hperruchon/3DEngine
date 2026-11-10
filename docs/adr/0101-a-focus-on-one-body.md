---
id: 0101
title: A focus on one body is a state of the client that draws one body and writes nothing to the log
status: Accepted
topic: Clients, selection
date: 2026-11-10
supersedes: []
superseded-by: []
amends: ['0081', '0098', '0100']
amended-by: []
affects:
  - 3DEngine.App/BodyActions.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/SceneRenderer.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.App/Ui/PanelAction.cs
  - 3DEngine.ImageTests/BodyFocusTests.cs
enforced-by: 3DEngine.ImageTests/BodyFocusTests.cs
---

# ADR-0101 — A focus on one body is a state of the client that draws one body and writes nothing to the log

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

To review one body, the person hides each other body. Each hide is the command `SetBodyVisibility` (ADR-0029). It goes
into the log and into Undo. "Show all" afterwards also shows the bodies that the person hid on purpose. Onshape and
Shapr3D give an Isolate that is a state of the view only, and it ends with one action.

The proposal `docs/proposals/2026-11-08-document-and-body-selection.md` gives the analysis. ADR-0100 is its option A, the
path line. This record is its option B, the focus. On 2026-11-08 the owner accepted each recommended answer:

- Q4 A: while a focus lasts, the other bodies are not drawn. A dimmed drawing, as in Onshape, needs a pass with
  transparency, and a later task can add it.
- Q5 A: the person enters the focus from the ring and from the command line. There is no double tap, because a double
  tap conflicts with the tap that selects a face (ADR-0075 item 2).

The task gave two questions to the judgement of the session: the order of Escape with a focus, and the place of the close
mark.

## Decision

1. **The focus.** The focus is a state of the client (ADR-0007): the identifier of one body, or null. `BodyActions` keeps it
   beside the selection. While a focus lasts, the selection is the body of the focus, or a face or an edge of it. A focus
   gives no command, so the log, Undo and the visibility of each body do not change. A saved document does not keep it.
2. **The draw and the pick.** `SceneRenderer` holds the one rule: while a focus lasts, it draws the body of the focus only,
   and a pick finds that body only. The camera does not move. The dimensions of the bodies show the box of the body of the
   focus only.
3. **The entry (Q5 A, amends ADR-0081 item 3 and ADR-0098 item 3).** "Focus" is a segment of the ring of a selected body,
   after Hide, and a command of the command line for a selected body. On a portrait display it is a segment of the thumb
   ring. While a focus lasts, the segment has the colour of an active control, as the tool in use of a sketch.
4. **The path line (amends ADR-0100 item 1).** While a focus lasts, the path line shows "Focus" in the quiet colour before
   the name of the body, and a close mark after the last segment. The close mark is 44 pixels wide and 44 pixels high
   (ADR-0027 item 6). A tap on it ends the focus. The close mark is at the end of the line because the face and the edge
   follow the body; at the end, it does not move when the person selects a face. Each segment keeps its tap.
5. **The exit.** These end the focus, and the body stays selected:
   - the close mark;
   - "Focus" again, on the ring or in the command line;
   - the key Escape at the level of the body.

   These end the focus and the selection of the body: a tap on the segment of the document, a hide of the body, and a body
   that leaves the session (for example a body that an agent consumes, or an Undo removes). A selection of another body
   ends the focus too.
6. **Escape (amends ADR-0100 item 3).** With a focus, Escape goes from the edge to the face, from the face to the body, from
   the body to the body with no focus, and then to the document. So the person needs one more Escape to reach the document.
7. **A tap inside the focus.** A tap on the body selects its face, and a tap near an edge of the selected face selects the
   edge, as before (ADR-0075 item 2, ADR-0077 item 3). A tap on no body goes up to the level of the body, and the focus
   stays. Before this record, a tap on no body cleared the selection.
8. **A command of the person on the body of the focus.** A move (`Translate`) or a cut (`Subtract`) makes a new body, which
   becomes the selection (ADR-0032 item 4, ADR-0073 item 2). When the source was the body of the focus, the focus goes to
   the new body. Otherwise the focus would end at each move.
9. **No contract change, no engine change, no change of the log.**

## Consequences

**Good:** the person reviews one body with one action and ends the review with one action. The log, Undo and the
visibility of each body do not change, so "Show all" keeps its meaning.

**Bad:** the other bodies are not drawn, so the person does not see where the body is among them. Escape needs one more
press to reach the document. The ring of a body has one more segment, so on a portrait display five segments are
narrower. No person used a focus in this window yet.

## History

- 2026-11-10 · TASK-0315 · The record, with the answers Q4 A and Q5 A of 2026-11-08 and the judgement of the session on
  items 4, 6, 7 and 8.
- 2026-11-10 · TASK-0315 · Shipped at v0.323.
