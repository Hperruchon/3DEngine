---
id: 0202
title: A document is a part or an assembly, and an assembly holds references to parts at a fixed version
status: Active
phase: P8b1
opened: 2026-11-06
depends-on: [0201]
governed-by: [0002, 0004, 0005, 0006, 0008, 0010, 0012, 0013, 0015, 0016, 0018, 0019, 0020, 0021, 0024, 0028, 0030, 0031, 0033, 0037, 0045, 0060, 0071]
writes:
  create:
    - tasks/TASK-0202-container-model-kind-and-references.md
    - docs/adr/0071-container-model.md
    - Engine.Contracts/Assembly/PartReference.cs
    - Engine.Core/Commands/AddPartReferenceCommand.cs
    - Engine.Core/Commands/AddPartReferenceCommandHandler.cs
    - Engine.Core/Commands/DocumentKinds.cs
    - Engine.Core/Queries/GetPartReferencesQuery.cs
    - Engine.Core/Queries/GetPartReferencesQueryHandler.cs
    - Engine.Tests/Assembly/ContainerModelTests.cs
  modify:
    - Engine.Contracts/Document.cs
    - Engine.Contracts/Handlers/ICommandHandler.cs
    - Engine.Contracts/Schema/FieldSchema.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/StepHistory.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/DocumentSession.cs
    - Engine.Core/Hosting/EngineHosting.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Commands/NoOpCommandHandler.cs
    - Engine.Core/Commands/UndoCommandHandler.cs
    - Engine.Core/Commands/RedoCommandHandler.cs
    - Engine.Core/Persistence/DocumentFile.cs
    - Engine.Core/Persistence/DocumentJournal.cs
    - Engine.Core/Persistence/EntryCodec.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/Endpoints/DocumentEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Api.Http/WebSockets/WireMessage.cs
    - Engine.Api.Http.Server/Program.cs
    - Engine.Cli/Cli.cs
    - Engine.Cli/Usage.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/Http/SchemaEndpointGateTests.cs
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/0015-command-log-persistence.md
    - docs/adr/0030-document-file-in-the-desktop.md
    - docs/adr/0031-undo-and-redo.md
    - docs/adr/0060-unit-kind-in-the-command-schema.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Kernel/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0202 — A document is a part or an assembly, and an assembly holds references to parts at a fixed version

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap row P8b is the container model: one document holds one part, and an assembly holds references at a fixed
version (5 to 8 evenings). On 2026-11-05 the owner ordered P8b in the engine lane, after K6, with the standing order of
2026-11-05. The change of `Engine.Contracts` needs an ADR in the same commit (`CLAUDE.md`, "Stop and ask"); under the
standing order ADR-0071 takes the recommended options. A saved document must still open with the same log, and a test
must prove it. The charter lists "Multi-Document in one runtime" as a non-goal, so this first slice records the
references and does not resolve them.

## Goal

A Document has the kind part or assembly; an assembly records references to part files of the store at a fixed version,
and each part file of an earlier version opens as a part with the same bytes.

## Scope (in)

1. `Document.Kind`, `PartReference`, `Document.References` and `CommandHandlerResult.CreatedReferences` in
   `Engine.Contracts`, with ADR-0071.
2. The rule of the kinds in the bus (`IDocumentKinds`, `E-CMD-KIND-REFUSED`).
3. `AddPartReference` and `GetPartReferences`, the events, undo and redo.
4. The kind in the header of an assembly file, the open of each kind, `engine-api-http --kind`, `engine --kind`,
   `GET /document` and the snapshot.
5. Tests, among them a part file in the form of each earlier version that opens with the same bytes.

## Scope (out)

- The resolution of a reference: a read of the part from the store, its bodies in the assembly, and a fault for absent
  bytes. The owner decides on the non-goal "Multi-Document in one runtime" first.
- A rotation of a reference, a change of its version, a removal other than `Undo`, and the desktop.

## Acceptance criteria

- [x] A part file in the form of each earlier version opens as a part, the open does not change its bytes, and a new part
      file with the same command has the same bytes.
- [x] An assembly refuses each command of a part with `E-CMD-KIND-REFUSED`, and a part refuses `AddPartReference`.
- [x] An assembly file opens again with its references, and only in a session of its kind.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p8b1-container-kind) · v0.TBD

## Method

Mechanical: the path of a reference through the bus, the step, the events and the snapshot, as for an image.
Judgement:  a reference names the bytes of the part in the store, not a path; the header of a part does not change, so
            no file changes; the format version stays 1; the rule of the kinds is an interface of `Engine.Core`, so
            `ICommandHandler` does not change; the engine does not resolve a reference in this slice.
Weakest:    an engine before TASK-0202 refuses an assembly file only because it does not have `AddPartReference`; an
            assembly with no reference opens there as an empty part. No test runs an older engine.

## Progress

- 2026-11-06: opened by the engine lane of the order of 2026-11-05. ADR-0071 with the recommended options, answered by
  the standing order of 2026-11-05: a kind fixed at the make, a reference by the name of the bytes in the store, no
  field kind in the header of a part, and no resolution of a reference. The contract, the bus, the command, the query,
  the file, the hosts, the command line and the tests. Four injections each fail a test: a field kind in the header of
  a part, no rule of the kinds, no check of the kind of a file at the open, and an Undo that keeps a reference.
- 2026-11-06: replayed on `main` of v0.197. Since TASK-0192 each integer parameter declares a unit, so ADR-0071 adds
  the unit kind `count` to ADR-0060 item 2 for `version`, answered by the standing order of 2026-11-05. The gate of
  the vocabulary failed with `count` in the code only, and passes with the list of item 2.
