---
id: 0452
title: The test of the cut of a plate by a sphere holds the work of the owned kernel by two counts
status: Active
phase: P0.182
opened: 2026-11-22
depends-on: [0451]
governed-by: [0057, 0076, 0089, 0112]
writes:
  create:
    - tasks/TASK-0452-the-cut-of-a-plate-by-a-sphere-holds-its-work-by-counts.md
    - Engine.Tests/Kernel/KernelWorkTests.cs
  modify:
    - Engine.Kernel/Bsp.cs
    - Engine.Kernel/KernelSolid.cs
    - Engine.Tests/Commands/CreateSphereAndTorusCommandTests.cs
    - docs/adr/0057-planar-subtract-in-the-owned-kernel.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - 3DEngine.App/**
---

# TASK-0452 — The test of the cut of a plate by a sphere holds the work of the owned kernel by two counts

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0073 (class debt, opened 2026-11-21, due 2027-05-20): "No test holds the cost of the cut of a plate by a
sphere in the owned kernel". TASK-0435 (P0.173, v0.419) removed the limit of 300 s of
`CreateSphereAndTorusCommandTests.A_Sphere_Cuts_A_Hole_Of_One_Name`, because no time decides a test (ADR-0173 item 4). The cut
took about 10 s on the computer of the owner and 64 s on a macOS runner, half of the hang limit of 2 minutes of the step
"Test". A count of the work needed a change of `Engine.Kernel`, which the write set of TASK-0435 forbade. The exit of the
entry: "a task of the kernel gives the count of the exact signs of a cut in a form that a test reads, and the test of the cut
holds that count below a bound; or the owner accepts the hang limit as the only guard".

TASK-0445 (P0.178, v0.429) added the count of the splits of a polygon by a plane, `Bsp.SplitsOnThisThread`, for the cut by a
plane. This task adds the count of the exact signs beside it.

## Goal

The test of the cut of a plate by a sphere holds the splits and the exact signs of the owned kernel below 1.5 times the counts
of today, and R-0073 closes.

## Scope (in)

1. `Bsp.ExactSignsOnThisThread`: the count of the sides that the filter of doubles cannot decide, on this thread, beside
   `Bsp.SplitsOnThisThread`.
2. The bounds of the two counts in `A_Sphere_Cuts_A_Hole_Of_One_Name`, at 1.5 times the counts of today, as T187 did for the
   fixed lengths (P0.174).
3. A correction of the work of the subtract that keeps each result the same, when the counts and a probe show where the time
   goes: the exact volume as a sum in pairs (see "Decisions").
4. The tests of the counter and of the sum, the History line of ADR-0057, the close of R-0073, the roadmap row P0.182, the
   ledger entry v0.436 and the counts of the tests.

## Scope (out)

- A change of the partition of the subtract, for example another choice of the plane of a node. It changes the parts of the
  polygons, so the result is not the same (see "Decisions").
- The second subtract of the same pair: the command `Subtract` subtracts the solids for the faults of its datums
  (`SubtractCommandHandler.GoneFaces`), and the query of the faces subtracts them again (`KernelProjection.OfRecord`). That is
  `Engine.Core`, outside R-0073.
- The B-Rep of the result (about 1.3 s of the query of the faces).
- A change of `Engine.Contracts`, of a command, of an event or of the Document; no new diagnostic code.

## Acceptance criteria

- [x] `Bsp.ExactSignsOnThisThread` counts each exact sign of a side on this thread only, and a test holds it.
- [x] `A_Sphere_Cuts_A_Hole_Of_One_Name` holds 13,875,936 splits and 189,876 exact signs below 1.5 times each count, and
  fails when a count is zero.
- [x] The volume is a sum in pairs; the polygons, the tags and the exact volume of nine cuts, unions, intersections and meshes
  are the same, byte for byte, before and after (see "Method").
- [x] An injection in each part makes a named test fail.
- [x] R-0073 is closed with the exit "resolved".
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

- **Two counts, not one.** A probe counted and timed each step of the subtract of the plate (20 by 20 by 4) by the sphere of
  radius 5 (3,968 triangles). One subtract makes 6,937,968 splits and 94,938 exact signs; 5,979,455 of the splits build the
  partition of the sphere, a chain of about one node for each triangle. A timer around the exact signs measured about 80 ms
  of the 3.5 s of a subtract (Debug), so the exact signs are not the work of today. But the filter of doubles of TASK-0289
  took the cut from about 3 minutes to about 10 s, and a filter that decides less makes the count of the exact signs grow
  and not the count of the splits. A partition that splits more makes the count of the splits grow. So the test holds both
  counts, each at 1.5 times the count of today, with the form of T187 (`count > 0 && count <= today * 3 / 2`).
- **A count on the thread.** Each count is a thread-static field, as the count of the splits of TASK-0445: one increment of a
  field of the thread, and no lock. The probe showed that the command and the query complete on the thread of the test: the
  handler returns a completed task, and the serial locks are free. A count of zero says that the work went to another
  thread, and the test fails then.
- **The time of the volume.** The probe showed that the exact volume of the result took 2.2 s of the 3.5 s to 3.9 s of a
  subtract (Debug). `Subtract` reads the sign of the volume to refuse an empty result. The sum in the order of the polygons
  kept a partial sum with a denominator of up to 69,520 bits, against 985 bits for the largest term and 400 bits for the
  sum. A sum in pairs (the first and the second term, the third and the fourth, then the same for the sums) takes 0.16 s.
  A sum of exact rationals in lowest terms has one value in each order, so each volume, each sign and each result is the
  same. The counts of the test do not change: the volume makes no split and no sign.
- **The partition stays.** The chain of the partition of a convex mesh costs about the square of its count of triangles. A
  better plane for each node makes fewer splits, but it gives other parts of the polygons, so the polygons of the result
  change. No box prune or cache of a side helps the chain: almost each polygon is behind each plane of the chain, and the filter
  decides each such side from three doubles already.
- No public member changes in the kernel: the count and `KernelSolid.SumInPairs` are internal. ADR-0057 gets a History line
  with no change of an item, as for TASK-0446.

## Outcome

Status: Active · commit (the merge of the branch p0182-cost-of-a-cut) · v0.436

## Method

Mechanical: the History line of ADR-0057, the close of R-0073, the roadmap rows, the ledger entry and the counts of
`eng/test-floor.txt`.

Judgement: see "Decisions".

Weakest: the counts do not hold the time of the volume. A return to the sum in order passes the bounds of the test and makes
the cut about two times slower again. `KernelWorkTests` holds that the sum in pairs gives the value of the sum in order, not
its cost.

Counts (`A_Sphere_Cuts_A_Hole_Of_One_Name`, the command `Subtract` and the query `GetFaces`, two subtracts): 13,875,936
splits and 189,876 exact signs, the same before and after the sum in pairs. The bounds are 20,813,904 and 284,814.

Times (a probe test in `%TEMP%\p0182`, removed before the commit; Debug, the computer of the owner, one run each):

| Work | Before | After |
|---|---|---|
| The volume of the result of the plate by the sphere | 2.2 s (in order) | 0.16 s (in pairs) |
| The command `Subtract` and the query `GetFaces` of the test, in a session | 8.6 s and 9.1 s | 4.6 s |
| The torus of radii 10 and 3 less the plate, with one more volume for the digest | 67.2 s | 10.0 s |

The same results: the probe wrote, for nine cases, the tag, the plane and the exact vertices of each polygon and the exact
volume, and gave the sha256 of the text. The cases: the plate less the sphere, the sphere less the plate, their union and
their intersection, the sphere of radius 20 less a large box, the torus less the plate, the cut by a plane of the sphere of
radius 20, and the meshes of the sphere and of the torus (`KernelSolid.Closed` reads the sign of the volume too). The nine
digests before and after are the same. Each test of `Engine.Tests` that holds an exact volume passes too.

Injections (`inject0452.js` in `%TEMP%\p0182`), each restored byte for byte (sha256). The script builds `Engine.Tests` and runs
`KernelWorkTests`, `A_Sphere_Cuts_A_Hole_Of_One_Name` and the tests of the kernel (`PlanarSubtractTests`,
`PlanarUnionIntersectTests`, `PlaneCutTests`, `SideFilterTests`) with no shell:

| Injection | File | The tests that failed |
|---|---|---|
| The count counts nothing (`exactSigns += 0`) | `Engine.Kernel/Bsp.cs` | `The_Count_Of_The_Exact_Signs_Holds_Each_Side_That_The_Filter_Cannot_Decide_On_This_Thread`, `A_Sphere_Cuts_A_Hole_Of_One_Name` ("0 exact signs") |
| The count is not thread-static | `Engine.Kernel/Bsp.cs` | `The_Count_Of_The_Exact_Signs_Holds_Each_Side_That_The_Filter_Cannot_Decide_On_This_Thread` |
| The count of the splits of today 100 times smaller | `CreateSphereAndTorusCommandTests.cs` | `A_Sphere_Cuts_A_Hole_Of_One_Name` |
| The count of the exact signs of today 100 times smaller | `CreateSphereAndTorusCommandTests.cs` | `A_Sphere_Cuts_A_Hole_Of_One_Name` |
| The subtract builds the partition of b one more time | `Engine.Kernel/PlanarBoolean.cs` | `A_Sphere_Cuts_A_Hole_Of_One_Name` (25,834,846 splits) |
| The filter of doubles decides nothing (`Known` false) | `Engine.Kernel/Bsp.cs` | `The_Count_Of_The_Exact_Signs_Holds_Each_Side_That_The_Filter_Cannot_Decide_On_This_Thread`, `A_Sphere_Cuts_A_Hole_Of_One_Name` (42,903,216 exact signs) |
| The sum in pairs drops the last value of an odd count | `Engine.Kernel/KernelSolid.cs` | 19 tests: `The_Sum_In_Pairs_Is_The_Sum_In_Order`, `The_Volume_Of_A_Cut_With_Exact_Split_Points_Is_The_Sum_In_Order` and 17 more tests of the kernel |
| The sum in pairs adds the first value of a pair two times | `Engine.Kernel/KernelSolid.cs` | 18 tests: the two above and 16 more tests of the kernel |
| The filter of doubles decides with 1e-6 in place of 1e-14 | `Engine.Kernel/Bsp.cs` | none |

The last form passes. It is not a fault: each side that the filter does not decide goes to the exact sign, so the result is
the same, and the count of the exact signs stayed below its bound. The bound holds a filter that is gone or that decides
much less, not each change of its constant. The first form of the first injection removed the line `exactSigns++`; the build
failed, because a field that nothing assigns is a warning, and a warning is an error. The form above assigns the field.

## Progress

- 2026-11-22: branch `p0182-cost-of-a-cut` from `a4e957e7` (P0.181, TASK-0451, v0.435, not merged yet). The count of the
  exact signs, the bounds of the sphere test, the volume as a sum in pairs, three new tests, the close of R-0073, the History
  line of ADR-0057, the roadmap row P0.182 and the ledger entry v0.436.
