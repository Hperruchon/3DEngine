---
id: 0047
title: The pipeline stops a hang, a project file cannot remove a gate, and a closed task governs no commit
status: Active
phase: governance
opened: 2026-10-04
depends-on: [0046]
governed-by: []
writes:
  create:
    - tasks/TASK-0047-gates-and-time-limits.md
  modify:
    - .github/workflows/ci.yml
    - eng/report-failed-tests.js
    - Engine.Tests/Governance/WriteSetGateTests.cs
    - Engine.Tests/Governance/RepositoryFiles.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/DeterminismCallGateTests.cs
    - tasks/TASK-0035-version-counts-applied-commands.md
    - docs/templates.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - Engine.Tests/Engine.Tests.csproj
    - docs/adr/**
    - CLAUDE.md
---

# TASK-0047 — The pipeline stops a hang, a project file cannot remove a gate, and a closed task governs no commit

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-04 (`docs/reviews/2026-10-04-codebase-review.md`) gives two high
findings in the pipeline and the gates, and a question about a third:

- **T8.** No job has a time limit, and `dotnet test` has no limit for a hang. A test that hangs holds
  a runner for 360 minutes, and the annotation of TASK-0045 never comes. Register entry R-0034.
- **T9.** `Engine.Tests/Engine.Tests.csproj` is not a gate file, so each task that permits
  `Engine.Tests/**` can remove each gate. A `Directory.Build.props` file gets past the dependency gate
  and the x86 gate. Register entry R-0031.
- **T3, the open part.** A commit can name a task that is `Done`. Commit `4715b88` did.

On 2026-10-04 the owner answered "yes to all" to the four questions of the review:

- **Q1.** TASK-0035 also corrects finding E7 and the replay part of E17. This task records it in
  TASK-0035.
- **Q2.** T8 and T9 are corrected before TASK-0035, in one governance task. This is that task.
- **Q3.** A commit is refused when it names a task that is `Done`, except a commit that also changes
  the status of that task. This task makes the gate do it.
- **Q4.** A sink must not throw, by contract, and a test holds each sink to it. This task records it
  in register entry R-0033. The correction waits for a task in the engine.

## Goal

A hang stops with a name, and no file outside the exact write set of a task can turn a gate off.

## Scope (in)

1. **T8.** Each job of `.github/workflows/ci.yml` has `timeout-minutes`. `dotnet test` stops a test
   that hangs with `--blame-hang-timeout`, and `eng/report-failed-tests.js` gives the reason that
   the test platform writes for an aborted run, so that the annotation names the test.
2. **T9, the write set.** `Engine.Tests/Engine.Tests.csproj`, each `Directory.Build.props`,
   `Directory.Build.targets` and `Directory.Packages.props`, `eng/write-set-cutoff.txt` and each file
   under `.github/workflows/` are gate files: a task must name each one exactly.
3. **T9, the two gates.** The dependency gate reads each `Directory.Build.props` and
   `Directory.Build.targets` above a project, and each file that a project imports. The x86 gate reads
   each `.csproj`, `.props` and `.targets` file.
4. **Q3.** When a commit names a task, the gate reads the task file at that commit and at its parent.
   If the status is `Done` at both, the gate refuses the commit. The write set comes from the task
   file at that commit, and not from the tip of the branch.
5. **The records.** TASK-0035 gets the scope of Q1 and depends on this task. Register entry R-0033
   gets the answer to Q4. `docs/templates.md`, section 3, gives the rule of Q3.

## Scope (out)

- No change to engine code or host code.
- No correction of the other gate findings of the review: T4, T5, T7, T10, T11 (except the part that
  item 4 gives), T12, and the other parts of T13.
- No change to `Engine.Tests/Engine.Tests.csproj` itself.

## Acceptance criteria

- [ ] A test that hangs, injected on this computer, stops at the hang limit, and the script names it.
- [ ] Each new gate rule fails on an injected violation before its change, and the violation fails
      after it.
- [ ] A commit that names a `Done` task and does not change its status fails the gate. A commit that
      closes a task, and a commit that reopens one, pass.
- [ ] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- The continuous integration of the write-set job runs the gate of the tip for each commit
  (finding T11). Item 4 therefore gives the gate the hash of the commit in a new variable,
  `WRITE_SET_COMMIT`. With no hash, on this computer before a commit, the gate reads the index and
  `HEAD`.
- MSBuild reads the first `Directory.Build.props` above a project. The gate reads each one up to the
  root, which is stricter and simpler.

## Progress

- 2026-10-04: the task is open, with the answers of the owner to Q1 to Q4.
- 2026-10-04: T9. Before the change, three injections passed: the project file of the tests under
  TASK-0035, a `Directory.Build.props` in `Engine.Core` with a reference to `3DEngine.Core` and the
  identifier `win-x86`, and an imported props file with the same reference. After the change each one
  fails, with the gate file, the reference or the identifier in the message. TASK-0038 names the
  workflow exactly and keeps its permit. The first form of the injection script wrote the props file
  with `printf`, which read `\3` as a control character; the gate then failed on invalid XML, which
  proves nothing, and a heredoc replaced it.
- 2026-10-04: Q3. Before the change, a staged progress line in TASK-0034, which is `Done`, with a change
  to a test passed the gate. After it, the gate refuses it with the rule in the message, and a commit
  that closes a task or reopens one passes. In the mode of the pipeline, a temporary local commit of
  the same form fails, and the real commit `4715b88` passes, because each commit up to `67c564f`
  keeps the earlier rule. A replay of the 51 commits after the cut-off, with the loop of the workflow
  and `WRITE_SET_COMMIT`, gives no failure.
- 2026-10-04: T8. An injected test that waits for ever, run with a hang limit of 20 s on this computer,
  stopped after 24 s with exit code 1. The `.trx` file then holds no result for it and says only that
  the test host crashed, so the script reported "no failed test". The blame collector writes a
  sequence file where the test has `Completed="False"`; the script now reads it, and the annotation
  names the test with the reason of the run. The script also reads the outcomes `Error`, `Timeout`
  and `Aborted` (a part of T13). Each job has `timeout-minutes`, and the step "Test" stops a test
  after 2 minutes. The full suite passes with the same options.
