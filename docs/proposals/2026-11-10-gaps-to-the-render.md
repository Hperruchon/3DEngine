# The gaps between the window and the render "mix", and the slices that close them

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-10.
- Asked by: the owner, on 2026-11-10: "is the actual design close to the render idea or we still have work to do and
  organized for the working session?", then "Yes, write it".
- Status: accepted by the owner on 2026-11-10 (section 5). It changes no code and no ADR. The slices are the roadmap
  rows R18a to R18j; the session that opens a later slice gives it its task number and its ADR number at that time
  (gate P0.80).

## 1. What I compared

- **The render:** the four pictures of the main flow in the designer brief
  (`https://claude.ai/artifact/4dPFQPdAPF39BZQSqbgYFD`, section "UX examples"), each at 1440 by 900:
  - Mix 1: the selected bracket.
  - Mix 2: the agent proposes a width of 75 mm.
  - Mix 3: the sketch under the body, during the proposal.
  - Mix 4: the change is applied.
- **The window:** `main` at v0.311 (`343bcda`). The pictures came from the three tests of
  `3DEngine.ImageTests/MixImageTests.cs` (`mix-selected.png`, `mix-proposal.png`, `mix-sketch.png`), which draw the
  bracket of the render on the GPU at 1440 by 900.

## 2. What is already as in the render

The layout is as in the render. Each element is in its place:

- The top bar at the upper left, and the command line at the upper right (ADR-0093, ADR-0098).
- The rows of the selected body at the upper left, with no panel (ADR-0099).
- The tool ring at the right of the body, with the caption "TOOLS FOR BRACKET" (ADR-0081, ADR-0095).
- The history arc at the lower left of the ring (ADR-0104).
- The values on the edges of the body and of an active sketch (ADR-0082, ADR-0105).
- The agent arc with Reject and Accept, and a ring that dims while a proposal waits (ADR-0092).
- Undo and Redo as round anchors at the lower left, and the status line at the lower right in the mono font (ADR-0080,
  ADR-0093).
- The fonts Instrument Sans and JetBrains Mono (ADR-0091).

## 3. The gaps

Each gap has a letter. "Window" means a change in `3DEngine.App` only. "Renderer" means a new pass in the Vulkan
renderer. "Engine" and "Contract" mean a change of `Engine.Core` or of `Engine.Contracts`, which needs an ADR first.

| Gap | The render | The window at v0.311 | Kind |
| --- | --- | --- | --- |
| G1 Ground and controls | A ground that is almost black, with a soft light at the middle; thin outlined controls with no fill | A grey ground; ring segments, chips, the command line and the buttons have a filled blue ground | Window (theme) |
| G2 The body | A transparent body; bright cyan edges, the selection in orange; hidden edges dashed; a soft shadow on the ground | A solid shaded body in the colour of the selection; no edge lines; no shadow | Renderer |
| G3 Values on edges | Dimension lines with extension lines; outlined pills in the mono font, "10.000" and "60.000 mm"; each pill outside the body | Filled boxes, "10 mm"; no dimension line; two boxes on the body | Window |
| G4 The rows of a body | "Bracket" larger and bold, "Extrude · correct" in green; a row "Made from: Plate on XY" (a tap opens the sketch); "Width C3" and "Depth C4"; values "60.000 mm" | One size of text, the state in grey; no row "Made from" (ADR-0099 Q2 waits); two rows named "Length"; values "60 mm" | Window |
| G5 The ring of a body | Five outlined segments: Edit sketch, Move, Distance, Hide, Rename | Three filled segments: Move, Distance, Hide | Window |
| G6 The history arc | A drawn arc with a glow; each node on the arc, its label beside it; the selected node filled | Two round buttons with labels, next to a faint arc | Window |
| G7 The rows of a sketch | Only the constraints, with their residual ("C1 horizontal L1  r 0"); a row "Makes: Bracket"; "solved · 2 free" in green after the name | "Next point" and "Extrude" buttons, then each point, each line and each constraint; the list scrolls | Window |
| G8 The sketch under the body | A glowing plate under the body, with its label "PLATE ON XY · SOLVED · 2 FREE" and the caption "SKETCH · UNDER THE BODY IT MAKES"; dotted lines down from the body | The sketch lines cross the body; no plate, no label, no caption | Window and renderer |
| G9 Fixed controls and status | Only Undo and Redo at the lower left; after an apply, "Undo: C3 = 75" beside them; status "log 7 · all correct · agent connected · 127.0.0.1:5180"; top bar "mm · saved" or "mm · not saved"; "AGENT IS WAITING" beside the command line | "Frame" and "Home" buttons beside Undo and Redo; status "1 body · all correct · address"; top bar "mm"; "AGENT IS WAITING" in the top bar | Window |
| G10 The command line | An outlined field "Type a command or a value", a search mark and "Ctrl K" | A filled field "Type a command"; a typed value does not apply (the owner chose it on 2026-11-08, ADR-0098 Q1 B) | Window |
| G11 A proposal | The old value crossed out and the new value in amber, in the rows and on the edge; a pending node "C3 to 75 ?" on the history arc; a glow on the edge of the screen; after Accept, "Applied · Bracket rebuilt · Undo" | The arc and a box with the text of the command | Window |
| G12 The preview of a proposal | The new shape in dashed amber lines; rows "Solver after: solved, 2 free" and "Command" | No preview; the engine applies a command or nothing | Engine (a dry run) |
| G13 The author of a step | "applied · agent, accepted by you" on the node of the history | No command records its author | Contract |

