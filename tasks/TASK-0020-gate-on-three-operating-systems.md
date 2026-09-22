---
id: 0020
title: Run the gate on Windows, Linux and macOS
status: Done
phase: P0.6
opened: 2026-09-20
depends-on: [0018]
governed-by: [0014]
writes:
  create:
    - tasks/TASK-0020-gate-on-three-operating-systems.md
  modify:
    - .github/workflows/ci.yml
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
---

# TASK-0020 — Run the gate on Windows, Linux and macOS

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0002 records this problem. Each job in `.github/workflows/ci.yml` uses
`ubuntu-latest`. A person verified the native geometry path on win-x64 by hand only. The desktop host
ran on Windows only. The due date is 2026-09-24.

The problem grew in v0.19. That milestone added 22 governance tests that read the working tree. Each
one uses a path, and no runner other than Windows has ever run them. A path defect would appear only
on a computer that nobody tested.

## Goal

The build, the tests and each smoke test pass on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Scope (in)

1. Give the gate job a matrix of three operating systems.
2. Join the build job and the smoke job, so that one runner builds once and then drives the binary.
3. Add a smoke test for `CreateBox`, which drives the geometry backend through a process boundary.
4. Close register entry R-0002 after continuous integration reports a pass on each runner.

## Scope (out)

- Do not change code in a project. This task changes the workflow only. A failure on a runner needs
  its own task, because the repair belongs to the code that fails.
- Do not add a native payload for Linux or macOS. ADR-0014 section 4 makes the managed backend the
  fallback, and the native tests skip when the library is absent. A second payload needs its own task
  and its own entry in the native build workflow.
- Do not add a runner for the Vulkan desktop host. That host needs a display and a driver. A headless
  render check needs its own task.

## Acceptance criteria

- [x] The gate job uses a matrix of `ubuntu-latest`, `windows-latest` and `macos-latest`.
- [x] `fail-fast` is false, therefore each runner reports.
- [x] One shell serves the three operating systems.
- [x] A step drives `CreateBox` through the command-line binary.
- [x] Continuous integration reports a pass on each of the three runners.
- [x] Register entry R-0002 is closed.

## Notes for the implementer

The last two criteria need a push. `CLAUDE.md`, section "Test discipline", forbids running the gate on
a local computer, therefore this task cannot verify itself. The entry R-0002 stays open until the
three runners report. Register rule 6 forbids closing an entry to make a gate pass.

Two results are expected and correct:

- The Windows runner runs the tests for the native Manifold backend. The other two runners skip them,
  because the payload carries the runtime identifier win-x64 only.
- The count of passed tests therefore differs between Windows and the other two runners.

## Outcome

Shipped as v0.24. Track 0 is complete.

Run `35786216373` reports a pass on `ubuntu-latest`, on `windows-latest` and on `macos-latest`. Each
job runs the build, each test, the smoke test for `NoOp` and the smoke test for `CreateBox`. Register
entry R-0002 is closed.

This task waited four days for one action by a person, and TASK-0023 removed that wait. The workflow
triggered on a push to the main branch only, therefore a branch gave no report and a person had to
open a pull request by hand. The trigger now names each branch. The push of v0.23 gave the first
automatic report.

**One statement in this task was false.** The "Notes for the implementer" block says that the native
Manifold payload carries the runtime identifier win-x64 only, and that the native tests therefore run
on the Windows runner and skip on the other two. TASK-0024 opened the package and found three sets of
binaries: `linux-x64`, `osx-arm64` and `win-x64`. Each runner has a payload. The comment in
`.github/workflows/ci.yml` is corrected.

The correction does not change the result, because each job passed. It does change the expectation:
the native backend is expected to run on each runner, and not on one.

The split between a test that ran and a test that skipped is not observable from outside. The log
endpoint of the workflow needs a token, and this session has none. The evidence for the correction is
the content of the package and not the log.

## Method

**Mechanical.** The matrix. The join of the build job and the smoke job. The shell.

**Judgement.** One shell for three operating systems, because a second form of each script would
drift. The `CreateBox` step, because register entry R-0002 named the native geometry path as the risk
and a unit test alone does not cross a process boundary.

**Weakest.** This task stated a fact about a binary package without opening the package. That is the
third error of this kind in three days: the dependency gate trusted a project name without checking
for a duplicate, a note cited a clamp that a previous milestone had removed, and this task described a
payload that it never read. Each one was a statement about the repository that did not come from the
repository. No gate catches this class, because each wrong statement sat in prose.
