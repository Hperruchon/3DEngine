---
id: 0442
title: The gate of governed-by builds the expression of each path pattern once, so it ends in less than a second
status: Done
phase: P0.177
opened: 2026-11-21
depends-on: [0441]
governed-by: []
writes:
  create:
    - tasks/TASK-0442-the-governance-gate-ends-in-seconds.md
    - Engine.Tests/Governance/PathPatternWorkTests.cs
  modify:
    - eng/write-set-check/TaskFiles.cs
    - Engine.Tests/Governance/ConstantStrings.cs
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
---

# TASK-0442 — The gate of governed-by builds the expression of each path pattern once, so it ends in less than a second

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

R-0074 (opened 2026-11-21): in attempt 1 of run 44869058349 the test host of `Engine.Tests` stopped on ubuntu-latest in
the step "Test". The test that did not complete was
`TaskGovernanceGateTests.Each_Open_Task_Lists_Exactly_The_Adrs_Whose_Affects_Intersect_Its_Write_Set`. The step took 247
seconds against 115 to 123 seconds in the other runs of ubuntu-latest.

TASK-0441 found that the step "Test" has a hang limit of 2 minutes for each test (`--blame-hang-timeout 2m`), and that a
test above the limit gives "Test host process crashed" with no dump. It measured the gate of R-0074 at 38 to 100 seconds
on the computer of the owner, and left the time of the gate to a task of its own.

`eng/repo-gates` compiles the same gate classes and runs them one after the other in the job "Gate".

## Goal

The gate of governed-by, and each other test of `Engine.Tests/Governance` above about 20 seconds, ends far below the hang
limit, and each gate keeps its rule and its messages.

## Scope (in)

1. `TaskFiles.PathPattern` (`eng/write-set-check`) keeps the expression of each pattern, so a pattern gives one
   expression for the life of the process.
2. `TaskFiles.PatternsBuiltOnThisThread`, a count of the expressions that one thread built.
3. `PathPatternWorkTests`: a pattern gives one expression built once, and the gate of governed-by builds at most one
   expression for each pattern of the ADRs and of the open tasks.
4. `ConstantStrings.Constants` binds the files on more than one thread, with the same constants in the same order.
5. R-0074 closes with the evidence.

## Scope (out)

- A change of a rule of a gate, of a message of a gate, or of the hang limit.
- A smaller set of the expressions that the compiler binds in `ConstantStrings` (see "Decisions").
- `DeterminismCallGateTests` (9 to 14 seconds) has its own compilation, below the limit of this task.

## Acceptance criteria

- [x] The gate of governed-by ends in less than one second on the computer of the owner (63 to 76 seconds before).
- [x] Three injections of a violation of the rule give the same failure messages before and after the change.
- [x] A test counts the expressions that the gate builds, and fails on the form before the change.
- [x] The constants of `ConstantStrings` are the same, in the same order, on one thread and on more than one thread.
- [x] Each injection makes a named test fail, and each file is restored byte for byte (sha256).
- [x] R-0074 is closed with the evidence.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

- The time went into the construction of regular expressions. `TaskFiles.PathPattern` built a new expression with
  `RegexOptions.Compiled` at each call, and `PatternsIntersect` called it two times for each pair. The gate of governed-by
  compares each affects pattern of each ADR in force with each path of the write set of each open task: it built 130,999
  compiled expressions for 429 distinct patterns (injection C1). The files were read once for each test, so the reads of
  the ADR files and of the task files were not the cause. `WriteSetGateTests.No_Task_Forbids_A_Path_That_It_Also_Writes`
  had the same cause, with a pattern of each forbid line of each task.
- A kept expression gives the same result as a new one: the text of a pattern gives its expression, and an expression
  of .NET is safe for matches on more than one thread. The cache holds one expression for each pattern that the
  repository gives, so it has a limit.
- The test counts expressions and not a time (ADR-0173 item 4). The counter is a field for each thread
  (`[ThreadStatic]`), so a test of another class that runs at the same time does not change the count. The bound is the
  number of distinct patterns of all ADRs and of the open tasks (429 today). The gate builds 429 expressions in a process
  where no other test built one.
- R-0074 had this cause, a stop at the hang limit and not a crash. The evidence:
  1. The test was the running test of the stop, and it took 38 to 100 seconds on the 28 logical processors of the
     computer of the owner. The runner of ubuntu-latest has four slower processors, and the step took 247 seconds.
  2. The form before the change, with a hang limit of 20 seconds, gave "The active test run was aborted. Reason: Test host
     process crashed" and named this test as the running test. The blame data collector said that the inactivity time
     of 20 seconds had elapsed, and wrote no dump (measure H1, French text on the computer of the owner).
  3. The test now takes 0.3 to 0.6 seconds, so it cannot reach the hang limit of 2 minutes.
