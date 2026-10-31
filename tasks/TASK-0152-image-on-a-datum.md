---
id: 0152
title: The Document holds an image on a datum, and a command calibrates it to real units
status: Done
phase: V2a
opened: 2026-10-31
depends-on: [0148]
governed-by: [0004, 0005, 0006, 0008, 0010, 0012, 0013, 0015, 0016, 0019, 0020, 0021, 0028, 0031, 0033, 0037, 0044, 0045]
writes:
  create:
    - tasks/TASK-0152-image-on-a-datum.md
    - docs/adr/0045-image-on-a-datum.md
    - Engine.Contracts/Images/ImageRecord.cs
    - Engine.Core/Commands/PlaceImageCommand.cs
    - Engine.Core/Commands/PlaceImageCommandHandler.cs
    - Engine.Core/Commands/CalibrateImageCommand.cs
    - Engine.Core/Commands/CalibrateImageCommandHandler.cs
    - Engine.Core/Commands/ImageResults.cs
    - Engine.Tests/Images/ImageCommandTests.cs
    - Engine.Tests/Http/HttpImageScenarioTests.cs
  modify:
    - Engine.Contracts/Document.cs
    - Engine.Contracts/Handlers/ICommandHandler.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/StepHistory.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Hosting/ParameterBinder.cs
    - Engine.Core/Assets/AssetStore.cs
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Api.Http/WebSockets/WireMessage.cs
    - Engine.Tests/Assets/AssetStoreTests.cs
    - Engine.Tests/Hosting/ParameterBinderTests.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/0031-undo-and-redo.md
    - docs/adr/0033-sketch-model.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - tasks/TASK-0149-engine-forms-of-the-seventeenth-review.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - Engine.Geometry.Manifold/**
---

# TASK-0152 — The Document holds an image on a datum, and a command calibrates it to real units

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner told the session on 2026-10-31: "do the review, then start V2". The review of 2026-10-31 (c) (TASK-0148)
asked three questions about V2, and the standing answer of the owner takes Q1 A, Q2 A and Q3 A. V2 changes the public
shape of `Engine.Contracts`, so ADR-0045 comes in the same commit (`CLAUDE.md`, section "Stop and ask"). V2 has two
slices: V2a, the record and the commands in the engine, and V2b, the image in the desktop.

## Goal

An agent places an image of the store on a datum and calibrates it, over HTTP, and the Document, the log, the events,
undo and redo, the snapshot and the document file hold it.

## Scope (in)

1. `ImageRecord`, `Document.Images` and the two lists of `CommandHandlerResult` (ADR-0045 items 1, 3 and 7).
2. `PlaceImage`@1 and `CalibrateImage`@1, with their codes (items 4 to 6).
3. The events, the step history and the snapshot (items 8 to 10).
4. E113 of the review: the pattern of a name of an asset ends with `\z`, because `PlaceImage` checks the name.

## Scope (out)

- The desktop (V2b): the decode of a PNG file, the put of its bytes, the quad of each image and the gesture of the
  calibration.
- A move, a rotation and a removal of an image.

## Acceptance criteria

- [x] Each command, each refusal, each event, undo and redo, the document file and the snapshot have a test.
- [x] An agent places and calibrates an image over HTTP with the name that `PUT /assets` gives.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch v2a-image-on-a-datum) · v0.151

## Method

Mechanical: each command, each refusal, the events of each commit, undo and redo, the document file, the snapshot over
            HTTP, and one injection for each rule.
Judgement:  the record in the event and no query; the origin at the lower-left corner; the client gives the size;
            a whole double up to 2^53 is an integer.
Weakest:    no client places an image yet, so the frame of ADR-0045 item 2 has no picture until V2b.

## Progress

- 2026-10-31: ADR-0045, the record, the two commands, the events, the steps and the snapshot. The HTTP test found that
  an integer field refused each JSON number, because the surface gives a number as a double; the binder takes a whole
  double up to 2^53 now. 16 new test cases in `Engine.Tests`. 9 injections caught.
- 2026-10-31: closed at v0.151.
