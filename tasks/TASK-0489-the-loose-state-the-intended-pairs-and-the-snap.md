---
id: 0489
title: A joint is tight or loose, each joint is an intended pair of the check, and PlaceByConnectors places a child one time
status: Active
phase: P11b
opened: 2026-11-28
depends-on: [0487]
governed-by: [0004, 0006, 0008, 0012, 0013, 0015, 0016, 0018, 0019, 0020, 0021, 0028, 0031, 0033, 0037, 0045, 0060, 0083, 0089, 0112, 0153, 0155, 0158, 0166, 0167, 0168, 0169, 0179, 0182, 0188, 0190, 0191, 0192, 0193, 0194]
writes:
  create:
    - tasks/TASK-0489-the-loose-state-the-intended-pairs-and-the-snap.md
    - docs/adr/0194-the-loose-state-the-intended-pairs-and-the-snap-by-connectors.md
    - Engine.Core/Commands/SetJointStateCommand.cs
    - Engine.Core/Commands/SetJointStateCommandHandler.cs
    - Engine.Core/Commands/PlaceByConnectorsCommand.cs
    - Engine.Core/Commands/PlaceByConnectorsCommandHandler.cs
    - Engine.Tests/Commands/JointStateTests.cs
  modify:
    - Engine.Contracts/Joints/JointRecord.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/Commands/JointTree.cs
    - Engine.Core/Commands/JointResults.cs
    - Engine.Core/Commands/PlaceBodyCommandHandler.cs
    - Engine.Core/Queries/CheckInterferenceQueryHandler.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - docs/adr/0182-the-pair-check-at-one-position.md
    - docs/adr/0193-the-joints-of-a-part.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/guides/agent-loop.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
---

# TASK-0489 — A joint is tight or loose, each joint is an intended pair of the check, and PlaceByConnectors places a child one time

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0193 item 10 left three parts of row P11 to P11b: the loose state (Q5 A), the intended pair of each joint in
`CheckInterference` (ADR-0157 item 5), and `PlaceByConnectors` (Q6 A). The owner answered "Yes, all A" for Q1 to Q7 of
`docs/proposals/2026-11-28-the-joints-of-p11.md` on 2026-11-28. The second codebase review of 2026-11-28 (TASK-0487) puts P11b
next, before P0.201.

## Goal

A joint is tight or loose, the check takes the pair of each joint as intended, and `PlaceByConnectors` places a child by two
connectors one time, with ADR-0194 in the same commit as the change of `Engine.Contracts`.

## Scope (in)

1. The field `State` of `JointRecord` and the command `SetJointState`.
2. The pairs of the joints in `CheckInterference`.
3. The command `PlaceByConnectors`.
4. The answer to Q1 of the second review of 2026-11-28 (section 7 of `docs/templates.md`: the first commit of the next task).

## Scope (out)

- E254 and the other findings of the second review of 2026-11-28: TASK-0488 (P0.201).
- The travel (K12), the envelope (K13) and the tools of the window (R23, R27).

## Acceptance criteria

- [x] A test of each item of ADR-0194, and two injections that make a test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · commit (the merge of the branch p11b-joints) · v0.472

## Method

Mechanical: the placement of `PlaceByConnectors` is the placement of a fixed joint of ADR-0193 item 3, with no record.
Judgement:  a state that the joint has is refused; a loose joint still counts for the tree; the loop of `PlaceByConnectors` is
            judged on the tight joints only, because only they move the parent.
Weakest:    `PlaceByConnectors` reads the frames at its place in the log, so a later change of a shape does not move the child
            again (R-0083).

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after the second review of 2026-11-28 (v0.471). The standing order
  answers Q1 of that review with A: a joint whose parent or child is not live does not hold the child, so a `PlaceBody` of the
  child applies, and the joint still counts for the one parent and for a loop. TASK-0488 (P0.201) does it with E254.
- 2026-11-28: ADR-0194, the field `State`, `SetJointState`, `PlaceByConnectors`, the pairs of the joints in the check, and 6
  tests in `Engine.Tests/Commands/JointStateTests.cs`. Two injections (a loose joint that places its child; a check with no pairs
  of the joints) each made a test fail. Ledger v0.472.
