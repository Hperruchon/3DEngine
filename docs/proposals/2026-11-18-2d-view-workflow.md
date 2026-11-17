# Design file — the workflow to see and design the 2D views of a part

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-18. Prepared by a side session, from the request of the owner of 2026-11-17.
- Status: for discussion. It changes no code, adds no roadmap row and adds no register entry. Section 9 gives the
  questions for the owner. `docs/proposals/2026-11-18-answer-sheet.md` gives the same questions in one table.
- State of the repository: `origin/main` at v0.385 (`22931ad`), the merge of P10c.
- TASK-0402 holds this file and the answer sheet.
- Answered on 2026-11-18, in the chat of the side session: option A for Q1 to Q4 and Q6 to Q12, and options A and B
  together for Q5 (the app computes the place, and the person can also tap the paper). The answer sheet holds the
  answers. No ADR and no roadmap row record them yet.
- Later on 2026-11-18 the owner asked for one coherent view, a drag to move the views, a section view, hidden and
  construction lines for a cylinder and a circle, and a check of the usual conventions. Section 13 answers, with the
  questions Q13 to Q19. The prototype is at `https://claude.ai/artifact/CRvuRqUYisC4EA2vanCVHC` (private to the owner).

**How to read the sources.** I read each repository statement in the file that it names, in this session. A web source has
a mark: **[S]** a search result or its summary, **[M]** from memory. No agent and no person used a page of a web product
for this file. Section 11 lists what I did not verify.

---

## 1 The request

On 2026-11-17 the owner saw the sheet of a plate with a hole (PDF, A3, 2 : 1) and the app with the same document side by
side. The owner said:

- "we should be able to define the view for the 2d drawing so the app should be able to show a view similar to the sheet
  if not the sheet";
- "open a design session we need a workflow to properly see and design 2d view".

So the workflow must answer four needs:

1. A person sees in the app what the sheet will print.
2. A person makes a view of the sheet from the window, with no list of nine numbers.
3. A person sees the sheet itself, moves its views, and exports it, on the desktop and on the phone.
4. The agent proposes the views, the scale, the places and the dimensions, and the person accepts or rejects them
   (ADR-0092, ADR-0118 item 5).

---

## 2 The current state

### 2.1 The engine

| Subject | Today | Source |
|---|---|---|
| A sheet | One record of the Document: name, paper A4 to A0, orientation, projection (ISO first angle by default), views, dimensions. Commands CreateSheet, RenameSheet, RemoveSheet. | ADR-0153 items 1 to 3, 6 |
| A view of a sheet | An exact frame of nine doubles (right, up, toward the eye), a scale of two whole numbers, a place of its centre on the paper in mm. AddSheetView, MoveSheetView, RemoveSheetView. No command changes the scale or the paper. | ADR-0155 items 1 to 4 |
| A dimension of a sheet | A source (a constraint `distance` or `radius`, or the distance of an extrude) and a place on the paper. PlaceSheetDimension, RemoveSheetDimension. The read gives the value or "lost". No dimension line. | ADR-0158 items 1 to 4 |
| The lines of a view | `ViewEdges.Of(document, frame)`: exact visible and hidden lines, with circles, arcs and ellipses, from the faces of the owned kernel. The type is `internal` to `Engine.Core`. | ADR-0142, ADR-0143, ADR-0144; `Engine.Core/Drawing/ViewEdges.cs` line 93 |
| The read | GetSheet gives the paper and the lines of each view on the paper. Only GetSheet reads the lines; no query gives the lines of a frame with no sheet. No cache: three views of a sphere take about 5 s and hold the session (R-0069). | ADR-0155 items 5 and 7; `Engine.Core/Queries/` |
| Export | SVG, DXF R12, PDF 1.4 from the same read as GetSheet, by HTTP (`GET /export/{svg,dxf,pdf}?sheetId=`) and by the command line. | ADR-0159 to ADR-0161 |
| Limits | A frame whose length is not 1 shows no circle and no ellipse (E211); each exact view at a slant has such a frame. A frame of a length near 1e-200 or 1e200 gives a read that is not finite (E212). P0.162 (TASK-0400) holds both. No pictorial view (R-0068). | Review of 2026-11-17, E211, E212; `docs/register.md` R-0068 |

### 2.2 The window

