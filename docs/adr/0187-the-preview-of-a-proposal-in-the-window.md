---
id: 0187
title: The preview of a proposal in the window, the new shapes in dashed amber lines with a switch and a panel of the differences
status: Amended
topic: Clients, agent
date: 2026-11-23
supersedes: []
superseded-by: []
amends: ['0092', '0109', '0125', '0185']
amended-by: ['0188']
affects:
  - Engine.Core/Preview/PreviewShape.cs
  - Engine.Core/Preview/DocumentFork.cs
  - Engine.Api.Http/Proposals.cs
  - Engine.Api.Http/Proposals.Preview.cs
  - 3DEngine/DesktopSession.cs
  - 3DEngine/HostedSession.cs
  - 3DEngine.App/ProposalPreview.cs
  - 3DEngine.App/Ui/DesktopUi.Preview.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/SceneRenderer.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.Overlay.cs
  - 3DEngine.Vulkan/RenderPipeline.cs
  - 3DEngine.Vulkan/DrawListExecutor.cs
  - 3DEngine.Vulkan/Shaders/highlight.frag
  - Engine.Tests/Http/ProposalPreviewTests.cs
  - 3DEngine.ImageTests/ProposalPreviewWindowTests.cs
enforced-by: 3DEngine.ImageTests/ProposalPreviewWindowTests.cs
notes: R25 (TASK-0462). ADR-0157 item 8 and gap G12 of docs/proposals/2026-11-10-gaps-to-the-render.md give the row; the owner ordered "Start R25" on 2026-11-23.
---

# ADR-0187 — The preview of a proposal in the window, the new shapes in dashed amber lines with a switch and a panel of the differences

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R25 of the roadmap: "The preview of a proposal in the window: the new shape in dashed amber lines on the present shape, a
switch "before, after, both", a table "measure, before, after", and the state of the solver after (gap G12 of the render,
ADR-0157 item 8). Desktop first. After P15." Gap G12 of the render asks for "the new shape in dashed amber lines; rows
"Solver after: solved, 2 free"". The owner ordered "Start R25" in the chat on 2026-11-23 (Q1 A of the codebase review of
2026-11-23).

What exists:

- ADR-0185 (P15a, P15b): the host runs the commands of each open proposal on a fork of the Document, off the serial lock, and
  keeps the last preview of each proposal: the outcome of each command, the bodies that the commands add, remove and change,
  the state of the solver of each changed sketch, the measures of each added or changed body and the pairs whose check
  changes. Item 5: "A preview holds records and results, not geometry; the backend of a fork lives only during the fork."
- ADR-0092 item 5: the window shows the summary and the text of each command, and "no simulated result". The window reads the
  queue through `HostedSession` (`IProposalDesk`), with the lock of the queue only, so the frame never waits for the session.
- ADR-0009: a host that draws projects the state of `Engine.Core` into the state of `3DEngine.Core`; a client does not read
  the internal state of the engine. ADR-0007: a tessellation is presentation, and no Document holds it.
- ADR-0109: the edge lines of a body, a visible pipeline and a hidden pipeline, each with a dash in pixels. The amber of a
  proposal is `DesktopUi.DimensionColour`, `#ffa040` (ADR-0125 item 1).

**The question.** The preview holds records, and the backend of a fork lives only during the fork. So how does the window get
the shape after the commands, to draw it in dashed amber lines?

## Decision

1. **The shapes come from the fork (amends ADR-0185 items 2 and 5).** A host that draws sets `Proposals.KeepShapes`. The
   desktop sets it where it makes its host (`DesktopSession.Open`), so a proposal that an agent posts before the window
   starts has its shapes too; `HostedSession` sets it as well. With it, `DocumentFork.Run` reads, after the commands and the
   values of ADR-0185 items 8 to 10 and before its session closes, the triangles of each body with a new solid: an added body,
   or a changed body whose geometry key or fault changes (the rule of the same solid, ADR-0185 item 9). The body must be
   visible after the commands and have no fault. The fork reads them with the query `GetTessellation`, in the order of the
   body identifiers (`PreviewShape`). A body that the query refuses has no shape. `engine-api-http` does not set the flag, so
   its forks read no shape. The answer of `GET /proposals/{id}/preview` does not change: a shape is presentation, for the
   window of the host only.
