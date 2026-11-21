---
id: 0445
title: The section of a sheet cuts each polygon of a solid by its plane in the owned kernel, with one cap
status: Active
phase: P0.178
opened: 2026-11-21
depends-on: [0444]
governed-by: [0004, 0057, 0089, 0169, 0181]
writes:
  create:
    - tasks/TASK-0445-the-section-cuts-each-polygon-by-its-plane.md
    - Engine.Tests/Kernel/PlaneCutTests.cs
    - Engine.Tests/Drawing/SectionCutTests.cs
  modify:
    - Engine.Kernel/PlanarBoolean.cs
    - Engine.Kernel/Bsp.cs
    - Engine.Core/Drawing/SectionCut.cs
    - docs/adr/0057-planar-subtract-in-the-owned-kernel.md
    - docs/adr/0169-the-section-view-of-a-sheet.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - 3DEngine.App/**
---

# TASK-0445 — The section of a sheet cuts each polygon of a solid by its plane in the owned kernel, with one cap

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Finding E222 of the codebase review of 2026-11-18 (b): the section of a sheet (R30h, ADR-0169 item 3,
`Engine.Core/Drawing/SectionCut.cs`) made its cut as an exact subtract of a large box from each solid, under the serial lock
of the session. A torus of 4,096 polygons took 1.8 s for its view alone and 25.5 s with one section; a sphere of radius 20
took 1.1 s and 3.6 s. The subtract builds a partition of the whole solid and breaks the cap into many pieces, and the B-Rep
of that result takes most of the rest.

TASK-0416 (P0.167, v0.405) kept the last cuts with their B-Rep, and TASK-0426 (P0.171, v0.410, finding E226) kept one cut for
each pair of a view and a body. The reviews of 2026-11-20 and of 2026-11-21 (two) say "fixed in part": the first read after
each change of a body still pays the whole subtract. Each review gives the correction "a cut of each polygon by the plane,
with one cap from the cut segments", which needs the kernel. Row R26 (the section view in the window) names the same kernel
boundary, "section by a plane", so the operation must serve R26 later.

## Goal

The owned kernel cuts a solid by a plane in work that grows with the count of its polygons, and the section of a sheet uses
that cut with the same result as the subtract of the box.

## Scope (in)

1. `PlanarBoolean.Cut(solid, point, normal, cap)`: the part behind the plane through the point, with exact split points and
   the cap faces on the plane with the tag `cap` (ADR-0057 item 10).
2. `SectionCut.Apply` uses the cut; the cache of TASK-0416 and TASK-0426 stays.
3. A count of the work of the two paths (`Bsp.SplitsOnThisThread`), and a test that compares the two paths on a box, a
   plate with a hole, a sphere, a cone through its axis and the panels of the bookcase.
4. ADR-0057 item 10, ADR-0169 item 3 and "Consequences", the roadmap row P0.178, the ledger entry v0.429, the counts of the
   tests.

## Scope (out)

- Row R26 itself: a section in the window, a pad for the offset, a datum as a cut plane.
- A cache of the B-Rep of a view with no section (the review names it, but it is not E222).
- A change of `Engine.Contracts`, of a command, of an event or of the Document; no new diagnostic code.
- A change of the subtract, the union or the intersection of the kernel.

## Acceptance criteria

- [x] A box cut at z = 3 keeps the part with z ≤ 3 and one cap of 20 by 20 with the tag of the cap, on the canonical plane;
  the normal −z keeps the other part.
- [x] A slanted plane splits each polygon that crosses it at exact points on the plane and on its edges; the two sides give
  the exact volume of the box.
- [x] A face on the plane stays when it looks along the normal and goes when it looks away; a normal of zero is refused.
- [x] A plate with a square hole gives two caps through the hole and one cap with a hole across it.
- [x] On 33 cut planes of a box, a plate with a hole, a sphere, a cone and the five panels of the bookcase, the cut and the
  subtract of the box give the same B-Rep faces (plane, tag and loops), the same exact volume, the same cap area on the
  plane with the tag of the view, the same lines of the view and the same hatch; for each solid but the sphere also the same
  polygons.
- [x] The cut of the sphere of radius 20 splits 10,220 times against 6,132,711, an exact count in a test.
- [x] An injection in each part makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
  (Local: each passes. Continuous integration runs after the push.)

## Decisions

- One operation of the kernel, `PlanarBoolean.Cut`, with a point and a normal as exact rationals and the tag of the cap as
  arguments, so R26 can use it with its own tag. It keeps the side behind the plane; the normal points to the side that goes.
  ADR-0057 gets item 10. No new ADR: no shape of `Engine.Contracts`, no command and no event changes.
- The parts of the polygons come from `Bsp.Split`, the split of the subtract, so each part, its vertices and its tag are
  those of the subtract. A face on the plane follows the rule of the subtract: it stays when it looks along the normal.
- The cap is the region that the edges of the kept parts on the plane bound. The edges on one line split at each end of the
  others, and opposite pieces cancel, so a kernel solid with T-junctions also gives a closed boundary. A square on the plane,
  larger than the boundary, is clipped by a partition of the walls of those edges (the plane through an edge and the
  normal). So each piece of the cap is convex, as each polygon of the kernel is, and a cap with holes or with several parts
  needs no new code. The work of the cap grows with the square of the count of its edges, not of the polygons of the solid.
- The comparison is at the faces of the B-Rep and not at the polygons. The subtract also splits the polygons of the solid by
  the partition of the solid itself, where a mesh is not exactly convex: the facets of a sphere are the doubles of sines.
  So for the sphere the two paths give the same faces and lines, but the subtract gives more polygons. The test compares the
  polygons too for each other solid.
- The tag and identity rules of ADR-0058 stay: each part keeps the tag of its face, and each cap piece has the tag of the
  view, as each face of the box had. The Document, the log and a replay do not change; the cut is presentation.
- The lines of a view are the same, but a line can have its two ends in the other order, because the B-Rep numbers its
  vertices in the order of the polygons. No test, file or record reads that order as a meaning.

## Outcome

Status: Active · commit (the merge of the branch p0178-plane-cut) · v0.429

## Method

Mechanical: the old `SectionCut.Apply` moved into the test as the reference `BoxCut`; the roadmap rows, the counts of
`eng/test-floor.txt`, the History lines of ADR-0057 and ADR-0169.

Judgement: the cap from the walls of its boundary edges, and the cancel of opposite pieces on one line. The proof of the
same region: both results are closed and have the same faces of the solid, so the two caps have one boundary on one plane,
and two regions of one plane with one boundary are the same region. The test also compares the cap faces, their area, the
lines and the hatch.

Weakest: a solid that is not closed (a kernel fault) gives a boundary that is not closed, and the partition of the walls then
gives some region; the subtract gives some result there too, but no test compares the two. The count of a cap of many edges
grows with the square of that count (a section with 10,000 edges on the plane needs about 50 million splits).

Counts of the work (`Bsp.SplitsOnThisThread`, `SectionCutTests.The_Cut_Of_A_Sphere_Splits_Far_Fewer_Polygons_Than_The_Subtract_Of_A_Box`):
the sphere of radius 20 (3,968 triangles) cut through its centre along Front with the look "left". The subtract of the box:
6,132,711 splits, 3,025 polygons, 37 cap pieces. The cut: 10,220 splits, 2,074 polygons, 28 cap pieces.

Times (a probe program in `%TEMP%\p0178`, not a test; three rounds, each with a new session, the last two given; the native
backend). GetSheet of a sheet with the Front view of the object, then the first read after AddSheetSection of Front with the
look "left" through the origin:

| Object, build | View alone | First read with a section, before | After |
|---|---|---|---|
| Sphere of radius 20, Debug | 1.31 s | 3.43 to 3.46 s | 1.81 to 1.83 s |
| Sphere of radius 20, Release | 0.74 to 0.85 s | 1.98 to 2.01 s | 1.14 to 1.16 s |
| Torus of radii 30 and 10, Release | 1.06 to 1.18 s | 33.2 to 33.4 s | 1.73 s |

The lines of the section of the sphere and of the torus are the same before and after, up to the order of the two ends of a
line (a script compared the two lists).

Injections (`inject.js` in `%TEMP%\p0178`), each restored byte for byte (sha256). Each of the nine injections made a named
test fail:

- the side in front of the plane kept, a cap with no pieces, a crossing polygon kept whole, and a cap with the tag of a face
  of the solid: `A_Cut_Keeps_The_Part_Behind_The_Plane_And_Closes_It_With_One_Cap` or
  `A_Polygon_That_Crosses_The_Plane_Splits_At_Exact_Points_On_The_Plane`, and the comparison of the box;
- a split point off the plane (`Bsp.Split`, t + 1/1,000,000): `A_Polygon_That_Crosses_The_Plane_Splits_At_Exact_Points_On_The_Plane`;
- a face on the plane kept when it looks away, and two opposite edges on the plane that do not cancel:
  `A_Face_On_The_Plane_Stays_When_It_Looks_Along_The_Normal_And_Goes_When_It_Looks_Away`, and the comparison of the box for
  the first;
- the section on the old path, the subtract of the box:
  `The_Cut_Of_A_Sphere_Splits_Far_Fewer_Polygons_Than_The_Subtract_Of_A_Box`, the only test that a correct result on the
  old path fails;
- the section with the normal away from the eye: the comparison of the box and four more.

The first run of the split point off the plane ran the comparison tests too; the subtract of the sphere with points off the
plane did not end in 20 minutes, and the run was stopped and the line restored by hand. The second run of that injection
runs `PlaneCutTests` only.

## Progress

- 2026-11-21: opened after K15c (TASK-0444, v0.428, not merged yet), branch `p0178-plane-cut` from `53772dce`. The cut of the
  kernel, `SectionCut.Apply` on it, ten new tests, ADR-0057 item 10, ADR-0169 item 3, the roadmap row P0.178 and the ledger
  entry v0.429.
