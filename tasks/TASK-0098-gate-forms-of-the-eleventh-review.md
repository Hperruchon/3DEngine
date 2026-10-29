---
id: 0098
title: The gates read the names, the packages and the assemblies that the build binds, and the review counter reads each heading
status: Ready
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
