---
id: 0461
title: The medium and low findings of the codebase review of 2026-11-23
status: Active
phase: P0.187
opened: 2026-11-23
depends-on: [0460]
governed-by: [0004, 0007, 0013, 0016, 0017, 0018, 0019, 0023, 0025, 0027, 0028, 0029, 0030, 0032, 0033, 0034, 0037, 0054, 0057, 0058, 0059, 0060, 0061, 0072, 0073, 0075, 0076, 0077, 0080, 0083, 0089, 0092, 0093, 0098, 0099, 0100, 0101, 0102, 0107, 0108, 0109, 0110, 0111, 0112, 0113, 0114, 0115, 0121, 0124, 0127, 0129, 0130, 0131, 0133, 0134, 0135, 0136, 0138, 0139, 0141, 0143, 0152, 0153, 0155, 0158, 0166, 0167, 0168, 0172, 0173, 0174, 0175, 0177, 0178, 0179, 0180, 0181, 0182, 0184, 0185, 0186, 0187, 0188]
writes:
  create:
    - tasks/TASK-0461-the-findings-of-the-review-of-2026-11-23.md
    - 3DEngine.ImageTests/PipelineStartTests.cs
    - 3DEngine.ImageTests/WaitingWordTests.cs
  modify:
    - .gitignore
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Core/Preview/DocumentFork.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Queries/CheckInterferenceQueryHandler.cs
    - Engine.Kernel/PlanarBoolean.cs
    - Engine.Api.Http/Proposals.Preview.cs
    - Engine.Api.Http/Proposals.cs
    - Engine.Api.Http/Endpoints/ProposalsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - 3DEngine.App/SectionView.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.Vulkan/GroundPipeline.cs
    - 3DEngine.Vulkan/EdgePipeline.cs
    - 3DEngine.Vulkan/3DEngine.Vulkan.csproj
    - eng/write-set-check/TaskFiles.cs
    - Engine.Tests/Features/KeptBooleanCollection.cs
    - Engine.Tests/Features/KeptBooleanTests.cs
    - Engine.Tests/Http/ProposalPreviewTests.cs
    - Engine.Tests/Http/HostGuardTests.cs
    - Engine.Tests/Kernel/PlanarSubtractTests.cs
    - Engine.Tests/Queries/CheckInterferenceQueryTests.cs
    - Engine.Tests/Governance/KeptBooleanCollectionGateTests.cs
    - Engine.Tests/Governance/PathPatternWorkTests.cs
    - Engine.Tests/Governance/TaskGovernanceGateTests.cs
    - Engine.Tests/Governance/MarkerGateTests.cs
    - 3DEngine.ImageTests/SectionViewTests.cs
    - docs/adr/0059-references-by-the-identity-of-a-face.md
    - docs/adr/0108-the-look-of-the-render.md
    - docs/adr/0113-the-layout-at-each-phone-size-and-small-window.md
    - docs/adr/0173-a-lost-runner-gets-one-automatic-re-run.md
    - docs/adr/0184-the-section-view.md
    - docs/adr/0185-the-preview-of-a-proposal-on-a-fork.md
    - docs/adr/0188-the-accept-after-a-preview-and-the-group-in-the-log.md
    - docs/diagnostics.md
    - docs/guides/agent-loop.md
    - docs/templates.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
---

# TASK-0461 — The medium and low findings of the codebase review of 2026-11-23

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-23 (`docs/reviews/2026-11-23-codebase-review.md`, TASK-0460) found one medium and nine low
findings and no critical or high one. By `docs/templates.md`, section 7, they come after the next product phase. R25 (v0.445)
and P16 (v0.446) came first. P16 changed `Proposals.Preview.cs`, `Engine.Core/Preview/**` and the accept after the review, so
this task read each "Where" again at `ce8d01ed`.

On 2026-11-24 the owner ordered "Start P0.187" in the chat. The owner also answered "Q8 A, add it to P0.187" (Q8 A,
2026-11-24): in the desktop window with a long file path, the title runs into the label "AGENT IS WAITING" (the picture
`r25-live.png` of the owner, top of the window). This task records it as the finding V235, the next free identifier of its
kind (V234 is the last of the review).

