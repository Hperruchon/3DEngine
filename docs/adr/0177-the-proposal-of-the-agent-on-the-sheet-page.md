---
id: 0177
title: The sheet page shows a proposal of the agent as amber boxes of views and amber values before the accept
status: Accepted
topic: Drawing, window
date: 2026-11-20
supersedes: []
superseded-by: []
amends: ['0092', '0168']
amended-by: []
affects:
  - 3DEngine.App/ProposedSheet.cs
  - 3DEngine/HostedSession.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.App/Ui/DesktopUi.Sheet.cs
  - 3DEngine.App/Ui/DesktopUi.SheetPage.cs
  - 3DEngine.ImageTests/ProposedSheetTests.cs
enforced-by: 3DEngine.ImageTests/ProposedSheetTests.cs
---

# ADR-0177 — The sheet page shows a proposal of the agent as amber boxes of views and amber values before the accept

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 item 8 records the answer D8 A of the owner of 2026-11-18: on the sheet page, a proposed view is an amber box at its
place, and a proposed dimension an amber value; the arc of ADR-0092 keeps Accept and Reject; no preview enters the log. Section
4.2 of `docs/proposals/2026-11-18-2d-view-workflow.md` gives the proposal of an agent: CreateSheet, the AddSheetView commands
with their scale and places, and the PlaceSheetDimension commands. The design file says that the size of a box comes from the
query of the lines of a frame (ADR-0166 item 1). ADR-0162 left open which commands show, which sheet the page shows, and what
the arc does on the page. This record gives those choices, without questions to the owner.

The identifier of a sheet, a view or a dimension is the identifier of its command (ADR-0153 item 6, ADR-0155 item 1, ADR-0158
item 1). An agent gives the identifier of each command in its envelope, so a proposed view names the proposed sheet, and a
proposed dimension names the proposed view.

## Decision

1. **The sheet items.** The window reads three commands of the oldest waiting proposal, the proposal that the arc shows:
   CreateSheet, AddSheetView and PlaceSheetDimension. Each gives an item with the identifier of its command. A parameter that
   does not bind to the type of its schema gives no item, as for a proposed value (ADR-0110 item 1). The page shows the items
   of one sheet: the sheet of the first item. Each other command of the proposal shows only as text on the arc.
2. **The read.** Off the frame thread, once for each proposal and each version of the Document, the host reads:
   - the box of each proposed view: the box of the edges and the outlines of GetViewLines on its frame, with each whole curve,
     around the centre of that box at the place of the view, times the scale. This is the box of the view that GetSheet gives
     after the accept, as SheetLayout places it. A centre line does not count (ADR-0165 item 1). A view with no line is a
     square of 10 mm, as for a view of the page (ADR-0168 item 2).
   - the text of each proposed dimension: the value of its constraint in the sketches of the window, or the distance of its
     extrude from GetBodyInfo, as the page prints a value (a radius on a whole circle is a diameter, ADR-0165 item 4). A source
     that the design does not hold gives "?". When the same proposal also holds a command that changes that source,
     SetSketchConstraintValue for a constraint or SetExtrudeDistance for an extrude, the text has the proposed number of that
     command in place of the number now, in the length unit of the document as each other value (ADR-0060 item 2). With two
     such commands for one source, the number of the last one shows, because the commands apply in their order
     (`ProposedValues.Of`, ADR-0110 item 1). With no such command the text keeps the value of the design.
   - the paper of a new sheet, from its CreateSheet.
   The read sends no command and writes nothing, so nothing enters the log before the accept.
3. **The page (amends ADR-0168 item 1).** While the proposal has sheet items, the button "Sheet" shows with an amber outline,
   also with no live sheet. A new proposal takes the page to its sheet one time; then the person can choose another sheet. A
   new sheet of a proposal has a button of its own with an amber text when the page has more than one sheet. Over the sheet
   of the proposal, each proposed view is an amber box with its name and its scale above its upper left corner ("Front 2:1",
   or "View" for a frame of no flat view), and each proposed dimension is its value in amber at its place, as the page prints
   the values of the sheet. The paper of a new sheet has an amber outline. A sheet that is not live and is not the sheet of the
   waiting proposal does not show; so after a reject of a new sheet, the page shows the first live sheet, or closes.
4. **A new sheet takes no tap.** The page draws the paper, the frame and the title block of the new sheet of a proposal, but
   it has no "Export", and a tap or a drag on its paper does nothing. The page reads no GetSheet for it. After the accept, the
   sheet is live with the same identifier, and the page stays on it.
5. **The arc on the page (amends ADR-0092 item 5).** While the page shows the sheet of the proposal, the arc keeps its
   caption, Reject, Accept and the count of the commands. It does not show the line of a command whose sheet item is on that
   sheet; the boxes and the values on the paper show those commands, and the lines covered the paper. It keeps the line of
   each other command, which the page does not show (Q3 A of the owner, 2026-11-20). While the page shows, the arc draws over
   the paper. The window of the page lies under the arc, so it does not stop a tap on Accept or Reject, and a tap whose press
   or release is on Accept or Reject is the tap of the arc only, also over the paper. Before this record the page took each
   tap on the arc.
6. **No contract change.** `Engine.Core`, `Engine.Api.Http` and `Engine.Contracts` do not change.
7. **The test.** `ProposedSheetTests` holds items 1 to 5. A proposal of a sheet, three views and two values is posted to the
   desktop over HTTP; before the accept the log does not change, and after the accept each box is the box of GetSheet for
   that view within 0.001 mm, and each value is the value of GetSheet at the same place.

## Consequences

**Good:** a person sees the place and the scale of each proposed view and each proposed value on the paper before the accept,
and judges them as on a drawing. The agent gets the same result as before the accept.

**Bad:** a proposed view shows as a box, not as its lines. A proposed move, scale, removal, change of lines or section shows
only as text on the arc. A proposal for two sheets shows the items of the first sheet only. The phone shows no proposal
(ADR-0092 item 8). The read of a proposal for each version of the Document costs one query of the lines for each frame.

**Next:** the lines of a proposed view in amber, and the other sheet commands, when the owner asks for them; the general
preview of a proposal on a fork (R25).

## Alternatives considered

- **The page opens by itself for a proposal of sheet items.** It changes the view under the person with no tap. The button
  "Sheet" with an amber outline asks for one tap.
- **The box from the box of the bodies (SheetPlacement), with no query.** That box comes from the meshes of the renderer, on
  the frame thread. GetViewLines gives the lines that GetSheet uses, so the test can compare the two boxes exactly.
- **The arc moves away from the paper on the page.** The arc of ADR-0107 has its place in the free room; without its command
  lines it covers only the band above the paper at 1280 by 720.

## History

- 2026-11-20 · TASK-0422 · The record, from the answer D8 A of the owner of 2026-11-18 and the order "Yes, start R30f" of
  2026-11-20.
- 2026-11-21 · TASK-0426 · Item 5: the arc keeps the line of each command that the page does not show (finding V222, Q3 A of
  the owner), draws over the paper, and a tap on Accept or Reject over the paper is the tap of the arc only (finding V221).
- 2026-11-22 · TASK-0453 · Item 2: the amber value of a source that the same proposal changes shows the proposed number of the
  last change (R-0071, the correction of finding V222, by the standing order of the owner of 2026-11-20).
