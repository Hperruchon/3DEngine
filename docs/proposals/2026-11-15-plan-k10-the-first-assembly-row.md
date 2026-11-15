# Plan of the first assembly row after P10 — K10, the reference case, and the start of P11

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-15. Prepared by a side session.
- Answered on 2026-11-15, in the chat: option A for Q1 to Q6 (K1 to K6 of the answer sheet) and for the four early
  questions of P11 (J1 to J4). Decision A2 of the design file took option C, so J1 and J3 wait for the measure of K10c.
- Sources that I read on `origin/p10-1-sheet-contract` (commit 5136d05): `docs/roadmap.md` rows P10 to P14, K9a to K14,
  R22d, R23 and the paragraph of the order (lines 634-644, 742-743, 790-797, 860-870); ADR-0126;
  `docs/proposals/2026-11-12-assembly-with-ai.md`; the list of the files `Engine.Core/Commands/*Command.cs`; the comment of
  `CreateCylinderCommand.cs`; the headings of ADR-0112; the list of `testdata/`. Also the memory note
  `assembly-ai-decisions-2026-11-12.md` of the owner.
- Section 7 lists each statement that I did not verify.

## 1 Which row is first

The first assembly row is **K10**, not P11. ADR-0126 item 8 gives the order: K10, P11, P13, R23, K11, K12, P14, K13, P12,
R24, then K14. The roadmap row of P11 says "After K10" (line 641), and the row of K10 says "After P10c" (line 793). P13 can
run with P11.

So "after P10" means after P10c, the PDF export. The rows between now and K10 are: P10.2, P10.3, P10a (SVG), P10b (DXF),
P10c (PDF), and the codebase reviews that the gate makes due. R22d needs P10 (line 742) and is not on the path to K10.
P10a to P10c have estimates of 1–2, 2 and 2–3 sessions (lines 638-640).

K10 is in track K, so it is a product milestone for the review counter.

## 2 What K10 must give

Row K10 (line 793): "The reference case in the engine: the crossbar with its two fixing points, the bracket, the arm and the
knob, and the volumes of the lamp, the housing and the cable, with the commands that exist; each missing feature (a revolve,
a union, the size of a primitive, a hole) gets its row (ADR-0126). After P10c. 1–2."

Its acceptance in the proposal (section 4): "The case opens in the window; each missing feature has a roadmap row."

The reference case (proposal, section 1): the mount uses the two fixing points that exist; the lamp tilts down from 0 to 150
degrees; the mount locks under the weight of the lamp; it stays in 60 mm under the crossbar, with the housing and the cable
excluded; the parts of the mechanism can be printed.

The commands that exist on the branch (the list of `Engine.Core/Commands/`): `CreateBox`, `CreateCylinder`, `CreateCone`,
`CreateSphere`, `CreateTorus` (its record is in `CreateSphereCommand.cs`), `ImportStl`, `CreateSketch` with points, lines,
circles, arcs and constraints, `Extrude`, `SetExtrudeDistance`, `SetSketchConstraintValue`, `Subtract`, `Translate`,
`Rotate`, `RenameBody`, `SetBodyVisibility`, `CreateDatum`, `AddSketchReference`. No `Union`, no `Revolve`, no hole command,
no fillet, and no command that changes the size of a primitive. `CreateCylinder` makes a cylinder "with its centre at the
origin and its axis along +z" (comment of `CreateCylinderCommand.cs`).

What the existing commands can and cannot model:

| Part | With the existing commands | Gap |
|---|---|---|
| Crossbar | A box, or an extrude of its profile; the two fixing holes by `CreateCylinder`, `Translate` and `Subtract`. | None for a fixed context part. |
| Lamp, housing, cable | Boxes and cylinders of the measured sizes, or `ImportStl` meshes. A mesh body takes no move and no edit (ADR-0083 item 4). | The role "context" or "excluded" waits for P14. |
| Bracket | A sketch and an extrude; holes by `Subtract` of cylinders. | A hole command (row named in K10). |
| Arm | A sketch and an extrude; the pivot hole by `Subtract`. | A turn about the pivot axis: `Rotate` turns about the origin only (ADR-0112 item 9). |
| Knob with a collar | Two extrudes of two circles, as two bodies. | A union or a revolve, to make one body. |
| Edits of the loop (P12) | `SetSketchConstraintValue`, `SetExtrudeDistance`. | The radius or the height of a `CreateCylinder` body. |

