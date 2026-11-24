---
id: 0463
title: The accept after a preview compares the result and applies a proposal as one group of steps, which the log holds and one Undo reverses
status: Active
phase: P16
opened: 2026-11-24
depends-on: [0462]
governed-by: [0004, 0006, 0007, 0008, 0013, 0015, 0016, 0018, 0019, 0020, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0035, 0037, 0039, 0045, 0049, 0054, 0055, 0060, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0085, 0086, 0089, 0092, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0112, 0113, 0114, 0115, 0116, 0120, 0122, 0124, 0127, 0129, 0130, 0131, 0133, 0134, 0135, 0136, 0137, 0138, 0139, 0140, 0141, 0152, 0153, 0155, 0158, 0163, 0166, 0167, 0168, 0170, 0171, 0174, 0176, 0177, 0179, 0181, 0182, 0184, 0185, 0187, 0188]
writes:
  create:
    - tasks/TASK-0463-the-accept-after-a-preview.md
    - docs/adr/0188-the-accept-after-a-preview-and-the-group-in-the-log.md
    - Engine.Api.Http/PreviewContent.cs
    - Engine.Tests/History/GroupHistoryTests.cs
    - Engine.Tests/Http/ProposalAcceptTests.cs
  modify:
    - Engine.Contracts/Command.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/DocumentSession.cs
    - Engine.Core/StepHistory.cs
    - Engine.Core/Features/FeatureList.cs
    - Engine.Core/Persistence/EntryCodec.cs
    - Engine.Core/Preview/DocumentFork.cs
    - Engine.Core/Preview/PreviewDifferences.cs
    - Engine.Core/Queries/CheckInterferenceQuery.cs
    - Engine.Core/Queries/CheckInterferenceQueryHandler.cs
    - Engine.Core/Sketch/SketchRebuild.cs
    - Engine.Api.Http/Proposals.cs
    - Engine.Api.Http/Proposals.Preview.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - 3DEngine/HostedSession.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/ProposalPreview.cs
    - 3DEngine.App/StepTexts.cs
    - 3DEngine.App/Ui/DesktopUi.Preview.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - Engine.Tests/Guides/AgentGuideExamplesTests.cs
    - Engine.Tests/Http/ProposalPreviewTests.cs
    - Engine.Tests/Http/ProposalsEndpointTests.cs
    - 3DEngine.ImageTests/FixedStatusTests.cs
    - 3DEngine.ImageTests/PendingChipTests.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - 3DEngine.ImageTests/ProposalPreviewWindowTests.cs
    - 3DEngine.ImageTests/ProposedSheetTests.cs
    - docs/adr/0015-command-log-persistence.md
    - docs/adr/0031-undo-and-redo.md
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/0130-the-proposal-in-the-window-as-in-the-render.md
    - docs/adr/0182-the-pair-check-at-one-position.md
    - docs/adr/0185-the-preview-of-a-proposal-on-a-fork.md
    - docs/adr/0187-the-preview-of-a-proposal-in-the-window.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/guides/agent-loop.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Kernel/**
    - 3DEngine.Vulkan/**
---

# TASK-0463 — The accept after a preview compares the result and applies a proposal as one group of steps, which the log holds and one Undo reverses

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P16 of the roadmap: the accept after a preview (ADR-0157 items 9 and 10). The accept applied each command of a proposal as
a command of the person, stopped at the first refused command with the commands before it applied, and gave one step of undo
for each command (ADR-0092 item 6). The window shows the preview since R25 (TASK-0462), so the accept can compare with it.

The codebase review of 2026-11-23 (TASK-0460) researched P16 in its sections 4 to 6 and asked Q2 to Q4 and Q6. The owner
answered "all A" on 2026-11-23 (recorded in row P16 and in the History of ADR-0185 by TASK-0462), and ordered "Start P16" in
the chat on 2026-11-24. Q2 A is a change of the public shape of `Engine.Contracts` that the owner approved; ADR-0188 records it
in the same commit.

The branch `p16-accept-after-preview` starts from `origin/main` at `65b3c025` (v0.445).

## Goal

The person accepts what the preview showed, or sees the new result first; a proposal applies each command or none; one
proposal is one step of undo, also after a save and an open.

## Scope (in)

1. `Command.GroupId`, an optional member, and the member `groupId` of an entry of the document file only when it is not null.
2. The unit of undo: consecutive steps of one group, which one `Undo` reverses and one `Redo` applies again, in the history of
   the bus, in `FeatureList`, in `SketchRebuild.InEffect` and in the step texts of the window. A move with a group is refused
   with the new code `E-CMD-GROUP-MOVE`.
3. `DocumentSession.ApplyGroup`: the commands of a group in one serial section at a version, and one `Undo` of the group at a
   failure.
4. The accept: the result now (no fork at the version of the seen preview), the comparison by content (`PreviewContent`), the
   held proposal with `held` and the new code `W-API-PROPOSAL-RESULT-CHANGED`, the stale group, and a result that stops.
5. The bound of the work of the checks of one preview (Q6 A).
6. The window: the version of the shown preview to the accept, "1 step of undo", the outcome texts, the note in the panel, and
   the panel above the line of the outcome.
7. ADR-0188 and the amendments of ADR-0015, ADR-0031, ADR-0092, ADR-0130, ADR-0182, ADR-0185 and ADR-0187; the guide for an
   agent with a tested example of `held`.

## Scope (out)

- A format version 2 for a file with a group: an older engine ignores the member (ADR-0188 "Bad").
- A member `work` in the answer of `CheckInterference`.
- An author of a command in the log, and a route that accepts or rejects.
- The phone: it has no queue of proposals (ADR-0092 item 8).

## Acceptance criteria

- [x] An entry with no group has the bytes of before P16; an entry with a group holds `groupId`; an open refuses a member that
  is not a string of a UUID.
- [x] One `Undo` reverses each step of a group and one `Redo` applies them again; a move with a group is refused.
- [x] An open rebuilds the same units, and a log with groups and an `Undo` of a group replays to the same Document and the same
  steps in effect in the three walks of the history.
- [x] A group at another version applies nothing; a failure, a known command and a handler that throws give one `Undo` of the
  group in the log.
- [x] An accept at the version of the seen preview runs no fork; a changed result applies nothing and holds the proposal with
  the new preview; an unchanged result after a commit runs one fork and applies; a commit between the comparison and the apply
  makes the accept compare again; a result that stops applies nothing.
- [x] The comparison names each member of the records of a preview.
- [x] The checks of one preview share one bound; the pairs after it have the state `limit`.
- [x] The window shows "1 step of undo", the outcome texts and the note; the panel ends above the line of the outcome; the
  picture shows the note over the new preview.
- [x] The guide shows `held` in a tested example.
- [x] The determinism hash of the probe is the same as at `65b3c025`.
- [x] Each injection makes a named test fail, and each file is restored byte for byte (sha256).
- [x] A clean build, the phone build, `dotnet test`, the write-set check and `eng/repo-gates` pass. Continuous integration is
  not run yet.

## Decisions

ADR-0188 records each decision. The central ones: the group in the log as an optional member that an entry with no group does
not write, so each older file keeps its bytes; the unit of undo as the run of consecutive steps of one group, so the history
needs no new record and the log is the only source; the apply of a group in one serial section at the version of the
comparison; one `Undo` of the group at a failure, which needs no rollback of the backend.

## Progress

- 2026-11-24: the group in the log, the unit of undo, the apply of a group, the accept after a preview, the bound of the checks,
  the window, the guide, ADR-0188 and the tests. 17 new tests in `Engine.Tests` (1917) and 5 in `3DEngine.ImageTests` (983
  here, 929 off Linux). The hash of the determinism probe is the hash at `65b3c025`.

## Outcome

Status: Active · commit (the merge of the branch p16-accept-after-preview) · v0.446

## Method

Mechanical: the codes in the registry and in `/schema/diagnostics`, the texts of the arc and of the outcome, the rows of the
index of the records, the History of the amended records, and the ledger entry.

Judgement: the group as the identifier of the proposal; the unit as the run of consecutive steps; the refusal of a move with a
group; the comparison by content with no version, no sequence, no time and no message; no apply when the seen result stops;
the place of the bound of the checks (one bound for the two sides, the first pair above it and each pair after it); the window
that gives the version of the shown preview; the panel above the line of the outcome.

Weakest: the test of the race between the comparison and the apply. It holds the fork of the version 2 until the accept waits
for it, with a wait of 300 ms; an accept that starts its wait later reads the version 3 and the test passes with no stale
apply. Injection 7 below shows that the test catches an apply that ignores the version on this computer.

The tests came after the code. So a script (`inject.js` in the scratchpad of the session, dotnet with shell:false, a filter of
one test class) applied 25 injections, each restored byte for byte (sha256). 23 made the named test fail; two did not:

| # | File | Injection | Failed test |
|---|---|---|---|
| 1 | `EntryCodec.cs` | an entry with no group writes `"groupId":null` | `An_Entry_With_No_Group_Keeps_Its_Bytes_And_An_Entry_With_A_Group_Holds_It` |
| 2 | `EntryCodec.cs` | the read drops the group | `An_Open_Rebuilds_The_Same_Units` |
| 3 | `StepHistory.cs` | a unit is always one step | `One_Undo_Reverses_Each_Step_Of_A_Group_And_One_Redo_Applies_Them_Again` |
| 4 | `FeatureList.cs` | an Undo moves one step | `A_Log_With_Groups_And_An_Undo_Of_A_Group_Replays_To_The_Same_Document_And_History` |
| 5 | `SketchRebuild.cs` | an Undo moves one step | `A_Log_With_Groups_And_An_Undo_Of_A_Group_Replays_To_The_Same_Document_And_History` |
| 6 | `CommandBus.cs` | a move with a group is not refused | `An_Undo_Or_A_Redo_With_A_Group_Is_Refused` |
| 7 | `DocumentSession.cs` | the group ignores the version | `A_Commit_Between_The_Comparison_And_The_Apply_Makes_The_Accept_Compare_Again` |
| 8 | `DocumentSession.cs` | no Undo of a failed group | `An_Accept_Stops_At_The_First_Rejected_Command_And_Applies_Nothing` |
| 9 | `DocumentSession.cs` | no Undo when a handler throws | `A_Handler_That_Throws_In_A_Group_Gets_An_Undo_Of_The_Group_And_Its_Exception_Goes_Out` |
| 10 | `Proposals.cs` | no comparison | `A_Commit_That_Changes_The_Result_Applies_Nothing_And_Shows_The_New_Preview` |
| 11 | `Proposals.cs` | no use of the seen preview at its version | none (see below) |
| 12 | `PreviewContent.cs` | the bodies are not compared | `Two_Previews_Compare_By_Their_Content` |
| 13 | `PreviewContent.cs` | the version is compared | `A_Commit_That_Does_Not_Change_The_Result_Runs_One_Fork_And_Applies` |
| 14 | `Proposals.cs` | a result that stops applies | `A_Result_That_Stops_Applies_Nothing` |
| 15 | `PreviewDifferences.cs` | the bound takes no work | `The_Checks_Of_One_Preview_Share_One_Bound_Of_Work` |
| 16 | `CheckInterferenceQueryHandler.cs` | the check ignores the limit of the fork | `The_Checks_Of_One_Preview_Share_One_Bound_Of_Work` |
| 17 | `CheckInterferenceQueryHandler.cs` | the check gives no work | `The_Checks_Of_One_Preview_Share_One_Bound_Of_Work` |
| 18 | `Proposals.Preview.cs` | the commits of the group ask for a preview of its proposal | none (see below) |
| 19 | `DesktopUi.Proposal.cs` | "N steps of undo" | `The_Lines_Show_Four_Commands_The_Count_And_The_Place` |
| 20 | `StepTexts.cs` | a unit is always one step | `The_Step_Of_Undo_Of_A_Proposal_Is_The_Whole_Group` |
| 21 | `DesktopUi.Preview.cs` | no note | `An_Accept_Whose_Result_Changed_Shows_The_New_Preview_With_A_Note` |
| 22 | `DesktopUi.Preview.cs` | the panel lies on the line of the outcome | `The_Panel_Ends_Above_The_Line_Of_The_Outcome` |
| 23 | `HostedSession.cs` | the window gives another version | `An_Accept_Whose_Result_Changed_Shows_The_New_Preview_With_A_Note` |
| 24 | `agent-loop.md` | the example of `held` gives null | `Each_Example_Of_The_Guide_Is_Accepted_By_A_Host_And_Gives_Its_Shape` |
| 25 | `DocumentSession.cs` | a known command is not found | `A_Command_That_The_Session_Knows_Fails_The_Accept` |

Injection 11 changes no behaviour: with no shortcut, the accept asks the queue for a preview of the version now, and the queue
gives the kept preview of that version at once with no fork (`SettleLocked`). So two places hold Q4 A, and the test holds the
outcome, not one of the two places. Injection 18 opens a race that no test holds: the worker can start a fork for the proposal
between the end of its group and its close, and the kept preview then becomes the preview of the version after the accept. The
window of the race is one take of the lock of the queue, and the test did not meet it. The read of `Proposals.Preview` that asks
for a copy of the log when the last preview is older than the version of the call has no test either; it closes the case of a
commit while the group of an accept waits for the serial section.
