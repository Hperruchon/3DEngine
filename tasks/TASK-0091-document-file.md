---
id: 0091
title: The document file in the engine and the server
status: Active
phase: P8a
opened: 2026-10-28
depends-on: [0088]
governed-by: [0001, 0004, 0005, 0006, 0007, 0008, 0010, 0012, 0014, 0015, 0016, 0018, 0019, 0020, 0021, 0024, 0025, 0028, 0029]
writes:
  create:
    - tasks/TASK-0091-document-file.md
    - Engine.Contracts/LogEntry.cs
    - Engine.Contracts/Geometry/BackendIdentity.cs
  modify:
    - Engine.Contracts/Document.cs
    - Engine.Contracts/Geometry/IGeometryBackend.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/DocumentSession.cs
    - Engine.Core/Replay.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Hosting/EngineHosting.cs
    - Engine.Core/Geometry/**
    - Engine.Core/Commands/**
    - Engine.Core/Persistence/**
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/WebSockets/WireMessage.cs
    - Engine.Api.Http.Server/**
    - Engine.Tests/Cli/**
    - Engine.Tests/Commands/**
    - Engine.Tests/Geometry/**
    - Engine.Tests/Http/**
    - Engine.Tests/Queries/**
    - Engine.Tests/Persistence/**
    - Engine.Tests/*.cs
    - Engine.Tests/ReplayDeterminism/ManifoldReplayRoundTripTests.cs
    - Engine.Tests/ReplayDeterminism/ReplayVersionTests.cs
    - Engine.Tests/ReplayDeterminism/ReplayDeterminismGateTests.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - 3DEngine/Documentation/Running.md
    - docs/adr/0015-command-log-persistence.md
    - docs/adr/0010-subscription-reset-snapshot-format.md
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/glossary.md
    - docs/INDEX.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
    - tasks/TASK-0090-host-and-desktop-gaps-of-the-tenth-review.md
  forbid:
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - Engine.Cli/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0091 — The document file in the engine and the server

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase P8a, the fourth phase of the path to "CAD on one part" (rule 4 of the order of the roadmap). The
owner decided KD3 and KD4 on 2026-10-27 and answered the questions Q1 to Q8 of the codebase review of 2026-10-28
on 2026-10-28, with option A for each. The owner accepted the contract of ADR-0015 on 2026-10-28, because it
changes the public shape of `Engine.Contracts`. P8f gives the file to the desktop and to an agent.

## Goal

Each applied command of a host with a file is in that file before its result, and a host that opens the file
rebuilds the same Document with the backend that each entry records.

## Scope (in)

1. ADR-0015 is accepted. It amends ADR-0010 §2 and ADR-0012 §1, whose texts change in the same commit.
2. `Engine.Contracts`: `BackendIdentity`, `LogEntry`, `Document.Log` as a list of `LogEntry`,
   `Document.LengthUnit`, and `IGeometryBackend.Identity`. Each backend gives its identity.
3. The bus writes the identity of its backend into each entry, and `Replay.ReplayLog` refuses an entry of another
   backend.
4. `Engine.Core/Persistence/`: the encoder of each handler, the line of an entry, the journal with its flush to
   disk, the open, and `DocumentSession.SaveAs`.
5. `engine-api-http --file`, and exit code 6 for a file that cannot open.
6. The snapshot gives `lengthUnit`.
7. The seven codes of the namespace `IO` in `docs/diagnostics.md`.
8. Tests: the round trip of each handler, the bits of the edge numbers, the write before the result, a failed
   write, a torn last line, a bad line, another backend, another version of the backend, the save, and the server
   with a file.

## Scope (out)

- The open and the save in the desktop, and the HTTP save and open (P8f).
- A list of unsaved documents for a recovery.
- Undo and redo (P8c).

## Acceptance criteria

- [ ] Each new test fails before its correction, or an injection shows that it can fail.
- [ ] A file that a host wrote opens in a second host and gives the same Document: the same version, log and
      bodies, with names and visibilities.
- [ ] A failed write rejects its command, and the Document does not change.
- [ ] The seven new codes are in `docs/diagnostics.md`.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The judge.** A commit that changes `Engine.Contracts/**` must also change a file under `docs/adr/`.
- **The size.** About 3 to 4 evenings.

## Progress

- 2026-10-28: opened after TASK-0088 (v0.90). The first commit holds ADR-0015, accepted by the owner, and the text
  changes of ADR-0010 and ADR-0012 that it amends.
