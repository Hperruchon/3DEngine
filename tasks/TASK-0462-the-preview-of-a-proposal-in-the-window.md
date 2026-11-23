---
id: 0462
title: The window shows the preview of a proposal, the new shapes in dashed amber lines with a switch and a panel of the differences
status: Active
phase: R25
opened: 2026-11-23
depends-on: [0460]
governed-by: [0004, 0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0043, 0061, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0109, 0110, 0111, 0113, 0114, 0116, 0117, 0119, 0120, 0123, 0124, 0125, 0127, 0129, 0130, 0131, 0137, 0138, 0140, 0163, 0166, 0167, 0168, 0170, 0171, 0174, 0176, 0177, 0184, 0185, 0186, 0187]
writes:
  create:
    - tasks/TASK-0462-the-preview-of-a-proposal-in-the-window.md
    - docs/adr/0187-the-preview-of-a-proposal-in-the-window.md
    - Engine.Core/Preview/PreviewShape.cs
    - 3DEngine.App/ProposalPreview.cs
    - 3DEngine.App/Ui/DesktopUi.Preview.cs
    - 3DEngine.ImageTests/ProposalPreviewWindowTests.cs
  modify:
    - Engine.Core/Preview/DocumentFork.cs
    - Engine.Api.Http/Proposals.cs
    - Engine.Api.Http/Proposals.Preview.cs
    - 3DEngine/DesktopSession.cs
    - 3DEngine/HostedSession.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/SceneRenderer.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - Engine.Tests/Http/ProposalPreviewTests.cs
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/0185-the-preview-of-a-proposal-on-a-fork.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - 3DEngine.Vulkan/**
---

# TASK-0462 — The window shows the preview of a proposal, the new shapes in dashed amber lines with a switch and a panel of the differences

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R25 of the roadmap: the preview of a proposal in the window (gap G12 of the render, ADR-0157 item 8). P15a and P15b
(ADR-0185) give the preview on a fork of the Document over HTTP; the window still shows no simulated result (ADR-0092 item 5).
The codebase review of 2026-11-23 (TASK-0460) asked the owner in Q1 if R25 comes before P16. On 2026-11-23 the owner answered
"all A" to Q1 to Q6 in the chat and ordered "Start R25". This task is the next task after the review, so its commit records the
answers (`docs/templates.md`, section 7): Q1 A in the order of the roadmap, Q2 A to Q4 A and Q6 A in the row P16, and Q5 A and
Q6 A in the History of ADR-0185. It does not implement Q2 to Q6.

The branch `r25-preview-window` starts from `origin/main` at `70fb0264` (v0.444).

## Goal

The person sees the shape that a proposal makes, on the present shape, with its measures, the state of the solver and the
checks that change, before Accept.

## Scope (in)

1. The fork reads the triangles of each body with a new solid for a host that draws (`KeepShapes`), under a bound of 100,000
   triangles for one preview; the queue keeps them for an open proposal only; the answer of the route does not change.
2. A read of the last preview with its shapes for the frame, with the lock of the queue only (`Proposals.Kept`,
   `IProposalDesk.Preview`).
3. The projection of the shapes into the render-side scene, off the thread of the frame, and their draw in dashed amber lines.
4. The switch "Before, After, Both" as a state of the client.
5. The panel at the lower right: the switch, the table "measure, before, after", the state of the solver after the commands,
   the checks that change, and the notes of a stop, a failed preview and a shape above the bound.
6. The record ADR-0187 and the amendments of ADR-0092 item 5 and ADR-0185 items 2 and 5; the answers Q1 to Q6 of the owner.

## Scope (out)

- P16 (the accept after a preview) and the answers Q2 to Q6, which this task records only.
- The phone: it has no queue of proposals (ADR-0092 item 8). A portrait display and a small window show no panel.
- A fill of the new shape, the centre of the mass in the table, a section of a new shape, and the shapes of a proposal that the
  window does not show.
- A change of `Engine.Contracts/**`, of the log, of a query, of an event kind, or of a shader (`3DEngine.Vulkan/**`).

## Acceptance criteria

- [x] With `KeepShapes` the queue keeps the shape of each body with a new solid, and none of a body that keeps its solid; the
  shape equals the tessellation of the body after the accept; a closed proposal drops its shapes.
- [x] With no `KeepShapes` the fork reads no shape, and the answer of the route has the same members in each case.
- [x] The shapes of one preview stay under the bound, and a body above it counts in `ShapesLeftOut`.
- [x] A kept preview is not current while a later version waits for its fork.
- [x] The desktop host gives the window the preview with the measures, the state of the solver, the pair that changes and the
  shape; the lines of the panel name them.
- [x] The switch sets the mode of the scene and gives no action; a new proposal starts in "Both"; "updating" shows while the
  preview is not current; a failed preview says so.
- [x] A phone size, a portrait tablet and a small window show no panel and draw "Both".
- [x] The projection puts the meshes up when they are ready and keeps the old meshes until then.
- [x] The pictures: "Before" has no amber, "Both" and "After" have dashed amber lines, and "After" does not draw the present
  bracket. The validation layer gives no message.
- [x] Each injection makes a named test fail, and each file is restored byte for byte (sha256).
- [x] A clean build, the phone build, `dotnet test`, the write-set check and `eng/repo-gates` pass. Continuous integration is
  not run yet.

## Decisions

ADR-0187 records each decision. The central one: the fork reads the triangles of the new solids while its backend lives, for a
host that asks for them, and the queue keeps them beside the preview, not in the answer of the route. The window replays
nothing and reads no log. The alternatives (a fork in the window, a fork that keeps its backend, moved triangles, a member of
the route) are in the record.

## Progress

- 2026-11-23: the shapes of the fork, the read of the window, the switch, the draw, the panel, the tests, ADR-0187 and the
  answers of the owner to the review of 2026-11-23.

## Outcome

Status: Active · commit (the merge of the branch r25-preview-window) · v0.445

## Method

Mechanical: the read of the queue, the panel lines, the rows of the roadmap, the ledger entry, the History of ADR-0092 and
ADR-0185, and the index of the records.

Judgement: the shapes from the fork for a host that draws, the bound of 100,000 triangles, the shapes of an open proposal only,
the bodies that "After" does not draw, the place of the panel at the lower right left of the ring, and no panel below 1024 by
600.

Weakest: the place of the panel. It ends left of the ring only when 300 pixels of room stay; else it covers a part of the ring.
`The_Switch_Sets_The_Mode_Of_The_Scene_And_Sends_Nothing` tests three landscape sizes with no ring; the picture at 1280 by 720
shows a ring of a body beside the panel. No test holds each place of a ring.

The tests came after the code. So a script (`r25-inject.js` in the scratchpad of the session, dotnet with shell:false, filter
`ProposalPreviewTests` or `ProposalPreviewWindowTests`) applied sixteen injections, each restored byte for byte (sha256), and
each made the named test fail:

| # | File | Injection | Failed test |
|---|---|---|---|
| 1 | `PreviewShape.cs` | no bound of the triangles | `The_Shapes_Of_One_Preview_Stay_Under_The_Bound_Of_The_Triangles` |
| 2 | `PreviewShape.cs` | a body that keeps its solid gets a shape | `With_KeepShapes_The_Queue_Keeps_The_Shape_Of_Each_New_Solid_Until_The_Proposal_Closes` |
| 3 | `Proposals.cs` | a closed proposal keeps its shapes | `With_KeepShapes_The_Queue_Keeps_The_Shape_Of_Each_New_Solid_Until_The_Proposal_Closes` |
| 4 | `Proposals.Preview.cs` | a kept preview is always current | `A_Kept_Preview_Is_Not_Current_While_A_Later_Version_Waits_For_Its_Fork` |
| 5 | `Proposals.Preview.cs` | the fork reads the shapes with no `KeepShapes` | `With_No_KeepShapes_The_Fork_Reads_No_Shape_And_The_Route_Gives_None` |
| 6 | `DesktopSession.cs` | the desktop does not set `KeepShapes` | `The_Host_Gives_The_Window_The_Preview_With_The_Shape_Of_Each_New_Solid` |
| 7 | `SceneRenderer.cs` | no draw of the shapes | `The_Pictures_Show_The_New_Shape_In_Dashed_Amber_Lines_In_Each_Mode` |
| 8 | `SceneRenderer.cs` | "After" draws the replaced bodies | `The_Pictures_Show_The_New_Shape_In_Dashed_Amber_Lines_In_Each_Mode` |
| 9 | `SceneRenderer.cs` | a solid line, dash 0 | `The_Pictures_Show_The_New_Shape_In_Dashed_Amber_Lines_In_Each_Mode` |
| 10 | `ProposalPreview.cs` | the old meshes go before the new ones are ready | `The_Shapes_Go_Up_When_They_Are_Ready_And_The_Old_Shapes_Stay_Until_Then` |
| 11 | `ProposalPreview.cs` | a body that the commands hide stays in "After" | `After_Replaces_The_Removed_Bodies_And_The_Bodies_With_A_New_Solid` |
| 12 | `DesktopUi.Preview.cs` | a tap on the switch sets no mode | `The_Switch_Sets_The_Mode_Of_The_Scene_And_Sends_Nothing` |
| 13 | `DesktopUi.Preview.cs` | a new proposal keeps the mode | `The_Switch_Sets_The_Mode_Of_The_Scene_And_Sends_Nothing` |
| 14 | `DesktopUi.Preview.cs` | a small window shows the panel | `A_Phone_And_A_Small_Window_Show_No_Panel_And_Draw_Both` |
| 15 | `DesktopUi.Preview.cs` | the panel does not end left of the ring | `The_Pictures_Show_The_New_Shape_In_Dashed_Amber_Lines_In_Each_Mode` |
| 16 | `DesktopUi.Preview.cs` | the scene keeps the mode of the switch with no panel | `The_Switch_Sets_The_Mode_Of_The_Scene_And_Sends_Nothing` |

The first run of the script missed four injections: three did not build (a constant condition gave a warning, which the build
treats as an error), and a solid line still passed the first test of the dashes, which counted gaps in each row. The test of the
dashes now samples the points along each feature edge of the new shape, and the switch test now changes to a small window with
the mode "After". The four injections in a form that builds then made their tests fail.
