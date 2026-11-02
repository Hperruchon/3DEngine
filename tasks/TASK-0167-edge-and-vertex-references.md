---
id: 0167
title: The edges and the vertices of a feature as references in a sketch
status: Active
phase: K0h
opened: 2026-11-03
depends-on: [0166]
governed-by: [0004, 0008, 0012, 0013, 0016, 0018, 0019, 0021, 0028, 0031, 0033, 0035, 0036, 0037, 0039, 0040, 0049, 0050, 0051, 0052]
writes:
  create:
    - tasks/TASK-0167-edge-and-vertex-references.md
    - docs/adr/0052-edge-and-vertex-references-in-a-sketch.md
    - Engine.Core/Commands/AddSketchReferenceCommand.cs
    - Engine.Core/Commands/AddSketchReferenceCommandHandler.cs
    - Engine.Tests/Sketch/SketchReferenceTests.cs
  modify:
    - Engine.Contracts/Sketch/SketchRecord.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Commands/MoveSketchPointCommandHandler.cs
    - Engine.Core/Commands/DeleteSketchItemCommandHandler.cs
    - Engine.Core/Commands/SetExtrudeDistanceCommandHandler.cs
    - Engine.Core/Queries/GetSketchQuery.cs
    - Engine.Core/Queries/GetSketchQueryHandler.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Sketch/SketchModel.cs
    - Engine.Core/Sketch/SketchProfile.cs
    - Engine.Core/Sketch/SketchRebuild.cs
    - Engine.Core/Sketch/SketchResults.cs
    - Engine.Core/Sketch/SketchSolver.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
    - Engine.Tests/Sketch/DatumTests.cs
    - docs/adr/0033-sketch-model.md
    - docs/adr/0035-sketch-solver.md
    - docs/adr/0036-extrude-of-a-sketch-profile.md
    - docs/adr/0051-references-and-datum-on-a-face.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
---

# TASK-0167 — The edges and the vertices of a feature as references in a sketch

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-02 the owner gave the order "start K0h", and answered four questions on the same day, each with the
recommended answer. ADR-0052 holds them. K0g (TASK-0166) reserved the names of the edges and the vertices in
ADR-0051 item 1.

## Goal

An agent adds to a sketch the projection of an edge or a vertex of an extrude or a box, constrains to it, and the
sketch follows an edit of the feature.

## Scope (in)

1. `AddSketchReference`, `SketchReference` and `SketchRecord.References` (ADR-0052 items 1 to 4 and 9).
2. A point of a reference has no variable; the move and the delete (item 5).
3. A line of a reference is not an edge of the profile (item 6).
4. The refusals and the check of a cycle (item 7).
5. The rebuild of each reference after an edit, with a fault (item 8), and the log of the determinism hash (item 10).

## Scope (out)

- A curved edge (K5), the desktop (K0i), and a reference that a person converts into a line of the profile.

## Acceptance criteria

- [ ] The vertices and the edges of an extrude and of a box project onto the plane of the sketch.
- [ ] A reference point holds in the solver, no command moves it, and only the whole reference is deleted.
- [ ] A sketch with a reference edge extrudes, and an edit of the base moves the references and the solid that
      follows them.
- [ ] A reference that does not resolve keeps its points with `W-SKETCH-REFERENCE-FAILED`, and the edit applies.
- [ ] A cycle and each other refusal give their codes, and an undo, a redo and an open give the same state.
- [ ] An injection of each part fails its test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners with equal determinism hashes, on the branch and on `main`.

## Outcome

Status: Active

## Method

Mechanical: the points of each element, the projection, the lists of the record, and the registrations.
Judgement:  the walk of the rebuild takes the datums and the references in one order of the log, and a reference
            whose points move solves its sketch again at once, so a later reference sees the solved record.
Weakest:    the check of a cycle reads the current records only; a cycle that a later command could make, for
            example a datum on a feature that a sketch with a reference depends on, is not possible today
            because a datum and a sketch cannot change their datum or their feature.

## Progress

- 2026-11-03: opened from the answers of the owner; ADR-0052 Accepted.
- 2026-11-03: the record, the command, the points of each element, the projection, the hold of the solver, the
  profile, the cycle and the rebuild; 10 new test cases, the hash log gains two references, and 15 injections caught.
