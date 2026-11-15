# Answer sheet of 2026-11-15 — the questions for the owner

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Prepared by a side session. Two files that this sheet names are not in the repository: the plan of P10.3 (the task of
  P10.3 copies it) and the sheet of the questions of P10.2 (ADR-0155 records the answers). `prep-p10-2.md` was a working
  file of the main session.
- Each question has its options. Option A is the recommended option, unless the line says a different thing.
- To answer, write one letter for each question. "All A" is a valid answer for a block.
- The detail of each question is in the file that the block names.

## The answers of the owner (2026-11-15, in the chat)

The owner answered: "All A, C for A2".

| Block | Questions | Answer |
|---|---|---|
| 1 — P10.3 | D1 to D7 | A for each |
| 2 — K10 | K1 to K6 | A for each |
| 3 — Early P11 | J1 to J4 | A for each. J1 and J3 apply only if A2 ends with option B, after the measure of K10. |
| 5 — Design file | A1, A3 to A15 | A for each |
| 5 — Design file | A2 | **C**: measure 150 turns in K10 first, then decide the model of a move |
| 6 — Plasticity | The order of section 6 | Accepted |

Block 4 (P10.2, Q1 to Q7) was not on this sheet, so the answer above does not cover it. The owner answered it later on
2026-11-15, from `docs/proposals/2026-11-15-questions-p10-2-the-views-of-a-sheet.md`, with "All A": A for each of Q1 to
Q7. The task of P10.2 records these answers in its ADR.

What the answers need from the owner:

- K1 and A12: the measured sizes of the lamp support, and the sizes or the file of `Bibliotheque120`.

What the answers change (TASK-0394 and ADR-0157 record them; the task of P10.3 records block 1 in its ADR):

- A11: the measure queries, the booleans, the pair check (K11) and the preview (S11) come before the joints (P11). This
  changes the order of 2026-11-12. K10 stays after P10c (K6).
- A8: the preview S11 gets its ADR for the fork and the dry run.
- A10: one undo for one proposal amends ADR-0130 item 4.
- A2: answer D3 of 2026-11-12 stays until the measure of K10.

The column "Answer" of each block below is empty; the table above is the record.

## Block 1 — P10.3, the dimensions of a sheet

Detail: `docs/proposals/2026-11-15-plan-p10-3-the-dimensions-of-a-sheet.md`, section 9.

| Q | The question | A (recommended) | B | C | Answer |
|---|---|---|---|---|---|
| D1 | Which constraints can be the source of a dimension | Kinds `distance` and `radius`, and the distance of an extrude | Also `angle` (the client computes the degrees) | — | |
| D2 | A new source or a new place for a dimension | `PlaceSheetDimension` takes an optional `dimensionId` and places that dimension again, in one step | A remove and a new place: two steps, a new identifier | Two more commands; ADR-0153 item 6 changes | |
| D3 | The same source two times on one sheet | Refuse the second one | Permit it | — | |
| D4 | The command tests that the view shows the length true | No test in the command; a later slice gives a flag in the read | The command refuses; it needs the lines of the view in each command | — | |
| D5 | The diagnostic codes | Three new codes; three existing codes get a new raise site | Two new codes; "source not in the design" and "wrong input" share one code | — | |
| D6 | A lost dimension in the read | A field `lost`, no diagnostic | Also a warning `W-SHEET-DIMENSION-LOST` | — | |
| D7 | The geometry of the dimension line (the ends on the paper) | Not in P10.3; P10a or R22d adds it | In P10.3; the slice can grow | — | |

## Block 2 — K10, the reference case of the assembly work

Detail: `docs/proposals/2026-11-15-plan-k10-the-first-assembly-row.md`, section 5.

