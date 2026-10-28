---
id: 0092
title: The document file in the desktop and for an agent
status: Active
phase: P8f
opened: 2026-10-28
depends-on: [0091]
governed-by: [0004, 0007, 0013, 0015, 0016, 0019, 0025, 0027, 0029, 0030]
writes:
  create:
    - tasks/TASK-0092-document-file-in-the-desktop.md
    - docs/adr/0030-document-file-in-the-desktop.md
    - Engine.Api.Http/Endpoints/DocumentEndpoint.cs
    - Engine.Api.Http/DocumentFolder.cs
  modify:
    - Engine.Api.Http/EngineSurface.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Persistence/DocumentFile.cs
    - 3DEngine/DesktopSurface.cs
    - 3DEngine/DesktopEngineHost.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/Ui/**
    - 3DEngine/Documentation/Running.md
    - Engine.Tests/Http/**
    - Engine.Tests/Persistence/**
    - 3DEngine.ImageTests/**
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/glossary.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
    - tasks/TASK-0090-host-and-desktop-gaps-of-the-tenth-review.md
  forbid:
    - Engine.Contracts/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0092 — The document file in the desktop and for an agent

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase P8f, after P8a (TASK-0091, v0.91), in the path to "CAD on one part". ADR-0015 gives the document file
to the engine and to `engine-api-http`. The owner answered Q4 to Q6 of the codebase review of 2026-10-28 on
2026-10-28 and accepted ADR-0030 on 2026-10-28. This phase changes no shape of `Engine.Contracts`.

## Goal

A person and an agent save and open the document of the desktop, and an open never leaves the person with no
window.

## Scope (in)

1. ADR-0030 is accepted.
2. `3DEngine --file`, the folder of unsaved documents with no `--file`, and exit code 6 for a file that does not
   open.
3. The document folder and the rule of a name (ADR-0030 item 2).
4. The routes `GET /document`, `POST /document/save` and `POST /document/open` (item 4).
5. The check of a file before an open, in a scratch session (item 5), and the start of the program again.
6. The panel: the name of the file, a name field, Save as and Open (item 3).
7. Tests: the routes, the rule of a name, the check before an open, the panel, and the desktop with a file.

## Scope (out)

- A list of unsaved documents for a recovery.
- An open over HTTP in `engine-api-http` (ADR-0030 item 4).

## Acceptance criteria

- [ ] Each new test fails before its correction, or an injection shows that it can fail.
- [ ] A name that leaves the document folder is refused.
- [ ] A file that does not open leaves the window open, with its code.
- [ ] A picture of the desktop with its file name, after a save and after an open. The owner accepts it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The tests of the desktop surface.** With no `--file` the desktop writes into the folder of unsaved documents, so
  each test that starts the desktop surface gives `--Desktop:UnsavedFolder` with a scratch folder.
- **The size.** About 2 to 3 evenings.

## Progress

- 2026-10-28: opened after TASK-0091 (v0.91). The first commit holds ADR-0030, accepted by the owner.
