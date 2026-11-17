# Answer sheet of 2026-11-18 — the workflow of the 2D views

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Prepared by a side session, with `docs/proposals/2026-11-18-2d-view-workflow.md` (TASK-0402).
- Each question has its options. Option A is the recommended option.
- To answer, write one letter for each question. "All A" is a valid answer.
- The detail of each question is in section 5 of the design file, under the decision that the column "D" names.

## The answers of the owner

The owner answered on 2026-11-18, in the chat of the side session: "1a 2a 3a 4a 5a/b 6a 7a 8a 9a 10a 11a 12a".

| Questions | Answer |
|---|---|
| Q1 to Q4, Q6 to Q12 | A for each |
| Q5 | **A and B**: the app computes the place of a new view from the projection, and the person can also tap the paper to give the place |

The session reads Q5 "a/b" as both options together: the computed place is the default, and a tap on the paper is a
second path. The task that records the answers asks the owner if this reading is wrong.

A later task records the answers in an ADR and puts the rows R30a to R30f in the roadmap. Until then, this table is the
record. The column "Answer" below repeats it.

## The questions

| Q | D | The question | A (recommended) | B | C | Answer |
|---|---|---|---|---|---|---|
| Q1 | D1 | Where does the sheet show | A sheet page that replaces the scene, on the desktop and the phone | A panel beside the scene | The paper in the 3D scene | A |
| Q2 | D2 | Which lines does a flat view show | The exact lines of the sheet, from a new query on the code of GetSheet, read off the frame thread | The hidden edges of the GPU for each body | None; the sheet page only | A |
| Q3 | D3 | When does a flat view look like the sheet | With a switch "Lines" (key L): light background, no fill, no sketch outline | Always, on top of the shaded body | Always, as the only look | A |
| Q4 | D4 | How does a person add a view | "Add to sheet" and "Standard views" in the net of the views | "Insert view" on the sheet page only | Only the agent | A |
| Q5 | D5 | Where does a new view go | The app computes the place from the projection (first angle or third angle) | The person taps the paper | The centre of the paper | A and B |
| Q6 | D6 | How does a person change a scale | A new command ScaleSheetView: one step of undo, the dimensions stay | A remove and an add | No change after the add | A |
| Q7 | D7 | How does a dimension go on the sheet | "Put on sheet" on a value chip of a flat view | A tap on an edge on the sheet page | Only the agent | A |
| Q8 | D8 | How does a proposal of the agent for a sheet show | Amber boxes and amber values on the sheet page | Text on the arc until R25 | No proposal; direct commands | A |
| Q9 | D9 | Where are the export buttons | One button "Export" on the sheet page: PDF, SVG, DXF | Three buttons in the left panel | No button | A |
| Q10 | D10 | Which order | P0.162, then R30a to R30f, then K10 | K10 first, then R30 | R30a only before K10 | A |
| Q11 | D11 | How does a pictorial view come | An exact frame of whole numbers of one length, after E211; it closes R-0068 | A sine and a cosine with a tolerance, and an ADR | No pictorial view | A |
| Q12 | D12 | How does a section view come | After R26, from the cut plane of the window, with an ADR and a contract question | A cut line on a view of the sheet, before R26 | — | A |

## The slices with all A

| Slice | Content | Size |
|---|---|---|
| P0.162 | E211 to E217 of the review of 2026-11-17 (E211 and E212 first) | 1–2 |
| R30a | The lines of the sheet in a flat view, with the switch "Lines" | 2 |
| R30b | "Add to sheet", "Standard views", the pad of the paper and the scale, ScaleSheetView | 2 |
| R30c | The sheet page: paper, views, values; Go to, Scale, Remove, drag; the phone | 2–3 |
| R30d | The button "Export": PDF, SVG, DXF | 1 |
| R30e | "Put on sheet" on a value chip | 1 |
| R30f | The proposal of the agent on the sheet page | 1–2 |

R30d, R30e and R30f can run at the same time after R30c.

What the answers change, with all A:

- The roadmap gets rows R30a to R30f after P0.162 and before K10.
- R30a adds a query and R30b adds a command, each with its ADR. No change of `Engine.Contracts`.
- R-0068 gets its exit (Q11 A), which frees one place of the register when a slice proves it.