| Q | The question | A (recommended) | B | C | Answer |
|---|---|---|---|---|---|
| K1 | The data of the lamp support | You give the measured sizes; the agent builds native bodies | You give STL files; they are mesh bodies with no move and no edit | The agent invents a case | |
| K2 | Public data | The sizes go into the public repository; no photo and no STL of the real lamp | The data stays outside; continuous integration cannot run the case | — | |
| K3 | The form of the case | A builder in `Engine.Tests` that sends commands | A committed document file | — | |
| K4 | A missing feature during K10 | Model around it and give it a roadmap row | Stop K10 and build the feature first | — | |
| K5 | The turn about the pivot in K10 | K10 builds the case at 0 degrees and measures 150 turns for the study of P11 | No turn in K10 | — | |
| K6 | The place of K10 in the order | Keep K10 after P10c | Start K10a (the sizes only) now | — | |

## Block 3 — The early questions of P11 (joints)

Detail: the same K10 plan, section 5, last part. The ADR of P11 asks them again with its study. J1 and J3 depend on
decision A2 of block 5: with a placement as data, J1 has no object and J3 is answered.

| Q | The question | A (recommended) | B | Answer |
|---|---|---|---|---|
| J1 | The turn about the axis of a pivot | A new command that turns a body about a point: one body for each move | Three commands for each move (`Translate`, `Rotate`, `Translate`) | |
| J2 | The record of a joint | One record for each joint in a new collection of the Document | A field of the body | |
| J3 | The angle of a joint now | Computed at each read from the turns in effect | Stored in the record of the joint | |
| J4 | The limits of a joint | A cosine and a sine for each limit (determinism rule 2) | — | |

## Block 4 — P10.2, the views of a sheet

These seven questions (Q1 to Q7) are in the plan of the main session, `prep-p10-2.md`. This sheet does not repeat them.
`docs/proposals/2026-11-15-questions-p10-2-the-views-of-a-sheet.md` gives each question with its facts, its options and
its effect on the plan of P10.3.

## Block 5 — The assembly and agent design file

Detail: `docs/proposals/2026-11-15-assembly-and-agent-design.md`, section 12. These decisions come before any task. A2 and
A11 can change a decision of 2026-11-12 (answer D3, and the order of the rows), so read sections 4 and 11.2 first.

| # | The decision | A | B | C | Answer |
|---|---|---|---|---|---|
| A1 | The scope of the assembly track | One Document first, then instances, then part references | The scope of ADR-0126 only | — | |
| A2 | The model of a move (**recommended: C**) | A placement as data and a joint value in the log | Keep D3: `Rotate` commands, with a turn about a point | Measure 150 turns in K10 first, then decide | |
| A3 | Constraints | Connectors and joints in a kinematic tree; no solver | Geometric mates and a 3D solver | — | |
| A4 | A connector | A named datum, plus implicit frames from a query | A new record | — | |
| A5 | Intended contacts | Data of the Document | An ignore list in the client | — | |
| A6 | A mesh body in a check | Permitted; the result says "for the mesh only" | Refused | — | |
| A7 | The rule of a gap | A requirement with a process value; class "depends on data" | A constant in the code | — | |
| A8 | The preview of a proposal (S11) | An ADR for the fork and the dry run now | Later | — | |
| A9 | A Document that changed before the accept | Run again; ask again only when the result differs | Refuse as stale | A banner only, as today | |
| A10 | The accept | Atomic, and one undo for one proposal | Atomic; N steps of undo | As today | |
| A11 | The order | Measure, booleans, pair check and preview before the joints | The order of 2026-11-12 | — | |
| A12 | The validation cases | The lamp support and `Bibliotheque120`; you give the sizes | The lamp support only | — | |
| A13 | A picture for the agent | The SVG of a view first | A rendered picture first | — | |
| A14 | The advanced checks | After the base: an order along the axes, tool access by a cylinder | None on the roadmap | — | |
| A15 | An MCP server | A guide first; a wrapper later | A wrapper now | — | |

## Block 6 — The features of Plasticity

Detail: `docs/proposals/2026-11-15-plasticity-features.md`, section 6. One question: do you accept this order for the
features that make sense? Union and intersection; the section view; the drag of a face as a parametric edit; linear and grid
patterns; the flat chamfer of one edge; the snap of a body to a body; STEP import and 3MF export later.

Answer:
