---
id: 0433
title: A client, the command line or an agent joins two bodies or keeps their shared volume with one command each, in the form of Subtract
status: Active
phase: K16
opened: 2026-11-21
depends-on: [0432]
governed-by: [0001, 0004, 0008, 0012, 0014, 0016, 0018, 0021, 0028, 0031, 0033, 0035, 0036, 0037, 0039, 0051, 0052, 0054, 0055, 0057, 0058, 0059, 0060, 0074, 0076, 0083, 0089, 0112, 0143, 0153, 0155, 0158, 0166, 0167, 0168, 0169, 0179, 0181]
writes:
  create:
    - tasks/TASK-0433-the-union-and-the-intersection-of-two-bodies.md
    - docs/adr/0181-the-union-and-the-intersection-of-two-bodies.md
    - Engine.Core/Commands/UnionCommand.cs
    - Engine.Core/Commands/UnionCommandHandler.cs
    - Engine.Core/Commands/IntersectCommand.cs
    - Engine.Core/Commands/IntersectCommandHandler.cs
    - Engine.Core/Commands/BooleanStep.cs
    - Engine.Tests/Commands/UnionIntersectCommandTests.cs
    - Engine.Tests/Kernel/PlanarUnionIntersectTests.cs
    - Engine.Tests/Http/HttpUnionIntersectScenarioTests.cs
  modify:
    - Engine.Contracts/Geometry/IBooleanOps.cs
    - Engine.Kernel/PlanarBoolean.cs
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Geometry.Manifold/Native/ManifoldNative.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Features/FeatureList.cs
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Core/Features/Regeneration.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Sketch/FaceGone.cs
    - Engine.Core/Determinism/CrossPlatformProbe.cs
    - Engine.Tests/Commands/BodyNameAndVisibilityTests.cs
    - Engine.Tests/Commands/OperandConsumptionTests.cs
    - Engine.Tests/Commands/UndoRedoTests.cs
    - Engine.Tests/Features/RegenerationTests.cs
    - Engine.Tests/Kernel/PlanarSubtractTests.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/0057-planar-subtract-in-the-owned-kernel.md
    - docs/adr/0058-the-engine-reads-the-owned-kernel.md
    - docs/adr/README.md
    - docs/android-test-sheet.md
    - docs/diagnostics.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - 3DEngine.App/**
---

# TASK-0433 — A client, the command line or an agent joins two bodies or keeps their shared volume with one command each, in the form of Subtract

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K16 of the roadmap (ADR-0126 item 8, ADR-0157 items 11 and 15) asks for `Union` and `Intersect` in the form of
`Subtract`: each consumes two bodies and makes one. The union gives the knob with a collar of K10, and the intersection
gives the overlap volume of K11. The codebase review of 2026-11-21 (section 4) put K15, K16 and R26 after K10, and K15a
shipped first (TASK-0432, v0.415). The branch `k16-union-intersect` starts from `6d78b908` (P0.172, TASK-0431, v0.416),
which is not merged yet.

## Goal

Two commands, `Union` and `Intersect`, give the union and the intersection of two bodies on each path that `Subtract`
takes: the backend, the owned kernel, the names of the faces, the rebuild, undo, replay and the file.

## Scope (in)

1. The records and the handlers of `Union` and `Intersect` in `Engine.Core/Commands`, in `HandlerCatalog` (ADR-0181
   items 1 to 3).
2. `IBooleanOps.Union` and `IBooleanOps.Intersect`, and the calls of `manifold_union` and `manifold_intersection`
   (item 4).
3. `PlanarBoolean.Union` and `PlanarBoolean.Intersect` in the owned kernel (item 5).
4. The projection, the chain of a datum, the check of a gone face, the curves, the feature list and the regeneration
   (items 6 and 7).
5. The determinism probe holds a union and an intersection, and the test sheet of the phone names the new hash (item 9).
6. ADR-0181, the amendments of ADR-0012, ADR-0057 and ADR-0058, the rows of the roadmap and the ledger entry.

## Scope (out)

- A button of the window (ADR-0181 item 8). `3DEngine.App/**` is outside the write set.
- A union or an intersection of more than two bodies in one command.
- The check of K11. It reads the measures of K15a.

## Acceptance criteria

- [x] The union and the intersection of two boxes that overlap, touch or are apart give the exact volume, the centre and
  the box computed by hand, and an intersection with no shared volume is refused with `E-GEOM-NATIVE-OP`.
- [x] Each refusal of `Subtract` holds for both commands, with its code.
- [x] The faces of the result keep their names, and a datum on a gone face gets `W-DATUM-REFERENCE-FAILED`.
- [x] Undo, redo, a replay of the log, a save and an open give the same bodies.
- [x] A new distance of an extrude rebuilds a union and an intersection.
- [x] The kernel agrees with Manifold over random models of boxes and L extrudes.
- [x] The five panels of the open case of the bookcase join into one body of 33,617,376 mm³.
- [x] An injection in each part makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
  (Local: the clean build, the phone build, the write-set check and `eng/repo-gates` pass. In two full runs of
  `dotnet test`, two or three tests of `HostGuardTests` failed on a time limit while other sessions used the computer;
  the class passed alone. Continuous integration runs after the push.)

## Decisions

The row, ADR-0126 and ADR-0157 leave these open. Each copies `Subtract` (ADR-0181, "Alternatives considered"):

- The names `Union` and `Intersect`, the parameters `firstBodyId` and `secondBodyId`, and the output `bodyId`.
- The records are in `Engine.Core/Commands`, as `SubtractCommand` is. `Engine.Contracts` changes in `IBooleanOps` only,
  and `Engine.Kernel` gets two public methods.
- The result takes the name and the visibility of the first body, and the command consumes both bodies.
- An intersection with no shared volume is refused with `E-GEOM-NATIVE-OP`, as an empty cut is; a union of two bodies
  apart is one body of two parts.
- No new diagnostic code and no new event kind.
- No button of the window: HTTP, the command line and an agent reach the commands through the generic path.

## Outcome

Status: Active · commit (the merge of the branch k16-union-intersect) · v0.417

## Method

Mechanical: each place that reads a `Subtract` reads a `Union` and an `Intersect` too; the handlers share the steps of
`SubtractCommandHandler` in `BooleanStep`, and `Subtract` keeps its own handler.

Judgement: the steps of the union and the intersection of csg.js on the exact planes of ADR-0057; a union and an
intersection pass the chain of a datum with no turn of a face; each open decision copies `Subtract`. The random test of
the kernel found that Manifold gives a flat solid of no volume, which it does not call empty, for two boxes that touch on
a part of a face; so the exact kernel decides an empty intersection before the backend (ADR-0181 item 3). The first full
run stopped on a stack overflow in `BookcaseCaseTests` (150 turns): the locals of the new case made the frame of the
recursive `KernelProjection.Solid` larger. The steps of the union and the intersection are now methods of their own in
`KernelProjection` and `Regeneration`, and the full run passes.

Weakest: the kernel keeps one of two coplanar faces with one normal, and the test of the faces of the intersection does
not say which; `Two_Cubes_That_Overlap_Give_The_Union_And_The_Intersection_With_The_Faces_Of_Both` asserts one face on
each side only.

Injections (`inject0433.js` in the scratchpad, Node.js, each `dotnet` call with `shell: false`), each restored byte for byte
(sha256). Each one made the named tests fail:

1. `Union` not in `HandlerCatalog`: 11 tests, among them `Two_Cubes_That_Overlap_Give_The_Exact_Union_And_Intersection`
   and the HTTP test;
2. `Intersect` not in `HandlerCatalog`: 11 tests, among them the bookcase test and the schema test;
3. the union of the kernel keeps both copies of a coplanar face: the random test, the face test of the kernel and 4 more,
   among them the HTTP test;
4. the intersection of the kernel does not turn back at the end: `A_Box_Inside_A_Cube_Is_The_Intersection_And_The_Cube_Is_The_Union`,
   the random test and 8 more;
5. `Union` of Manifold calls `manifold_difference`: 6 tests, among them the random test, the bookcase test and the HTTP test;
6. `Intersect` of Manifold calls `manifold_union`: the random test, the overlap test, the rebuild test and the HTTP test;
7. the result takes the name of the second body: the overlap test and the bookcase test;
8. the command consumes the first body only: the overlap test, the undo test and the bookcase test;
9. no refusal of one body twice: `Each_Refusal_Of_Subtract_Holds_For_Union_And_Intersect` and the HTTP test;
10. no refusal of a mesh body: the refusal test;
11. no check of the kernel before the backend: `Touching_And_Apart_Cubes_Join_And_Share_No_Volume`. The first form of the
    test passed this injection, because Manifold refuses two cubes that touch on a whole face; the box that touches a part
    of a face catches it now;
12. no fault for a datum on a gone face: `The_Faces_Of_The_Result_Keep_Their_Names_And_A_Datum_On_A_Gone_Face_Gets_A_Fault`.
    The first form of this injection did not build, and the second form keeps each face;
13. the projection swaps the union and the intersection: 8 tests, among them the overlap test and the bookcase test;
14. a union ends the chain of a datum (`KernelProjection.End`): the face test (a new datum on a gone face is not refused);
15. the curves of the second body are lost: `The_Result_Keeps_The_Curve_Of_A_Cylinder`;
16. a union turns the faces of a datum (`DatumResolver.Turned`): the face test (the normal of a new datum);
17. the regeneration does not run a union or an intersection again: `A_New_Distance_Of_An_Extrude_Rebuilds_The_Union_And_The_Intersection`;
18. the feature list has no operands of a union: the face test and the rebuild test;
19. the feature list does not know that a union or an intersection creates a body: 9 tests, among them the bookcase test,
    the curve test and the HTTP test.

Injections 13, 15 and 17 ran again after the steps moved into methods of their own, with the same result.

## Progress

- 2026-11-21: opened after K15a and P0.172, branch `k16-union-intersect` from `6d78b908` (v0.416), which is not merged
  yet. The two commands, ADR-0181, the kernel, the backend, the probe and 16 tests; 19 injections. Ledger entry v0.417.
- 2026-11-21: run 44869058349 of `14f93eb` failed on ubuntu-latest in the step "Test": the test host of `Engine.Tests`
  crashed, and the annotation names no test. Windows and macOS passed. The 150 turns of the bookcase pass on Windows with a
  stack of 512 KB, so a stack overflow is not shown. This line starts a second run on Linux.
