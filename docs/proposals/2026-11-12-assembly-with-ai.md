# Proposal — Design and validate an assembly with the AI

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

Date: 2026-11-12. Task: TASK-0353 (P0.145). Decision record: ADR-0126.

## 1. The request

On 2026-11-12 the owner asked for a work package "design and validate an assembly with the AI", after the work in progress
and the 2D drawing track up to P10c. R18g and the current queue continue in their order. The checks R-0063 and R-0065 and the
decision R-0064 keep their state.

**The reference case** is the tilting support of a light:

- the mount uses the two fixing points that exist;
- the lamp tilts down from 0 to 150 degrees;
- the mount locks under the weight of the lamp;
- the mount stays in an envelope of 60 mm under the crossbar, with the housing and the cable excluded;
- the parts of the mechanism can be printed.

On that project several variants were made again, because a view at 45 degrees showed neither the collisions during the
turn nor the absence of a real locking link.

**The acceptance scenario.** The AI finds a collision of the knob with the arm, corrects a dimension, and then shows that
the envelope stays within 60 mm. The geometric check and the physical test of the hold under load each have their own
result.

## 2. What exists at `main` bf864f0 (v0.337)

| Capability | Exists | Missing |
|---|---|---|
| 1. Joints and states | A gizmo moves a body and gives one `Translate` (ADR-0038); `Rotate` turns a copy of a body about the origin by a quaternion (ADR-0112); a datum follows the `Translate` and `Rotate` steps of its body (ADR-0112 items 6 and 7); an assembly Document references part files at a fixed version, placed by x, y and z only (ADR-0071) | A joint; an axis taken from the geometry; limits; the states tight and loose; a turn about an axis that is not through the origin |
| 2. Collisions, gap and envelope | `Subtract`, `GetBoundingBox`, the dimension overlay (ADR-0056, ADR-0125) | An interference query; the gap between two bodies; the check of a travel; the extent below a reference face; excluded bodies |
| 3. Parametric edit | `SetSketchConstraintValue`, `SetExtrudeDistance`, the rebuild (ADR-0037), the proposal of an agent (ADR-0092) | A change of the size of a box or of a cylinder; the checks that run again after a rebuild |
| 4. Requirements and assumptions | Nothing | The whole model |
| 5. Print export | STL of all the live bodies in one file, in millimetres (ADR-0048); STEP | One file for each chosen part; a check of the mesh; the role of each body |

Two facts limit the reference case:

- The kernel boundary of the charter names six operations. Commands exist for extrude, boolean (`Subtract` only) and
  transform. No command gives a revolve, a union, a fillet, a hole or a thread. A knob with a collar needs a revolve or a
  union, unless the knob is two extrudes in one sketch.
- A body that comes from an STL file is a mesh body: it takes no move and no edit (ADR-0083 item 4). So the parametric loop
  works only on native parts made from sketches. The crossbar and the lamp can stay mesh bodies, as context volumes.

## 3. The decisions of the owner (2026-11-12)

| # | Question | Answer |
|---|---|---|
| D1 | How the work enters the charter | An objective with a date after P10c; the load check (physics) also gets a date, after the assembly work |
| D2 | Where the parts of a mechanism live | One part Document: each part is a body, and the joints connect the bodies |
| D3 | How a joint moves a part | Each move is a `Rotate`; the effect on the log is studied first (section 5) |
| D4 | What the check of a travel guarantees between two angles | Sampled with a margin: no contact between two tested angles is missed, and the first contact angle is found to a tolerance |
| D5 | Where the requirements live | In the Document, with a status |
| D6 | How a body says its role | An attribute of the body in the Document |
| D7 | The print format | One STL for each part, in millimetres, with a check of the mesh; 3MF later |

## 4. The phases and their order

Each phase opens its task, and its ADR when it changes the Document or the contract, at its start. The estimates are in
evenings.

