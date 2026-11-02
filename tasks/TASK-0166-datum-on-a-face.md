---
id: 0166
title: A datum on a face of an extrude or a box, and a sketch on it
status: Done
phase: K0g
opened: 2026-11-02
depends-on: [0165]
governed-by: [0004, 0005, 0006, 0008, 0010, 0012, 0013, 0015, 0016, 0018, 0019, 0020, 0021, 0028, 0031, 0033, 0035, 0036, 0037, 0039, 0040, 0045, 0049, 0051]
writes:
  create:
    - tasks/TASK-0166-datum-on-a-face.md
    - docs/adr/0051-references-and-datum-on-a-face.md
    - Engine.Contracts/Sketch/DatumRecord.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Commands/CreateDatumCommand.cs
    - Engine.Core/Commands/CreateDatumCommandHandler.cs
    - Engine.Core/Queries/GetDatumQuery.cs
    - Engine.Core/Queries/GetDatumQueryHandler.cs
    - Engine.Tests/Sketch/DatumTests.cs
    - Engine.Tests/Http/HttpDatumScenarioTests.cs
  modify:
    - Engine.Contracts/Document.cs
    - Engine.Contracts/Handlers/ICommandHandler.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/StepHistory.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Commands/CreateSketchCommandHandler.cs
    - Engine.Core/Commands/ExtrudeCommandHandler.cs
    - Engine.Core/Commands/SetExtrudeDistanceCommandHandler.cs
    - Engine.Core/Sketch/ExtrudeProfile.cs
    - Engine.Core/Sketch/SketchRebuild.cs
    - Engine.Core/Sketch/SketchResults.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Api.Http/WebSockets/WireMessage.cs
    - Engine.Tests/Http/EventPayloadSchemaTests.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/0031-undo-and-redo.md
    - docs/adr/0033-sketch-model.md
    - docs/adr/0036-extrude-of-a-sketch-profile.md
    - docs/adr/0039-chain-of-a-consumed-extrude.md
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

# TASK-0166 — A datum on a face of an extrude or a box, and a sketch on it

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-02 the owner gave the order "start K0". The roadmap row K0 holds the deferred-query references, and no
document defined its rest. The owner answered eight questions on the same day, and ADR-0051 holds them. The rest of
K0 is three slices: K0g gives the faces and a datum on a face, K0h gives the edges and the vertices, and K0i gives the
desktop.

## Goal

An agent makes a datum on a face of an extrude or a box, makes a sketch on it, and extrudes the sketch. An edit of the
base moves the datum and the solid on it.

## Scope (in)

1. The reference and the resolution at its place in the log (ADR-0051 items 1 to 3 and 8).
2. `CreateDatum`, `DatumRecord`, `Document.Datums`, the three datum events, the snapshot and `GetDatum` (items 4, 7
   and 9).
3. A sketch on a datum, and an extrude from its frame (item 5).
4. The rebuild of each datum after an edit, with a fault for a reference that does not resolve (item 6).
5. The log of the determinism hash gains a sketch on the side of a slanted line (item 10).

## Scope (out)

- The edges and the vertices (K0h), and the desktop (K0i).
- An image on a datum of the Document, a delete or a move of a datum, and a datum of typed numbers (item 11).

## Acceptance criteria

- [x] The caps and the sides of an extrude and the six faces of a box give right-handed frames that face out.
- [x] A Translate before the datum moves it, and a Translate after it does not.
- [x] A sketch on a datum extrudes from its face, and an edit of the base moves the datum and the solid on it.
- [x] A reference that does not resolve keeps its last plane with `W-DATUM-REFERENCE-FAILED`, and the edit applies.
- [x] Each refusal gives its code, an undo and a redo remove and give back a datum, and an open gives the same state.
- [x] Each datum event agrees with `/schema/events`, and an agent does the chain over HTTP.
- [x] An injection of each part fails its test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners with equal determinism hashes, on the branch and on `main`.

## Outcome

Status: Done · commit (the merge of the branch k0g-datum-on-a-face) · v0.168

## Method

Mechanical: the frames of item 3, the walk of the chain, the lists of the bus, and the registrations of a new entity.
Judgement:  the order of the two ends of a side from the loop of the profile; a datum that moves makes each extrude of a
            sketch on it a target of the edit, so the rebuild of ADR-0039 needs no second walk.
Weakest:    the bits of a solid on a side with general axes depend on `manifold_transform` in native code; only the
            comparison of the hashes on the three runners proves them.

## Progress

- 2026-11-02: opened from the answers of the owner; ADR-0051 Accepted.
- 2026-11-02: the contract, the bus, the resolver, CreateDatum, GetDatum, the rebuild of each datum and the surface;
  18 new test cases (17 in DatumTests and an HTTP scenario), the payload test and the hash log gain the datums, and
  13 injections caught. ADR-0051 also amends ADR-0012 §6 and ADR-0031, as ADR-0045 did for the images.
- 2026-11-02: the branch run 41539101412 gave equal determinism hashes on the three runners; closed at v0.168.
