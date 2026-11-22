---
id: 0455
title: The test of the work of the path patterns reads no file a second time and gives the same result in each run
status: Done
phase: P0.185
opened: 2026-11-22
depends-on: [0454]
governed-by: []
writes:
  create:
    - tasks/TASK-0455-the-pattern-work-test-reads-no-file-twice.md
  modify:
    - eng/write-set-check/TaskFiles.cs
    - Engine.Tests/Governance/PathPatternWorkTests.cs
    - docs/adr/0173-a-lost-runner-gets-one-automatic-re-run.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
---

# TASK-0455 — The test of the work of the path patterns reads no file a second time and gives the same result in each run

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`Engine.Tests/Governance/PathPatternWorkTests.cs` came with P0.177 (TASK-0442). Its two tests count the regular expressions
that `TaskFiles.PathPattern` (`eng/write-set-check`) builds. The counter is a field for each thread
(`TaskFiles.PatternsBuiltOnThisThread`, `[ThreadStatic]`). The expressions go into one cache for the process (a
`ConcurrentDictionary`).

On 2026-11-22 the session of TASK-0454 (P0.184) reported that one of the two tests failed once in a full local run, and passed
in the next run. The output of the failed run was not kept, so the failed test and its message are not known. A test that fails
at random can make a run of continuous integration red and stop the merges of a session.

## Goal

Each test of `PathPatternWorkTests` gives the same result in each run, and the two tests still hold that the gate of governed-by
builds the expression of each pattern once.

## Scope (in)

1. The cause of the failure, by reasoning and by reproduction.
2. The smallest change of the two tests and of `TaskFiles` that removes the cause.
3. The proof: the loops of the tests before and after the change, three probes, and the injections.
4. A History line of ADR-0173 (item 4: a test that protects a cost compares a count of the work).

## Scope (out)

- A change of a rule of a gate, or of a message of a gate.
- A cache that builds each pattern once in the process (`Lazy<Regex>`). Two threads can each build the expression of one
  pattern, and the cache keeps one of them. That costs one expression, it does not change a result, and no test reads it.
- The reads of the files of the repository by the gates. A gate that reads a file while a session writes it can fail. Probe B
  (see "Method") had 1 to 4 refused reads in 200 rounds, before and after the change. Each gate that reads the repository has
  this property, and this test now has it only through the gate that it measures.

## Acceptance criteria

- [x] The cause is reproduced: a probe makes the test fail before the change, and the same probe passes after the change.
- [x] The candidates that the reasoning excludes are tested, and they give no failure in the suite.
- [x] The loops of the Governance tests and of all of `Engine.Tests` (20 runs each) give no failure before or after the change.
- [x] Each injection makes a named test fail, and each file is restored byte for byte (sha256).
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

- The cause. The second test read the ADR files and the task files to get its bound: the affects patterns of each ADR and the
  write set of each open task. The gate of governed-by then read the same files a second time. With no expression in the
  cache, the gate builds one expression for each of its patterns, so the count equals the bound (478 of 478 today). A change of
  a file between the two reads that adds one pattern gives "built 479 expressions for 478 patterns". The next run reads one
  state of the files and passes. A session that edits a task file or an ADR while a test run of its worktree is in progress
  makes this change. The session of TASK-0454 changed its task file and two ADRs on that day. The output of its failed run is
  not kept, so this is the only mechanism that the probes found, and not a fact of that run.
- The candidates of the threads do not give the failure in the suite:
  1. `ConcurrentDictionary.GetOrAdd` can call the factory on two threads for one pattern. One call calls the factory at most
     once, and a later call on the same thread finds the kept expression. So one thread builds one pattern at most once.
  2. A test of another class that builds a pattern first makes this thread build fewer. That direction passes.
  3. The two tests are synchronous and have no await, so a test does not change its thread.

  Probe A ran the two tests beside the gate of governed-by and `WriteSetGateTests` on eight threads, with a cold cache in each
  round: no failure in 320 calls, before and after the change.
- The first test used one fixed pattern. A second call of the test on another thread at the same time can build the pattern
  first, and the test then counts 0 (probe A2: 11 failures of 320 calls before the change). The suite calls the test once in a
  process, so this did not cause the report. The test now uses a new pattern for each call, so its result does not depend on
  another thread (A2 after the change: 0 of 320).
- The bound of the second test is now the growth of the cache during the gate (`TaskFiles.PatternsKept`). A thread builds the
  expression of a pattern only when the cache does not keep it. The cache keeps the pattern before the call returns, and it
  never removes a pattern. So the expressions that one thread builds are at most the new patterns of the cache, with each other
  thread and each change of a file. The test reads no file for its bound.
