# The support of the lamp as the second case of K10 (K10b)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-25. TASK-0466, phase K10b.
- Decision: on 2026-11-25 the owner asked for a recommended size of the lamp. The session gave stand-in sizes, and the owner
  answered "use the stand-in sizes, start K10b after R32a". This replaces the answer Q2 A of the same day ("the owner gives the
  sizes") until the owner measures the real lamp. Register entry R-0070 stays open for the real sizes.
- The builder: `Engine.Tests/Assembly/LampSupportCase.cs`. The test: `Engine.Tests/Assembly/LampSupportCaseTests.cs`.
- The case: `docs/proposals/2026-11-12-assembly-with-ai.md`, section 1. The plan: `docs/proposals/2026-11-15-plan-k10-the-first-assembly-row.md`,
  section 3.

## 1 The sizes

Two kinds of size are in the table:

- **Stand-in:** the context of the owner, which the owner accepted on 2026-11-25 until the owner measures it: the crossbar, its
  two fixing points, the lamp, the housing and the cable. A physical test (R24, K14) needs the real sizes.
- **First size:** a part of the mechanism, which the session chose so that the parts fit: the bracket, the arm and the knob.
  The parametric loop (P12) and the scenario of R24 change them.

The axes: z is up, and the lower face of the crossbar is at z = 0. The crossbar runs along y. The axis of the pivot is along y
through the point (15, y, -20), so the lamp tilts down in the plane x-z. At 0 degrees the arm points along +x.

| Body | Kind of size | Shape | Box: minimum (x, y, z) to maximum (x, y, z), mm |
|---|---|---|---|
| Crossbar | Stand-in | Square tube 30 × 30 mm, wall 2 mm, length 600 mm; two holes of 5.5 mm (M5) on the vertical, at y = -20 and y = +20 | (-15, -300, 0) to (15, 300, 30) |
| Bracket | First size | Plate 30 × 60 × 5 mm under the crossbar with the two holes of 5.5 mm; a lug 20 × 6 mm down to z = -30 with a hole of 6.5 mm on the pivot | (-15, -30, -30) to (25, 30, 0) |
| Arm | First size | 80 × 6 × 20 mm, from 10 mm behind the pivot to the lamp, with a hole of 6.5 mm on the pivot | (5, -9, -30) to (85, -3, -10) |
| Knob | First size | A shaft of 6 mm, 13 mm long, through the arm and the lug; a collar of 12 mm that presses on the arm; a head of 24 mm, 8 mm long | (3, -15, -32) to (27, 9, -8) |
| Lamp | Stand-in | Cylinder of 70 mm, 100 mm long, on an axis along x at the end of the arm | (85, -41, -55) to (185, 29, 15) |
| Housing | Stand-in | Box 40 × 80 × 25 mm on the crossbar | (-20, -40, 30) to (20, 40, 55) |
| Cable | Stand-in | Cylinder of 5 mm, a straight piece of 90 mm from the housing to the front, above the crossbar; the real path is not known | (20, -8.5, 30) to (110, -3.5, 35) |

The box of a body of plane outside faces is exact. The knob, the lamp and the cable have a curved outside face, so their box is
the box of their facets, within 0.5 mm of the table. The mount (the bracket, the arm and the knob) goes down to z = -32, inside
the envelope of 60 mm under the crossbar at 0 degrees. K13 checks the envelope over the travel.

## 2 The contacts at 0 degrees

Six contacts are intended: the bracket on the crossbar, the arm on the lug of the bracket, the collar of the knob on the arm,
the lamp on the end of the arm, the housing on the crossbar, and the cable at the housing. Each other pair is apart. The shaft
of the knob is 0.25 mm from the wall of each hole of the pivot.

The pair check of K11 (`CheckInterference`, ADR-0182) answers 13 of the 21 pairs: the three intended contacts that it answers
(the lamp on the arm, the housing on the crossbar and the cable at the housing) touch, and the 10 other pairs that it answers
are apart. The bound of work of ADR-0182 item 8 refuses the other 8 pairs with `E-CHECK-WORK-LIMIT`. A
body with a hole has many pieces: the 8 pairs need from 126,968 to 2,801,550 products of pieces, above the limit of 100,000,
and the check of the whole case needs 6,133,070. The refused pairs include three intended contacts: the bracket on the
crossbar, the arm on the bracket and the knob on the arm. Row K24 removes this gap.

## 3 The gaps

The builder uses the commands that exist. It models around each gap, and a comment in the builder names it.

| Gap | How the builder models around it | Row |
|---|---|---|
| A hole command | A hole is a `Subtract` of a cylinder, which `CreateCylinder` centres on the origin | K23 (new) |
| The pair check of a body with holes | One check for each pair; 8 pairs are refused | K24 (new) |
| A turn about the axis of the pivot | Not needed at 0 degrees; a turn is a `Translate`, a `Rotate` and a `Translate` (ADR-0112 item 9) | P11 (the model of placements of Q1 A) |
| A cylinder on an axis along x or y | `CreateCylinder`, then a `Rotate` by (1, 1, 0, 0) or (1, 0, 1, 0), an exact turn of 90 degrees, then a `Translate` | None: three commands give it exactly |
| A knob of one profile | Three cylinders joined by two `Union` (K16) | K7 (a revolve, optional) |
| The size of a primitive | Not needed for the build; the loop of R24 changes the diameter of the collar | P12 |
| The role of a body (context, to print) | The lamp, the housing and the cable are bodies as the others | P14 |
| The thread of the knob and the screws of the fixing points | Not modelled | P19, K18 |

K10 names five expected gaps (plan, section 3, K10c): a union, a revolve, a hole, the size of a primitive, and the turn about an
axis. K16 gave the union. The pair check of bodies with holes is a sixth gap, which only this case found.