The branch `p0187-review-findings` starts from `origin/main` at `ce8d01ed` (v0.446).

## Goal

Each finding has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: E239 (each fork and each base of a preview makes each boolean of the log again, and pushes out the kept booleans of
   the first Document).
2. Low: E240, E241, T192, V233, V234, T193, T194, T195, T196.
3. ADR-0185 names the store of the kept booleans and its cost in items 7 and 11 and in "Bad" (E239, T192).
4. V235, a finding recorded by the answer Q8 A of the owner of 2026-11-24: with a long path the title of the top bar runs into
   "AGENT IS WAITING".

## Scope (out)

- The answers Q1 to Q6 of the review: R25 (TASK-0462) and P16 (TASK-0463) recorded them.
- The renderer of the images (`ImageRenderer`), which V234 also names: its start makes more objects, so the correction is not
  the same small form; R-0077 holds it.
- No change of `Engine.Contracts/**`, and no new ADR: each correction follows an ADR that exists, and the History of each
  amended record names the task.

## Acceptance criteria

- [x] Each finding has a test that fails before its correction, or a register entry (R-0077 for the part of V234 that stays).
- [x] Each injection of a correction makes a named test fail.
- [x] The determinism hash of the probe is the hash that v0.446 records.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes. The local
  part passes; continuous integration is not run yet.

## Findings

