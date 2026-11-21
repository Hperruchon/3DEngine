---
id: 0440
title: The message of a crash of a test host starts with the running test, and its cause leaves out the output of other tests
status: Done
phase: P0.175
opened: 2026-11-21
depends-on: [0438]
governed-by: [0173]
writes:
  create:
    - tasks/TASK-0440-a-crash-names-its-test.md
  modify:
    - eng/report-failed-tests.js
    - Engine.Tests/Pipeline/CrashReportTests.cs
    - eng/test-floor.txt
    - docs/adr/0173-a-lost-runner-gets-one-automatic-re-run.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
---

# TASK-0440 — The message of a crash of a test host starts with the running test, and its cause leaves out the output of other tests

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The session of 2026-11-21 found the defect in run 44908442870 (windows-latest, step "Test", commit `9073010`). The test host
of `3DEngine.ImageTests` crashed. The public annotation, read from the API on 2026-11-21, had this form:

- Title: "Failed test on Windows: ThreeDEngine.ImageTests.ResponsiveLayoutTests.Each_Size_Of_A_Resize_From_The_Minimum_Keeps_Each_Control_Free".
- Message (1259 characters): "The test did not complete: it was running when the test platform stopped the run. Cause: ... es
  not import bracket.stl, because its new part did not start", three lines "The file ... did not exist, so the host made a
  new empty document file there", and "Data collection : Test case event channel failed: Unable to read data from the
  transport connection: An existing connection was forcibly closed by the remote host..".

The message named no test, and its cause was the last 1000 characters of the output of other tests (TASK-0435 took them when
the error output holds no report of the runtime). The artifact `test-host-crash-windows-latest` (20 KB) held the sequence file
and no dump. The order of the session said that the annotation named no test; its title named the test, and its message did
not. The page of a run shows the message, so the name must be at its start.

The three crashes of a test host of the weekend:

1. Attempt 1 of run 44758589835 (windows-latest): `3DEngine.ImageTests`, running
   `ResponsiveLayoutTests.Each_Size_Of_A_Resize_From_The_Minimum_Keeps_Each_Control_Free`.
2. Attempt 1 of run 44869058349 (ubuntu-latest): `Engine.Tests`, running
   `TaskGovernanceGateTests.Each_Open_Task_Lists_Exactly_The_Adrs_Whose_Affects_Intersect_Its_Write_Set` (R-0074).
3. Run 44908442870 (windows-latest): `3DEngine.ImageTests`, running the same test as crash 1.

The rule of the owner: a red run must name its cause, and a crash stays red.

## Goal

The message of each crash of a test host starts with the running test, or says that the test is not known.

## Scope (in)

1. `eng/report-failed-tests.js`: the message of a running test starts with its name, then the dumps or "No dump of the
   crash.", then the cause.
2. The cause: the words of the test platform before the error output, the lines of createdump and of an exit code, the
   count of the lines of the tests that it leaves out, and the report of the runtime. Each cause has a cut of 1200 characters.
3. The running tests come from the sequence files, and then from the message of the blame data collector in a .trx file.
4. A crash with no running test says "No sequence file: the crashed test is not known." or "No sequence file names a running
   test: the crashed test is not known.", also beside a failed test.
5. `CrashReportTests` with the facts of run 44908442870, a crash with a dump, and a crash with neither.

## Scope (out)

- The cause of the three crashes. The next dump gives it, and R-0074 holds the crash on Linux. The register is full (15 of
  15), so the two crashes on Windows of the same test get no entry; the ledger entry v0.424 names them.
- A change of the step "Test" or of the artifact of a crash in `.github/workflows/ci.yml`.
- A cause for each assembly. The script joins the causes of each .trx file, as before; one crash in one step is the case of
  each run so far.

## Acceptance criteria

- [x] With the facts of run 44908442870, the message starts with the name of the running test, says "No dump of the
  crash.", and holds no line of the output of other tests (`CrashReportTests`).
- [x] A crash with a dump gives the dump, the lines of createdump and the report of the runtime after the name.
- [x] A crash with no sequence file says "No sequence file: the crashed test is not known."
- [x] Each new test fails on the script of `6cea608e`, and each injection makes a named test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

- The name first. The message of a running test starts with "<name>: the test did not complete, because the test host
  stopped while it ran." The script keeps its cut of 2000 characters (`MaxMessageLength`), and the name is in the first
  characters of each message. The title keeps the name too.
