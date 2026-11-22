---
id: 0454
title: The command and each reader of a cut make one exact boolean for each change of the Document
status: Active
phase: P0.184
opened: 2026-11-22
depends-on: [0453]
governed-by: [0004, 0012, 0016, 0021, 0028, 0031, 0033, 0037, 0054, 0055, 0057, 0058, 0059, 0060, 0076, 0083, 0089, 0112, 0143, 0180, 0181]
writes:
  create:
    - tasks/TASK-0454-the-command-and-each-reader-of-a-cut-make-one-boolean.md
    - Engine.Tests/Features/KeptBooleanTests.cs
  modify:
    - Engine.Kernel/PlanarBoolean.cs
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Core/Features/Regeneration.cs
    - Engine.Core/Commands/SubtractCommandHandler.cs
    - Engine.Core/Commands/BooleanStep.cs
    - Engine.Core/Commands/UnionCommandHandler.cs
    - Engine.Core/Commands/IntersectCommandHandler.cs
    - Engine.Tests/Commands/CreateSphereAndTorusCommandTests.cs
    - docs/adr/0057-planar-subtract-in-the-owned-kernel.md
    - docs/adr/0059-references-by-the-identity-of-a-face.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - 3DEngine.App/**
---

# TASK-0454 — The command and each reader of a cut make one exact boolean for each change of the Document

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

TASK-0452 (P0.182, 2026-11-22) found the defect and put it out of its scope: "The command `Subtract` subtracts the solids for
the faults of its datums (`SubtractCommandHandler.GoneFaces`), and the query of the faces subtracts them again". The exact
subtract of the owned kernel is the costly step: the plate of 20 by 20 by 4 less the sphere of radius 5 took about 3.5 s of
exact work before P0.182, and still seconds after it.

The second subtract came from the walk of the projection (ADR-0058 item 2). `GetFaces` calls `KernelProjection.Of`, which walks
from the body to its creator and calls `PlanarBoolean.Subtract` again for each `Subtract` on the way. `KernelProjection.OfRecord`
keeps the solid of a committed geometry key (ADR-0059 item 2), but the command makes its boolean before the commit, so no key
held it, and the walk itself kept nothing. The same is true of `Union` and `Intersect` (ADR-0181), of each later reader of the
solid (the measures, `GetConnectors`, `CheckInterference`, the views of a sheet, the STEP export), and of each walk of a body
whose history holds a boolean under a later step, for example a `Translate` after the cut.

## Goal

Each exact boolean of the history of a body is made one time for each change of the Document, and not one time for each reader.

## Scope (in)

1. `KernelProjection.Boolean`: the kept result of each boolean step under the Document and the step, with the kind of the step
   and its two operand solids. The command of the step, the regeneration and each walk take it.
2. The bound of the kept results (256, the oldest use goes first), the removal of a step that is no longer in effect, and a lock
   for the threads.
3. `PlanarBoolean.BooleansOnThisThread`: the count of the booleans of two solids on this thread, for the tests.
4. The tests, the amendment of ADR-0059 item 2, a History line of ADR-0057, the bounds of the counts of
   `A_Sphere_Cuts_A_Hole_Of_One_Name`, the roadmap rows P0.184, the ledger entry v0.438 and the counts of the tests.

## Scope (out)

- The time of the first read after the 150 turns of K10a (row K10a, register entry R-0070). Its walk holds no boolean: it is 450
  steps of `Translate` and `Rotate` with exact numbers that grow. This task measures it only.
- The B-Rep of the result (about 1.3 s of the query of the faces of the sphere cut). `GetFaces` keeps it already (ADR-0058
  item 5).
- The booleans that are not a step of the history of a body: the pairs of `CheckInterference`, the probe of `GetDistance` and the
  cut of a section (`SectionCut`, which keeps its own results).
- A change of `Engine.Contracts`, of a command, of an event, of the log or of the Document; no new diagnostic code.

## Acceptance criteria

- [x] One `Subtract` and one `GetFaces` of its body make one exact boolean, and not two; the solid of the projection and the kept
  solid of the key add none. One `Union` and one `Intersect` with their readers make one each.
- [x] A change of an operand (an edit of the distance of the plate) makes the boolean again, and the solid has the new volume.
- [x] A kept result answers only the same Document, the same kind of step and the same operands, value for value.
- [x] An undo of a step removes its kept result at the next walk of the Document, and the kept results stay at 256.
- [x] The results are the same: the digests of the exact solids, the faces and the records of three documents are the same
  before and after, and the same after a replay of their logs (see "Method").
- [x] An injection in each part makes a named test fail (see "Method").
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

- **The key: the Document and the step, then the exact content.** The identifier of a step does not name one result: the
  operands of a step change with an edit of an earlier step (ADR-0055), and the regeneration reads the state of an edit before
  its commit (ADR-0059 item 1). So the identifier of the step only finds the entry. The kept result
  answers when the kind of the step is the same and each polygon of each operand is the same: the vertices, the plane and the
  tag, in order. The boolean is a function of that content only, so a kept result is the result that a new boolean gives. A
  hash of the content was not chosen: an exact compare costs one pass over the polygons, and it has no collision.
- **One result for each step.** The entry of a step keeps its last result only, as the cut of a section keeps one cut for each
  pair of a view and a body (finding E226). An undo of an edit makes the boolean again; a read after a command that changes no
  operand makes none.
- **The bound and the removal.** At most 256 results stay; the result of the oldest use goes first. Each walk from a committed
  Document (`KernelProjection.Of`) first removes the results of the steps of that Document that are no longer in effect: an undo
  removes a step, and a command that fails after its handler commits no step. A read that runs between a command and its commit
  removes the result of that command too; the next reader makes the boolean again, and the result is the same.
- **The threads.** One lock (the gate of the kept solids) holds the dictionary and the order of use. The compare of the content
  and the boolean run outside the lock, so two threads can make the same boolean one time each; each result is the same.
- **The frame of the walk.** The walk takes the identifier of the Document as one more parameter. The 150 turns of K10a then
  overflowed the stack of the test, because the recursive method `Solid` had a larger frame. The subtract is now a step of
  `PairSolid`, as the union and the intersection are (TASK-0433), with the same message, and the frame is smaller again.
- **The count in the kernel.** The count of the booleans is in `PlanarBoolean`, not in the projection, so a reader that calls
  the kernel past the kept results is counted too. It is a thread-static field, as the counts of TASK-0445 and TASK-0452.
- **Union and Intersect.** `BooleanStep` takes the command of the step in place of the function of the kernel, so the two
  commands keep their result under their step as `Subtract` does.
- **Replay.** A kept result is not in the log and not in the Document. A replay makes each boolean again, or takes a kept result
  that is the same value, so the replay gives the same Document.

## Outcome

Status: Active · commit (the merge of the branch p0184-one-subtract) · v0.438

## Method

Mechanical: the History lines of ADR-0057 and ADR-0059, the bounds of `A_Sphere_Cuts_A_Hole_Of_One_Name`, the roadmap rows, the
ledger entry and the counts of `eng/test-floor.txt`.

Judgement: see "Decisions".

Weakest: a fault of the threads shows only by chance. `Many_Threads_Share_The_Kept_Results` failed with no lock in the one run of
that injection, but a test of threads can pass with a fault. Also, the removal of a step that is no longer in effect runs only
in a walk from a committed Document; a read between a command and its commit removes the result of that command, which costs
one more boolean and no wrong result. No test holds that case.

Counts (`PlanarBoolean.BooleansOnThisThread`, `Bsp.SplitsOnThisThread` and `Bsp.ExactSignsOnThisThread`, the command `Subtract`
of the plate by the sphere and the query `GetFaces` of its body):

| Count | Before | After |
|---|---|---|
| Exact booleans | 2 | 1 |
| Splits of a polygon by a plane | 13,875,936 | 6,937,968 |
| Exact signs | 189,876 | 94,938 |

The bounds of `A_Sphere_Cuts_A_Hole_Of_One_Name` are now 1.5 times the counts after (10,406,952 and 142,407), and the test holds
the count of the booleans at 1. In the probe (below) the three documents made 3, 14 and 6 booleans before (the command, the
queries and the walks of the digest) and 1, 4 and 4 after: one for each boolean step and for each edit of an operand.

Times (a probe test in `%TEMP%\p0184`, removed before the commit; Debug, the computer of the owner; one run each, so a
difference of less than about 0.5 s is noise):

| Work | Before | After |
|---|---|---|
| The command `Subtract` of the plate by the sphere and the query `GetFaces`, in a session | 5.3 s and 5.6 s | 4.6 s, 3.7 s and 4.3 s |
| Of that, the query `GetFaces` | 3.0 s and 3.0 s | 1.6 s, 1.5 s and 1.6 s |
| `A_Sphere_Cuts_A_Hole_Of_One_Name`, as the runner gives it | 5 s | 4 s |
| The first read of the solid after the 150 turns of K10a | 2.2 s | 2.4 s and 2.3 s |

The query of the faces no longer subtracts: its time is the walk (the mesh of the sphere, the compare of the operands) and the
B-Rep. The first read after the 150 turns does not change: its walk holds no boolean (see "Scope (out)").

The same results: the probe built three documents with fixed identifiers: the plate less the sphere; a chain of a cut with a
datum on a face that it removes, a `Translate`, a second cut, a `Rotate`, a `Union` and an `Intersect` with an undo and a redo;
and an extrude with a cut, a datum on a gone face, two moves of a point of the sketch and an edit of the distance. For each live
body it wrote the tag, the plane and the exact vertices of each polygon of the solid of the projection, its exact volume and the
faces and edges of `GetFaces`, and for each datum its fault and its frame, and gave the sha256 of the text. It did the same after
a replay of the log of each document in a new session. The six digests before and after are the same:
`1DDD767E…` (the sphere), `9B1BB73F…` (the chain) and `5522F8D3…` (the edit), and each replay gives the digest of its session.
`A_Replay_Gives_The_Same_Records_And_Solids_As_The_Session_That_Kept_The_Results` holds the same for a session with edits, an
undo and a redo.

Injections (`inject0454.js` in `%TEMP%\p0184`), each restored byte for byte (sha256). The script changes one exact text, builds
`Engine.Tests` and runs `KeptBooleanTests`, `A_Sphere_Cuts_A_Hole_Of_One_Name`, `FaceGoneTests`, `UnionIntersectCommandTests`
and `GetFacesTests` (41 tests) with no shell, and lists the tests that failed. The short names: One (`One_Boolean_Command_And_Its_Readers_Make_One_Boolean`),
Change (`A_Change_Of_An_Operand_Makes_The_Boolean_Again`), Same (`A_Kept_Result_Needs_The_Same_Kind_And_The_Same_Operands`),
Effect (`A_Step_That_Is_No_Longer_In_Effect_Leaves_The_Kept_Results`), Limit (`The_Kept_Results_Stay_At_Their_Limit`), Threads
(`Many_Threads_Share_The_Kept_Results`) and Sphere (`A_Sphere_Cuts_A_Hole_Of_One_Name`).

| Injection | File | The tests that failed |
|---|---|---|
| The key ignores the operands | `Engine.Core/Features/KernelProjection.cs` | Change, Same, `FaceGoneTests.An_Edit_That_Moves_A_Face_Out_Of_And_Into_A_Cut_Changes_The_Fault`, `UnionIntersectCommandTests.A_New_Distance_Of_An_Extrude_Rebuilds_The_Union_And_The_Intersection` |
| The key ignores the kind of the step | `Engine.Core/Features/KernelProjection.cs` | Same |
| The key ignores the Document (`Guid.Empty`) | `Engine.Core/Features/KernelProjection.cs` | Same, Effect, Limit |
| No removal of a step that is no longer in effect | `Engine.Core/Features/KernelProjection.cs` | Effect |
| No bound (1,000 times the limit) | `Engine.Core/Features/KernelProjection.cs` | Limit |
| No lock around the lookup and the store | `Engine.Core/Features/KernelProjection.cs` | Threads, Same, Limit, Sphere |
| The walk calls the kernel past the kept results | `Engine.Core/Features/KernelProjection.cs` | One, Change, Sphere |
| The command `Subtract` calls the kernel past the kept results | `Engine.Core/Commands/SubtractCommandHandler.cs` | One, Effect, Sphere |
| `Union` and `Intersect` call the kernel past the kept results | `Engine.Core/Commands/BooleanStep.cs` | One |
| The regeneration keeps its results under another Document | `Engine.Core/Features/Regeneration.cs` | Change |
| The count of the booleans counts nothing (`booleans += 0`) | `Engine.Kernel/PlanarBoolean.cs` | One, Change, Same, Sphere |

The first form of the lock injection did not build (a name of a local was used two times), and the second form passed with the
first form of Threads: 512 calls on 128 steps. Threads now makes 16,384 calls on 512 pairs of a Document and a step, more than
the limit, so the order of use changes at each call; with no lock it fails, and the corrupted list fails three more tests.

## Progress

- 2026-11-22: branch `p0184-one-subtract` from `0f84f26d` (P0.183, TASK-0453, v0.437, not merged yet). The kept results of the
  boolean steps in `KernelProjection`, the count of the booleans in `PlanarBoolean`, seven new tests in `KeptBooleanTests`, the
  bounds of the sphere test, the amendment of ADR-0059 item 2, the History line of ADR-0057, the roadmap rows P0.184 and the
  ledger entry v0.438.
