---
id: 0045
title: A failed test in the pipeline has a name that a person without a sign-in can read
status: Done
phase: governance
opened: 2026-10-03
depends-on: []
governed-by: []
writes:
  create:
    - tasks/TASK-0045-failed-test-names.md
    - eng/report-failed-tests.js
  modify:
    - .github/workflows/ci.yml
    - docs/INDEX.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0045 — A failed test in the pipeline has a name that a person without a sign-in can read

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0032. The log of a job needs admin rights: on 2026-10-01 the endpoint of the job
log answered 403. The annotations of a check run are public. Two runs failed in the step "Test" with
no name that the session could read: Ubuntu on 2026-09-30 (v0.38) and Windows on 2026-10-01 (run
36920665584). In each case the session could only guess the test. The owner asked for the correction
on 2026-10-03.

## Goal

When a test fails in the pipeline, a public annotation gives its name and its message.

## Scope (in)

1. `eng/report-failed-tests.js` reads each `.trx` file in a directory and writes one `::error`
   workflow command for each failed test, with a maximum of ten, which is the limit of GitHub for one
   step. It also writes the list to the summary of the job. Node.js only, with no package.
2. The step "Test" of `.github/workflows/ci.yml` writes a `.trx` file and calls the script when
   `dotnet test` fails. The step still fails.
3. When no file names a failed test, the script says so, so that an empty report is not read as "no
   test failed".

## Scope (out)

- No upload of the result files. An artifact also needs a sign-in to download.
- No new test package, for example a logger for GitHub. A package is a dependency for one script.
- No search for the cause of the two old failures. Their logs are gone for a reader with no rights.

## Acceptance criteria

- [x] A failing test injected on this computer gives one annotation line with its name and its
      message, and the script exits with 0.
- [x] A failing test injected on a branch in the pipeline gives a public annotation with its name,
      which the API of the check run returns with no sign-in.
- [x] A run with no failed test is unchanged: the three runners pass.

## Notes for the implementer

- A passed result in a `.trx` file is an empty element, `<UnitTestResult ... />`. A pattern that
  expects a closing tag reads past it into the next result. The first form of the script did that,
  and it found no failed test.
- The escape rules of a workflow command: `%`, a carriage return and a line feed in the message; also
  `:` and `,` in a property such as `title`.

## Outcome

Status: Done · v0.40 · the commit that carries this block.

## Method

**Mechanical.** One Node.js script with no package, and one changed step in the workflow.

**Judgement.** The script reads the `.trx` file with a regular expression and not with an XML
parser, because Node.js has no parser and a package is a dependency for one script. It writes at most
ten annotations, the limit of GitHub for one step, and the tenth names the rest. When no file names a
failed test, it says so. The proof in the pipeline used a branch that is never merged,
`r0032-injection-2026-10-03`, so that no failing commit enters the history of this branch.

**Weakest.** The regular expression depends on the form of the `.trx` file of the test platform. A
new version that changes the form makes the script report "no failed test", which the message then
says. A crash of the test host before it writes the file gives the same message and no name.

## Progress

- 2026-10-03: the script and the step. On this computer, an injected `Assert.Fail` gave one annotation
  with the name, the message and six lines of the stack. A file with twelve failures gave nine
  annotations and a tenth that names the other three. The first form of the pattern found no failed
  test, because it read past an empty element; the test of the injection found that.
- 2026-10-03: the proof in the pipeline. Run 37102594075 on `r0032-injection-2026-10-03` failed on the
  three runners, and the check-run API, with no sign-in, gave an annotation with the name of the
  injected test on each one. On Windows it also named a real failure: `HttpConcurrencyTests.`
  `Queries_And_Commands_In_Parallel_Get_No_Http_500`, with `TaskCanceledException`. TASK-0034
  corrects that test. The run on this branch, 37102588109 on 67a5ff1, passed on the three runners.
- 2026-10-03: the script now gives a line feed for each line end of a message, so that no `%0D` appears
  in an annotation. Closed. R-0032 is closed, and ledger entry v0.40 records the work.