## 3 The slices of K10

Each slice is one task. K10 changes no contract: it uses commands that exist. If a slice needs a new command or a new field
of `Engine.Contracts`, it stops and asks (`CLAUDE.md`, "Stop and ask").

### K10a — The sizes of the case (governance, no code)

- Content: a table of the sizes of the case in `docs/proposals/`: the crossbar section and length; the place and the
  diameter of the two fixing points; the volume of the lamp, the housing and the cable; the first sizes of the bracket, the
  arm and the knob; the axis of the pivot; the 60 mm limit and its reference face.
- Needs: the measurements from the owner (Q1). The repository has no file of the case: `testdata/` holds only
  `stl/bracket.stl` and `stl/bracket-damaged.stl`.
- Exit: the owner accepts the table.

### K10b — The case as commands

- Content: one builder in `Engine.Tests` (for example `Engine.Tests/Assembly/LampSupportCase.cs`) that sends the commands of
  the case through a `DocumentSession`, and a test (for example `LampSupportCaseTests.cs`) that checks:
  - each body exists with its name (`RenameBody`);
  - the bounding box of each body agrees with the table of K10a (`GetBoundingBox`);
  - the arm at 0 degrees and the knob do not overlap at the place that the table gives (a check by bounding boxes only:
    the interference query is K11);
  - a save and an open give the same bodies.
- Needs: K10a; the commands that exist. Each gap of section 2 is modelled around (two bodies for the knob, holes by
  `Subtract`) and named in the test with a comment that names its future row.
- Exit: the test passes on the three operating systems in continuous integration.

### K10c — The case in the window and the rows of the gaps

- Content: the test of K10b saves a document file; the desktop opens it; a screenshot of the window goes to the owner (memory
  "Visual checks by screenshot"). One roadmap row for each gap that K10b found, each with its estimate and its place in the
  order of ADR-0126. Expected rows: a union, a revolve (or a decision that the union is enough), a hole, the size of a
  primitive, and the turn about an axis that is not through the origin (P11 can take it, see section 4).
- One measure for P11: a test turns the arm by the three commands of ADR-0112 item 9 (`Translate`, `Rotate`, `Translate`)
  for 150 steps of one degree, and records the time of the rebuild and of a replay, and the count of bodies. ADR-0126 item 3
  asks the ADR of P11 to study the length of the log; this gives it a number.
- Exit: the owner sees the picture; each gap has its row; the ledger has the entry of K10.

Estimate: K10a is short and waits for the owner; K10b and K10c fit the 1–2 sessions of the row if no gap blocks the model.

## 4 After K10: the slices of P11 (for the order only)

P11 changes the Document and the contract: a joint is a new record (ADR-0126 item 2 and 3). So P11 starts with its ADR and
questions to the owner, as P10 did with R-0067. A proposed split, each after the one before it:

1. **P11.0 — The study of D3 (ADR only).** The answers of ADR-0126 item 3: one command for each move; the identity of a body
   through the chain of `Rotate` and `Translate`; the turn about an axis that is not through the origin; the length of the
   log, with the measure of K10c; the check of a travel is a query that writes nothing. Questions to the owner.
2. **P11.1 — The record of a joint.** A collection of joints in the Document (a fixed joint and a pivot between two bodies),
   the events, and the commands to create and remove a joint, with undo and redo. A change of `Engine.Contracts`.
3. **P11.2 — The axis and the limits.** The axis of a pivot from the geometry by identity (a cylindrical face or an edge, as
   a datum follows its body, ADR-0112 items 6 and 7); the two limits, each as a cosine and a sine (determinism rule 2).
4. **P11.3 — The turn and the states.** A command turns the moving body of a pivot by a quaternion; it refuses a turn past a
   limit and a turn in the tight state. A query gives each joint, its angle now, its limits and its state for the AI.

