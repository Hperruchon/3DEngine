---
id: 0163
title: A command changes the distance of a live extrude, and the extrude and its chain rebuild
status: Done
phase: K0f
opened: 2026-11-01
depends-on: [0159]
governed-by: [0004, 0007, 0012, 0013, 0016, 0018, 0019, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0035, 0037, 0039, 0049]
writes:
  create:
    - tasks/TASK-0163-edit-of-an-extrude-distance.md
    - docs/adr/0049-edit-of-an-extrude-distance.md
    - Engine.Core/Commands/SetExtrudeDistanceCommand.cs
    - Engine.Core/Commands/SetExtrudeDistanceCommandHandler.cs
    - Engine.Tests/Sketch/ExtrudeDistanceTests.cs
  modify:
    - Engine.Core/Sketch/SketchRebuild.cs
    - Engine.Core/Queries/GetBodyInfoQuery.cs
    - Engine.Core/Queries/GetBodyInfoQueryHandler.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - 3DEngine/BodyActions.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine/Ui/PanelAction.cs
    - 3DEngine.ImageTests/NumericPadUiTests.cs
    - docs/adr/0028-body-name-and-visibility.md
    - docs/adr/0037-rebuild-of-an-extrude.md
    - docs/adr/0039-chain-of-a-consumed-extrude.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Cli/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0163 — A command changes the distance of a live extrude, and the extrude and its chain rebuild

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-01 the owner answered A to Q1 and Q2 of the codebase review of 2026-11-01: the next product phase is K0f, and
its edit is one command that records the extrude and its new distance. P0.68 (v0.161) came before it. ADR-0049 holds the
decision.

## Goal

A person and an agent change the distance of an extrude after the fact, and the extrude and each move and cut of it
follow, with undo, redo, replay and the document file as they are.

## Scope (in)

1. `SetExtrudeDistance`, its refusals and its codes (ADR-0049 items 1, 2 and 11).
2. The distance in effect in each rebuild, and the rebuild of the extrude and of its chain (items 3 to 6).
3. The field `distance` of `GetBodyInfo` (item 7).
4. The button "Set distance" of a selected extrude in the desktop, with its pad (item 8).

## Scope (out)

- The edit of the extrude of a moved or cut body in the desktop.
- The edit of the other numbers of a feature, for example the offsets of a `Translate`.
- The findings of the codebase review of 2026-11-01 (P0.69 to P0.71).

## Acceptance criteria

- [x] A live extrude takes the new distance under its handle, with one `body.changed`, and the end of a chain follows.
- [x] An undo, a redo, a later edit of the sketch and an open read the distance in effect.
- [x] Each refusal gives its code, and a broken profile gives the fault `W-FEATURE-REBUILD-FAILED`.
- [x] The button shows only for an extrude, its pad starts with the distance in effect, and an apply gives the command.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch k0f-extrude-distance) · v0.162

## Method

Mechanical: a live extrude, a chain of a translate and a cut, an undo, a redo, a later edit of the sketch, a broken
            profile, the query, an open, the button and its pad, and one injection for each rule (6). A visual check
            of the drawing after a command over the loopback surface.
Judgement:  one path of the rebuild for the two edits; new codes of the token FEATURE, because the codes of a sketch
            edit name a sketch command; the distance in the query, so that a client does not read the log.
Weakest:    no live window took a tap on the button; the test of the panel holds it with no GPU.

## Progress

- 2026-11-01: ADR-0049, the command, the distance in effect and the shared path of the rebuild in `SketchRebuild`, the
  field of the query and the button of the desktop. 10 new test cases. 6 injections, one for each rule; each one
  failed a test. The label of the button is "Set distance", because the window of the images has a field "Distance".
- 2026-11-01: closed at v0.162.
