---
id: 0457
title: The tests that fill or count the kept booleans run alone, so the order of the tests does not decide their result
status: Active
phase: P0.186
opened: 2026-11-22
depends-on: [0456]
governed-by: []
writes:
  create:
    - tasks/TASK-0457-the-tests-of-the-kept-booleans-run-alone.md
    - Engine.Tests/Features/KeptBooleanCollection.cs
    - Engine.Tests/Governance/KeptBooleanCollectionGateTests.cs
  modify:
    - Engine.Tests/Features/KeptBooleanTests.cs
    - Engine.Tests/Commands/CreateSphereAndTorusCommandTests.cs
    - docs/templates.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
---

# TASK-0457 — The tests that fill or count the kept booleans run alone, so the order of the tests does not decide their result

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The run 45021528072 on `main`, the merge of P0.184 (TASK-0454) at `58ea382`, failed on Windows. The test
`Engine.Tests.Commands.CreateSphereAndTorusCommandTests.A_Sphere_Cuts_A_Hole_Of_One_Name` failed at line 180,
`Assert.Equal(1, booleans)`: the actual count was 2. The same code passed in the runs of the branch. On 2026-11-22 the owner
answered "Yes, both" to the question "Re-run 45021528072 and fix the test race as P0.186 after P15a?".

P0.184 keeps the result of each boolean step in one static store of the process: `Booleans` and `BooleanUse` in
`Engine.Core/Features/KernelProjection.cs`, at most `BooleanLimit` (256) results, and the result of the oldest use goes first
(ADR-0059 item 2). xUnit runs the test classes in parallel. Two tests of `Engine.Tests/Features/KeptBooleanTests.cs` add more
results than the limit: `The_Kept_Results_Stay_At_Their_Limit` (264 new steps) and `Many_Threads_Share_The_Kept_Results` (4
Documents by 128 steps, 512 keys, 16,384 calls). When one of them runs between the command `Subtract` of the sphere test and its
query of the faces, the kept result of the cut leaves, and the query makes the boolean again.

The product is correct: a result that leaves is made again with the same content. Only a test that counts the booleans of the
kernel or the kept results depends on the tests that run beside it.

## Goal

Each test class that fills the kept booleans past their limit, or that counts the booleans or the kept results, runs alone.

## Scope (in)

1. The collection "kept booleans" with parallel runs off (`Engine.Tests/Features/KeptBooleanCollection.cs`), in the form of
   the collection "real host" (`Engine.Tests/Http/RealHostCollection.cs`).
2. The attribute `[Collection("kept booleans")]` on `KeptBooleanTests` and on `CreateSphereAndTorusCommandTests`, the two
   classes whose source names `BooleansOnThisThread`, `BooleansKept`, `IsBooleanKept` or `BooleanLimit`.
3. A gate that holds the rule (`KeptBooleanCollectionGateTests`), and a test that makes the race on purpose.
4. The row of the gate in the gate table of `docs/templates.md` (section 5, `DocumentPathGateTests`), the roadmap rows
   P0.186, the ledger entry v0.441 and the count of `Engine.Tests` in `eng/test-floor.txt`.

## Scope (out)

- A change of the product: of the store, of its limit or of the count of the booleans. `Engine.Core` and `Engine.Kernel` are
  in the forbid list.
- The tests that count the splits of the kernel and do not use the kept results (`SectionCutTests`, `KernelWorkTests`). They
  call the kernel directly, so the store does not change their counts.
- The other test projects. Each test project runs in its own process, and no test of another project names the four members.

## Acceptance criteria

- [x] The two classes run in the collection "kept booleans", and xUnit runs that collection after the parallel collections and
  alone (a probe, see "Method").
- [x] A test makes the race on purpose and shows the count 2 and the same content of the result.
- [x] A gate fails by the name of the class when a class that names one of the four members is not in the collection, and when
  the collection is not defined with parallel runs off (see the injections in "Method").
- [x] No change of `Engine.Core`, `Engine.Kernel` or the limit.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

- **A collection with parallel runs off, and not a seam in the store.** A test seam that gives each test its own store, or a
  store for each Document, keeps the tests parallel. But it changes `KernelProjection`, which is product code, and the store is
  one store of the process by ADR-0059 item 2: a test of the limit must fill that store. A second form, a lock that the tests
  take, also stops a test of another class that makes a boolean, and it is a rule in text only. The collection changes two
  attributes and adds one file. Its cost is the time of the two classes, which now run after the other tests (see "Method").
- **The whole class of the sphere test.** `A_Sphere_Cuts_A_Hole_Of_One_Name` is the only test of
  `CreateSphereAndTorusCommandTests` that counts the booleans. A new class for that one test was not chosen: the other tests of
  the class take about 1.3 s together when they run alone, and the rule of the gate is then "a class that names a member",
  with no exception.
- **The names that the gate reads.** The gate reads the four members in the code of each source file of `Engine.Tests`, as
  identifiers, and not in a comment or a string. The scan uses the C# parser, as `DeterminismCallGateTests` does, so the
  attribute must be on the outermost class that holds the name. The gate is a gate file, so `eng/repo-gates` runs it too.