I did not compare the other sizes of the brief (1920 by 1080, 1280 by 720, the tablet and the phone) or the workflows
"Create a part" and "Recover from a fault". Section 6 lists this.

## 4. The slices

A slice is one task, one ADR or one amendment, one branch, and a picture at 1440 by 900 beside the render, as for R16.
The size is in the unit of the roadmap column "Size". Slices S1 to S10 are window or renderer work, with no contract
change. S11 and S12 need a decision of the owner first.

| Slice | Gaps | What it does | Size | ADRs |
| --- | --- | --- | --- | --- |
| S1 The look | G1, part of G9, G10 | The ground of the render; ring segments, chips, the command line and the buttons as outlines; the green state and the red fault; the search mark and "Ctrl K" in the command line; "AGENT IS WAITING" beside the command line | 1–2 | Amends ADR-0078, ADR-0081, ADR-0095, ADR-0098 |
| S2 The edges of the body | G2 | An edge pass in the renderer: visible edges as bright lines, hidden edges dashed, the selection in orange; a transparent fill for the selected body; a soft shadow under it | 2–3 | New ADR |
| S3 The values | G3 | Dimension lines with extension lines for each value; pills in the mono font with three decimals and the unit; a place outside the silhouette of the body | 2 | Amends ADR-0082, ADR-0105 |
| S4 The rows of a body | G4 | The name larger and bold; the state in its colour; the row "Made from" with a tap that opens the sketch (ADR-0099 Q2); labels from the direction of each value and the name of its constraint ("Width C3") | 1–2 | Amends ADR-0084, ADR-0099 |
| S5 The ring and the history arc | G5, G6 | "Edit sketch" and "Rename" on the ring; the history arc drawn as in the render, with the nodes on the arc | 1–2 | Amends ADR-0081, ADR-0104 |
| S6 The sketch | G7, G8 | The rows of a sketch with the constraints, their residual and "Makes"; "Next point" and "Extrude" to the ring and the command line; the plate under the body with its label and its caption | 2 | Amends ADR-0085, ADR-0097 |
| S7 Fixed controls and status | G9 | Frame and Home out of the row of the anchors (see Q3); "Undo:" and the last step beside Undo; the log count and the state of the agent in the status line; "saved" or "not saved" in the top bar | 1 | Amends ADR-0080, ADR-0083, ADR-0093 |
| S8 A typed value | G10 | A number typed in the command line goes to the pad of the command, through the evaluator (ADR-0098 Q1 B, already chosen) | 1 | Amends ADR-0098, ADR-0032 |
| S9 The proposal in the window | G11 | The old value crossed out and the new value in the rows and on the edge, from the commands of the proposal; the pending node on the history arc; the glow on the edge of the screen; the message after Accept | 2 | Amends ADR-0092 |
| S10 The other sizes | — | The same comparison at 1280 by 720, 1024 by 768 and the phone, and the gaps that it finds | 1 | — |
| S11 The preview of a proposal | G12 | The engine runs the commands of a proposal on a second Document and gives the shape and the state of the solver, with no change of the log. The owner allowed a second Document in one runtime on 2026-11-09 (R-0055) | 3–5 | New ADR; engine |
| S12 The author of a step | G13 | Each command records its author, and the history shows it | 3 or more | New ADR; contract |

