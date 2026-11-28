---
id: 0479
title: The joints of P11, a record of a joint between two bodies in a kinematic tree with no solver
status: Active
phase: P11a
opened: 2026-11-28
depends-on: [0477]
governed-by: [0004, 0006, 0008, 0012, 0013, 0015, 0016, 0018, 0019, 0020, 0021, 0028, 0031, 0033, 0037, 0045, 0054, 0060, 0074, 0076, 0083, 0089, 0112, 0114, 0153, 0155, 0158, 0166, 0167, 0168, 0169, 0179, 0181, 0183, 0188, 0190, 0191, 0193]
writes:
  create:
    - tasks/TASK-0479-the-joints-of-p11.md
    - docs/proposals/2026-11-28-the-joints-of-p11.md
    - docs/adr/0193-the-joints-of-a-part.md
    - Engine.Contracts/Joints/JointRecord.cs
    - Engine.Core/Commands/AddJointCommand.cs
    - Engine.Core/Commands/AddJointCommandHandler.cs
    - Engine.Core/Commands/SetJointValueCommand.cs
    - Engine.Core/Commands/SetJointValueCommandHandler.cs
    - Engine.Core/Commands/RemoveJointCommand.cs
    - Engine.Core/Commands/RemoveJointCommandHandler.cs
    - Engine.Core/Commands/JointResults.cs
    - Engine.Core/Commands/JointTree.cs
    - Engine.Core/Queries/GetJointsQuery.cs
    - Engine.Core/Queries/GetJointsQueryHandler.cs
    - Engine.Tests/Commands/JointTests.cs
  modify:
    - docs/proposals/2026-11-28-the-joints-of-p11.md
    - tasks/TASK-0479-the-joints-of-p11.md
    - Engine.Contracts/Document.cs
    - Engine.Contracts/Handlers/ICommandHandler.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/StepHistory.cs
    - Engine.Core/Features/FeatureList.cs
    - Engine.Core/Commands/PlaceBodyCommandHandler.cs
    - Engine.Core/Queries/GetConnectorsQueryHandler.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Core/Determinism/CrossPlatformProbe.cs
    - Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
    - docs/adr/0157-the-foundations-and-the-order-of-the-assembly-track.md
    - docs/adr/0191-the-placement-of-a-body.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/register.md
    - docs/guides/agent-loop.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
---

# TASK-0479 — The joints of P11, a record of a joint between two bodies in a kinematic tree with no solver

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P11 of the roadmap: joints in the Document. P11.0 (TASK-0474, ADR-0191) gave each body a placement. The order of the review
of 2026-11-28 (TASK-0475, the answer Q1 A) puts K25 and P0.193 next, then R34 and R35, which wait for a check on the phone, so
P11 is the next phase that needs no person. The joints change the public shape of `Engine.Contracts`, and `CLAUDE.md` ("Stop and
ask") asks the owner before such a change. So the first commit of this task is a proposal with the questions, and the forbid
holds `Engine.Contracts/**` until the answers.

## Goal

The Document holds joints between bodies in a kinematic tree, and each tight joint gives the placement of its child, with an
ADR in the same commit as the change of `Engine.Contracts`.

## Scope (in)

1. The proposal `docs/proposals/2026-11-28-the-joints-of-p11.md`: the facts, a recommended model and the questions Q1 to Q7.
2. After the answers: the ADR, the record, the commands and a test of each rule. The write set grows then.
3. The answer Q7 A: the record, the three kinds, the values, the limits, the tree and the refusals (ADR-0193). The loose state,
   the intended pairs in `CheckInterference` and `PlaceByConnectors` are P11b.

## Scope (out)

- Each change of `Engine.Contracts` before the answers of the owner.
- The travel (K12), the envelope (K13) and the tools of the window (R23, R27).

## Acceptance criteria

- [x] The proposal gives each fact with its file, and each question with a recommended option.
- [x] The owner answers Q1 to Q7, and the first commit after the answers records them.
- [x] The ADR and the change of `Engine.Contracts` are in one commit, with a test of each rule.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · commit (the merge of the branch p11a-joints) · v0.470

## Method

Mechanical: the facts of section 2 of the proposal, each read in its file.
Judgement:  the recommended model; a proposal and not a decision under the standing order, because the change of the public
            shape needs the owner.
Weakest:    a joint keeps the connector frames of its AddJoint, so a later change of a shape does not move the child, and a
            joint of a consumed body places nothing (R-0083).

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28; the proposal with the questions Q1 to Q7. Not merged before the
  answers.
- 2026-11-28: the owner answered "Yes, all A" for Q1 to Q7: a connector is a face or a named datum in the frame of the body;
  the two normals are opposite; the value of a pivot is a cosine and a sine; each command of a joint sets the placements of the
  subtree in the same step; a loose joint keeps its record; a command `PlaceByConnectors` records no joint; P11a is the record,
  the three kinds, the values, the limits, the tree and the refusals, and P11b is the loose state, the intended pairs and
  `PlaceByConnectors`. This commit records them.
- 2026-11-28: ADR-0193, the record, the commands `AddJoint`, `SetJointValue` and `RemoveJoint`, the query `GetJoints`,
  eight codes `E-JOINT-*`, and 14 tests in `Engine.Tests/Commands/JointTests.cs`. The walks of the steps read each placement
  from the `PlaceBody` commands of the log, so a first run of the tests read each child at its first place; the command bus now
  records the placements of each step, and the feature list reads them (ADR-0193 item 9). Register entry R-0083. Ledger v0.470.
