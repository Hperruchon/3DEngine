---
id: 0436
title: The pair check CheckInterference gives the class, the overlap, the gap and the marks of each pair of bodies at one position
status: Active
phase: K11
opened: 2026-11-21
depends-on: [0434, 0433]
governed-by: [0004, 0013, 0016, 0018, 0019, 0028, 0033, 0037, 0060, 0083, 0089, 0112, 0114, 0153, 0155, 0158, 0166, 0167, 0168, 0169, 0179, 0182]
writes:
  create:
    - tasks/TASK-0436-the-pair-check-at-one-position.md
    - docs/adr/0182-the-pair-check-at-one-position.md
    - Engine.Core/Queries/CheckInterferenceQuery.cs
    - Engine.Core/Queries/CheckInterferenceQueryHandler.cs
    - Engine.Tests/Queries/CheckInterferenceQueryTests.cs
    - Engine.Tests/Http/HttpInterferenceScenarioTests.cs
  modify:
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - 3DEngine.App/**
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0436 — The pair check CheckInterference gives the class, the overlap, the gap and the marks of each pair of bodies at one position

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K11 follows K15 (K15a, K15b: TASK-0432, TASK-0434, ADR-0180) and K16 (TASK-0433, ADR-0181). The owner decided the rules
of a check on 2026-11-15 (ADR-0157 items 5 to 7): a declared pair is not a fault, a mesh body is accepted with a result that
is exact for the mesh only, and each result states its class. Card N3 of
`docs/proposals/2026-11-15-assembly-and-agent-design.md` names the query `CheckInterference`. The kernel gives the exact
intersection (`PlanarBoolean.Intersect`) and the exact nearest pair of two solids (`NearestPoints`), and no query gives the
contacts and the overlaps of a part.

## Goal

One query gives, for each pair of the given bodies or of the live bodies, the class from the exact kernel, the volume and the
box of an overlap, the minimum gap, the marks of an intended pair and of a fault, and the mark of a mesh, with no change of
the Document.

## Scope (in)

1. The query `CheckInterference` with the optional texts `bodyIds` and `intendedPairs`, registered in `HandlerCatalog`.
2. The class from the exact intersection and the exact nearest pair, after a test of the boxes.
3. A closed mesh body of an STL file and a body with a curved face, with the mark `meshOnly`.
4. The code `E-CHECK-PAIR-INVALID`, ADR-0182, the amendment of ADR-0083 item 4, the ledger entry and the counts of the tests.

## Scope (out)

- The intended pairs in the Document (P11, P13), the travel (K12), and a rule of a gap (P13).
- The closed forms of the curve of K15b in a check: a curved body is checked on its facets, with the mark.
- A change of `Engine.Contracts` or of `Engine.Kernel`, and a tool of the window.

## Acceptance criteria

- [x] Two boxes that overlap give the exact volume and box of the overlap; a box inside a box gives its whole volume.
- [x] Two boxes that touch on a face, on a part of a face, on an edge and on a point are `Touching` with a gap of 0; two boxes
  apart give the gap √1300 and its two points.
- [x] With no list each pair of the live bodies is checked once, in the order of the Document; an intended pair that touches,
  one that is apart and one that overlaps are not faults; the version, the log and the bodies do not change.
- [x] A cylinder and a closed mesh body are checked on their facets and triangles with `meshOnly`; an open mesh is refused.
- [x] A body that is not live and each list that is not valid are refused with their codes.
- [x] The bookcase of K10a gives 171 pairs, no overlap, and the class and the gap of each pair from the boxes of its table.
- [x] An agent reaches the query through `/queries` of HTTP.
- [x] An injection in each part makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

ADR-0182 records each decision; ADR-0157 items 5 to 7 give the rules of the owner. This task takes these decisions:

- The name `CheckInterference` of card N3, and two texts as parameters, because a parameter of a query is flat and the binder
  refuses the type "array".
- The class from the exact kernel, not from Manifold, because Manifold gives a flat solid for a partial contact.
- A contact is not a fault; a fault is an overlap that is not intended.
- `basis` is `Guaranteed` for each pair, the class of ADR-0157 item 7 for a result from the geometry alone.
- A curved body is checked on its facets, with `meshOnly`, because the closed forms of K15b refuse many pairs.
- A closed mesh body is read from the triangles of the backend; an open one is refused with `E-MESH-UNSUPPORTED`. This amends
  ADR-0083 item 4.

## Outcome

Status: Active · commit (the merge of the branch k11-pair-check) · v0.420

## Method

Mechanical: the query and its registration in the form of the measure queries; the mirror of the code in
`SchemaDiagnosticsEndpoint`; the rows of `docs/diagnostics.md`, of the roadmap and of the index of the ADRs; the counts of
`eng/test-floor.txt`.

Judgement: the order of the steps of a pair. Boxes that share no volume cannot hold an overlap, so the intersection runs only
for them. With no shared volume one body cannot lie inside the other, so the exact nearest pair alone gives the gap, with no
test of inside.

Weakest: the classes of a touch on an edge and on a point rest on the partition of the kernel with two solids that meet on a
line or a point. The tests hold boxes only; a touch of two turned bodies on an edge is not tested. The test of the boxes is a
cost only: a wrong test that keeps too many pairs gives the same answer, and no test sees it.

Tests first: the eight new tests failed against a stub of the handler that gave no pair.

Injections (`inject0436.js` in `%TEMP%\k11`), each restored byte for byte (sha256). Each one made the named tests fail:

- the test of the boxes inverted, and the volume of the first body for the overlap: the overlap test, the test of each pair,
  the cylinder, the mesh and the HTTP test;
- no class `Touching`, and the square of the gap for the gap: the test of the contacts, the bookcase, the mesh, the test of
  each pair and the HTTP test;
- each overlap a fault: `Each_Pair_Of_The_Live_Bodies_Is_Checked_Once_And_An_Intended_Pair_Is_Not_A_Fault`;
- the key of a pair in its order, and no intended pair: that test, and the refusals or the HTTP test;
- the box of the first body for the overlap: the overlap test, the cylinder, the mesh and the HTTP test;
- no mark for a curved body: `A_Curved_Body_Is_Checked_On_Its_Facets_With_The_Mark_Of_A_Mesh`;
- no mark for a mesh body, and no test of a closed mesh: `A_Closed_Mesh_Body_Is_Checked_On_Its_Triangles_With_The_Mark_Of_A_Mesh`;
- one body as a list, a pair with one body of the check, and a pair twice:
  `A_Body_That_Is_Not_Live_Or_A_List_That_Is_Not_Valid_Is_Refused` (and the HTTP test for the first);
- the two points swapped: the test of the contacts and the HTTP test;
- only the pairs of the first body (the injection of card N3), and the live bodies with no last one: the test of each pair,
  the bookcase and the mesh;
- the wire name `bodyId` for `bodyIds`: `An_Agent_Checks_The_Pairs_Of_Bodies_Through_The_Route_Of_The_Queries`;
- no registration in `HandlerCatalog`: each of the eight tests.

## Progress

- 2026-11-21: opened after K15b (TASK-0434, v0.418), K16 (TASK-0433, v0.417) and P0.173 (TASK-0435, v0.419), branch
  `k11-pair-check` from `76be5479`. ADR-0182, the query, eight tests, the code `E-CHECK-PAIR-INVALID`. Ledger entry v0.420.
- 2026-11-21: the first full run of `dotnet test` failed `SchemaEndpointGateTests.Every_DiagnosticCodes_Constant_Appears_In_Schema_Diagnostics`:
  the new code had no mirror in `SchemaDiagnosticsEndpoint`. The write set gets that file, and the governed-by list gets
  ADR-0013 and ADR-0019, whose affects name it.