- What the tests protect. A new expression at each call (the form before TASK-0442, C1) and a cache that keeps the expression
  but builds one at each call (C3) fail both tests. A cache that keeps a new entry for each call (C4) grows as fast as the
  thread builds, so the second test no longer catches it; the first test catches it (`Assert.Same`). The old bound of the second
  test also caught C4, so the two tests together hold the same set of defects as before. With C4 the run of the second test
  took two hours on the computer of the owner, so in the pipeline the hang limit of 2 minutes also stops it.
- Not a collection with no parallel run. The test then still reads the files two times, and an edit during the run still makes
  it fail. The threads were not the cause.

## Outcome

Status: Done · commit (the merge of the branch p0185-pattern-work-test) · v0.439

## Method

Mechanical: the count of the kept patterns, the bound of the second test, the pattern of the first test, the History line of
ADR-0173, the roadmap rows and the ledger entry.

Judgement: the cause from the reasoning and the probes, the choice of the bound, and the decision that the first test holds the
defect C4.

Weakest: the output of the failed run of TASK-0454 is not kept, so the cause of that run is not proved. The probes prove that
the edit of a file between the two reads gives the failure, and that the candidates of the threads do not give it in the suite.

The loops (`loop.js` in `%TEMP%\p0185`, dotnet with shell:false, `--no-build`, the probe class left out). Other sessions ran
work on this computer at the same time during the loops before the change, so a run of all of `Engine.Tests` took 2.5 to 7.7
minutes before and about 1 minute after. The loops before ran the form of `6770ff34` with the probe class in the build.

| Loop | Before the change | After the change |
|---|---|---|
| `--filter FullyQualifiedName~Engine.Tests.Governance`, 20 runs | 0 failed runs (540 tests each) | 0 failed runs (540 tests each) |
| All of `Engine.Tests`, 20 runs | 0 failed runs (1871 tests each) | 0 failed runs (1871 tests each) |

A first loop of 20 Governance runs, after a build of `Engine.Tests` only, failed in each run in
`BuiltAssemblyReferenceTests` ("Build the solution before the tests"), and `PathPatternWorkTests` passed in each run. The loops
above come after a build of the solution.

The probes (`ProbeP0185Tests.cs` in `%TEMP%\p0185`, in `Engine.Tests` for the measure and then removed). Each round clears the
cache by reflection.

| Probe | Before the change | After the change |
|---|---|---|
| A: the two tests, the gate of governed-by and `WriteSetGateTests` on eight threads, the first test on one thread, 40 rounds | 0 failures of 320 calls | 0 of 320 |
| A2: the same, with the first test on two threads | 11 failures of 320 calls ("Expected: 1 Actual: 0") | 0 of 320 |
| B: the second test, 200 rounds, while a thread adds and removes the line `probe-p0185/<n>.txt` in the write set of TASK-0454 | 10 failures ("built 479 expressions for 478 patterns"), 4 refused reads | 0 failures, 1 and 2 refused reads (two runs) |

The file of TASK-0454 had the same sha256 after each run of probe B.

Injections (`inject.js` in `%TEMP%\p0185`, each in `TaskFiles.cs`, dotnet with shell:false, filter `PathPatternWorkTests`), each
restored byte for byte (sha256):

| Injection | Failing test |
|---|---|
| C0 no change | none (2 of 2 pass) |
| C1 `PathPattern` builds a new expression at each call | both tests: `Assert.Same` fails, and "built 463339 expressions, and the cache kept 0 new patterns" |
| C2 `Build` does not count | `A_Pattern_Gives_One_Expression_That_Is_Built_Once`: expected 1, actual 0 |
| C3 the cache keeps the expression and still builds one at each call | both tests: expected 1, actual 2, and "built 463339 expressions, and the cache kept 478 new patterns" |
| C4 the cache keeps a new entry for each call | `A_Pattern_Gives_One_Expression_That_Is_Built_Once`: `Assert.Same` fails; the second test passes after 2 hours |
| C5 `PatternsKept` counts nothing | `The_Gate_Of_Governed_By_Builds_At_Most_One_Expression_For_Each_Pattern`: "built 478 expressions, and the cache kept 0 new patterns" |

## Progress

- 2026-11-22: opened, branch `p0185-pattern-work-test` from `6770ff34` (P0.184, TASK-0454, v0.438, not merged yet). The cause by
  reasoning and by three probes, the bound of the second test by the growth of the cache, a new pattern for each call of the
  first test, the loops before and after, the injections, the History line of ADR-0173, the roadmap rows P0.185 and the ledger
  entry v0.439.
- 2026-11-22: run 45018469481 of `e4f8aee5` passed on each job. Closed at v0.439.