| Finding | Correction | Test | Before the correction |
|---|---|---|---|
| E239 | A fork reads the kept booleans of its source and keeps its own in a store that leaves with it (`KernelProjection.BeginFork`, `EndFork`, `ForkSource.DocumentId`) | `KeptBooleanTests.A_Fork_Takes_The_Kept_Results_Of_The_First_Document_And_Adds_None_To_Them`, `KeptBooleanTests.The_Forks_Of_Eight_Proposals_And_Sixteen_Commits_Push_Out_No_Kept_Result_Of_The_First_Document` | a rename made 1 boolean and a move 2; the store grew from 1 to 17 |
| E240 | A proposal that leaves gives null (HTTP 404); a stop gives HTTP 503 with `E-API-HOST-STOPPED` | `ProposalPreviewTests.A_Proposal_That_Leaves_The_Queue_While_A_Caller_Waits_Gives_Not_Found`, `ProposalPreviewTests.The_Stop_Of_The_Host_During_A_Fork_Frees_Its_Backend_And_The_Route_Gives_Host_Stopped` | `TaskCanceledException`; HTTP 500 |
| E241 | `PlanarBoolean.Subtract(a, b, CancellationToken)`; the subtract of a void gives `ct` | `PlanarSubtractTests.A_Subtract_With_A_Token_Stops_At_A_Cancel_And_Gives_The_Same_Result_With_No_Cancel`, `CheckInterferenceQueryTests.Each_Boolean_Of_The_Check_Gives_The_Token_Of_The_Query` | no overload (no build); the call had two arguments |
| T192 | Tests of items 2, 3, 7 and 10; item 7 and the comment of `StopPreviews` say where the fork reads the token | `ProposalPreviewTests.Each_Backend_Of_A_Fork_Is_Freed_And_At_Most_One_Lives_At_A_Time`, `..._Stop_Of_The_Host_During_A_Fork_...`, `A_Cancelled_Wait_Leaves_The_Fork_And_A_Later_Wait_Gets_The_Preview`, `A_Burst_Of_Commits_During_A_Fork_Gives_One_More_Fork_At_The_Last_Version`, `A_Body_With_A_Fault_Gives_A_Measure_With_An_Error_And_A_Pair_With_The_State_Refused` | the finding is the absence of tests; the code was correct, so the injections hold them |
| V233 | `SectionView` keeps the running cut of a body that leaves, and the body that comes back waits for it | `SectionViewTests.A_Body_Runs_One_Cut_At_A_Time_And_Keeps_Its_Last_Cut_Until_The_New_One` (two new cases) | injection 13 gives the old form |
| V234 | `GroundPipeline` and `EdgePipeline` make the modules inside the `try` and free the layout of a failed pipeline; R-0077 for `ImageRenderer` | `PipelineStartTests.A_Failed_Start_Of_A_Pipeline_Frees_Each_Object_That_It_Made` | injections 14 and 15 give the old forms; the validation layer names the object |
| T193 | `Exited` fails a host whose output holds "Now listening on"; ADR-0173 item 4 states the limit of the rule of 60 seconds | `HostGuardTests.Exited_Fails_A_Process_That_Writes_Its_Address_After_The_Exit` | no failure: the test saw the exit first |
| T194 | The gate judges the innermost type, which must declare a test; `ForksOpen` is a shared name | `KeptBooleanCollectionGateTests.The_Scan_Finds_Each_Class_That_Names_A_Shared_Member` (rows of a nested class and of a helper) | injection 17 gives the old form |
| T195 | `PathPatternWorkTests` also bounds the expressions by the distinct patterns of the run (`TaskFiles.PatternsAskedOnThisThread`); the work covers the open tasks and the three newest tasks of each status (`TaskGovernanceGateTests.GovernedByProblems`) | `PathPatternWorkTests.The_Gate_Of_Governed_By_Builds_At_Most_One_Expression_For_Each_Pattern` | injection 19 (a key for each call) passed the old bound |
| T196 | `MarkerGateTests` reads each tracked text file outside `docs/`, `tasks/` and `Engine.Tests/Governance/`, except a Markdown document; the row of `docs/templates.md` section 5 says so | `MarkerGateTests.Every_Impermanence_Marker_In_Live_Code_Carries_A_Register_Identifier`, `MarkerGateTests.The_Gate_Reads_Each_Tracked_Text_File_Outside_The_Records` | the old gate did not read the root `.gitignore` or `eng/*.txt` |
| V235 | While a proposal waits, the path of the top bar leaves the room of the word; on a narrow landscape display the panel starts under the word | `WaitingWordTests.The_Word_Of_A_Waiting_Proposal_Lies_On_No_Text_And_No_Panel_With_A_Long_Folder` (4 sizes), `WaitingWordTests.The_Picture_Shows_The_Word_Clear_Of_A_Long_Title` | each size failed: the word lay on the panel of the rows |
| Run 45362050126 | A caller that waits marks the queue behind only when no preview, no fork that runs (`Entry.ComputingVersion`) and no copy that waits is at its version (ADR-0188 item 5, History) | `ProposalPreviewTests.A_Caller_That_Waits_For_A_Fork_At_Its_Version_Does_Not_Mark_The_Queue_Behind` | 5 of 5 runs failed: the queue was behind while the fork at the version of the call ran |

## Progress

- 2026-11-24: the eleven corrections, ADR-0185 items 3, 7 and 11 and "Bad", the History of ADR-0059, ADR-0108, ADR-0113,
  ADR-0173 and ADR-0184, the code `E-API-HOST-STOPPED`, R-0077 and the tests. 26 new tests in `Engine.Tests` (1943) and 6 in
  `3DEngine.ImageTests` (989 here and on Linux, 933 off Linux). The hash of the determinism probe is `068ba4a8…`, as v0.446.
- 2026-11-24: a full local run failed `The_Stop_Of_The_Host_During_A_Fork_Frees_Its_Backend_And_The_Route_Gives_Host_Stopped`
  once, with no message in the summary of the run. The probable cause (not verified): the test opened the gate of the fork
  before the stop started, so the fork could end before the stop. The test now opens
  the gate only after `Proposals.Stopped` (a new member for the test) is true; eight runs of the class and a full run passed.

## Outcome

Status: Active · commit (the merge of the branch p0187-review-findings) · v0.447

## Method

Mechanical: the code in the registry, in `/schema/diagnostics` and in the guide; the rows of the gates; the History lines; the
counts of the tests.