- `UiLibraryGateTests.Only_The_Ui_Folder_Of_The_Host_Names_The_Library` took 26 seconds alone and 33 to 35 seconds in
  the run of the class. The compiler binds about 570,000 expressions of 686 files: 22 seconds on one thread. A
  compilation and its semantic models are safe on more than one thread, so each file binds on a thread of its own. A
  probe compared the 60,086 constants (file, line, place, type and value) of the form on one thread with the form on
  more than one thread: the same, in the same order. The test now takes 14 seconds alone and 16 to 19 seconds in a run
  of the class. A runner with four processors gains less.
- The task does not skip the expressions in a context of a type only (`SyntaxFacts.IsInTypeOnlyContext`). The same probe
  gave the same 60,086 constants with 63,059 fewer queries, and half of the time on one thread. That is a change of the
  set of the expressions that the gate reads, so it needs a review of findings T64 to T110 and is not done here.

## Outcome

Status: Done · commit (the merge of the branch p0177-fast-governance-gate) · v0.426

## Method

Mechanical: the cache of the expressions, the counter, the test of the count, the binding on more than one thread, the
counts of `eng/test-floor.txt`, the move of R-0074.

Judgement: the cause of R-0074 from the evidence above; the decision not to skip the expressions of a type.

Weakest: the time of the gate on the runner of ubuntu-latest is not seen. The pipeline keeps no .trx file of a passed
job. The cause rests on the local times, the count of 130,999 expressions, and a local stop at a smaller limit with the
same facts. Only the next runs of ubuntu-latest prove it: no stop of the step "Test" of `Engine.Tests`.

The times (results in `%TEMP%\p0177`). Other sessions ran tests on this computer at the same time, so the times vary
between runs.

| Test or run | Before | After |
|---|---|---|
| `TaskGovernanceGateTests.Each_Open_Task_Lists_Exactly_The_Adrs_Whose_Affects_Intersect_Its_Write_Set` | 63 s (the class alone), 76 s (the Governance filter), 43 to 67 s (the injections R1 to R3) | 0.3 s (the class alone), 0.4 s (the Governance filter), 1.2 to 1.4 s (each injection run in all) |
| The class `TaskGovernanceGateTests` | 64 s | 1.5 s (with `PathPatternWorkTests`) |
| `WriteSetGateTests.No_Task_Forbids_A_Path_That_It_Also_Writes` | 35 s | 0.2 s |
| `UiLibraryGateTests.Only_The_Ui_Folder_Of_The_Host_Names_The_Library` | 26 s alone, 33 to 35 s (the Governance filter) | 14 s alone, 16 to 19 s (the Governance filter) |
| `DeterminismCallGateTests.No_Source_In_The_Log_Path_Calls_A_Function_That_The_Determinism_Rules_Forbid` | 10 s | 9 to 14 s (no change of the test) |
| `dotnet test Engine.Tests --filter "FullyQualifiedName~Governance"` | 78 s (524 tests) | 23 s (526 tests) |
| `dotnet run --project eng/repo-gates -- --root .` (no build) | 155 s | 43 s |

Injections (`inject.js` in `%TEMP%\p0177`, dotnet with shell:false), each restored byte for byte (sha256):

| Injection | Failing test |
|---|---|
| R1 TASK-0441 without 0173 in governed-by | `Each_Open_Task_Lists_Exactly_The_Adrs_Whose_Affects_Intersect_Its_Write_Set`: "TASK-0441: governed-by lacks 0173, whose affects intersect the write set", the same message before and after |
| R2 TASK-0441 with 0001 in governed-by | the same test: "TASK-0441: governed-by names 0001, which affect no path in the write set or are not in force", the same message before and after |
| R3 ADR-0173 also affects `docs/register.md` | the same test: "TASK-0438: governed-by lacks 0173, whose affects intersect the write set", the same message before and after |
| C1 `PathPattern` builds a new expression at each call | `A_Pattern_Gives_One_Expression_That_Is_Built_Once` and `The_Gate_Of_Governed_By_Builds_At_Most_One_Expression_For_Each_Pattern`: "built 130999 expressions for 429 patterns" |
| C2 `Build` does not count | `A_Pattern_Gives_One_Expression_That_Is_Built_Once`: expected 1, actual 0 |
| C3 the parallel loop loses the constants of its first file | 48 tests: 10 cases of `UiLibraryGateTests` (findings T64, T70, T82 and the upper case extension) and 38 of `DiagnosticsRegistryGateTests` |

Measures, not injections: H1, the form of C1 with `--blame-hang-timeout 20s --blame-hang-dump-type none --blame-crash`,
gave the stop of R-0074 (see "Decisions"); M1, a bound of -1, gave the count of the gate after the change: 429
expressions for 429 patterns.

## Progress

- 2026-11-21: opened, branch `p0177-fast-governance-gate` from `fe1809fd` (P0.176, TASK-0441, v0.425, not merged yet).
  The cause, the cache of the expressions with a count, the binding on more than one thread, the injections, R-0074
  closed, the roadmap and ledger entry v0.426.
- 2026-11-21: run 44923430533 of `e8112673` passed on each job. Closed at v0.426.