| Subject | Today | Source |
|---|---|---|
| Flat views | Six flat views with no perspective (Front, Back, Left, Right, Top, Bottom) and the 3D view. A button "Views" opens a net of the cells; keys 1, 3, 7, with Control 1, 3, 7, and 0. A flat view has a scale of its own. | ADR-0137, ADR-0138, ADR-0141; `3DEngine.App/Ui/DesktopUi.Views.cs` |
| Lines in the window | The feature edges of the mesh (a crease of more than 30 degrees), on the GPU. Only the selected body shows its hidden edges, dashed, in the colour of the selection. The outline of a curved side is not a feature edge, so the window does not draw it. | ADR-0109 items 1 to 3; `3DEngine.App/SceneRenderer.cs` lines 36 to 64 |
| Values on a flat view | "Dimensions" shows the values of the design on their edges: only where the length is true (R22a), a chain with one reference (R22b), a free size in a separate form (R22c). This is presentation; it does not read the sheet. | ADR-0146, ADR-0148, ADR-0149; `3DEngine.App/EdgeValues.cs` |
| The sheet in the window | None. No file of `3DEngine.App` reads GetSheet, shows a sheet, or exports one. | `grep -rn "GetSheet\|Sheet" 3DEngine.App` gives only the bottom sheet of a portrait proposal |
| Proposals | The agent posts a proposal of 1 to 16 command envelopes; the window shows the summary and the text of the commands on an amber arc; Accept or Reject. A proposal of AddSheetView commands is valid today, but the person sees only text. | ADR-0092 items 2, 5 to 7 |

### 2.3 The evidence of 2026-11-17

The session of 2026-11-17 opened `plate.3de` (a plate of 60 by 40 by 10 mm with a hole of radius 8, one sheet on A3 at
2 : 1 with a front, a top and a side view) and pressed the keys 1, 7 and 3, then F, with a PowerShell script. I looked at
the pictures and at the PDF.

- **The PDF.** Each view has the visible lines thick and the hidden lines of the hole dashed. The top view shows the hole
  as one whole circle. The views are where first angle puts them: the top view under the front view, and the view
  from the right (eye (1, 0, 0) in `plate.3de`) at the left of the front view.
- **The front view of the app** (`app-front.png`). A small shaded plate in the middle of the scene, about 100 pixels wide,
  between the panels. No hidden line of the hole. A yellow line with two yellow points under the plate: the outline of
  the live sketch on the plane XY.
- **The top view of the app** (`app-top.png`). The shaded plate with a dark hole, and the yellow outline of the sketch
  with its four corner points.
- **The link.** No control of the app goes from a flat view to the view of the sheet, or back. The status line says
  "Undo: Add sheet view", so the log holds the sheet, but the window shows nothing of it.
- **F.** The pictures show no change of the size after F. The session did not verify that the key reached the window
  (`SendKeys` while the window had the focus). The code gives F the action Frame (`DesktopUi.Command.cs` line 96).

---

## 3 The gaps

| # | Gap | Effect for the person |
|---|---|---|
| G1 | A flat view of the window draws the edges of the mesh, not the lines of the sheet. No hidden line, no outline of a curved side. | The window and the PDF disagree. The person cannot check a view before the export. |
| G2 | A flat view draws the outline of the live sketch and the shaded faces. | The picture is not a drawing. |
| G3 | No control makes a view of a sheet from the window. | Only the command line with nine numbers, or the agent, makes a view. |
| G4 | No control shows a sheet, moves a view, or removes one. | The person sees the sheet only in an exported file. |
| G5 | No button exports a sheet. | Only the command line or HTTP exports. |
| G6 | No command changes the scale of a view. | A new scale needs a remove and an add: a new identifier, and the dimensions of the view go. |
| G7 | A proposal of sheet commands shows as text. | The person cannot judge a place or a scale from text. |
| G8 | A read of the lines is slow for a curved body and holds the session (R-0069). | A page that reads at each frame stops the window. |
| G9 | A slant or a pictorial view cannot show a circle (E211, R-0068). | Only the six standard views are useful now. |
| G10 | No section view (R26 is after K10, presentation only). | A drawing of a part with an inner shape needs hidden lines only. |
| G11 | The flat view keeps the part small after F, in the evidence (not verified). | The person sees a small part in the middle. |

---

## 4 The workflow that I propose, end to end

### 4.1 A person

1. The person opens the button "Views" and taps "Front", or presses 1. The flat view shows, as today.
2. The person taps the switch "Lines" in the net (or presses L). The flat view shows the lines of the sheet: a light
   background, the visible lines thick, the hidden lines thin and dashed, no fill, no sketch outline. The lines come from
   the same code as GetSheet. This is "a view similar to the sheet".
3. The person taps "Add to sheet" in the net. With no sheet, a pad asks the paper (A3 landscape by default) and the scale
   (the largest scale of ISO 5455 that fits three views, prefilled). One tap on "Add" creates the sheet and adds the view.
   With a sheet, the view goes on it at once, at the scale of the first view.
4. The app puts the view at the place that the projection gives: under the front view for the top view, at the left of the
   front view for the view from the right (first angle). A "Standard views" action adds the front, the top and the side
   view in one tap.
5. The person taps "Sheet" (a button beside "Views"). The sheet page replaces the scene: the white paper with the frame,
   the title block, each view and each dimension, from GetSheet. This is "the sheet".
6. On the sheet page the person taps a view. The view gets an outline and four actions: "Go to" (the scene in that flat
   view), "Scale", "Remove", and a drag that moves it. A drag of a projected view stays on its line of projection unless
   the person holds Shift or drags far from it.