Judgement: the store of a fork, which reads the store of its source with no change of it, and not a key of the step only,
because two Documents can share an identifier of a command and the walk of the steps in effect is for each Document; a new code
for the stop of the host, because no code of the surface means a stop; the room of the word in the top bar, which keeps the
place of ADR-0108 item 6, with the line under the top bar only for a narrow landscape display; the text of ADR-0173 item 4 and
not a count of the lines only, which would fail a host that starts slowly on a loaded runner; the exclusion of the Markdown
documents from the marker gate (a document defines or describes, as `docs/`); a helper type with no test is an offence of the
gate of the kept booleans.

Weakest: the test of the token of the pair check reads the source of the handler, because a test cannot time a cancel inside
one walk; a call through a local variable or a helper method passes it. The gate of the kept booleans does not follow a call, so
a public member of a test class of the collection can still give a count to another class. The test of `Exited` holds the
order with a child process that writes the line about two seconds after the exit of its shell.

The tests came with the code. A script (`inject.js` in the scratchpad of the session, dotnet with shell:false, a filter of one
test) applied 24 injections, each restored byte for byte (sha256), and each made the named test fail:

| # | File | Injection | Failed test |
|---|---|---|---|
| 1 | `KernelProjection.cs` | a fork does not read the store of its source | `A_Fork_Takes_The_Kept_Results_Of_The_First_Document_And_Adds_None_To_Them` |
| 2 | `KernelProjection.cs` | a fork puts its results in the store of the first Document | `A_Fork_Takes_The_Kept_Results_Of_The_First_Document_And_Adds_None_To_Them` |
| 3 | `DocumentFork.cs` | the store of a fork does not leave with it | `A_Fork_Takes_The_Kept_Results_Of_The_First_Document_And_Adds_None_To_Them` |
| 4 | `Proposals.Preview.cs` | a proposal that leaves cancels the wait | `A_Proposal_That_Leaves_The_Queue_While_A_Caller_Waits_Gives_Not_Found` |
| 5 | `ProposalsEndpoint.cs` | the route lets the stop of the host escape | `The_Stop_Of_The_Host_During_A_Fork_Frees_Its_Backend_And_The_Route_Gives_Host_Stopped` |
| 6 | `PlanarBoolean.cs` | the subtract ignores its token | `A_Subtract_With_A_Token_Stops_At_A_Cancel_And_Gives_The_Same_Result_With_No_Cancel` |
| 7 | `CheckInterferenceQueryHandler.cs` | the subtract of a void gives no token | `Each_Boolean_Of_The_Check_Gives_The_Token_Of_The_Query` |
| 8 | `DocumentFork.cs` | the fork does not free its first backend | `Each_Backend_Of_A_Fork_Is_Freed_And_At_Most_One_Lives_At_A_Time` |
| 9 | `DocumentFork.cs` | the base takes one more backend that nothing frees | `Each_Backend_Of_A_Fork_Is_Freed_And_At_Most_One_Lives_At_A_Time` |
| 10 | `Proposals.Preview.cs` | each job runs two forks | `A_Burst_Of_Commits_During_A_Fork_Gives_One_More_Fork_At_The_Last_Version` |
| 11 | `Proposals.Preview.cs` | the cancel of one wait stops the fork | `A_Cancelled_Wait_Leaves_The_Fork_And_A_Later_Wait_Gets_The_Preview` |
| 12 | `PreviewDifferences.cs` | a refused pair has the state `limit` | `A_Body_With_A_Fault_Gives_A_Measure_With_An_Error_And_A_Pair_With_The_State_Refused` |
| 13 | `SectionView.cs` | a body that leaves forgets its running cut | `A_Body_Runs_One_Cut_At_A_Time_And_Keeps_Its_Last_Cut_Until_The_New_One` |
| 14 | `GroundPipeline.cs` | a failed pipeline keeps its layout | `A_Failed_Start_Of_A_Pipeline_Frees_Each_Object_That_It_Made` |
| 15 | `EdgePipeline.cs` | a failed start keeps its fragment module | `A_Failed_Start_Of_A_Pipeline_Frees_Each_Object_That_It_Made` |
| 16 | `HostGuardTests.cs` | `Exited` reads no line after the exit | `Exited_Fails_A_Process_That_Writes_Its_Address_After_The_Exit` |
| 17 | `KeptBooleanCollectionGateTests.cs` | the gate judges the outermost type | `The_Scan_Finds_Each_Class_That_Names_A_Shared_Member` (3 rows) |
| 18 | `KeptBooleanCollectionGateTests.cs` | the gate takes a helper with no test | `The_Scan_Finds_Each_Class_That_Names_A_Shared_Member` |
| 19 | `TaskFiles.cs` | the cache has a key for each call | `The_Gate_Of_Governed_By_Builds_At_Most_One_Expression_For_Each_Pattern` |
| 20 | `.gitignore` | a `TODO` with no identifier (a violation) | `Every_Impermanence_Marker_In_Live_Code_Carries_A_Register_Identifier` |
| 21 | `eng/test-floor.txt` | a `TODO` with no identifier (a violation) | `Every_Impermanence_Marker_In_Live_Code_Carries_A_Register_Identifier` |
| 22 | `DesktopUi.Fixed.cs` | the path takes the room of the word | `The_Word_Of_A_Waiting_Proposal_Lies_On_No_Text_And_No_Panel_With_A_Long_Folder` (2 sizes) |
| 23 | `DesktopUi.Panel.cs` | the panel of a landscape display does not start under the word | `The_Word_Of_A_Waiting_Proposal_Lies_On_No_Text_And_No_Panel_With_A_Long_Folder` |
| 24 | `SchemaDiagnosticsEndpoint.cs` | `/schema/diagnostics` does not give the new code | `SchemaEndpointGateTests.Every_DiagnosticCodes_Constant_Appears_In_Schema_Diagnostics` |
| 25 | `Proposals.Preview.cs` | the old condition: only the last preview covers the version of the call | `A_Caller_That_Waits_For_A_Fork_At_Its_Version_Does_Not_Mark_The_Queue_Behind` (5 of 5 runs) |

