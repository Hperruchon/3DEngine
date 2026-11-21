---
id: 0429
title: The bookcase of the owner is the first case of K10, as a builder of its 19 bodies and a measure of 150 turns of a module
status: Done
phase: K10a
opened: 2026-11-21
depends-on: [0424]
governed-by: []
writes:
  create:
    - tasks/TASK-0429-the-bookcase-as-the-first-case-of-k10.md
    - docs/proposals/2026-11-21-the-bookcase-case-of-k10.md
    - Engine.Tests/Assembly/BookcaseCase.cs
    - Engine.Tests/Assembly/BookcaseCaseTests.cs
  modify:
    - docs/roadmap.md
    - docs/register.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - 3DEngine.App/**
---

# TASK-0429 — The bookcase of the owner is the first case of K10, as a builder of its 19 bodies and a measure of 150 turns of a module

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K10 builds the reference cases of the assembly track with the commands that exist (ADR-0126, ADR-0157 items 2 and 12).
Register entry R-0070 holds two open points: the sizes of the cases are not in the repository, and no test measured many
turns of a body (ADR-0126 item 3, ADR-0112 items 5 and 9). On 2026-11-20 the owner chose the bookcase `Bibliotheque120` as
the first case of K10, in the standing order of the weekend. The support of the lamp follows when the owner gives its sizes.
The document file of the bookcase is on the computer of the owner and not in the repository.

## Goal

The repository holds the bookcase of the owner as a builder of commands with its table, and the measure of 150 turns that
R-0070 asks for.

## Scope (in)

1. The table of the 19 bodies of the bookcase (name, kind, size, place), the commands that it uses, and the gaps, in
   `docs/proposals/2026-11-21-the-bookcase-case-of-k10.md`.
2. `BookcaseCase`, a builder that sends the commands of the bookcase through a `DocumentSession`, and `BookcaseCaseTests`:
   each body has its name and the box of the table; a save and an open give the same bodies; the builder gives the bodies
   of a replay of the file of the owner when the file is present.
3. The measure: 150 turns of one module about an axis that is not through the origin, by `Translate`, `Rotate` and
   `Translate`, with an exact rational turn.
4. The rows K10a and K10b in `docs/roadmap.md`, a line under R-0070, the ledger entry and the counts of the tests.

## Scope (out)

- The support of the lamp (K10b). It waits for its sizes from the owner.
- A new command, a new query or a change of `Engine.Contracts`. K10 changes no contract.
- The document file of the owner. It does not go into the repository, and the task does not change it.
- A picture of the bookcase in the window, and the rows of the gaps that need the lamp (K10c of the plan of K10).

## Acceptance criteria

- [x] The table gives the 19 bodies, the commands and the gaps.
- [x] `BookcaseCaseTests` checks each name and each box exactly, a save and an open, and the file of the owner when it is
  present; without the file the last test returns early and passes.
- [x] The measure test turns a module 150 times about its hinge, asserts only stable results, and writes the times, the
  change of a length and the size of the numbers to its output.
- [x] An injection of each part makes a named test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
  (Local: each passes. Continuous integration runs after the push.)

## Decisions

- The owner, 2026-11-20 (R-0070, the standing order of the weekend): K10 starts with the bookcase `Bibliotheque120` as its
  builder; the support of the lamp follows when the owner gives its sizes. The sizes of the cases go into the repository
  (answer Q2 A of `docs/proposals/2026-11-15-plan-k10-the-first-assembly-row.md`), as a builder of commands and not as a
  document file (answer Q3 A).

## Outcome

Status: Done · commit (the merge of the branch k10a-bookcase-case) · v0.413

## Method

Mechanical: the replay of the file of the owner (240 commands, 90 of them `Undo`) by a script, which gave 19 bodies and 60
commands in effect; the table; the builder with one `CreateBox`, one `Translate` and one `RenameBody` for each body; the
expected boxes in the test, apart from the builder; the counts of `eng/test-floor.txt`.

Judgement:

- The builder sums the two moves of "Fond", "Bas" and "Haut" into one `Translate`, so it sends 57 commands and not 60. The
  final bodies are the same, and the test with the file of the owner checks it.
- The test with the file of the owner returns early when the file is absent, with a line in its output. The repository has
  a skip mechanism, `NativeManifoldFact`, but a skip removes the test from the executed tests, and `eng/test-floor.txt` gives
  one exact count for each platform. The file is on the computer of the owner only, so a skip would make the local count
  differ from the count of continuous integration.
- The module that turns is "Module 1 - volume indicatif": it is at the left end of the bookcase, so it turns out to the
  front about its front left edge as a door on a hinge, and its three lengths are different.
- The turn is the quaternion (115, 0, 0, -1): with t = 1/115 it gives cos = (1 - t²) / (1 + t²) and sin = 2t / (1 + t²),
  each from one division, about 0.9965 degree. The test asserts the two fractions, and computes the exact box after 150
  turns with `BigInteger` and `Rational`.
- No public query gives the exact numbers of the owned kernel. The test reads them through the internal `KernelProjection`,
  as `GetFacesTests` does, and writes their size to its output only.
- The bound of the box is 1e-6 mm. The roundings of 450 commands give less than 1e-9 mm; the measured difference is
  1.6e-12 mm.

Weakest: the times come from one computer and a Debug build, and no gate reads the output of the measure in continuous
integration. The bound of 1e-6 mm does not detect a slow growth of the error that stays under it.

Measured (Windows, x64, Debug; three runs): the 450 commands of the turns 40 to 46 ms; the first read of the solid of the
owned kernel after the turns 1953 to 2036 ms; a replay of the 507 commands 17 to 19 ms; 19 bodies; the size on z 1000
exactly; the largest difference between a bound of the box and the exact turn 1.6e-12 mm; the largest change of the length
of an edge from `GetFaces` 9.1e-13 mm; the largest relative change of the square of the length of an edge in the owned
kernel 2.4e-15; the longest numerator of a coordinate of the owned kernel 2576 digits, the longest denominator 2573 digits.

Injections (`inject0429.js` in the scratchpad), each restored byte for byte (sha256):

- the height of Module 3, 950 to 951: `Each_Body_Of_The_Bookcase_Has_Its_Name_And_The_Box_Of_The_Table` and
  `The_Builder_Gives_The_Bodies_Of_The_File_Of_The_Owner_When_The_File_Is_Present`;
- the place of "Fond", y 197 to 196: the same two tests;
- the name "Cote droit" to "Cote droite": the same two tests;
- the journal writes no `RenameBody` (`Engine.Core/Persistence/DocumentJournal.cs`): `A_Save_And_An_Open_Give_The_Same_Bodies`
  and the measure test;
- 149 turns in place of 150: the measure test;
- the quaternion (114, 0, 0, -1) in the command: the measure test;
- a turn about the origin (no move to the hinge): the measure test;
- control, the file of the owner absent: each test passes, and the output says that the file is absent.

## Progress

- 2026-11-21: opened after R20e, branch `k10a-bookcase-case` from `040f238a` (R20e, TASK-0424, v0.412), which is not merged
  yet. The table, the builder, four tests and eight injections. Ledger entry v0.413.
- 2026-11-21: run 44846510217 of `92d1c54e` passed on each job. Closed at v0.413.
- 2026-11-21: run 44866049139 of the close commit `a21425d` failed on macos-latest in the step "Test":
  `FixedLengthsTests.The_Fixed_Lengths_Of_A_Stair_Of_600_Lines_Take_A_Time_Near_The_Status` gave 0.96 s against 0.12 s. The
  commit changes this file only, so the time of the runner decided the test. The task is Active again for a new run.
- 2026-11-21: run 44867884480 of `a1a5c7f4` passed on each job. Closed at v0.413.
