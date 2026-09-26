---
id: 0039
title: The rules are fewer, each one has one home, and ten more have a gate
status: Ready
phase: governance
opened: 2026-09-26
depends-on: [0033]
governed-by: []
writes:
  create:
    - tasks/TASK-0039-simplify-the-rules.md
    - tasks/TASK-0040-living-adrs.md
    - docs/reviews/2026-09-25-rules-review.md
    - docs/archive/working-agreement.md
    - Engine.Tests/Governance/AdrEnforcementExistsGateTests.cs
    - Engine.Tests/Governance/TaskGovernanceGateTests.cs
    - Engine.Tests/Governance/DeterminismCallGateTests.cs
    - Engine.Tests/Governance/DocumentPathGateTests.cs
  modify:
    - CLAUDE.md
    - docs/INDEX.md
    - docs/glossary.md
    - docs/templates.md
    - docs/register.md
    - docs/diagnostics.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - docs/adr/README.md
    - docs/CHARTER.md
    - docs/working-agreement.md
    - docs/conventions.md
    - docs/open-questions.md
    - .github/workflows/ci.yml
    - .github/PULL_REQUEST_TEMPLATE.md
    - Engine.Tests/Governance/WriteSetGateTests.cs
    - Engine.Tests/Governance/RepositoryFiles.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/MarkerGateTests.cs
    - Engine.Tests/Hosting/DispatchSurfaceGateTests.cs
    - Engine.Tests/Http/SchemaEndpointGateTests.cs
    - Engine.Tests/Diagnostics/DiagnosticsScanner.cs
    - Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/0*.md
    - nuget/**
---

# TASK-0039 — The rules are fewer, each one has one home, and ten more have a gate

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`docs/reviews/2026-09-25-rules-review.md` examined each rule and the organization of the
repository. It found 124 rules in thirteen files, 70 of them in text only, fourteen statements about
a repository that no longer exists, five rules that the practice contradicts, and eighteen files
that a session reads to find its position. The owner agreed with the proposal on 2026-09-26, with
four directions: show the organization, change the rules that contradict the practice, clean each
statement that does not need to exist, and keep each ADR in agreement with the code.

Register entry R-0019 holds the stale statements. Register entry R-0026 holds the permit defect of
the write-set gate.

## Goal

Each rule has one home, each stale statement is gone, and ten rules that were text have a gate.

## Scope (in)

1. `docs/working-agreement.md`, `docs/conventions.md` and `docs/open-questions.md` are deleted. Each
   live rule moves to `CLAUDE.md` or `docs/templates.md`, as section 2 of the review says. The
   rationale of the working agreement moves to `docs/archive/`.
2. `CLAUDE.md` becomes a position file of about 120 lines, with the sections of review section 4.2.
   The determinism rules stay in force and unchanged.
3. `docs/templates.md` absorbs the code conventions, the ADR rules and the rule for rules. Its gate
   table gets one row for each gate class.
4. Four new gates: the `enforced-by` file of an in-force ADR exists; `governed-by` and `depends-on`
   of a Ready task are correct; no source in the log path calls a function that the determinism
   rules forbid; each path in `docs/INDEX.md` exists and each gate class has a row in the gate
   table.
5. The write-set gate reads the task that the commit message names.
6. Five gates read a scan instead of a fixed list: the dispatch gate, the diagnostics scanner, the
   schema gate, the dependency gate and the marker gate.
7. Each stale statement of review section 3.5 is corrected or removed.
8. One status vocabulary for a task. The roadmap loses its status column.
9. The contract gate runs on each push.
10. Register entries R-0019 and R-0026 close. One ledger entry, v0.34, records the work.
11. `tasks/TASK-0040-living-adrs.md` opens with the work list of review section 4.7.

## Scope (out)

- No change to the text of an ADR. The folds of review section 4.7 are TASK-0040.
- No change to the objectives, the anti-objectives or the non-goals of `docs/CHARTER.md`. Two
  pointer lines that name a deleted file change. The label `V1` gets its definition in the glossary.
- No change to engine code, to `Engine.Contracts`, or to a host. The gates are tests.
- No start of TASK-0028 or TASK-0034 to TASK-0038.

## Acceptance criteria

- [ ] `docs/working-agreement.md`, `docs/conventions.md` and `docs/open-questions.md` do not exist.
- [ ] `CLAUDE.md` has at most 150 lines and holds the six determinism rules unchanged. The first
      estimate was 130; the diagram and the six rules, which stay, take forty lines.
- [ ] Each new gate failed on an injected violation before its commit, and the task records the
      injection.
- [ ] Each corrected gate catches a violation that the old list missed, and the task records it.
- [ ] `WRITE_SET_TASK=TASK-0032` with the change list of v0.32 plus `Engine.Core/CommandBus.cs`
      fails, which is the case of R-0026.
- [ ] No path in `docs/INDEX.md` is absent. Each `*GateTests.cs` class has a row in
      `docs/templates.md`.
- [ ] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.
- [ ] The measures of review section 5 are repeated in the ledger entry with the same method.

## Notes for the implementer

Make the changes in small commits, one topic in each commit. Before each commit, run the governance
tests with `set -o pipefail`, and run the write-set check with `WRITE_SET_FILES` set from
`git diff --cached --name-only --no-renames` and `WRITE_SET_TASK=TASK-0039`. When you change a gate,
inject a violation that the gate catches today, then show what catches it after the change.

## Progress

- 2026-09-26: the task and the review are in the repository.
- 2026-09-26: the enforced-by gate. Injected: ADR-0001 named a missing file, ADR-0018 named a Done task. Each one failed.
- 2026-09-26: the task governance gate. Injected on TASK-0034: governed-by with 0099, governed-by without 0020, depends-on 0099. Each one failed.
- 2026-09-26: the determinism call gate. Injected: Math.Pow in Engine.Core, win-x86 in Engine.Cli.csproj. Each one failed.
- 2026-09-26: five gates read a scan instead of a fixed list. Injected: a new host file with "CreateBox", a new code in Engine.Api.Http, a project in no class, a TODO in that project, a quoted query name in the schema endpoint. The old lists passed the first four; each scan fails all five.
- 2026-09-26: the write-set gate reads the task that the commit trailer names, and the contract gate runs on each push. Injected the case of R-0026: the change list of v0.32 plus Engine.Core/CommandBus.cs failed with TASK-0032 named, and failed again with no task named. Each commit after the cut-off passes the rule. A prose line of the v0.27 message that starts with an identifier made the first form of the rule fail; the trailer form corrects it.
- 2026-09-26: docs/templates.md absorbs the working agreement and the conventions; docs/INDEX.md is the one map; docs/open-questions.md is gone; the rationale of the working agreement is in docs/archive/. Two pointer lines of docs/CHARTER.md named the deleted file and now point at CLAUDE.md; the write set of this task moved the charter from forbid to modify for those two lines, and the gate had refused the commit before that change. The path gate: injected a row in INDEX.md with a path that does not exist, and the gate table without MarkerGateTests. Each one failed.
- 2026-09-26: one status vocabulary (the roadmap loses its Status column, Active goes), V1 and V1.x defined in the glossary, the diagnostics registry says do not remove and do not change the meaning, the ADR index points at the form, the pull request template has five lines.
