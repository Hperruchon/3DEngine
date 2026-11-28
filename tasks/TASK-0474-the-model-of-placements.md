---
id: 0474
title: The model of placements, so that a move changes a stored placement and not the shape of a body
status: Active
phase: P11.0
opened: 2026-11-27
depends-on: [0468]
governed-by: [0004, 0005, 0006, 0007, 0008, 0010, 0012, 0013, 0015, 0016, 0018, 0019, 0020, 0021, 0025, 0028, 0029, 0031, 0033, 0035, 0037, 0039, 0045, 0048, 0049, 0051, 0052, 0054, 0055, 0058, 0059, 0060, 0072, 0074, 0075, 0076, 0077, 0083, 0089, 0099, 0109, 0112, 0114, 0121, 0131, 0143, 0153, 0155, 0158, 0166, 0167, 0168, 0169, 0179, 0180, 0181, 0182, 0184, 0185, 0187, 0188, 0189, 0190, 0191]
writes:
  create:
    - tasks/TASK-0474-the-model-of-placements.md
    - docs/proposals/2026-11-27-the-model-of-placements-of-p11-0.md
    - docs/adr/0191-the-placement-of-a-body.md
    - Engine.Contracts/BodyPlacement.cs
    - Engine.Core/Placements.cs
    - Engine.Core/Commands/PlaceBodyCommand.cs
    - Engine.Core/Commands/PlaceBodyCommandHandler.cs
    - Engine.Tests/Commands/PlaceBodyTests.cs
  modify:
    - Engine.Contracts/BodyRecord.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Features/FeatureList.cs
    - Engine.Core/Sketch/FeatureGraph.cs
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Features/Regeneration.cs
    - Engine.Core/Commands/TranslateCommandHandler.cs
    - Engine.Core/Commands/RotateCommandHandler.cs
    - Engine.Core/Commands/SubtractCommandHandler.cs
    - Engine.Core/Commands/BooleanStep.cs
    - Engine.Core/Queries/GetTessellationQueryHandler.cs
    - Engine.Core/Queries/GetBoundingBoxQueryHandler.cs
    - Engine.Core/Queries/GetFacesQueryHandler.cs
    - Engine.Core/Queries/CheckInterferenceQueryHandler.cs
    - Engine.Core/Queries/CutMass.cs
    - Engine.Core/Queries/GetBodyInfoQuery.cs
    - Engine.Core/Queries/GetBodyInfoQueryHandler.cs
    - Engine.Core/Queries/GetBodiesQueryHandler.cs
    - Engine.Core/Export/StlExport.cs
    - Engine.Core/Preview/PreviewDifferences.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/Determinism/CrossPlatformProbe.cs
    - Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Api.Http/WebSockets/WireMessage.cs
    - 3DEngine.App/SessionBodies.cs
    - 3DEngine.App/SectionView.cs
    - 3DEngine.App/ProposalPreview.cs
    - Engine.Tests/Assembly/BookcaseCaseTests.cs
    - Engine.Tests/Http/EventPayloadSchemaTests.cs
    - Engine.Tests/Http/HttpBodyNameScenarioTests.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - 3DEngine.ImageTests/DesktopFormsTests.cs
    - 3DEngine.ImageTests/ProposalPreviewWindowTests.cs
    - docs/adr/0037-rebuild-of-an-extrude.md
    - docs/adr/0112-a-command-turns-a-body.md
    - docs/adr/0126-assemblies-with-joints-and-geometric-checks.md
    - docs/adr/0157-the-foundations-and-the-order-of-the-assembly-track.md
    - docs/adr/README.md
    - docs/guides/agent-loop.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid: []
---

