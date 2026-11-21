# The bookcase of the owner as the first case of K10 (K10a)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-21. TASK-0429, phase K10a.
- Decision: on 2026-11-20 the owner chose the bookcase `Bibliotheque120` as the first builder of K10. The support of the lamp
  follows when the owner gives its sizes (register entry R-0070).
- Source: the document file `Bibliotheque120.3de` of the owner, in the folder `3DProjects/Bibliotheque120` of the documents
  of the owner. The file is not in the repository. The owner accepted that the sizes go into the repository (answer Q2 A
  of `docs/proposals/2026-11-15-plan-k10-the-first-assembly-row.md`). I read the file and did not change it.
- The builder: `Engine.Tests/Assembly/BookcaseCase.cs`. The test: `Engine.Tests/Assembly/BookcaseCaseTests.cs`.

## 1 The file

The file has a header (`"lengthUnit":"mm"`, backend `manifold` 3.5.2) and 240 commands: 48 `CreateBox`, 54 `Translate`,
48 `RenameBody` and 90 `Undo`. An `Undo` reverses the last step that is not undone (ADR-0031 item 1). After the undos, 60
commands are in effect: 19 `CreateBox`, 22 `Translate` and 19 `RenameBody`. The undone steps made a central upright
("Montant central") and four shelves ("Tablette gauche 268", "Tablette droite 268", "Tablette gauche 530", "Tablette droite
530"). They are not in the final document.

`CreateBox` makes a box with its centre at the origin of the Document. Each body is one box, moved by one `Translate` and
named by one `RenameBody`. Three bodies have a second `Translate`, a correction on y: "Fond" by -197 then +394, "Bas" and
"Haut" by +3 then -6. The builder sends one `Translate` for each body, the sum of the moves, so it sends 57 commands. The
final bodies are the same: a test compared them with a replay of the file when the file was present. Since TASK-0431 (finding
T186, Q7 A of the codebase review of 2026-11-21) the program `eng/bookcase-compare` makes that comparison, and the owner runs
it where the file is.

## 2 The 19 bodies

Each body has the kind `Solid`. The sizes are in mm on x, y and z. The front of the bookcase is at y = -200, and the wall is
at y = +200. z is up, and the floor is at z = 0. Each number is an integer, so each bound is exact in a double.

| Name | Size x × y × z | Centre (x, y, z) | Minimum (x, y, z) | Maximum (x, y, z) |
|---|---|---|---|---|
| Module 1 - volume indicatif | 800 × 400 × 1000 | -1300, 0, 500 | -1700, -200, 0 | -900, 200, 1000 |
| Module 2 - volume indicatif | 800 × 400 × 1000 | 1300, 0, 500 | 900, -200, 0 | 1700, 200, 1000 |
| Module 3 - volume indicatif | 400 × 400 × 950 | 100, 0, 1875 | -100, -200, 1400 | 300, 200, 2350 |
| Module 5 - volume indicatif | 400 × 400 × 550 | 1300, 0, 2075 | 1100, -200, 1800 | 1500, 200, 2350 |
| Module 6 - volume indicatif | 600 × 400 × 600 | 200, 0, 300 | -100, -200, 0 | 500, 200, 600 |
| Module 7 - volume indicatif | 800 × 400 × 400 | -1300, 0, 1200 | -1700, -200, 1000 | -900, 200, 1400 |
| Module 8 - volume indicatif | 600 × 400 × 400 | -600, 0, 800 | -900, -200, 600 | -300, 200, 1000 |
| Module 9 - volume indicatif | 800 × 400 × 600 | -500, 0, 300 | -900, -200, 0 | -100, 200, 600 |
| Module 10 - volume indicatif | 400 × 400 × 600 | 700, 0, 300 | 500, -200, 0 | 900, 200, 600 |
| Module 11 - volume indicatif | 400 × 400 × 400 | 1300, 0, 1600 | 1100, -200, 1400 | 1500, 200, 1800 |
| Module 12 - volume indicatif | 600 × 400 × 400 | -600, 0, 1200 | -900, -200, 1000 | -300, 200, 1400 |
| Module 14 - volume indicatif | 800 × 400 × 800 | 700, 0, 1800 | 300, -200, 1400 | 1100, 200, 2200 |
| Module 15 - volume indicatif | 800 × 400 × 950 | -1300, 0, 1875 | -1700, -200, 1400 | -900, 200, 2350 |
| Module 16 - volume indicatif | 800 × 400 × 950 | -500, 0, 1875 | -900, -200, 1400 | -100, 200, 2350 |
| Cote gauche | 18 × 400 × 800 | -291, 0, 1000 | -300, -200, 600 | -282, 200, 1400 |
| Cote droit | 18 × 400 × 800 | 891, 0, 1000 | 882, -200, 600 | 900, 200, 1400 |
| Bas | 1164 × 394 × 18 | 300, -3, 609 | -282, -200, 600 | 882, 194, 618 |
| Haut | 1164 × 394 × 18 | 300, -3, 1391 | -282, -200, 1382 | 882, 194, 1400 |
| Fond | 1164 × 6 × 800 | 300, 197, 1000 | -282, 194, 600 | 882, 200, 1400 |

The 14 modules are volumes ("volume indicatif"): one box for each module, with no panel inside. No body has the name of a
module 4 or of a module 13. The last five bodies are the panels of one open case of 1200 × 400 × 800 between x = -300 and
x = 900 and between z = 600 and z = 1400: two sides of 18 mm, a bottom and a top of 18 mm between the sides, and a back of
6 mm behind the bottom and the top. The whole bookcase is 3400 mm wide (x = -1700 to 1700), 400 mm deep and 2350 mm high.

## 3 The commands

The builder uses three commands that exist: `CreateBox`, `Translate` and `RenameBody`. The measure of section 5 also uses
`Rotate` (ADR-0112). The builder needs no new command and no new field of `Engine.Contracts`.

## 4 What the existing commands could not model

Each body of the file is a box, so the three commands model each body with its name, its size and its place. No shape is
missing. These points are not in the builder, or need more than one command:

| Point | With the existing commands | Gap and its row |
|---|---|---|
| The five panels of the open case are one piece of furniture. | Five separate bodies. | The Document has no group of bodies and no component. The roles and the groups of an assembly are P14 and later rows. |
| A door or a module turns on a hinge. | `Translate`, `Rotate`, `Translate`: three commands and three new bodies for each turn (ADR-0112 item 9). | A turn about a point that is not the origin is the first question of P11.0; section 5 gives its measure. |
| A hinge or a fixed joint between two bodies. | Nothing: two bodies only touch. | The joint is P11. |
| Two bodies touch or overlap. | No query. The bounding boxes show that the panels and the modules touch. | The pair check is K11. |
| The history of the owner: the undone upright and shelves. | Not in the builder. The builder holds the final state only. | None: an undone step is not part of the design. |

## 5 The measure for R-0070 and P11.0

`One_Hundred_And_Fifty_Turns_Of_A_Module_About_Its_Hinge_Keep_Its_Box_And_Replay_The_Same` turns "Module 1 - volume
indicatif" 150 times about the vertical axis through its front left edge, the point (-1700, -200, 0), as a door turns on a
hinge. Module 1 is at the left end of the bookcase, so it turns out to the front and no other body is on its path. Its three
lengths are different (800, 400 and 1000), so each length can show a change. Each turn is a `Translate` by (1700, 200, 0),
a `Rotate` and a `Translate` by (-1700, -200, 0), so the 150 turns are 450 commands.

The turn is exact. With t = 1/115, cos = (1 - t²) / (1 + t²) = 13224 / 13226 and sin = 2t / (1 + t²) = 230 / 13226, about
0.9965 degree. The quaternion (115, 0, 0, -1) gives that turn about -z, and `Rotation.FromQuaternion` computes the same two
fractions, each with one division. No sine or cosine function is called (`CLAUDE.md`, determinism rule 1). 150 turns give
about 149.5 degrees. The test computes the exact box after 150 turns from the complex number (115 - i)^300 / 13226^150 with
`BigInteger` and `Rational`.

Measured on the computer of the owner (Windows, x64, Debug build), TASK-0429, three runs:

| Quantity | Value |
|---|---|
| Bodies after the turns | 19 (each command consumes its source) |
| Commands in the log | 507 (57 of the builder and 450 of the turns) |
| Time of the 450 commands of the turns | 40 to 46 ms |
| Time of the first read of the solid of the owned kernel after the turns | 1953 to 2036 ms |
| Time of a replay of the log (an open of the saved file) | 17 to 19 ms |
| Size of the box on z after the turns | 1000, exactly |
| Largest difference between a bound of the box and the exact turn | 1.6e-12 mm |
| Largest change of the length of an edge (`GetFaces`) | 9.1e-13 mm |
| Largest relative change of the square of the length of an edge in the owned kernel, exact | 2.4e-15 |
| Size of the numbers of the owned kernel | the longest numerator of a coordinate has 2576 digits, the longest denominator 2573 digits (24 vertices) |

What the numbers say:

- The backend turns a body fast: the 450 commands take about 0.1 ms each, and a replay is as fast, because neither builds the
  solid of the owned kernel.
- The owned kernel reads the nine doubles of each turn exactly (ADR-0112 item 5). The numbers of a coordinate grow by about
  17 digits for each turn: 2576 digits after 150 turns. The first read of the solid after the turns takes about 2 s. Each
  query that reads the owned kernel (the faces, a datum on a face, the view lines, the STEP export) pays this cost once for
  each new body, and a longer chain of turns pays more.
- The shape changes by a very small amount: the nine doubles of a turn are not exactly a turn, so each length changes by
  about one part in 10^15 after 150 turns, less than 1e-12 mm on this module.
- No public query gives the size of the exact numbers. The test reads the solid of the owned kernel through the internal
  `KernelProjection`, as `GetFacesTests` does.

The test asserts only what is stable: 19 bodies, 507 commands, the box of the turned module within 1e-6 mm of the exact
turn, the size 1000 on z exactly, the 18 other boxes unchanged, and a replay that gives the same bodies and the same boxes.
No time decides the test (ADR-0173 item 4). The bound 1e-6 mm is far above the sum of the roundings of 450 commands (less
than 1e-9 mm) and far below a size that a person can make or measure.

## 6 Not verified

- The times come from one computer and a Debug build. Continuous integration on the three operating systems runs the test,
  but its output is not read by a gate.
- The replay of the file of the owner (since TASK-0431 in `eng/bookcase-compare`) uses the backend of this repository, not the backend version 3.5.2 of the
  header. The boxes agree exactly.
- No picture of the bookcase in the window was made. The desktop was not run.
- The sizes of the support of the lamp are not known. The second case of K10 (K10b) waits for them.
