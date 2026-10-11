---
id: 0064
title: The gates that read the repository run as a program that the pipeline builds from main
status: Done
phase: P0.25
opened: 2026-10-11
depends-on: [0062]
governed-by: []
writes:
  create:
    - tasks/TASK-0064-repository-gates-run-from-main.md
    - eng/repo-gates/RepoGates.csproj
    - eng/repo-gates/Program.cs
  modify:
    - .github/workflows/ci.yml
    - 3DEngine.sln
    - eng/write-set-check/Judge.cs
    - Engine.Tests/Governance/RepositoryFiles.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/WriteSetJudgeTests.cs
    - Engine.Tests/Diagnostics/DiagnosticsScanner.cs
    - CLAUDE.md
    - docs/templates.md
    - docs/INDEX.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0064 — The gates that read the repository run as a program that the pipeline builds from main

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Finding T31 (high), the attack class of T14: a file of the judged commit can change what a gate of
`Engine.Tests` reads before the gate runs. On 2026-10-08 the owner chose option A, a refusal of four
texts, and kept option B for a new form. The codebase review of 2026-10-11 found six forms that pass
option A, and two of them ran in a test process; a module initializer in a referenced project also
runs there. On 2026-10-11 the owner answered question Q1 of that review: option B.

## Goal

A gate that reads only files of the repository runs from a build of `main`, so a judged commit cannot
change it or what it reads.

## Scope (in)

1. Each gate class that reads only files of the repository moves into the judge program
   `eng/write-set-check`, or into a program beside it that the pipeline builds from `main` in the same
   way: the 13 classes under `Engine.Tests/Governance/` and `Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs`.
   Each rule keeps its text and its message.
2. The pipeline runs those gates from the build of `main` on the checkout of each judged commit, and
   the job fails on each failure.
3. A gate that runs engine code stays a test: `SchemaEndpointGateTests`, `ReplayDeterminismGateTests`
   and `DispatchSurfaceGateTests`, which reads `HandlerCatalog`. The task records how far the attack
   class reaches them.
4. `CLAUDE.md`, section "Method", and `docs/templates.md`, section 6, give the new local command.
5. The four-text refusal of TASK-0057 stays as a second line, or leaves with a reason.

## Scope (out)

- No new rule. The gates move; their rules stay.

## Acceptance criteria

- [x] Each moved gate fails on its own injection from the program, as it failed as a test. Fourteen
      injections, one for each gate class, each one in the data of the checkout.
- [x] Two forms of the review that passed option A, the escaped attribute and `AppDomain.SetData`, no
      longer change what a moved gate reads.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.63 · the commit that carries this block.

The program `eng/repo-gates` compiles the fourteen gate classes that read only files of the repository,
with their helpers, and runs each `[Fact]` and each row of each `[Theory]` of them on the checkout that
`--root` gives. The job "Write set of the governing task" builds it from the same checkout of `main` as
the judge, and runs it on the judged checkout. When the judged commits change a source of the program,
the job builds it from the judged commit instead. The gate files keep their paths under `Engine.Tests/`,
and `Engine.Tests` still runs them as tests. The judge counts `eng/repo-gates/` as gate files. The
four-text refusal of TASK-0057 stays as a second line for the three gates that run engine code.

## Method

**Mechanical.** One injection for each gate class, in the data of the checkout and not in a gate: an
absent file in `enforced-by`, an ADR status outside the set, a review commit that is not a hash, a
reference from `Engine.Core` to `3DEngine.Core`, a call of `Math.Sin` in `Engine.Core`, an unregistered
code, a reserved code with a short reason, an absent path in the index, a marker with no identifier, a
changed checksum, a citation of an objective that does not exist, a due date in the past, a dependency on an absent task,
and a backslash in a write set. Each one failed its own gate class from the program, with exit code 1,
and the run after the restore passed. The attack of the review: a test file with the escaped attribute
name and `AppDomain.CurrentDomain.SetData` moved `AppContext.BaseDirectory` to a clean worktree of
`main`, and none of the four texts was in the file. With a marker with no identifier in
`Engine.Core`, the marker gate passed as a test and failed from the program. Without the redirect the
test failed too. The first run of the program also found that the new project `RepoGates` was in no
class of `DependencyDirectionGateTests`.

**Judgement.** The gate files stay at their paths, because three ADRs name one of them in
`enforced-by` and this task forbids `docs/adr/**`; a program that compiles them changes no rule and no
message. The gates of the judged commit run when the judged commits change a source of the program.
Without that, a task that changes a rule and the files that the rule reads fails on its own branch,
and its merge fails on `main` once. Such a source is a gate file, which its task must name exactly,
and the diff shows the change: the condition of R-0038. `Engine.Tests` keeps the gates as tests for a
fast local run. Scope item 4 named section 6 of `docs/templates.md`; the jobs of the pipeline are in
section 5, so the command is there.

**Weakest.** The three gates that run engine code stay in the test process, and the attack class
still reaches them; register entry R-0031 records it. The switch to the gates of the judged commit is a
line of bash in the workflow, which a local run tested on two ranges. The runner reads `[InlineData]`
only; a theory with another source of rows fails, and no gate has one today.

## Notes for the implementer

- **The write set.** A file of the judge program and a file under `Engine.Tests/Governance/` is a gate
  file, which the write set must name exactly. Name each file that the task creates, moves or removes in
  the first commit of the task, before the work.
- **The size.** About 3 to 4 evenings. If it does not fit one session, split it by gate.

## Progress

- 2026-10-12: the task is open, in the order of the owner of 2026-10-12. The gate files keep their
  paths, because ADR-0004, ADR-0009 and ADR-0017 name `Engine.Tests/Governance/DependencyDirectionGateTests.cs`
  in `enforced-by`, and this task forbids `docs/adr/**`. The program `eng/repo-gates` compiles those
  files from a checkout of `main` and runs each check of them. This commit names each file of the
  work exactly.
- 2026-10-12: closed, after a full local run of 339 tests and 70 runs of the program.