**Recommended order:** S1, S2, S3, S4, S5, S6, S7, S8, S9, then S10. S1 and S2 give the largest part of the look of the
render. S2 changes the renderer and S3 changes the window, so the two can run at the same time in two worktrees. S11
and S12 come after the answers to Q4 and Q5.

**Before the slices:** the tasks that are `Ready` now come first, in the order of the roadmap: TASK-0330 (P0.138, the
high finding of the review of 2026-11-09 (b): Accept of the agent arc takes a tap in the default window), TASK-0331, the
close of R17a (TASK-0314, its branch is green), then R17b (TASK-0315).

## 5. Questions for the owner

| # | Question | Options | Recommended |
| --- | --- | --- | --- |
| Q1 | Do the slices go on the task list, in the order of section 4? | A. Yes, S1 to S10 in that order. B. Another order or a subset. | **A.** The order gives the largest change of the look first. |
| Q2 | The ground of the window | A. The ground of the render, almost black. B. Keep the grey of the theme "Proposed (harmonized)" (ADR-0078). | **A.** Each render has it, and the glow of S2 needs a dark ground. Each image test that reads the clear changes with it. |
| Q3 | "Frame" and "Home" beside Undo and Redo | A. Out of the row; the command line and the keys keep them. B. Keep them there. | **A.** The render has only Undo and Redo. The command line already holds Frame. Home needs a command there. |
| Q4 | The preview of a proposal (S11) | A. An ADR for a dry run on a second Document, then a task. B. Not now. | **B** until S9 is done, then **A**. S9 shows the new values with no engine change. |
| Q5 | The author of a step (S12) | A. An ADR now. B. Later, after S11. | **B.** It is a change of the contract and of each saved log. |
| Q6 | The form of a value | A. Three decimals and the unit ("60.000 mm"), more decimals when the value has them. B. As today ("60 mm"). | **A.** Principle 3 of the brief says that a value is exact, with equal-width digits. |
| Q7 | Mode of work | A. As for R16: merge each slice after green CI and send its picture beside the render. B. Ask before each merge. | **A.** It is the mode of 2026-11-08. |

### The answers of the owner

On 2026-11-10 the owner accepted each recommended answer: Q1 A, Q2 A, Q3 A, Q4 B (then A after S9), Q5 B, Q6 A, Q7 A.
So:

1. S1 to S10 go on the roadmap as R18a to R18j, in the order of section 4, after the tasks that are `Ready` now.
2. The ground of the window becomes the ground of the render (S1).
3. Frame and Home leave the row of the anchors; the command line and the keys keep them (S7).
4. The preview of a proposal (S11) waits until S9 is done; then an ADR for a dry run on a second Document.
5. The author of a step (S12) waits until after S11.
6. A value shows three decimals and its unit, and more decimals when it has them (S3).
7. Each slice merges after green CI, and its session sends its picture beside the render.

R18a (TASK-0332, ADR-0108) and R18b (TASK-0333, ADR-0109) are open: they touch different files, so two sessions can do
them at the same time. Each later row opens its task when it starts.

## 6. What I did not verify

- I compared the four pictures at 1440 by 900 only. The brief also has 1920 by 1080, 1280 by 720, the two tablet sizes,
  the phone, and the workflows "Create a part", "Recover from a fault" and "Activity". S10 is that work.
- I did not measure the cost of an edge pass (S2) in the renderer. The faces of `GetFaces` give the edges of each body
  (ADR-0077), but a hidden line needs a second depth test.
- The labels "Width" and "Depth" in the render have no source in the engine. S4 proposes a label from the direction of
  the value; the owner can choose another rule.
- The pictures of the window come from the image tests, not from a run of the desktop program.
