---
id: 0043
title: Each injected violation of the second review fails its gate
status: Ready
phase: governance
opened: 2026-09-30
depends-on: [0042]
governed-by: []
writes:
  create:
    - tasks/TASK-0043-gate-holes.md
    - tasks/TASK-0044-host-checks.md
  modify:
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/WriteSetGateTests.cs
    - Engine.Tests/Governance/DeterminismCallGateTests.cs
    - Engine.Tests/Governance/MarkerGateTests.cs
    - Engine.Tests/Governance/TaskGovernanceGateTests.cs
    - Engine.Tests/Governance/RegisterGateTests.cs
    - .github/workflows/ci.yml
    - docs/templates.md
    - docs/diagnostics.md
    - docs/register.md
    - docs/CURRENT-STATE.md
    - tasks/TASK-0034-document-session.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - CLAUDE.md
    - docs/CHARTER.md
    - docs/adr/**
---

# TASK-0043 — Each injected violation of the second review fails its gate

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-09-30 injected eight violations that pass their gate, findings T3 to
T6, and register entry R-0031 holds them. One of them is a reference from `Engine.Core` to
`3DEngine.Core` inside an `Include` with two paths. The owner answered yes to question Q4 of the
review: correct the high gate holes before TASK-0034 starts. The owner also answered yes to Q1, Q3
and Q5, and this task records those three decisions in the files that hold them.

## Goal

Each violation that the review injected fails its gate, and the three decisions are in their files.

## Scope (in)

1. The dependency gate reads each project file as XML, and it takes each path of each `Include`.
2. The write-set gate: a merge commit is checked on the changes that the merge made itself; a commit
   that names a task also changes the file of that task; a gate file must be named exactly, so a
   pattern such as `Engine.Tests/**` does not permit it; a test holds the cut-off commit.
3. The determinism gate reads each source with its comments removed, and it also refuses
   `double.Pow`, `float.Sin`, a static import of `System.Math`, and a call with the parenthesis on the
   next line.
4. The marker gate refuses an identifier that is not in `docs/register.md`.
5. The task gate refuses a status outside `Ready`, `Active`, `Done` and `Deferred`, and an identifier
   that differs from the file name.
6. The register gate compares the count of entries that it parsed with the count of headings in the
   section, so an open code fence cannot hide an entry.
7. The two small jobs of the workflow use `bash` with `pipefail`, and each git command is tested.
8. The decisions: TASK-0034 gains "change the Document first, publish second" and three tests (Q1);
   `docs/diagnostics.md` gives the close status 1007 (Q3); TASK-0044 opens for the host checks (Q5).

## Scope (out)

- The low findings of T7, and the parts of T5 that need a new parser: an `affects` value with quotes,
  the reserve table heading, and the codebase review gate. R-0031 stays open for them.
- No change to a gate rule that a document states, other than the three that section 8 names.
- No change to engine or host code.

## Acceptance criteria

- [ ] Each of the eight injected violations of the review fails its gate, and the task records each
      run.
- [ ] Each commit after the cut-off passes the write-set rule, with the same replay as TASK-0039.
- [ ] A merge with a change of its own is refused, shown on a local merge that is then removed.
- [ ] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Notes for the implementer

Inject each violation before the change and show that the gate passes; then change the gate and
show that it fails; then remove the violation. The review gives each injection in section 3.

## Progress

- 2026-09-30: the task is open.
- 2026-09-30: three decisions of the owner are in their files: TASK-0034 (Q1), docs/diagnostics.md (Q3), TASK-0044 (Q5).
- 2026-09-30: the dependency gate reads XML. Injected two paths in one Include: passed before, fails now.
- 2026-09-30: the write-set gate checks a merge on its own changes, requires the named task in the change, requires an exact name for a gate file, and holds the cut-off; the two small jobs use bash with pipefail. Injected: a gate file under TASK-0038, a named task not changed, a moved cut-off; each one fails. A merge with its own change, in a throwaway clone, lists the change under diff-tree --cc and nothing under the plain form.
- 2026-09-30: the determinism gate reads sources without comments and refuses double.Pow, a static import and a call over two lines; the marker gate reads the register. Injected: the three forms and R-9999, each one fails; a comment that names a function passes.
