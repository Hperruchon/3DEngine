---
id: 0064
title: The gates that read the repository run as a program that the pipeline builds from main
status: Active
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

- [ ] Each moved gate fails on its own injection from the program, as it failed as a test.
- [ ] Two forms of the review that passed option A, the escaped attribute and `AppDomain.SetData`, no
      longer change what a moved gate reads.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

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