7. In a flat view with "Dimensions" on, the person taps a value and then "Put on sheet". The value goes on the view of the
   sheet with the same frame, at the place of its chip.
8. On the sheet page the person taps "Export" and picks PDF, SVG or DXF. The desktop opens a save dialog; the phone opens
   the save or share sheet of the system.
9. Each change is a command of the log, so Undo works on the sheet page as in the scene.

### 4.2 The agent

1. The agent reads the Document and the sizes of the part, and posts one proposal: CreateSheet, the AddSheetView commands
   with their scale and places, and the PlaceSheetDimension commands, with a summary ("3 views at 2 : 1 on A3, 4
   dimensions").
2. The window shows the proposal on the sheet page: each proposed view as an amber box with its name at its place, each
   proposed dimension as an amber value. The arc of ADR-0092 keeps Accept and Reject.
3. The person accepts or rejects. After the accept, each command is one step of undo, as today (ADR-0092 item 6), until the
   batch of ADR-0157 item 10.
4. The agent reads the result as SVG (`GET /export/svg`), the picture that ADR-0157 answer A13 chose for the agent.

### 4.3 Two layouts, one model

```
Desktop, landscape                                   Phone, portrait
+--------------------------------------------------+  +----------------------+
| 3DEngine plate.3de   Sheet 1 · A3 · 2:1 · 1st    |  | Sheet 1 · A3 · 2:1 ⋯ |
|  +--------------------------------------------+  |  | +------------------+ |
|  |  paper: frame, views, values               |  |  | | paper, fit width | |
|  |   [Right]     [Front]                      |  |  | | pinch to zoom    | |
|  |               [Top]          +----------+  |  |  | +------------------+ |
|  |                              | title    |  |  |  | Front · 2:1          |
|  +--------------------------------------------+  |  | Go to Scale Remove   |
| (Undo)(Redo) Views Sheet Export | Front 2:1      |  | (Undo)(Redo) Views   |
|                    Go to · Scale · Remove        |  | Sheet  Export        |
+--------------------------------------------------+  +----------------------+
```

The same page, the same actions and the same commands on both devices. The phone moves a view with a long press and a
drag, as a body moves today.

---

## 5 The decisions

Each decision has two or three options. Option A is the recommended option.

### D1 Where the sheet shows (gap G4)

- **A. A sheet page that replaces the scene.** A button "Sheet" opens it; "3D" or "Views" goes back. The paper fills the
  free part of the window. Onshape and Shapr3D work this way (section 6).
- B. A panel beside the scene. The desktop has room; a phone has not: the paper of A3 on a phone in a half panel is about
  180 pixels wide.
- C. The paper as a plane in the 3D scene. The camera, the pick and the gizmo then need a second meaning in one picture.

**Recommendation: A.** It is the same on both devices, and the paper gets the whole free part.

### D2 The source of the lines in a flat view (gaps G1, G8)

- **A. A new query of the lines of one frame, from `ViewEdges`, the code of GetSheet.** The window reads it off the frame
  thread when the flat view starts or the Document version changes, keeps the last lines until the new ones come, and
  draws them in the overlay in pixels. A body that takes long shows "Lines…" until the read ends.
- B. The hidden edges of the GPU (ADR-0109) for each body. It is fast, but the source is the mesh with a limit of 30
  degrees: no outline of a curved side, and lines that can differ from the PDF.
- C. No lines in the window; the sheet page only.

**Recommendation: A.** One source of truth for the window, the sheet and the three files. The query needs no contract
change; its result records live in `Engine.Core`, as those of GetSheet.

### D3 The look of the lines in the window (gap G2)

- **A. A switch "Lines" in a flat view.** On: a light background, visible lines of 2 pixels, hidden lines of 1 pixel with
  dashes, no fill, no sketch outline, no grid. Off: the shaded look of today. The switch stays across the six views.
- B. The lines always on top of the shaded body in a flat view.
- C. The look of the lines as the only look of a flat view.

**Recommendation: A.** The shaded flat view stays useful for the work on a sketch and a body; the lines answer "what
does the sheet print".

### D4 How a person adds a view (gap G3)

- **A. "Add to sheet" in the net of the views, and the command "Add view to sheet".** It adds the current flat view. The
  first one opens a pad for the paper and the scale. "Standard views" adds the front, the top and the side view in one
  tap.
- B. "Insert view" on the sheet page only, as the base view of Onshape.
- C. Only the agent makes views.

**Recommendation: A**, and B later as a second path on the sheet page. The person already looks at the part in the
window, so the view is "this one".

### D5 The place of a new view

- **A. The app computes the place.** The first view goes at the centre of the free part of the paper, left of the title
  block. A projected view goes where the projection of the sheet puts it (first angle or third angle), with a gap of
  10 mm, scaled to the paper. A view that does not fit goes in the largest free part. The code uses + − × ÷ only.
- B. The person taps the paper to place each view; the sheet page must be open.
- C. The centre of the paper, and the person moves it.

**Recommendation: A.** The person moves it after, on the sheet page.

### D6 The scale (gap G6)

- **A. A new command ScaleSheetView for one view, with "all views" in the pad.** The pad offers the scales of ISO 5455
  (1:1, 1:2, 1:5, 1:10, 2:1, 5:1, 10:1) and a free ratio. The first view gets the largest scale that fits the standard
  views; each next view takes the scale of the first view, as a projected view of Onshape takes the scale of its parent.
  No contract change: the view record has its scale (ADR-0153 item 4).
- B. A remove and an add with the new scale: two steps of undo, a new identifier, and the dimensions of the view go.
- C. The scale stays as it was at the add.

**Recommendation: A.** One step of undo, and the dimensions stay.

### D7 A dimension from the window to the sheet

- **A. A tap on a value chip in a flat view, then "Put on sheet".** The command is PlaceSheetDimension on the view of the
  sheet with the same frame and the nearest scale, at the place of the chip mapped to the paper. The rules of R22a to
  R22c already chose which values show.
- B. A tap on an edge on the sheet page.
- C. Only the agent places dimensions (R22d).

**Recommendation: A.** The window already knows the right value of each edge. R22d keeps the agent proposal and the
dimension line.

### D8 How a proposal of the agent shows (gap G7)

- **A. On the sheet page, each proposed view is an amber box with its name at its place, and each proposed dimension an
  amber value.** The size of a box comes from the query of D2 on the frame of the command. The arc of ADR-0092 keeps
  Accept and Reject. No preview enters the log (anti-objective 2).
- B. The text on the arc, as today, until the preview of a proposal on a fork (R25).
- C. The agent changes the sheet with commands and no proposal.

**Recommendation: A.** A place and a scale are visual decisions; text is not enough. R25 later gives the general preview.

### D9 The export buttons (gap G5)

- **A. One button "Export" on the sheet page, with PDF, SVG and DXF.** The desktop opens a save dialog, as "Export STL…"
  does; the phone saves or shares the file. The command line of the window gets "Export PDF", "Export SVG" and "Export
  DXF".
- B. Three buttons in the left panel beside "Export STL…".
- C. No button; the command line of the engine only.

**Recommendation: A.** The export is a thing of the sheet, so it lives on its page. PDF comes first in the list.

### D10 The order (gaps G1 to G8)

- **A. P0.162 (E211, E212) first, then R30a to R30f, then K10.** The views must be correct before the window shows them
  to the person. The drawing track is fresh, and K10 waits for the sizes of the owner (R-0070).
- B. K10 first, as in the order of 2026-11-15, then R30.
- C. R30a only before K10, the other slices after it.

**Recommendation: A.** It answers the request of 2026-11-17 while the work of P10 is fresh.

### D11 A pictorial view (R-0068, gap G9, a later step)

- **A. A frame of whole numbers of one length, after the fix of E211.** An example that I computed by hand: right
  (2, 2, −1), up (−1, 2, 2), toward the eye (2, −1, 2). Each has length 3, right · up = 0, and right × up = 3 · (2, −1, 2).
  The eye is in front, at the right and above, at about 42 degrees over the floor. The frame is exact, and
  `ViewFrame.IsValid` (`Engine.Core/Drawing/ViewEdges.cs` line 41) asks only for two equal lengths, a right angle and a
  cross product toward the eye, so AddSheetView accepts it today, and determinism rule 2 holds. It shows circles only after E211.
- B. A new kind of view from a sine and a cosine, with a tolerance in the check: a change of ADR-0155 item 1 and an ADR.
- C. No pictorial view.

**Recommendation: A.** It closes R-0068 with "the frames of whole numbers", the exit that the entry names, and needs no
new field. A test in P0.162 or in the slice must prove the frame and its circles.

### D12 A section view (R26, gap G10, a later step)

- **A. After R26: the cut plane of the window becomes a view of a sheet.** A view of a sheet then needs a cut plane, a new
  field of `SheetView` and a change of `Engine.Contracts`: an ADR and a question to the owner first.
- B. A cut line drawn on a view of the sheet, as Onshape does, before R26 in the window.

**Recommendation: A.** One cut model for the window and the sheet, and the contract change comes with its own ADR.

---

## 6 The comparison with four products

| Subject | Onshape | Shapr3D | Plasticity | Zoo |
|---|---|---|---|---|
| Where the drawing lives | A drawing of its own in the document, with sheets **[S]** | A drawing from the project sidebar, a workspace of its own **[S]** | No sheet. "Export Hidden Line" from the scene, to SVG **[S]** | No drawing workflow found; its glossary defines a drawing **[S]** |
| The first view | "Insert view" from a Part Studio or an assembly; named views of the modelling, also with a section **[S]** | "Create Drawing": a body and a scale; "Include 4 views" gives the front, the left, the top and an isometric view **[S]** | Left, front and right orthographic views **[S]** | — |
| The next views | "Projected view" (key P) from a parent view, at the scale of the parent **[S]** | Projections from the views menu; each view refers to the base view **[S]** | — | — |
| Section and detail | A section line on a view; a detail circle **[S]** | Section and detail views **[S]** | — | — |
| Export | PDF, DWG, DXF **[M]** | PDF, DWG, DXF **[S]** | SVG **[S]** | — |

What I take from them:

- **A sheet page** (Onshape, Shapr3D): D1 A.
- **A base view and projected views at the scale of the parent** (Onshape): D5 A and D6 A.
- **Four views in one tap** (Shapr3D): "Standard views" in D4 A.
- **Named views of the modelling go to the drawing** (Onshape): "Add to sheet" from the flat view in D4 A.
- **A hidden-line picture of the scene** (Plasticity): the switch "Lines" in D3 A, with the exact lines of the sheet.
- No product of the four shows a proposal of an agent on a sheet. D8 A is new.

---

## 7 The later steps

- **A view at a slant, normal to a face (auxiliary view).** It needs an exact frame from the normal of a face, and the fix
  of E211. The window then needs a flat view of any exact frame, not only the six of ADR-0137.
- **A pictorial view.** D11 A, after E211.
- **A section view.** D12 A, after R26, with an ADR and a question for the contract.
- **A detail view** (a part of a view at a larger scale). A later row; not asked.
- **The dimension line and the flag "true length".** R22d (answer D7 A of 2026-11-15).
- **A cache of the lines.** If the read of R30a is too slow on a curved body, a cache in the engine changes ADR-0155 item 7
  (R-0069). The client keeps the last lines until then.

---

## 8 The slices in order

Each slice opens its task and its ADR at its start. Sizes are in evenings, as in the roadmap.

| Slice | Content | Decisions | After | Size |
|---|---|---|---|---|
| P0.162 | E211 to E217 of the review of 2026-11-17, as the roadmap row says. E211 and E212 first. | — | P10c | 1–2 |
| R30a | The lines of a view in the window: a query of the lines of one frame (code of GetSheet); a read off the frame thread; the switch "Lines" in the net, and the key L (a new key of `InputKey`); no sketch outline and no fill while it is on; Frame fills the free part (check G11 first). | D2, D3 | P0.162 | 2 |
| R30b | "Add to sheet" and "Standard views" in the net and in the command line; the pad of the paper and the scale for a first view; the place by the projection; the command ScaleSheetView. With the answer Q5 A and B, the tap on the paper that places a view comes in R30c, because it needs the sheet page. | D4, D5, D6 | R30a | 2 |
| R30c | The sheet page: the button "Sheet"; the paper, the frame, the title block, the views and the values from GetSheet, read off the frame thread; a tap on a view gives "Go to", "Scale", "Remove"; a drag gives MoveSheetView; a tap on the free paper places a new view (Q5 B); the list of the sheets in the panel; the phone layout. | D1 | R30b | 2–3 |
| R30d | The button "Export" on the sheet page: PDF, SVG, DXF; the save dialog of the desktop and the save or share of the phone; three words of the command line. | D9 | R30c | 1 |
| R30e | "Put on sheet" on a value chip of a flat view. | D7 | R30c | 1 |
| R30f | The proposal of the agent on the sheet page: amber boxes and values. | D8 | R30c | 1–2 |
| later | The pictorial view (D11), the auxiliary view, the section view after R26 (D12), the detail view, R22d. | D11, D12 | each its own row | — |

The total of R30a to R30f is 9 to 11 evenings. R30d, R30e and R30f can run at the same time after R30c, because they
touch different files. R30a alone already answers "a view similar to the sheet"; R30c answers "the sheet".

---

## 9 The questions for the owner

Option A is the recommended option for each question. "All A" is a valid answer.

1. **Q1 (D1).** Where does the sheet show? A: a sheet page that replaces the scene, on both devices. B: a panel beside the
   scene. C: the paper in the 3D scene.
2. **Q2 (D2).** Which lines does a flat view show? A: the exact lines of the sheet, from a new query on the same code, read
   off the frame thread. B: the hidden edges of the GPU for each body. C: none; the sheet page only.
3. **Q3 (D3).** When does a flat view look like the sheet? A: with a switch "Lines" (key L). B: always, on top of the
   shaded body. C: always, as the only look.
4. **Q4 (D4).** How does a person add a view? A: "Add to sheet" and "Standard views" in the net of the views. B: "Insert
   view" on the sheet page only. C: only the agent.
5. **Q5 (D5).** Where does a new view go? A: the app computes the place from the projection. B: the person taps the
   paper. C: the centre of the paper.
6. **Q6 (D6).** How does a person change a scale? A: a new command ScaleSheetView, one step of undo, the dimensions stay.
   B: a remove and an add. C: no change after the add.
7. **Q7 (D7).** How does a dimension go on the sheet? A: "Put on sheet" on a value chip of a flat view. B: a tap on an
   edge on the sheet page. C: only the agent.
8. **Q8 (D8).** How does a proposal of the agent for a sheet show? A: amber boxes and values on the sheet page. B: text on
   the arc until R25. C: no proposal; direct commands.
9. **Q9 (D9).** Where are the export buttons? A: one button "Export" on the sheet page with PDF, SVG and DXF. B: three
   buttons in the left panel. C: no button.
10. **Q10 (D10).** Which order? A: P0.162, then R30a to R30f, then K10. B: K10 first. C: R30a only before K10.
11. **Q11 (D11).** How does a pictorial view come? A: an exact frame of whole numbers of one length, after E211; this
    closes R-0068. B: a sine and a cosine with a tolerance, and an ADR. C: no pictorial view.
12. **Q12 (D12).** How does a section view come? A: after R26, from the cut plane of the window, with an ADR and a contract
    question. B: a cut line on a view of the sheet, before R26.

What the answers change, with all A:

- The roadmap gets rows R30a to R30f after P0.162 and before K10, and the paragraph of the order changes.
- R30b adds a command (ScaleSheetView) with its ADR; R30a adds a query. No change of `Engine.Contracts`.
- R-0068 gets its exit (D11 A) and closes in the slice that proves the frame. This frees one place of the register.
- R-0069 stays: R30a and R30c read off the frame thread, and a cache waits for a measure.

---

## 10 The criteria to judge the slices

- **C1.** For the plate of 2026-11-17, the front view of the window with "Lines" on shows the two dashed lines of the hole,
  as the PDF does. A test of the pixels proves it.
- **C2.** From an empty sheet, three taps give the front, the top and the side view at a scale of ISO 5455 on A3, at the
  places of first angle.
- **C3.** The sheet page and the PDF of the same sheet give the same views at the same places, within one pixel of the
  paper at the size of the window.
- **C4.** A read of a sphere in a view does not stop the frames of the window: the window draws at least 30 frames in each
  second while the read runs.
- **C5.** Each action of the sheet page is one command and one step of undo.
- **C6.** The phone at 1440 by 3088 shows the whole paper of A3 in the width, and each action has a target of 44 pixels.
- **C7.** A proposal of the agent with three views shows three amber boxes at their places before the accept.

---

## 11 Not verified

- I did not run the desktop. The statements about the window come from the code, the ADRs and the four pictures of
  2026-11-17.
- I did not verify that F reached the window in the run of 2026-11-17 (G11).
- The web statements come from search summaries, not from pages that I read (**[S]**), and one from memory (**[M]**). The
  Plasticity summary came from a copy of its documentation on a third-party host.
- I computed the frame of D11 A by hand. No command ran with it.
- The size of the paper on a phone in a half panel (D1 B) is an estimate.

## 12 Sources

The repository, at `22931ad`: `CLAUDE.md`; `docs/CURRENT-STATE.md` entries v0.377 to v0.385; ADR-0092, ADR-0109,
ADR-0118, ADR-0137, ADR-0138, ADR-0141, ADR-0142, ADR-0146, ADR-0153, ADR-0155, ADR-0158 to ADR-0161;
`docs/reviews/2026-11-17-codebase-review.md` (E211, E212); `docs/register.md` (R-0068, R-0069); `docs/roadmap.md` (rows
R21, R22, R22a to R22d, R26, P0.162, K10, and the order); `3DEngine.App/Ui/DesktopUi.Views.cs`,
`3DEngine.App/Ui/DesktopUi.Command.cs`, `3DEngine.App/EdgeValues.cs`, `3DEngine.App/SceneRenderer.cs`,
`Engine.Core/Drawing/SheetLayout.cs`, `Engine.Core/Drawing/ViewEdges.cs`.

The evidence of 2026-11-17, in the scratchpad of that session: `plate-sheet.pdf`, `app-front.png`, `app-top.png`,
`views.ps1`.

The web, by search (**[S]**):

- Onshape help, drawing views: https://cad.onshape.com/help/Content/drawings-views.htm
- Onshape help, drawing section view: https://cad.onshape.com/help/Content/Drawing/drawing_section_view.htm
- Onshape tech tip, named views in a drawing: https://www.onshape.com/en/resource-center/tech-tips/use-named-views-assembly-cutaway-drawing
- Shapr3D support, 2D drawings: https://support.shapr3d.com/hc/en-us/articles/7874466328348
- Shapr3D product page, 2D drawings: https://www.shapr3d.com/product/2d-drawings
- Plasticity, Export Hidden Line (third-party copy): https://plasticity-document-revisions.vercel.app/plasticity-essentials/export-hidden-line
- Zoo glossary, drawing: https://zoo.dev/docs/glossary/drawing
- ISO 128-24 sample (line types 02.1 and 04.1): https://cdn.standards.iteh.ai/samples/29095/b2e50c3f669649ae9387373c7a73346d/ISO-128-24-1999.pdf
- ISO 7200 sample (fields of a title block): https://cdn.standards.iteh.ai/samples/35446/d3b0887cb4fa47f49f8718807d3b8903/ISO-7200-2004.pdf
- Lines, lettering and dimensions (centre marks, ⌀, sections): https://pressbooks.atlanticoer-relatlantique.ca/lined/?p=78
- ISO 2768 status: https://www.wevolver.com/article/iso-2768-general-tolerances-tables-classes-current-status

---

## 13 The second request of 2026-11-18: drag, section, lines and conventions

After the first visual page, the owner asked for one coherent view and a drag to move the views. The owner also asked for
a section view, hidden and construction lines for a cylinder and a circle, and a check of the usual conventions.

**The coherent view.** The page is now one working prototype: the window mode and the sheet mode share one state, a view
added in the window shows on the sheet, and "Go to" on the sheet opens that flat view. The same page shows the desktop and
the phone layout. Each action names the command that the app sends, and Undo works on each step.

**How I read "construction line".** For a cylinder and a circle, a drawing shows centre lines: the axis of a cylinder and
a cross on the centre of a circle, as a long-dash-dot thin line. A construction line of a sketch is a help to design, and
a drawing does not print it. The engine prints none today, which is correct.

### 13.1 The conventions that the sheet misses

The rules come from search summaries and memory (**[S]**, **[M]**), not from the paid standards.

| Convention | The rule | The engine today | Where |
|---|---|---|---|
| Visible and hidden lines | ISO 128: visible thick 0.5; hidden dashed thin 0.25, type 02.1 **[S]** | Yes, in the three files | Have |
| Hidden lines on or off for each view | A hidden line shows only where it adds information; a section shows none **[M]** | Each view shows them | D14 |
| Centre lines and centre marks | Type 04.1, long-dash-dot thin, on each axis and as a cross on each circle, 2 to 3 mm past the outline **[S]** | None | D15 |
| ⌀ for a whole circle | ISO 129-1: a whole circle takes ⌀ and its diameter; R is for an arc **[S]** | A radius constraint on a hole prints "R8" | D17 |
| Dimension lines | Thin dimension and extension lines, arrows, text 3.5 mm **[M]** | A number at a place | R22d |
| Hole callout | "⌀16" for a through-hole; "2× ⌀8 ↧12" for a pattern and a depth **[S]** | None | After D17 |
| Section view | A cut line (chain, thick ends, arrows, letters), a hatch at 45°, the label A–A, no hidden lines **[S]** | None; ADR-0118 item 7 puts the hatch out | D16 |
| Views on lines of projection | A projected view stays aligned with its parent **[M]** | The place is free | D13 |
| Symbol of the projection | ISO 5456-2 symbol in the title block **[M]** | The text "First angle" | D18 |
| Title block | ISO 7200: legal owner, identification number, date of issue and sheet number are mandatory; revision and others optional **[S]** | Name, paper, projection, scale, unit | D18 |
| General tolerances | A note such as "ISO 2768-1 m" **[S]**; ISO 2768-2 is withdrawn **[S]** | None; ADR-0118 item 7 puts tolerances out | D18, as a field |
| Centring marks | A mark at the middle of each side of the frame **[M]** | Frame only | D18 |
| Scale of a view | The scale under a view whose scale is not the scale of the sheet **[M]** | Title block only, or "as shown" | R30b |
| Pictorial view | One pictorial view helps the reader **[M]** | None (R-0068) | D11 |
| Detail view, surface texture, threads | ISO 21920, ISO 6410 **[M]** | None | Later; out of the record |

### 13.2 The new decisions

**D13 The drag of a view.**

- **A. The client keeps the alignment, and one drag is one command.** Top and a section slide on the vertical line of
  Front, and Right slides on its horizontal line. A drag of Front takes its projected views with it. Shift, or "Keep
  aligned" off, moves a view free. A drag sends one new command, MoveSheetViews, with each moved view and its place: one
  step of undo. The command lives in `Engine.Core`; no contract change.
- B. The record holds the parent of each projected view (a new field of `SheetView`, a contract change), and the read
  places a child from its parent. The engine keeps the alignment for the agent too.
- C. Each view moves alone, with MoveSheetView for each, and nothing keeps the alignment.

**Recommendation: A.** The agent uses the same command. B can come later if the agent breaks the alignment.

**D14 The hidden lines of each view.**

- **A. A flag of each view, on by default and off for a section.** A new field of `SheetView` and a command that sets it:
  a contract change, an ADR and a question first. GetSheet and the three files leave out the hidden lines of a view with
  the flag off.
- B. One flag for the whole sheet.
- C. Always on, as today.

**Recommendation: A.** A drawing often shows the hidden lines in one view only.

**D15 Centre lines and centre marks.**

- **A. GetSheet gives them, and the three files draw them.** A new kind of line, "centre", from each whole circle that
  faces the view (a cross) and from each cylinder seen from the side (its axis), 3 mm of paper past the outline. The
  curves of the design already give the centre and the axis (ADR-0143). A flag of each view, as in D14, turns them off.
- B. The window draws them, and the files do not.
- C. None.

**Recommendation: A.** One source for the window, the page and the files, as in D2.

**D16 The section view.** This question replaces Q12 if the owner agrees.

- **A. A section of a sheet now, as R30h, before R26.** On the sheet page a person taps a view, then "Section", and
  drags the cut line; it snaps to the centre of a circle. A section view takes a cut plane (a point, and a normal along
  the right or the up of its parent, exact) and a letter: new fields of `SheetView`, a contract change, an ADR, and a
  change of ADR-0118 item 7, which puts the hatch out. GetSheet gives the cut faces as hatch regions and the lines behind
  the plane. "Section by a plane" is one of the six operations of the charter (`docs/CHARTER.md` line 161); no code gives
  it yet. R26 later uses the same cut in the window.
- B. Keep Q12 A: a section comes after R26, from the cut of the window.

**Recommendation: A.** The owner asked for it, and a part with a hole is clear only in a section.

**D17 The diameter of a whole circle.**

- **A. A dimension of a radius whose edge is a whole circle prints ⌀ and the diameter.** "⌀16" for the hole. An arc
  keeps "R". The label of the read changes from "radius" to "diameter" for such a dimension.
- B. Print the value of the constraint, as today.

**Recommendation: A.** "R8" on a hole is wrong by ISO 129-1.

**D18 The title block.**

- **A. The fields of ISO 7200 in the sheet record, set by one command, and the symbol of the projection.** Title,
  identification number, revision, date of issue, sheet number, legal owner, material, creator and a note of the general
  tolerances. A contract change and an ADR. The frame gets the centring marks.
- B. The symbol of the projection and the centring marks only; the fields later.
- C. No change.

**Recommendation: A.** A workshop cannot file a drawing that has no number and no date.

### 13.3 The slices again

| Slice | Content | After | Size |
|---|---|---|---|
| R30a to R30f | As in section 8; R30c adds MoveSheetViews and the alignment (D13) | — | 9–11 |
| R30g | The flag of each view (D14), the centre lines (D15), and ⌀ for a whole circle (D17), in GetSheet and the three files | P0.162 | 1–2 |
| R30h | The section view (D16): the cut, the hatch and the label, in GetSheet, the files and the page | R30c, R30g | 2–3 |
| R30i | The title block of ISO 7200, the symbol and the centring marks (D18) | R30c | 1 |

R30g changes only the engine and the files, so it can run beside R30a to R30c. The contract changes of D14, D16 and D18
can share one ADR and one question to the owner.

### 13.4 The questions

13. **Q13 (D13).** How does a view move? A: a drag, aligned with Front, one command MoveSheetViews. B: a parent field
    in the record. C: each view alone.
14. **Q14 (D14).** Hidden lines? A: a flag of each view (contract change). B: one flag for the sheet. C: always on.
15. **Q15 (D15).** Centre lines and centre marks? A: in GetSheet and the three files, with a flag of each view. B: in the
    window only. C: none.
16. **Q16 (D16).** The section view? A: R30h now, before R26 (replaces Q12 A, changes ADR-0118 item 7). B: keep Q12 A.
17. **Q17 (D17).** A whole circle? A: ⌀ and the diameter. B: the value of the constraint.
18. **Q18 (D18).** The title block? A: the fields of ISO 7200 by one command, the symbol and the centring marks. B: the
    symbol and the marks only. C: no change.
19. **Q19.** The order? A: P0.162; R30g beside R30a to R30c; then R30h; then R30d, R30e, R30f and R30i; then K10. B: R30a
    to R30f first, then R30g to R30i after K10.

### 13.5 Not verified in this section

- The rules of the standards come from search summaries and memory.
- The prototype computes its lines in the page; the engine gives none of the new lines yet.
- I did not measure the cost of a section in the owned kernel.

### 13.6 Out of this file: the size of the desktop window

The owner asked why the desktop window has a fixed size. `3DEngine/DesktopEngineHost.cs` line 27 creates the window with
`new Window("3DEngine Desktop", 1280, 720)` and no flag. `3DEngine.Vulkan/Window.cs` lines 64 to 66 add
`SDL_WINDOW_RESIZABLE` only for the flag `WindowFlags.Resizable`. So the window has no maximize button and no resize
border. `3DEngine.Vulkan/Presenter.cs` makes the swapchain again when the surface changes (lines 113 to 121 and 174 to
176). ADR-0113 tested the layout at fixed sizes with pictures, not with a live resize. This is a separate task, not a
part of the 2D work.
