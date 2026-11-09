---
id: 0320
title: The small gate holes of R-0031 are closed
status: Active
phase: P0.133
opened: 2026-11-09
depends-on: [0319]
governed-by: []
writes:
  create:
    - tasks/TASK-0320-the-small-gate-holes-of-r-0031.md
  modify:
    - Engine.Tests/DocumentSessionConcurrencyTests.cs
    - Engine.Tests/Http/HttpConcurrencyTests.cs
    - Engine.Tests/Governance/AdrGateTests.cs
    - Engine.Tests/Governance/AffectsCitationGateTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/MarkerGateTests.cs
    - Engine.Tests/Governance/NativePackageGateTests.cs
    - Engine.Tests/Governance/RegisterGateTests.cs
    - Engine.Tests/Governance/RepositoryFiles.cs
    - Engine.Tests/Governance/TaskGovernanceGateTests.cs
    - eng/test-floor.txt
    - nuget/.gitignore
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0320 — The small gate holes of R-0031 are closed

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0031 ("A gate can pass while its rule is broken") is due on 2026-12-24. On 2026-11-09 the owner chose a
cleanup of the old findings before the codebase review that is due, with the small findings fixed. A read-only check of the
gates on 2026-11-09 gave the state of each part; an agent made the changes in a worktree, and this session reviewed the diff
and repeated an injection.

## Goal

Each small hole of R-0031 fails on an injection, and the register gives the parts that stay for the owner.

## Scope (in)

1. T4 marker part and T13 part 3: the marker gate accepts only the identifiers of open and accepted entries, and reads
   `nuget/.gitignore`, whose comment now names the acceptance of R-0007.
2. T5: the gates strip the quotes and the brackets of an `affects` list and refuse what stays.
3. T7: the index of the ADRs compares the columns "Amends" and "Amended by"; `previous: none` is for the first review only.
4. T10: the register gate reads the exact heading of an entry and refuses a repeated field.
5. T13 part 4: the two tests of concurrency assert that each reader ran.
6. T17: the folder feed holds only the packages that the gate reads.
7. T23: the path gate finds a path of a Windows runner.
8. A gate refuses a response file (`*.rsp`) outside `bin` and `obj`.

## Scope (out)

- T4 dispatch part (medium), and the parts for the owner to accept: the clock of the register gate (T7), the case of a path
  on Windows (T7), the provenance of the native binaries (T23), the local mode of the write-set check (T25), and the three
  gates that run engine code in the test process (T31).

## Acceptance criteria

- [ ] Each change fails on an injection, and `repo-gates` and `Engine.Tests` pass.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the text of each finding and the code of each gate.
Judgement:  T5 strips the quotes and the brackets and refuses what stays, because a refusal of each bracket would fail
            the empty list `affects: []` of ADR-0092; the judge `eng/write-set-check` does not change.
Weakest:    to be written at the close.

## Progress

- 2026-11-09: the eight changes and ten new test cases (eight forms of the path of T23, the folder feed, and the response
  file). Each change failed on an injection first (the record of the agent); this session repeated one: a "TODO" with the
  closed identifier R-0024 passed the old marker gate and fails the new one.
