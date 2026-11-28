---
id: 0488
title: The medium and low findings of the second codebase review of 2026-11-28
status: Active
phase: P0.201
opened: 2026-11-28
depends-on: [0487]
governed-by: [0004, 0006, 0012, 0015, 0016, 0019, 0020, 0021, 0028, 0031, 0033, 0037, 0045, 0054, 0060, 0076, 0083, 0089, 0112, 0153, 0179, 0181, 0188, 0190, 0191, 0193, 0194]
writes:
  create:
    - tasks/TASK-0488-the-findings-of-the-review-of-2026-11-28-b.md
    - Engine.Tests/Commands/JointFindingsTests.cs
  modify:
    - tasks/TASK-0488-the-findings-of-the-review-of-2026-11-28-b.md
    - Engine.Api.Http/Json/JsonRequest.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/Features/FeatureList.cs
    - Engine.Core/Commands/JointTree.cs
    - Engine.Core/Commands/PlaceBodyCommandHandler.cs
    - Engine.Core/Commands/PlaceByConnectorsCommandHandler.cs
    - Engine.Core/DocumentSession.cs
    - Engine.Tests/Commands/JointTests.cs
    - Engine.Tests/History/GroupHistoryTests.cs
    - Engine.Tests/Http/RequestTextTests.cs
    - docs/adr/0193-the-joints-of-a-part.md
    - docs/adr/0194-the-loose-state-the-intended-pairs-and-the-snap-by-connectors.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
---

# TASK-0488 — The medium and low findings of the second codebase review of 2026-11-28

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second codebase review of 2026-11-28 (`docs/reviews/2026-11-28-b-codebase-review.md`) found two medium and five low findings.
By the order of the review they come after the next product phase, P11b.

## Goal

Each finding of the list below is corrected with a test, or recorded with its reason.

## Scope (in)

1. E251: key the table of the placements of a step by the place of the entry in the log, and keep the placements of each step
   beside the step in the feature list. The probe of the review as a test, with an open.
2. E252: the check of the charset in the form that the read takes; `utf-7` and a quoted charset in `RequestTextTests`.
3. E253: the order of two directions of a pivot with exact numbers.
4. E254: the answer of the owner to Q1 of the review.
5. E255: the exception of the handler and the two keys when the reversal of a group throws a cancellation.
6. T206: the tests of the joints compare the faces after an open, an `Undo` and a `Redo`, and test a boolean of a child, a
   cylinder connector and a datum with a fault.
7. T207: the hosts of the tests set no time limit of the subscribe frame.

The write set grows when the task starts.

## Scope (out)

- Each other open finding.

## Acceptance criteria

- [x] Each finding of the scope has a test that fails with the old code, or a reason in the ledger. T207 stays open (see the
      ledger).
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · commit (the merge of the branch p0201-review-findings) · v0.473

## Method

Mechanical: the probes of the review.
Judgement:  the correction of each finding.
Weakest:    T206 has no test of a datum with a fault as a connector: no command of a part gives a named datum a fault while
            its body stays. The example of E253 in the review was not on the limit in exact numbers (see the ledger).

## Progress

- 2026-11-28: opened by TASK-0487, the second codebase review of 2026-11-28.
- 2026-11-28: started after P11b (v0.472). E251, E252, E253, E254 (Q1 A), E255 and T206, each with a test; five injections of
  the old code each made a test fail. T207 stays open: a module initializer that sets no limit for each host of the tests is
  refused by the write-set check (finding T31), and a limit in each host touches the shared test client and each scenario test.
  Ledger v0.473.