- No output of the tests in the cause. The error output of the test host holds the output of each test of the assembly, and
  the report of the runtime at its end. TASK-0435 gave the last 1000 characters when there is no report; in run 44908442870
  those were the output of other tests. Now the cause gives the count of the lines that it leaves out, and "The error output
  holds no report of the runtime." A last line of the output of a test can be the last word of the crashed test; the
  sequence file names that test, and the dump holds its state.
- The words of the platform. The strings of the test platform of the SDK 10.0.400 (read from its files on 2026-11-21):
  "Test host process crashed : <error output>" and "Testhost process for source(s) '<file>' exited with error: <error
  output>. Please check the diagnostic logs for more information." A reason with neither form, for example "Data collection
  : Test case event channel failed", is a text of the platform, and the cause gives it whole, with the cut.
- The kept lines of the error output: "[createdump] ..." (the runtime on Linux and macOS says if it wrote the dump), a line
  that starts with "Test host process", and a line with "exited with exit code". A line of a test with those words stays,
  inside the cut.
- The cut of 1200 characters for each cause keeps a second cause, for example the reason of the data collection after a long
  stack of the runtime, inside the cut of the message.
- The blame data collector of the SDK holds "The test running when the crash occurred:" and "This test may, or may not be
  the source of the crash." The script reads the names between them from each message of a .trx file. No run showed that
  message in a .trx file; the sequence file stays the first source.
- A crash is a reason with the words of the platform or the word "aborted". A crash with no running test gives its own
  annotation "Test host stopped on <system>" beside the failed tests; with no failed test, the annotation of the step says it.

## Outcome

Status: Done · commit (the merge of the branch p0175-crash-names-its-test) · v0.424

## Method

Mechanical: the order of the message, the texts of a crash with no known test, the cut of a cause, the counts of
`eng/test-floor.txt`, the history of ADR-0173.

Judgement: the cause leaves out each line of the tests; the kept lines of createdump and of an exit code; a reason with no
words of a stopped host is a text of the platform; the names of the blame data collector in a .trx file.

Weakest: the form of a real reason. The fixtures use the strings of the test platform and the facts of the annotation of run
44908442870, not the .trx file of that run, because the artifact keeps the sequence file and the dumps only. The form of the
message of the blame data collector in a .trx file, and the lines of createdump, come from the files of the SDK and from
createdump, not from a run. Only the next crash in the pipeline proves them.

The seven tests of `CrashReportTests` (two changed, five new) each fail on the script of `6cea608e`. Injections (`inject.js`
in `%TEMP%\p0175`), each restored byte for byte (sha256), each with the named test failing:

| Injection | Failing test |
|---|---|
| I1 the name is not first | `A_Crash_Like_Run_44908442870_Names_The_Running_Test_First_And_Leaves_Out_The_Output_Of_Other_Tests` |
| I2 the cause is the whole reason | the same |
| I3 no text for a crash with no dump | the same |
| I4 no cut of a cause | `Each_Cause_Is_Cut_So_That_A_Second_Cause_Stays_In_The_Message` |
| I5 no report of the runtime | `A_Crash_Names_The_Running_Test_The_Dump_And_The_Report_Of_The_Runtime` |
| I6 no lines of createdump | the same |
| I7 the output of the tests is kept | `A_Crash_Like_Run_44908442870_…` |
| I8 no text for a crash with no sequence file | `A_Crash_With_No_Sequence_File_Says_That_The_Crashed_Test_Is_Not_Known` |
| I9 one text for no sequence file and for no running test | `A_Crash_Whose_Sequence_Names_No_Running_Test_Says_So` |
| I10 no running test from the .trx file | `A_Crash_Whose_Trx_Names_The_Running_Test_Names_It_With_No_Sequence_File` |
| I11 no annotation of the crash beside a failed test | `A_Failed_Test_Beside_A_Crash_With_No_Sequence_File_Gives_The_Crash_Its_Own_Annotation` |
| I12 no reason is a crash | `A_Crash_With_No_Sequence_File_Says_That_The_Crashed_Test_Is_Not_Known` |

## Progress

- 2026-11-21: opened, branch `p0175-crash-names-its-test` from `6cea608e` (P0.174, TASK-0438, v0.423, not merged yet). The
  script, seven tests, twelve injections, ADR-0173 (the consequence of a crash and its history), the roadmap and ledger
  entry v0.424.
- 2026-11-21: run 44916936570 of `391c66a6` passed on each job. Closed at v0.424.
