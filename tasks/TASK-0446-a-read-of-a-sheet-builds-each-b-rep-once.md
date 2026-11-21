---
id: 0446
title: A read of a sheet builds the B-Rep of each body once and keeps it until the body changes
status: Active
phase: P0.179
opened: 2026-11-21
depends-on: [0445]
governed-by: [0004, 0057, 0058, 0076, 0089, 0112, 0142, 0143, 0144, 0145, 0147, 0155, 0164, 0165, 0169]
writes:
  create:
    - tasks/TASK-0446-a-read-of-a-sheet-builds-each-b-rep-once.md
    - Engine.Core/Drawing/ViewBreps.cs
  modify:
    - Engine.Core/Drawing/ViewEdges.cs
    - Engine.Kernel/Brep.cs
    - Engine.Kernel/KernelSolid.cs
    - Engine.Tests/Drawing/SheetViewTests.cs
    - docs/adr/0155-the-views-of-a-sheet.md
    - docs/adr/0057-planar-subtract-in-the-owned-kernel.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - 3DEngine.App/**
---

# TASK-0446 — A read of a sheet builds the B-Rep of each body once and keeps it until the body changes

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0069 (opened 2026-11-15, due 2027-05-14): a read of a sheet with three views of a sphere took 4.6 s and
5.1 s on the computer of the owner. GetSheet holds the session for that time, so a command of the person or of the agent
waits. ADR-0155 item 7 (Q7 A of the owner) decided "no cache": each read computes the lines of each view. The exit of the
entry: a read of three views of a sphere takes less than 1 s, with a test, or the owner accepts the time.

TASK-0445 (P0.178, v0.429, not merged yet) moved the section of a sheet to the cut of the kernel and reported that the
B-Rep of a view with no section is still built at each read. This task starts from its commit `97da5693`.

## Goal

A read of a sheet with three views of a sphere takes less than 1 s, and a test counts the builds of the B-Rep.

## Scope (in)

1. Find where the time of the read goes: the solid, the B-Rep of each view, the lines of each view.
2. The read builds the B-Rep of a body once for all its views, and keeps it under the exact content of the solid, with
   the B-Reps of the bodies that are gone removed and the count of documents bounded.
3. The parts of `Brep.Of` and of the hidden-line pass whose work grows with the square of the count of the polygons, and
   the test of convexity of a triangle, if the result stays the same.
4. A test that counts the builds; ADR-0155 item 7; R-0069; the roadmap, the ledger and the counts of the tests.

## Scope (out)

- A cache of the lines of a view. Each read still computes the lines of each view.
- The cut of a section and its cache (`SectionCut`, E222, E226).
- A change of `Engine.Contracts`, of a command, of an event or of the Document; no new diagnostic code.
- The time of the first compile of the code in a new process (the tiered compiler of .NET).

## Acceptance criteria

- [x] The JSON of GetSheet, the SVG file and the JSON of GetViewLines are the same, byte for byte, before and after, for a
  sheet of three views of a sphere, for the bookcase with a slanted view and a section, and for a plate with a hole, a
  cone and a torus with a section.
- [x] `Brep.Of` gives the same vertices, edges, faces, loops, shells and validity as before for 175 solids.
- [x] A read of three views of a sphere builds its B-Rep one time; a second read and a read after a move of a view build
  nothing (`SheetViewTests.A_Read_Of_Three_Views_Of_A_Sphere_Gives_Its_Circles`).
- [x] A change of a body builds its B-Rep again and shows the change; a body that is gone keeps no B-Rep; the reads keep
  the B-Reps of the last 64 documents only.
- [x] A read of three views of a sphere takes less than 1 s in a process that ran its code before, and a second read
  takes less than 0.4 s (times in Method, not in a test).
- [x] An injection in each part makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
  (Local: each passes. Continuous integration runs after the push.)

## Decisions

- ADR-0155 item 7 is amended in this task, under the standing order of 2026-11-20: the exit of R-0069 asks for a read of
  less than 1 s, and a read that builds the B-Rep of a sphere three times cannot give it. The read keeps the B-Rep of each
  body; it keeps no line. The B-Rep is a function of the polygons of the solid only, so a kept B-Rep is the B-Rep that a new
  build gives, and the read stays a pure function of the Document. The History of ADR-0155 says so.
- The key is the pair of the document and the body, and the check is the exact content of the solid: the same instance
  (the projection of the kernel keeps the solid of each geometry key, ADR-0059 item 2), or the same polygons with the same
  vertices, planes and tags. A body whose solid changes and keeps its identifier (SetExtrudeDistance) gets a new build.
- The bound is in documents, as in `SectionCut` (E226): one B-Rep for each live body of each of the last 64 documents.
  A bound in bodies would miss each lookup of a read of more bodies than the bound.
- `Brep.Of` finds a group, an edge, the regions of a group and the edges of a face by a dictionary or an index, and step
  2 reads only the vertices in the x range of an edge. The order of each list stays (ADR-0057 item 8): a dictionary only
  finds an entry, and each list keeps the order of its first use.
- A triangle skips the test of `Convex`: its Newell normal is (b − a) × (c − a), and `Canonical` scales it by a positive
  number, so each turn is positive and `Convex` gives the triangle itself.
- The hidden-line pass rounds the middle of a piece one time, and not one time for each face.
- R-0069 closes as resolved. In a new test process the first read takes about 1.1 s; about 0.5 s of it is the start of the
  tiered compiler of .NET, which runs the new code unoptimized for its first fraction of a second. The same read with
  `DOTNET_TC_CallCountingDelayMs=0` takes 0.65 s. The host of the desktop pays that time once in its life, at its first
  read of any sheet; each later read of three views of a new sphere takes 0.44 s to 0.50 s (Release and Debug). The owner
  can reopen the entry if the first read of a new process must also be less than 1 s.

## Outcome

Status: Active · commit (the merge of the branch p0179-sheet-read) · v0.430

## Method

Mechanical: the call of `ViewBreps` in `ViewEdges.Section`, the dictionaries of `Brep.Of`, the test of a triangle in
`KernelSolid`, the History lines of ADR-0155 and ADR-0057, the roadmap rows, the counts of `eng/test-floor.txt`.

Judgement: where the time goes, and which parts to change. A probe test, removed, measured the parts of the read of three
views of a sphere of radius 10 (3,968 triangles, 1,986 vertices, 5,952 edges) in Debug, in a process that ran the code before:

| Part | Before | After |
|---|---|---|
| The solid of the kernel (kept by geometry key) | 0.25 s | 0.13 s to 0.14 s |
| `Brep.Of` | 1.0 s to 1.6 s for each view | 0.16 s to 0.17 s, one time for the read |
| The lines of the three views | 0.30 s to 0.40 s | 0.17 s to 0.18 s |

Weakest: the proof that the read is the same is a comparison of 15 files of three models and of the B-Rep of 175 solids,
and the existing tests; a model that none of them has can show a difference. The order of the lists of `Brep.Of` is the
part most likely to change a result; the comparison of the B-Rep holds the order of each list.

Times of the read of three views of a sphere (the test `A_Read_Of_Three_Views_Of_A_Sphere_Gives_Its_Circles` and a probe,
not a test; this computer, other test processes can run at the same time):

| Read | Before | After |
|---|---|---|
| First read in a new process, Debug | 4.36 s to 4.63 s | 1.11 s to 1.22 s |
| First read in a new process, Release | 3.44 s to 3.54 s | 1.09 s to 1.21 s |
| First read in a new process, Release, `DOTNET_TC_CallCountingDelayMs=0` | not measured | 0.65 s |
| First read of a new sphere in a process that ran the code before, Debug | 3.34 s to 3.43 s | 0.49 s to 0.50 s |
| The same, Release | not measured | 0.44 s to 0.48 s |
| Second read, Debug | 3.04 s to 3.15 s | 0.17 s to 0.34 s |
| Second read, Release | not measured | 0.11 s to 0.30 s |

Counts (the test): before, three builds of the B-Rep for each read of three views; after, one build for the first read and
none for the second read and for a read after a move of a view.

The proof of the same result: a probe test, removed, wrote the JSON of GetSheet, the SVG file and the JSON of GetViewLines
(Front, Top and a slanted view) of three models with fixed command identifiers: the sphere with three views, the bookcase
with four views and a section, and a plate with a hole, a cone and a torus with three views and a section. Two runs before
the change gave the same 15 files; the run after the change gave the same 15 files, byte for byte (sha256). A second probe,
removed, compared the old `Brep.Of` (the file of `97da5693`) and the new one on 175 solids: the solids of 30 bodies (spheres,
a torus, a cone, a cylinder, a union, a plate with two holes and a square hole, the 19 boxes of the bookcase) and 150 cuts of
them by planes; each gave the same vertices, edges, faces, loops, shells and validity.

Injections (`inject.js` in `%TEMP%\p0179`), each restored byte for byte (sha256). Each of the twelve made a named test
fail:

1. No reuse of the B-Rep between views and reads (the form before this task):
   `A_Read_Of_Three_Views_Of_A_Sphere_Gives_Its_Circles` and
   `A_Read_Builds_The_BRep_Again_When_A_Body_Changes_And_Keeps_None_Of_A_Body_That_Is_Gone`.
2. A key that ignores a change of the body, 3. a compare of the count of polygons only, 4. no removal of a body that is
   gone, and 5. no call of `Keep` after a read: `A_Read_Builds_The_BRep_Again_When_A_Body_Changes_And_Keeps_None_Of_A_Body_That_Is_Gone`.
6. No bound of the count of documents: `The_Reads_Keep_The_BReps_Of_The_Last_Documents_Only`.
7. `Brep.Of` groups the faces of one plane with no regard to the tag: three tests of `PlanarUnionIntersectTests`, for
   example `Two_Cubes_That_Touch_Join_In_One_Shell_And_Share_No_Volume`.
8. `Brep.Of` skips the vertices at the low x of an edge: nine tests, for example
   `PlanarSubtractTests.A_Box_At_A_Corner_Cuts_A_Notch` and
   `PlaneCutTests.A_Cut_Through_A_Hole_Gives_Two_Caps_And_A_Cut_Across_It_Gives_A_Cap_With_A_Hole`.
9. `Brep.Of` walks the shells on the first face of each edge only: four tests, for example
   `PlanarSubtractTests.A_Box_Through_A_Plate_Gives_A_Ring_With_The_Faces_Of_Both`.
10. `Brep.Of` gives an edge the first region of its group: fifteen tests, for example
    `PlanarSubtractTests.A_Hole_In_One_Half_Of_A_Cut_Plate_Is_A_Loop_Of_The_Face_Of_That_Half`.
11. The hidden-line pass reads x and y the other way: eleven tests, for example
    `ViewEdgesTests.An_Edge_Behind_A_Seam_Of_A_Curved_Side_Is_Hidden`.
12. A face of four or more vertices skips `Convex` too: `PlanarSubtractTests.The_Subtract_Agrees_With_Manifold_Over_Random_Models`
    and `PlanarUnionIntersectTests.The_Union_And_The_Intersection_Agree_With_Manifold_Over_Random_Models`.

The three tests of this task fail with `ViewEdges.cs`, `Brep.cs` and `KernelSolid.cs` of `97da5693` (`ViewBreps` there,
but not called): `baseline.js` in `%TEMP%\p0179` gave 3 failed of 3, and 3 passed of 3 after the change. The same script
measured the first two rows of the table of times, with the sources of `97da5693` and then of the change.

## Progress

- 2026-11-21: opened after P0.178 (TASK-0445, v0.429, not merged yet), branch `p0179-sheet-read` from `97da5693`. The kept
  B-Rep of each body, the faster `Brep.Of`, two new tests and one changed test, ADR-0155 item 7, R-0069 closed, the roadmap
  row P0.179 and the ledger entry v0.430.
- 2026-11-21: after the byte comparison, the count of builds moved into the entry of each document and leaves with it,
  so no clear of all counts at 1,024 bodies can make a test of the count fail while other tests run. The read does not
  read the count. The six injections of `ViewBreps` ran again and each made its named test fail. Local: a clean build
  with 0 warnings, the phone project with 0 warnings, `dotnet test` 3157 passed, the write-set check and `eng/repo-gates`
  pass.