P13 (requirements) can run beside P11 (line 643). It also changes the contract, so it has its own ADR and questions.

## 5 Questions for the owner

Questions for K10 (the recommended option first):

**Q1 — The data of the case.**
- A (recommended): the owner gives the measured sizes (crossbar section, the two fixing points, the volumes of the lamp,
  the housing and the cable, the 60 mm reference face). The agent builds each part as native bodies. Native bodies can move
  and change, and the physical load test needs the real fixing points.
- B: the owner gives STL files of the crossbar and the lamp. They become mesh bodies (no move, no edit, ADR-0083 item 4).
- C: the agent invents a plausible case. It cannot match the physical test of R24.

**Q2 — Public data.** The repository is public. The sizes and any file of the case go into it.
- A (recommended): the sizes go into the repository (a table and a builder). No photo and no STL of the real lamp.
- B: the data stays outside the repository. Continuous integration then cannot run the case.

**Q3 — The form of the case in the repository.**
- A (recommended): a builder that sends commands, in `Engine.Tests`. It stays valid when the schema of a command grows, and
  a test proves it on three operating systems.
- B: a committed document file. A later change of a command can need a migration of the file.

**Q4 — A missing feature during K10.**
- A (recommended): model around it (two bodies for a knob with a collar, holes by `Subtract`) and give it its row, as
  ADR-0126 and row K10 say. K10 stays at 1–2 sessions.
- B: stop K10 and build the feature first.

**Q5 — The turn about the pivot in K10.**
- A (recommended): K10 builds the case at 0 degrees, and one test measures 150 turns by `Translate`, `Rotate` and
  `Translate` (section 3, K10c). The number goes into the study of P11.0.
- B: no turn in K10; P11.0 measures it.

**Q6 — The place of K10 in the order.** K10 uses no work of P10.
- A (recommended): keep the order of the owner of 2026-11-12: K10 after P10c.
- B: start K10a now (the sizes only, no code), so the measurements are ready when P10c closes. K10a needs no session of
  code, only the answers of the owner.

Questions that P11.0 will ask (given here so the owner can think about them early):

- **The turn about the axis of a pivot.** A (recommended): a new command that turns a body by a quaternion about a point,
  one body for each move. B: three commands for each move (ADR-0112 item 9), three bodies for each move.
- **The record of a joint.** A (recommended): one record for each joint in a new collection of the Document, as a sheet is
  (ADR-0153). B: a field of the body.
- **The angle now.** A (recommended): computed at each read from the turns in effect since the joint was made. B: stored in
  the record of the joint, and changed by each turn.
- **The limits.** A (recommended): each limit as a cosine and a sine in the command (determinism rule 2). An angle in degrees
  is not permitted in the log.

## 6 Stop and ask

- K10 needs no change of `Engine.Contracts` with the recommended options.
- P11.1, P11.2, P13 and P14 each change the public shape of `Engine.Contracts` (a new collection, new event kinds, new fields
  of a body). Each needs its ADR and the answers of the owner before code.
- A new command for a turn about a point (the first question of P11.0) is not a change of `Engine.Contracts` by itself,
  because commands are in `Engine.Core/Commands/`. But it amends ADR-0112 item 9, so it needs the owner.

## 7 Not verified

- I read the roadmap and the ADRs on the branch `origin/p10-1-sheet-contract`, not on `main` after the merges of the other
  session. A merge can change the line numbers and the rows.
- The sizes of the real case: no file of the repository holds them. A search for "crossbar" and "lamp" in the files of the
  branch gives files of the window, the tests of images, and `docs/CURRENT-STATE.md`; I did not read those hits.
- That `GetBoundingBox` gives an exact box for a body after `Subtract` and `Rotate`.
- That the desktop opens a document file that a test saved. I did not run the desktop.
- That a test can turn a body 150 times with three commands each in a reasonable time. That is the purpose of the measure
  of K10c.
- That an interference query (K11) can read a mesh body. This matters only for option B of Q1.
- I did not run a build or a test. This plan changes no code.