- **The race on purpose.** The test fills the store from a second thread, as a test of another class does, so the count of the
  booleans on the thread of the test is the count of the command and of the query only.

## Outcome

Status: Active · commit (the merge of the branch p0186-kept-boolean-tests) · v0.441

## Method

Mechanical: the attributes, the roadmap rows, the ledger entry and the count of `eng/test-floor.txt`.

Judgement: see "Decisions".

Weakest: the gate reads names. A test that makes a boolean of the projection and counts the splits of `Bsp`, with none of the
four names, is not seen. Today only `A_Sphere_Cuts_A_Hole_Of_One_Name` counts the splits after a boolean of the projection, and
it also counts the booleans.

The cause. The race test `A_Result_That_Other_Steps_Push_Out_Is_Made_Again_With_The_Same_Content` makes the failure of the run
45021528072 on purpose: after the command `Subtract`, a second thread adds 256 new results; the kept result of the cut leaves,
the query of the faces makes the boolean again, and the count on the thread of the test is 2. The solid that the query made
has the same exact content as the solid that left. Six runs of the two classes beside each other before the change gave no
failure, so the window is small on this computer; the run on `main` is the only observed failure.

`The_Kept_Results_Stay_At_Their_Limit` can also fail alone in theory: its last check `IsBooleanKept(document, steps[^1])` fails
when another test adds 256 results between its last boolean and the check. Before the change only
`Many_Threads_Share_The_Kept_Results` adds that many, and it is in the same class, so xUnit never ran the two at the same time.
The check `BooleansKept <= BooleanLimit` cannot fail, because the store removes the old results under the same lock as the add.
No other test of the suite names the four members, and no other test calls `KernelProjection.Boolean`.

The order of xUnit 2.9.3. A probe (three test classes, removed before the commit) gave a class of no collection that sleeps
3 s, a class in "kept booleans" and a class in "real host". In two runs each class of a collection with parallel runs off
started after the end of the parallel class, and the two collections ran one after the other.

The cost in time (`timing.js` in `%TEMP%\p0186`, Debug, the computer of the owner, four runs of all of `Engine.Tests` in
turn, with the two attributes taken out for "before" and restored byte for byte):

| Run | Before | After |
|---|---|---|
| Wall time of `dotnet test`, first pair | 83.8 s | 87.4 s |
| Wall time of `dotnet test`, second pair | 86.7 s | 86.6 s |
| The tests of the two classes, the sum of their times (second pair) | 58.5 s | 9.4 s |

The two classes now take about 9 s alone at the end of the run. Beside the other tests they took longer, so the time of the
whole run does not change by more than the noise (about 4 s).

Injections (`inject.js` in `%TEMP%\p0186`), each restored byte for byte (sha256). The script changes one exact text, builds
`Engine.Tests` and runs `KeptBooleanCollectionGateTests` (9 tests) with no shell. The short names: Class
(`Each_Test_Class_That_Reads_The_Kept_Booleans_Is_In_The_Collection_Kept_Booleans`) and Definition
(`Engine_Tests_Defines_The_Collection_Kept_Booleans_With_Parallel_Runs_Off`).

| Injection | File | The test that failed | Its message |
|---|---|---|---|
| No attribute on `KeptBooleanTests` | `Engine.Tests/Features/KeptBooleanTests.cs` | Class | `KeptBooleanTests.cs:22 class KeptBooleanTests names a shared member and is not in the collection "kept booleans"` |
| No attribute on `CreateSphereAndTorusCommandTests` | `Engine.Tests/Commands/CreateSphereAndTorusCommandTests.cs` | Class | `CreateSphereAndTorusCommandTests.cs:19 class CreateSphereAndTorusCommandTests names a shared member ...` |
| `KeptBooleanTests` in the collection "real host" | `Engine.Tests/Features/KeptBooleanTests.cs` | Class | `KeptBooleanTests.cs:23 class KeptBooleanTests names a shared member ...` |
| The collection with parallel runs on | `Engine.Tests/Features/KeptBooleanCollection.cs` | Definition | `... defines the collection "kept booleans" with parallel runs on` |
| The collection under another name ("kept boolean") | `Engine.Tests/Features/KeptBooleanCollection.cs` | Definition | `Engine.Tests defines the collection "kept booleans" 0 times, and not one time` |

The last injection matters because xUnit 2 makes a collection of its own for a name with no definition, and that collection
runs in parallel. The theory `The_Scan_Finds_Each_Class_That_Names_A_Shared_Member` holds the forms of the scan: a class with no
attribute, a class in another collection, a name in a nested class, a static import, the attribute with its namespace and its
suffix, and a name in a comment or a text only.

## Progress

- 2026-11-22: branch `p0186-kept-boolean-tests` from `486d4bfc` (P15a, TASK-0456, v0.440, not merged yet). The collection
  "kept booleans", the attribute on the two classes, the gate, the race test, the roadmap rows P0.186 and the ledger entry
  v0.441.