2. **The bound.** One preview keeps at most `PreviewShape.MostTriangles`, 100,000 triangles. A body whose shape passes the
   bound, with the shapes before it, has no shape and counts in `ShapesLeftOut`. Only an open proposal keeps its shapes; a
   proposal that closes drops them and keeps its preview (ADR-0185 item 5). A tessellation takes about 24 bytes for each
   triangle (12 bytes of indices, and 24 bytes of positions for each vertex with about one vertex for two triangles). So one
   preview keeps at most about 2.4 MB, and the 8 open proposals of the queue at most about 20 MB. The GPU holds the shapes of
   the shown proposal only, at most 100,000 triangles and their edge lines.
3. **The read of the window.** `Proposals.Kept(id)` gives the last preview of a proposal with its shapes, the count of the
   bodies above the bound, and `Current`. It takes the lock of the queue only, with no read of the session and no wait, so the
   frame calls it as it calls `Waiting`. `Current` is false while a later version of the Document waits for its fork, or while
   that fork runs; the panel then says "updating", and the scene keeps the last shapes until the new ones come.
   `IProposalDesk.Preview` gives it to the window as a `PreviewCard`. The phone gives no desk (ADR-0092 item 8). ADR-0188
   items 6 and 9: `Kept` also gives the code of the note of an accept that applied nothing, and the window gives the version
   of the card that its panel showed to the accept.
4. **The switch "Before, After, Both".** A state of the client (ADR-0007); it sends nothing to the engine. "Before" draws the
   scene as the Document is. "After" does not draw each body that the commands remove, each changed body with a new solid and
   each changed body that the commands hide, and draws the new shapes. "Both" draws the scene as the Document is and the new
   shapes on it. The mode starts as "Both", and a new shown proposal starts in "Both" again.
5. **The draw.** `PreviewMeshes` projects the shapes into a scene of the render-side kernel (ADR-0009): one entity and one
   mesh with its edge lines for each shape, narrowed around the centre of its box (ADR-0024 item 2). The narrowing and the
   search of the edges run on the thread pool, so the thread of the frame only puts the meshes on the GPU; the meshes of the
   shown shapes stay until the new ones are ready. `SceneRenderer` draws the edge lines of the shapes after each body, with
   the two edge pipelines of ADR-0109: dashes of 5 pixels, a half width of 1 pixel, in the amber of a proposal, first the
   hidden pipeline at alpha 0.45 and then the visible pipeline at alpha 1. So a line inside a present body shows, fainter. In
   "After" a shape also has a faint fill (item 9). The far plane holds the box of the shapes, but the camera does not frame
   them. While a focus lasts, only the shape of the body of the focus draws (ADR-0101 item 2). A section does not cut a shape.
6. **The panel (amends ADR-0092 item 5).** The window now shows a simulated result. On a landscape display of at least 1024
   by 600, and not on the sheet page, a panel at the lower right, 6 pixels above the status line, has the width of the command
   line, or less, down to 300 pixels, so that it ends 8 pixels left of each target of a tool of the ring at its height. It
   holds:
   - the title "Preview of the proposal", with "· updating" while the preview is not current;
   - ADR-0188 items 6 and 9: after an accept that applied nothing because the result changed, the note "The result changed:
     check it" in amber; the panel then ends 6 pixels above the line of the outcome of the accept;
   - the switch: three segments "Before", "After" and "Both", each 44 pixels high (ADR-0027 item 6), the mode in use in amber;
   - "Stops at command i of n (code)" when the fork stops at a command, as the accept stops (ADR-0092 item 6);
   - the table "Measure, Before, After": the volume of each body that the commands add or change, with groups of three
     digits and no decimal from 100, "new" for an added body, the value after in amber when it changes, and the line
     "Measure: volume in cubic mm" (the atlas of the fonts has no cube sign);
   - "N bodies go" for the removed bodies;
   - "Solver after" and, for each sketch that the commands add or change, "Plate: under, 6 free", with the residual for
     `over`;
   - "Checks that change" and, for each pair, "Bracket · Post: apart → overlapping";
   - "N bodies are too large to draw" for the bodies above the bound.
   Each part shows at most 4 rows and "and N more". The lines that do not fit under the top bar go, and the last line says
   how many. A failed preview shows "The preview failed" and its code.
7. **Desktop first.** A portrait display, a window smaller than 1024 by 600 and the sheet page show no panel, and the scene
   then draws "Both". The phone shows no proposal (ADR-0092 item 8).
8. **No change of the contract.** No change of `Engine.Contracts`, of the log, of a query or of an event kind, and no new
   diagnostic code.