# TASK-0474 — The model of placements, so that a move changes a stored placement and not the shape of a body

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P11 of the roadmap: P11.0 is the ADR of the joints, and it decides the model of a move from the measure of K10 (ADR-0126
items 2 and 3, ADR-0157 items 2 to 5, R-0070). On 2026-11-25 the owner answered Q1 A: P11.0 decides a model of placements. Each
body keeps its shape in its own frame, and a joint or a move changes only a stored placement, so the exact numbers do not grow.
K10a measured a numerator of 2576 digits after 150 turns.

The standing order of 2026-11-26 puts P11.0 after P0.189. The model changes the public shape of `Engine.Contracts`, and
`CLAUDE.md` ("Stop and ask") asks the owner before such a change. The direction is answered; its details are not. So the
first commit of this task is a proposal with the questions, and the forbid holds `Engine.Contracts/**` until the answers.

## Goal

The Document stores a placement for each body, a command sets it, and each reader of the geometry applies it, so that a move
keeps the identity and the exact shape of a body, with an ADR in the same commit as the change of `Engine.Contracts`.

## Scope (in)

1. The proposal `docs/proposals/2026-11-27-the-model-of-placements-of-p11-0.md`: the facts, a recommended model and the
   questions Q1 to Q7 for the owner.
2. After the answers: the ADR, the field of `BodyRecord`, the command, the change of each reader, and a test of each reader
   with a body that is not at the identity. The write set grows then.

## Scope (out)

- The connectors, the kinds of joint, the limits and the refusal of a loop: P11a and after.
- Each change of `Engine.Contracts` before the answers of the owner. Until 2026-11-28 the forbid was `Engine.Contracts/**`;
  the owner answered on 2026-11-27.
- A tool of the window that moves a body, and a model matrix of a mesh in the window (ADR-0191, "Alternatives considered").

## Acceptance criteria

- [x] The proposal gives each fact with its file, and each question with a recommended option.
- [x] The owner answers Q1 to Q7, and the first commit after the answers records them.
- [x] The ADR and the change of `Engine.Contracts` are in one commit, with a test of each reader.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Notes for the implementer

- The branch `p110-placements` starts at `ea2f4dac` (v0.456). Do not merge it before the answers.
- The case of K10a (`docs/proposals/2026-11-21-the-bookcase-case-of-k10.md`) gives the numbers to measure again (Q7).

## Outcome

Status: Active · commit (the merge of the branch p110-placements) · v0.458

## Method

Mechanical: the facts of section 2 of the proposal, each read in its file.
Judgement:  the recommended model; a proposal and not a decision under the standing order, because the change of the public
            shape needs the owner.
Weakest:    The list of the readers of ADR-0191 item 7 comes from a search of each read of a geometry key, of OfRecord, of
            KernelProjection and of GetTessellation; a reader that the search did not find has no test, and no gate finds it.
            The window was not run by hand: computer use on the window was denied on 2026-11-27, so DesktopFormsTests holds
            its mesh.

## Progress

- 2026-11-27: opened under the standing order of 2026-11-26; the proposal with the questions Q1 to Q7.
- 2026-11-27: the owner answered "All A" for Q1 to Q7: a new command `PlaceBody` with an absolute placement, and the body
  keeps its handle; a quaternion and an offset, seven doubles; the frame and the placement of the first body for a boolean; a
  placement also for a mesh body; `Rotate` and `Translate` keep their meaning; the ADR, the field, the command and the test of
  each reader in one commit, then the joints; and a new measure of the case of K10a with `PlaceBody`. This commit records them.
- 2026-11-28: under the standing order of 2026-11-28 ("same as every previous week"), the branch moved onto `97eb4287` (v0.457).
  ADR-0191 with the field `Placement` of `BodyRecord`, the command `PlaceBody`, the placement in each reader of the geometry,
  and `PlaceBodyTests` (25 tests): each test of a reader builds one scene with `Rotate` and `Translate` and again with one
  `PlaceBody`, and the answers agree. Nine injections, each of which removes the placement from one reader, each make a named
  test fail. The measure of K10a again (Q7 A): 150 placements give a numerator of 19 digits (2576 with `Rotate`).
