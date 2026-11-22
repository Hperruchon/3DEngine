---
id: 0456
title: The preview of a proposal gives the outcome of each command and the changed bodies from a fork of the Document
status: Done
phase: P15a
opened: 2026-11-22
depends-on: [0455]
governed-by: [0004, 0007, 0013, 0015, 0016, 0019, 0024, 0029, 0030, 0061, 0083, 0092, 0102, 0111, 0114, 0127, 0153, 0155, 0158, 0159, 0160, 0161, 0166, 0179, 0185]
writes:
  create:
    - tasks/TASK-0456-the-preview-of-a-proposal-on-a-fork.md
    - docs/adr/0185-the-preview-of-a-proposal-on-a-fork.md
    - Engine.Core/Preview/DocumentFork.cs
    - Engine.Api.Http/Proposals.Preview.cs
    - Engine.Tests/Http/ProposalPreviewTests.cs
  modify:
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Proposals.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/EngineSurface.cs
    - Engine.Api.Http/Endpoints/ProposalsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Api.Http.Server/ServerEngine.cs
    - 3DEngine/DesktopSession.cs
    - Engine.Tests/Guides/AgentGuideExamplesTests.cs
    - Engine.Tests/Http/ProposalsEndpointTests.cs
    - docs/guides/agent-loop.md
    - docs/diagnostics.md
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - 3DEngine.App/**
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0456 — The preview of a proposal gives the outcome of each command and the changed bodies from a fork of the Document

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P15 of the roadmap: the preview of a proposal on a fork of the Document (S11 of
`docs/proposals/2026-11-10-gaps-to-the-render.md`, ADR-0157 item 8). The owner allowed a second Document in one runtime on
2026-11-09 (R-0055, ADR-0071). The third codebase review of 2026-11-21 (`docs/reviews/2026-11-21-c-codebase-review.md`,
sections 4 to 6) researched the design and asked Q1 to Q5. On 2026-11-22 the owner ordered in the chat "start P15 with the A
answers", then "Resume all".

The branch `p15a-preview-fork-b` starts from `e4f8aee5` (P0.185, TASK-0455, v0.439, not merged yet). A local branch
`p15a-preview-fork` of a stopped attempt exists in another worktree; this task does not use it.

## Goal

An agent reads, before the person decides, the outcome of each command of its proposal and the bodies that the proposal adds,
removes and changes, from a fork of the Document that changes nothing in the Document.

## Scope (in)

1. ADR-0185: the whole design of P15 (the fork, when it runs, the surface, the four differences, the bound, the lock, the
   events, the cancellation, the cost, the slices P15a and P15b), and the amendment of ADR-0092 item 2.
2. The fork (`Engine.Core/Preview/DocumentFork.cs`): a replay of a copy of the log in a new session with its own backend, off
   the serial lock, then the commands of the proposal as an accept applies them.
3. The previews of the queue (`Engine.Api.Http/Proposals.Preview.cs`): at the post and at each new version, one worker, one
   preview for each proposal of the queue; the route `GET /proposals/{id}/preview`; the factory of backends of each host.
4. The first difference: the bodies, added, removed and changed, by content.
5. The code `E-API-PREVIEW-FAILED`; the guide for an agent gets a tested example of the preview.
6. The roadmap row P15 splits into P15a and P15b.

## Scope (out)

- P15b: the state of the solver, the measures and the checks of the preview (ADR-0185 items 8 to 10).
- P16 (the accept after a preview) and R25 (the preview in the window). The window does not change (`3DEngine.App/**`).
- A change of `Engine.Contracts/**`, of the log format, or a new event kind.
- A key of the caches of the kernel by the source Document for a fork (ADR-0185, "Bad").

## Acceptance criteria

- [x] A proposal of three commands on the bookcase gives three outcomes and the body differences, and the version, the log,
  the bodies and the events of the first Document do not change.
- [x] A refused command gives its code and stops the rest, with the same outcome as the accept; a known command identifier
  gives `E-API-PROPOSAL-COMMAND-KNOWN`, as the accept.
- [x] A new version of the Document computes the preview again on the new log.
- [x] No event of a fork reaches a subscriber of the host.
- [x] A command of the person applies while a fork waits: the fork is off the serial lock.
- [x] The queue keeps one preview for each proposal, at most 40; a closed proposal keeps its last preview.
- [x] The route gives the preview, HTTP 404 for an unknown proposal and HTTP 501 on a host with no factory.
- [x] A fork that cannot replay the log gives a failed preview with `E-API-PREVIEW-FAILED`.
- [x] The guide for an agent has a preview request that `AgentGuideExamplesTests` sends to a host.
- [x] Each injection makes a named test fail, and each file is restored byte for byte (sha256).
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

The owner answered on 2026-11-22 (option A of Q1 to Q5 of the third codebase review of 2026-11-21):

- Q1 A: P15 is the next product phase, with K15d beside it (K15d is done).
- Q2 A: the fork is a copy of the log under the lock, then a replay in a new session off the lock, with its own backend.
- Q3 A: the preview runs when the proposal arrives, and again at each new version of the Document.
- Q4 A: the checks cover only the pairs with a changed body, under the work limit of `CheckInterference`
  (`E-CHECK-WORK-LIMIT`). This is P15b.
- Q5 A: two results compare by the content of their records, and the measures to the double.

Decisions of this task that the answers do not settle (ADR-0185 records each one):

- The surface is a route, `GET /proposals/{id}/preview`, and not a query: a query runs under the lock of the first session,
  and a proposal lives in the host (ADR-0092 item 1). The route waits for a preview of the version at the request.
- The host gives a factory of backends (`EngineHost`, parameter `forks`); a host with no factory makes no preview and its
  route gives HTTP 501 with the existing code `E-API-NOT-SUPPORTED`.
- One worker computes the previews one after the other, so one fork at a time. A fork that a new version makes old runs to its
  end and serves the callers that asked before its copy; a cancellation at each new version could hold a caller for ever under
  a stream of commits. Only the stop of the host stops a fork.
- The preview of a closed proposal stays (the last before the close) until the proposal leaves the queue, for P16.
- The difference of the bodies compares the bodies of the fork before the first command and after the last command. The base
  of the fork equals the first Document at its version by the replay rule (ADR-0012), so the first Document is not read again.
- A failed replay gets one new code, `E-API-PREVIEW-FAILED`, with the cause in the message, and no outcome.

## Outcome

Status: Done · commit (the merge of the branch p15a-preview-fork-b) · v0.440

## Method

Mechanical: the route, the record of the preview, the factory in each host, the code in the registry and in the schema list,
the rows of the roadmap, the ledger entry, the README row of ADR-0185 and the History of ADR-0092.

Judgement: the surface as a route, the worker with no cancellation at a new version, the preview of a closed proposal, and the
difference from the base of the fork.

Weakest: the worker of `Proposals.Preview.cs` and its waiters. The tests show the order of a post, a commit and a read in one
thread of a test. They do not show a race between a commit and the copy of the log; the reasoning is in the comments of the
file (the mark clears inside the read that copies the log).

The tests came first. With a stub of `Proposals.Preview.cs` that makes no fork (I0 below), nine of the ten new tests failed;
the tenth, the guard of the body record, holds a property that exists, and I12 shows that it fails. The first red run used the
managed backend for the bookcase, which has no `Translate`; the five tests of the bookcase now use the native backend
(`NativeManifoldFact`), and I0 is the red run of the final tests. `ProposalsEndpointTests.No_Route_Accepts_Or_Rejects` lists
the fifth route.

Injections (`inject.js` and `inject2.js` in `%TEMP%\p15a`, dotnet with shell:false, filter `ProposalPreviewTests`,
`AgentGuideExamplesTests` and `ProposalsEndpointTests`, 40 tests), each restored byte for byte (sha256). A constant `false`
in I2, I9 and I11 failed the build (CS0162), so `inject2.js` runs them with a condition that is never true. The first form of
the test of the bound missed I7: no proposal was open at the new version, so no copy of the log came; the test now keeps a
witness open, and I7 fails it.

| Injection | Failing test |
|---|---|
| I0 the stub of stage A: no fork, `Preview` gives null | the nine behaviour tests of `ProposalPreviewTests` and `Each_Example_Of_The_Guide_Is_Accepted_By_A_Host_And_Gives_Its_Shape` |
| I1 the fork does not stop at a refused command | `A_Refused_Command_Gives_Its_Code_And_Stops_The_Rest_As_The_Accept_Does` |
| I2 the fork does not detect a known command identifier | `A_Known_Command_Identifier_Stops_The_Preview_As_It_Stops_The_Accept` |
| I3 the difference of the bodies compares the handle only | `Three_Commands_On_The_Bookcase_...`, `A_Refused_Command_...`, `A_New_Version_...`, `The_Fork_Runs_Off_The_Serial_Lock` |
| I4 a new version asks for no new preview | `A_New_Version_...`, `No_Event_Of_A_Fork_Reaches_A_Subscriber`, `The_Fork_Runs_Off_The_Serial_Lock` (each after the limit of 30 s) |
| I5 the preview applies the commands on the first session | the nine behaviour tests and the guide example |
| I6 the fork runs under the serial lock of the first session | `The_Fork_Runs_Off_The_Serial_Lock` (after the limit of 30 s) |
| I7 a closed proposal gets a new preview at a new version | `The_Queue_Keeps_One_Preview_For_Each_Proposal_And_A_Closed_Proposal_Keeps_Its_Last_Preview` |
| I8 the route is not mapped | `The_Route_Gives_The_Preview_And_Its_Refusals`, the guide example, `No_Route_Accepts_Or_Rejects` |
| I9 the route does not refuse a host with no forks | `The_Route_Gives_The_Preview_And_Its_Refusals` |
| I10 a fault of a fork is not a failed preview | `A_Fork_That_Cannot_Replay_The_Log_Gives_A_Failed_Preview` (after the limit of 30 s) |
| I11 the fork does not check the backend of each entry | `A_Fork_That_Cannot_Replay_The_Log_Gives_A_Failed_Preview` |
| I12 a body record gets a list member (`Engine.Contracts`, restored) | `A_Body_Record_Has_No_List_So_Its_Equality_Compares_Content` |
| I13 a waiter takes a preview of an older version | `A_New_Version_...`, `No_Event_Of_A_Fork_Reaches_A_Subscriber`, `The_Fork_Runs_Off_The_Serial_Lock` |
| I14 the guide has no request of the preview | `Each_Block_Of_The_Guide_Is_A_Marked_Example_And_The_Loop_Has_Each_Route`, the guide example |
| I15 the fork applies the commands as entries of a log | `A_Known_Command_Identifier_Stops_The_Preview_As_It_Stops_The_Accept` |

The full run (local, Windows): `dotnet test 3DEngine.sln` passes 3231 tests (1881 in `Engine.Tests`, 390 in `3DEngine.Tests`,
957 in `3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`). A clean build of the solution and the build of the phone project
give 0 warnings.

## Progress

- 2026-11-22: opened, branch `p15a-preview-fork-b` from `e4f8aee5`. ADR-0185, the fork, the previews of the queue, the
  route, the factory of each host, the code `E-API-PREVIEW-FAILED`, the guide example, ten tests, the injections, the rows
  P15a and P15b, and the ledger entry v0.440.
- 2026-11-22: run 45022828650 of `486d4bfc` passed on each job. Closed at v0.440.