| Phase | Content | After | Acceptance | Evenings |
|---|---|---|---|---|
| K10 | The reference case in the engine: the crossbar with its two fixing points, the bracket, the arm and the knob, and the volumes of the lamp, the housing and the cable, with the commands that exist. A list of each missing feature, each with its phase | P10c | The case opens in the window; each missing feature has a roadmap row | 1–2 |
| P11 | Joints in the Document: a fixed joint and a pivot between two bodies, an axis from the geometry by identity, limits, the states tight and loose, and the angle set by `Rotate` commands. Commands and queries for the AI | K10 | A test turns the arm by a pivot about an axis that is not through the origin, refuses an angle out of the limits, refuses a turn in the tight state, and replays the log | 3–4 |
| P13 | Requirements and assumptions in the Document: text, value, unit, references to bodies, faces or joints, and a status: measured, estimated, to verify physically, verified physically with a date and a result | P0.145; can run with P11 | The AI reads the 60 mm envelope, the travel and the hold under load with their units and states | 2–3 |
| R23 | A joint in the window: a rotation handle on the pivot and a pad for the angle; one `Rotate` at the end of a drag | P11 | A drag of 40 degrees gives one command; the preview writes nothing | 2 |
| K11 | Interference at one position: a query gives each pair of bodies that touch or overlap, where, and how deep; the window shows the contact | P11 | The knob and the arm of the case report their contact at a known angle | 2–3 |
| K12 | The travel: a query checks a pivot from one limit to the other, sampled with a margin, and gives the first contact angle to a stated tolerance | K11 | A thin part that passes through another between two samples is found; the report states its guarantee | 2–3 |
| P14 | The roles of a body (to print, context, excluded from the envelope) and the print export: one STL for each chosen part, in millimetres, with a check that the mesh is closed and has no degenerate triangle | P11 | The parts to print of the case export as separate files; the crossbar and the lamp do not | 1–2 |
| K13 | The envelope: a query gives the extent below a reference face over the travel, and names the bodies that the roles exclude | K12, P14 | The case reports its extent under the crossbar with the housing and the cable excluded | 1–2 |
| P12 | The parametric loop: the missing edits that K10 found, and the checks that run again after a rebuild, in one window | K12, K13 | A change of the diameter of the collar of the knob rebuilds the part and runs the checks again, with no STL and no manual history | 1–2 |
| R24 | The end-to-end scenario of the lamp support: the AI finds the contact of the knob with the arm, proposes a new value, runs the checks again, and shows the envelope within 60 mm. The physical load test has its own recorded result | each phase above | The scenario passes on the desktop and through the surface | 1–2 |
| K14 | The load check of a printed part (D1). Its own ADR decides the method | R24 | To decide in its ADR | to estimate |

Total before K14: about 16 to 24 evenings, and two codebase reviews.

## 5. The study of D3 (a move is a `Rotate`)

`Rotate` consumes the body and makes a new one (ADR-0021, ADR-0112). Before P11 writes code, its ADR answers these points:

- **One command for each move, not for each frame.** The gizmo of a move already shows a preview during the drag and sends
  one `Translate` at its end (ADR-0038 item 2). A turn by a joint does the same: one `Rotate` at the end of a drag or of an
  apply of the pad.
- **The identity of a part.** A datum on a face already follows its body through each `Translate` and `Rotate`
  (DatumResolver). A joint names its bodies and its axis in the same way, so it follows the chain to the live body.
- **The axis.** `Rotate` turns about the origin of the Document, and ADR-0112 item 9 says that a turn about another point
  is a `Translate`, a `Rotate` and a `Translate` back. A turn about the axis of a pivot is those three commands, or a new
  command that takes a point. The ADR chooses, and states the cost in the log.
- **The check of a travel writes nothing.** K12 turns the bodies in a query, with no command, so a check of 150 degrees adds
  no entry to the log.
- **The length of the log.** Each move adds a command and a body. A test of many moves measures the cost of the rebuild and
  of the replay; if the cost is too high, the ADR names a later compaction and records it in the register.
- **Determinism.** A turn records a quaternion or a matrix, never an angle (`CLAUDE.md`, rule 2). The client computes the
  numbers from the degrees. A rational form of the half angle gives exact numbers with `+ - * /` only.

## 6. The guarantee of D4

For a pivot, a point at the distance r from the axis moves at most r times the step of the angle between two samples. If,
at each sample, the gap between two bodies is more than the largest such movement of the bodies that turn, no contact
happens between the two samples. K12 needs a query of the gap between two bodies, not only of their overlap. Where the gap
is too small, the check takes a smaller step, and it finds the first contact angle by halving the step to a stated tolerance.
The report states the step, the largest radius and the tolerance. An exact swept volume is not in the kernel boundary.

## 7. What stays out

- A physical proof of the hold under load: the engine checks that the tight state has no free movement and that a locking
  part is declared; the hold itself is a requirement "to verify physically" with its recorded result, until K14.
- An assembly of several part files (ADR-0071): a later phase, after this work.
- 3MF: after P14 (D7).
