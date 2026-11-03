---
id: 0171
title: The desktop starts a sketch on a face, and draws a sketch on a datum of the Document
status: Active
phase: K0i
opened: 2026-11-03
depends-on: [0167, 0169, 0170]
governed-by: [0004, 0007, 0009, 0013, 0016, 0018, 0019, 0022, 0023, 0025, 0027, 0028, 0029, 0030, 0032, 0033, 0034, 0035, 0037, 0039, 0041, 0053]
writes:
  create:
    - tasks/TASK-0171-desktop-sketch-on-a-face.md
    - docs/adr/0053-desktop-sketch-on-a-face.md
    - Engine.Core/Queries/GetFaceAtQuery.cs
    - Engine.Core/Queries/GetFaceAtQueryHandler.cs
    - Engine.Core/Sketch/FaceFinder.cs
    - Engine.Tests/Sketch/FaceFinderTests.cs
    - 3DEngine.ImageTests/SketchOnFaceTests.cs
  modify:
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Queries/GetSketchQuery.cs
    - Engine.Core/Queries/GetSketchQueryHandler.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - 3DEngine.Core/Picking/Picker.cs
    - 3DEngine.Tests/PickingTests.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/SessionSketches.cs
    - 3DEngine/SketchActions.cs
    - 3DEngine/SketchOverlay.cs
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine/Ui/PanelAction.cs
    - docs/adr/0029-selection-and-hide-gestures.md
    - docs/adr/0034-sketch-tools.md
    - docs/adr/0051-references-and-datum-on-a-face.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Vulkan/**
---

# TASK-0171 — The desktop starts a sketch on a face, and draws a sketch on a datum of the Document

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

K0g and K0h gave an agent a datum on a face and the references of a sketch. ADR-0051 item 11 kept the desktop out, so a
person cannot start a sketch on a face, and the desktop does not draw a sketch on a datum of the Document. The owner
chose K0i as the product phase after P0.72 and P0.73 (Q1 A of the review of v0.169), gave the order "go ahead" on
2026-11-03, and answered four questions on the same day, each with the recommended answer. ADR-0053 holds them.

## Goal

A person taps a face of a body and gets a sketch on a datum of that face, and the desktop draws each sketch where its
datum is, with the items of a reference dim.

## Scope (in)

1. The query `GetFaceAt` and `E-DATUM-FACE-NOT-FOUND` (ADR-0053 items 1 to 3).
2. The point and the normal of a pick (item 5).
3. The button `Face`, the armed tap, and the reuse or the creation of the datum (item 6).
4. The frame of a sketch in `GetSketch`, the overlay, the tool, the mark, the camera and the projection (item 7).
5. The dim items of a reference (item 8).
6. A register entry for the failure of the run on `main` of v0.172 (R-0050).

## Scope (out)

- A tool that adds a reference by a tap, an image on a datum of the Document, and a sketch on a curved face.

## Acceptance criteria

- [ ] Each item has a test that fails when its change is removed.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the query and its tests, the pick, the frame of a sketch, and the desktop tests with a session and no GPU.
Judgement:  the tolerance of the match of a face, and the order of the faces.
Weakest:    no one opened the window; the armed tap of the render loop has no test.

## Progress

- 2026-11-03: opened with the four answers of the owner; ADR-0053.