Injection 2 first ran with the test of the eight proposals, which passed: their commands make no new boolean, so a fork that
writes to the store of the first Document finds each result in it first. The first test holds it with the new subtract of its
third preview.
- 2026-11-24: run 45358850044 of `52fd0888` passed on each job. Closed at v0.447.
- 2026-11-24: run 45359984114 of the close commit `ddc26e7a` failed the step "Test" on each operating system:
  `The_Gate_Of_Governed_By_Builds_At_Most_One_Expression_For_Each_Pattern` said "The gate of governed-by gave no pattern to
  PathPattern". The close set the status of the last open task to Done, so the gate judged no task and gave no pattern. The
  local runs had TASK-0461 Active, so they did not show it. The gate now takes a filter of the tasks
  (`TaskGovernanceGateTests.GovernedByProblems`), and the test gives it the open tasks and the three newest tasks of each
  status. With no open task the test passes; injection 19 again (a key for each call) made it fail with 143751 expressions
  for 445 distinct patterns, and the file came back byte for byte (sha256). The task is Active again until a green run.
- 2026-11-24: run 45362050126 of `f7668d9c` failed on macOS only: `ProposalPreviewWindowTests.The_Host_Gives_The_Window_The_Preview_With_The_Shape_Of_Each_New_Solid`
  (line 165, `Assert.True(first.Current)`). `Proposals.Preview` marked the queue behind when the version of the call was later
  than the last preview only. The first preview of a new proposal has no last preview, so a caller that waited for it marked
  the queue behind while its own fork ran at that version; after the fork, `Kept` gave `Current` false until the worker read
  the log again, and the slow runner read `Kept` in that window. The race exists on `main` since P16 (v0.446). The queue now
  is behind only when no preview, no fork that runs (`Entry.ComputingVersion`) and no copy that waits is at the version of the
  call, so a commit during the group of an accept still gets its copy (ADR-0188 item 5). The new test holds the fork and reads
  `Proposals.Behind` (a new member for the test); injection 25 made it fail 5 times of 5. 1 new test in `Engine.Tests` (1944).