9. **The colours while a preview shows (R33, TASK-0470; amends ADR-0109 item 3 and ADR-0125 item 1).** The owner answered
   Q6 A and Q7 A on 2026-11-25 (TASK-0464).
   - While the window gives a preview to the scene, in each mode, the selected body takes the accent of the theme
     (`Theme.Accent`, #30eaff) in the place of the orange of the selection (#ff8c1a): its visible and hidden edges, its fill at
     alpha 0.25 and its dimensions. The accent is the colour of a tool in use (ADR-0108 item 2) and of the selected view of
     a sheet (ADR-0168 item 2), and of the colours of the theme it is the farthest from the amber. So no new colour comes.
   - In "After" each shape has a faint fill in the amber of its lines, at alpha 0.15. After the lines, the depth pipeline
     writes the depth of the shapes, and the highlight pipeline tints one time each pixel where a shape is nearest. The fill
     has the colour of the lines, so it does not hide a line. "Before" and "Both" have no fill.
   - The highlight pipeline takes its colour from a push constant of the fragment shader at offset 128, after the two
     matrices of `mesh.vert`. The depth pipeline takes `mesh.frag`, which reads no colour. Until TASK-0470 the colour was a
     constant of `highlight.frag`.

## Consequences

**Good:** the person sees the new shape on the present shape before Accept, and switches between the two in one tap. The
table, the state of the solver and the checks come from the same fork as the answer that an agent reads, so the window and
the agent see one result. The shape is the tessellation of the body after the accept (a test compares the two), and the
authority diagram stays: the host projects a result of the engine into the render-side kernel.

**Bad:**

- Each fork of the desktop reads the triangles of each body with a new solid, also when the window does not show that
  proposal. A large body costs one copy of up to 100,000 triangles for each commit with open proposals.
- While a preview shows, the selected body has the accent, which is also the colour of a tool in use and of the hatch of a
  cap of a section. The colour of the selection changes when a preview starts and when it ends. Until TASK-0470 the orange
  of the selected body was near the amber of a proposal, and in "Both" the edges of the two differed by the dashes only.
- When the room right of the ring is less than 300 pixels, the panel covers a part of the ring of a body, and a tap there goes
  to the panel.
- The fill of a shape in "After" is faint, so a body behind the new shape shows through it, tinted. Until TASK-0470 a shape
  had no fill.
- A face of a present body in the plane of a face of a new shape takes the fill in parts only, because the two depths are
  nearly equal. The fill of a selected body has the same fault (ADR-0109 item 3).
- The table gives the volume only; the centre of the mass and the faces of the measures do not show.
- Only the shown proposal, the oldest one, draws its shapes. The scene follows a new preview one frame after the panel.
- A sketch that the commands add has the name "New sketch", because the frame does not know it.
- A section does not cut a new shape.

## Alternatives considered

- **The window replays the log in its own fork.** The host gives the copy of the log and the commands, and the window runs a
  second fork for the shape when it shows a proposal. Each shown preview then costs a second replay and a second backend
  (ADR-0185 item 11), the client reads the log, which is the internal state of the host, and the phone has no factory of
  backends.
- **The fork keeps its session and its backend until the window reads the shapes.** Each kept preview then holds a native
  backend, up to 40 (ADR-0185 item 5), and the fork no longer frees each backend that it takes (ADR-0185 item 2).
- **The window moves or scales the present triangles.** Only a move gives a new shape so; a new value of a sketch or a
  boolean does not.
- **A member `shapes` in the answer of the route.** An agent does not draw, and the JSON of 100,000 triangles is about 3 MB.
  A tessellation is presentation (ADR-0007), not a result of the engine for a client.

## History

- 2026-11-23 · TASK-0462 · The record, Accepted, and the slice R25: the shapes of the fork, the read of the window, the switch,
  the draw and the panel.
- 2026-11-24 · TASK-0463 · ADR-0188 amends items 3 and 6: the window gives the version of the shown preview to the accept, and
  the panel shows the note of an accept that applied nothing because the result changed.
- 2026-11-25 · TASK-0464 · The owner answered two points of "Bad" (Q6 A and Q7 A of 2026-11-25): while a preview shows, the
  outline of the selected body takes a colour apart from the amber of a proposal, and the new shape in "After" gets a faint
  amber fill. Row R33 does both; no item changes now.
- 2026-11-26 · TASK-0470 · R33 (Q6 A and Q7 A): item 9 gives the accent to the selected body while a preview shows and a faint
  amber fill to a shape in "After"; item 5 and "Bad" follow, and ADR-0109 item 3 and ADR-0125 item 1 get the change.
