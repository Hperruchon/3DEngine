---
id: 0098
title: The gates read the names, the packages and the assemblies that the build binds, and the review counter reads each heading
status: Active
phase: P0.39
opened: 2026-10-28
depends-on: [0102]
governed-by: [0022]
writes:
  create:
    - tasks/TASK-0098-gate-forms-of-the-eleventh-review.md
  modify:
    - Engine.Tests/Governance/UiLibraryGateTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - Engine.Tests/Governance/AdrGateTests.cs
    - Engine.Tests/Hosting/DispatchSurfaceGateTests.cs
    - eng/repo-gates/DiscardedTasks.cs
    - eng/repo-gates/Program.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - docs/templates.md
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

# TASK-0098 — The gates read the names, the packages and the assemblies that the build binds, and the review counter reads each heading

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second codebase review of 2026-10-28 found new forms that pass the gates that TASK-0089 corrected. Each finding
gives its evidence in `docs/reviews/2026-10-28-b-codebase-review.md`.

- T70 (medium): a `global using ImGuiNET;` in `3DEngine/Ui/` opens the namespace to each file of the host, and a
  concatenated constant in a `DllImport` hides `cimgui`.
- T71 (medium): a package name with an MSBuild escape (`ImGui%2ENET`) passes both package gates, and NuGet restores
  the package.
- T72 (medium): a client can take the assembly name `Engine.Tests` and read the internals of the kernel.
- T73 (low): the review counter loses a product milestone in three more ways.
- T74 (low): five more forms of a check that does not wait pass the gate runner.
- T75 (low): an archived ADR passes with a file that holds front matter only.
- T77 (low): a test file holds a NUL byte, so git and GitHub show no text diff of it.

`docs/templates.md`, section 7, puts a medium or low finding into a task after the next product phase, so this
task comes after K2.

## Goal

Each form of the review fails the gate program.

## Scope (in)

1. T70: the gate refuses a global using of `ImGuiNET` in each file, and reads the constant value of the argument of
   a native import.
2. T71: each package gate unescapes an `Include` before it compares it, or refuses a `%` in it.
3. T72: the dependency gate refuses an `AssemblyName` that differs from the name of its project file.
4. T73: the review counter requires ledger versions that rise, one task in each heading, and the phase of each task
   that a product row names.
5. T74: the runner refuses each expression of type `Task` or `ValueTask` whose value a check does not await or
   return, or it waits for each pending task after a check.
6. T75: an archived ADR keeps its identifier, its title and its body.
7. T77: the NUL byte of `3DEngine.Tests/SdlInputTests.cs` becomes the escape `\0`, and a gate refuses a control
   byte in a `.cs` file.
8. `DispatchSurfaceGateTests` and `DependencyDirectionGateTests` skip `bin/` and `obj/` at the root of a project
   only, with the rule of T68 (TASK-0097).
9. T78, by the answer A of the owner to Q10 (2026-10-28): the measures of a codebase review gain three rows, the
   lines of the rule files, the count of "must" and the count of "do not", with the method of
   `docs/reviews/2026-09-25-rules-review.md`, section 1. `CodebaseReviewGateTests` requires the rows in a review
   after this task, and `docs/templates.md`, sections 6 and 7, say so. The inventory of the rules stays a separate
   review when the owner asks for it.
10. The scans of `UiLibraryGateTests` skip `.claude/`, as `DependencyDirectionGateTests` does. On the computer of the
    owner the folder holds the worktrees of other sessions, and the gate failed there on their copies (TASK-0102,
    v0.103).

## Scope (out)

- T76: the record of the review closes it.
- The three gates that run engine code stay in the test process (T31, register entry R-0031).

## Acceptance criteria

- [ ] Each form of the review fails the gate program after its correction, and passed it before.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The judge.** Each gate file must be named exactly in `writes`.
- **The size.** About 2 to 3 evenings.

## Progress

- 2026-10-28: opened by the second codebase review of 2026-10-28 (TASK-0095).
- 2026-10-29: the owner put K3a (TASK-0102) before this task, so it depends on TASK-0102.
- 2026-10-29: K3a is Done at v0.103, and this task starts. Item 10 is new: the local failure of the UI gate that
  TASK-0102 reported. T72 keeps the four programs whose assembly name is not the name of their file (`engine`,
  `engine-api-http`, `repo-gates`, `write-set-check`): the gate requires assembly names that are unique and that are
  not the name of another project, which closes the same hole.
- 2026-10-29: T70, T71, T72, T77 and items 8 and 10. `UiLibraryGateTests` refuses a global using of the namespace in
  each file, reads the value of a join, an interpolation and a constant of the file, and unescapes %XX before it
  compares a package. `DependencyDirectionGateTests` refuses an Include with an MSBuild escape, an assembly name of
  another project or a computed one, an AssemblyName outside a project file, and a control byte in a source file; it
  and `DispatchSurfaceGateTests` leave out bin/ and obj/ at the root of a project only. The NUL byte of
  `SdlInputTests.cs` is now \0. The eight new forms of T70 and T71 failed their tests on the gate before the change.
- 2026-10-29: T73, T75 and T78. The review counter counts each heading, counts a product milestone when the row of a
  product phase names one of its tasks, and refuses a version that does not rise, a heading after the last review
  with two tasks, and a task file whose phase is not the phase of its row. An archived ADR keeps its id, its title
  and its sections "## Context" and "## Decision". A review with the ledger v0.104 or later gives the rows "Lines in
  the rule files", "Sentences with "must"" and "Sentences with "do not""; `docs/templates.md`, sections 6 and 7, say so.
- 2026-10-29: T74. The runner binds each gate file with the compiler, and refuses each statement whose value is a
  task and each local of a task type that the check never reads. The runner checks its own seven forms of T67 and
  T74 and four forms that wait at each run. With a type check that sees no task, the run fails on the five forms of
  T74, which is the behaviour of the runner before the change.
