---
id: 0089
title: The gates bind the names that the compiler and MSBuild bind, and the archive and the review counter check their links
status: Done
phase: P0.35
opened: 2026-10-28
depends-on: [0088]
governed-by: []
writes:
  create:
    - tasks/TASK-0089-gate-holes-of-the-tenth-review.md
    - eng/repo-gates/DiscardedTasks.cs
  modify:
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/DeterminismCallGateTests.cs
    - Engine.Tests/Governance/UiLibraryGateTests.cs
    - Engine.Tests/Governance/AdrGateTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - eng/repo-gates/Program.cs
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
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

# TASK-0089 — The gates bind the names that the compiler and MSBuild bind, and the archive and the review counter check their links

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-28 found gaps in the gates. Each finding gives its evidence in
`docs/reviews/2026-10-28-codebase-review.md`.

- T63 (medium): a project under a `bin/` folder with the name of a permitted project hides from the dependency
  and determinism gates.
- T64 (medium): the gate of the UI library reads text, and four forms pass it.
- T65 (low): the ADR gate accepts an archived record that has no file.
- T66 (low): the review counter can lose a product milestone with no change to a gate file.
- T67 (low): two forms of a check that does not wait pass the gate runner.

`docs/templates.md`, section 7, puts a medium or low finding into a task after the next product phase, so this
task comes after P8a.

## Goal

Each gate of the review binds what the compiler or MSBuild binds, and checks each link that it reads.

## Scope (in)

1. T63: the dependency gate resolves each reference to its full path, and refuses a reference to a project
   outside the scanned set or into a `bin/` or `obj/` folder.
2. T64: the gate of the UI library binds the names of the namespace with Roslyn, reads the package references in
   each form and each imported file, and refuses a native import of `cimgui` outside `3DEngine/Ui/`.
3. T65: an archived record has its file in `docs/adr/archive/` and the status Withdrawn or Superseded.
4. T66: the counter takes the phase from the task file only, and a heading that names no task, or a product task
   that its roadmap row does not name, is a problem.
5. T67: the runner refuses a discarded task in a gate file.

## Scope (out)

- T62 (TASK-0088), and the findings of the hosts and the desktop (TASK-0090).

## Acceptance criteria

- [x] Each form of the review fails its gate, and each injection of the review is in a test where a test can hold it.
- [x] Each new test fails before its correction, or an injection shows that it can fail.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The merge
      records the run.

## Outcome

Status: Done · v0.94 · the commit that carries this block.

A reference into `bin/` or `obj/` is refused (T63); the gate of the UI library reads names, XML and imports as the
compiler and MSBuild do, and refuses a native import of `cimgui` outside the UI folder (T64); an archived ADR needs
its file and its status (T65); a product task that its roadmap row does not name is a problem (T66); and the gate
runner refuses a check that starts a task and does not wait for it (T67).

## Method

**Mechanical.** 16 new tests, with each theory case counted. The form of each finding of the review failed the gate
program after its correction, and each had passed it before: a shim project under `bin/`, an import of
`ui-packages.xml`, a deleted ADR with its row moved under "Archived", a roadmap row with no task, and a probe check
with a discarded task and a `Task.Run`. Injections of the old rule failed the new test cases.

**Judgement.** T66 keeps the rule D3 of TASK-0083 (`docs/templates.md`, section 7): a product task counts only when
its roadmap row names it, and an entry with no task counts toward the limit of twelve only. Scope item 4 also asked
that a heading with no task be a problem; that changes a rule of the owner, so the gate makes a product task with no
row a problem and keeps the rest. The gate of the UI library refuses each string literal that names `cimgui`
outside the UI folder, which also covers `NativeLibrary.Load`.

**Weakest.** The runner has no test of its own, as for T59 and T60; the probe check is the proof of T67. The gate of
the UI library still reads text for the comments and the global usings, which is stricter than the compiler.

## Notes for the implementer

- **The write set.** Each file of `Engine.Tests/Governance/` and of the gate program is a gate file, which the
  write set must name exactly.
- **T66 and the ledger.** A check of the link from a heading to its task reads each heading after the last review
  only, so that an old entry does not fail.
- **The size.** About 2 to 3 evenings.

## Progress

- 2026-10-28: opened by the codebase review of 2026-10-28 (TASK-0087).
- 2026-10-28: the task is active, after P0.36 (v0.93), as the owner ordered on 2026-10-28.
- 2026-10-28: T65 and T67. `AdrGateTests.ArchiveProblems` requires the file of each archived number in
  `docs/adr/archive/`, with the status Withdrawn or Superseded, and the row with that status; the form of the review
  (ADR-0001 deleted and its row moved under "Archived") failed the gate program, which passed it before. Four test
  cases. The gate runner reads each gate file and refuses a discard of a call and a `Task.Run` or `StartNew` as a
  statement (`eng/repo-gates/DiscardedTasks.cs`); both forms of the review, in a probe check, failed the runner. The
  runner has no test of its own (T59 and T60 had none), so the probe is the proof.
- 2026-10-28: T66. The rule D3 of TASK-0083 stays: the counter counts a product task only when its roadmap row names
  it. A product task after the last review whose row does not name it is now a problem of the gate
  (`UnnamedProductTasks`), so a milestone cannot become an entry with no trace. The phase still comes from the task
  file. Two tests; the form of the review (the row of P8a with no TASK-0091) failed the gate program.
- 2026-10-28: T63. Each project reference must resolve, from the folder of its project, to a project file that the
  dependency gate reads, so a reference into `bin/` or `obj/`, outside the repository or to no file is refused. Five
  tests. A probe with `shim/bin/Engine.Contracts.csproj` and a reference to it from `Engine.Core` failed the new
  check in the gate program. The determinism gate needs no change: the call of the shim cannot reach the log path
  when the reference is refused.
- 2026-10-28: T64. The gate of the UI library reads the tokens of the compiler, so an escaped name of `ImGuiNET`
  and a string that names `cimgui` outside `3DEngine/Ui/` are offences; it reads each project file as XML, in any
  quote and case; and it reads each file that MSBuild reads for a project, imports too. Five test cases. The import
  of `ui-packages.xml` into `3DEngine.Vulkan` failed the gate program, and three injections each failed the cases.
- 2026-10-28: closed after a clean build and a full local run of 754 tests (607 in `Engine.Tests`, 94 in
  `3DEngine.Tests`, 53 in `3DEngine.ImageTests`).
