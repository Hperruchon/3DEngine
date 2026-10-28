---
id: 0092
title: The document file in the desktop and for an agent
status: Done
phase: P8f
opened: 2026-10-28
depends-on: [0091]
governed-by: [0004, 0007, 0013, 0015, 0016, 0019, 0024, 0025, 0027, 0029, 0030]
writes:
  create:
    - tasks/TASK-0092-document-file-in-the-desktop.md
    - docs/adr/0030-document-file-in-the-desktop.md
    - Engine.Api.Http/Endpoints/DocumentEndpoint.cs
    - Engine.Api.Http/DocumentFolder.cs
    - 3DEngine/DesktopDocument.cs
  modify:
    - Engine.Api.Http/EngineSurface.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Persistence/**
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

- [x] Each new test fails before its correction, or an injection shows that it can fail.
- [x] A name that leaves the document folder is refused.
- [x] A file that does not open leaves the window open, with its code.
- [x] A picture of the desktop with its file name, after a save and after an open. The owner accepts it. The
      owner accepted two pictures and the log on 2026-10-28.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The merge
      records the run.

## Outcome

Status: Done · v0.92 · the commit that carries this block.

The desktop opens the file that `--file` names, writes an unsaved document into the folder of unsaved documents,
and stops with exit code 6 when the file does not open. A person saves and opens by name in the panel, and an agent
does the same over HTTP. An open checks the file in a scratch session, so a bad file leaves the window, and a good
file starts the program again on the same port.

## Method

**Mechanical.** 26 new tests, with each theory case counted: 18 in `Engine.Tests` (the routes, the rule of a name,
the default folder, the check) and 8 in `3DEngine.ImageTests` (the panel, the arguments of the restart, the check
on the native backend, and a process test of the desktop program). Nine injections each failed a test. Run
40676618897 passed on the three runners. The visual check drove the desktop over HTTP with scratch folders and
synchronization validation: a save, a refused open of a bad file, and an open that started the program again
and rebuilt the same box; no validation message. The owner accepted the two pictures.

**Judgement.** The open starts the program again, as Q5 chose, and the old process checks the file first with its
own backend, so the person never loses the window. The reader of a file shares the write access, so the check reads
the file that the session holds. The buttons of the file have their own line, because the first layout put Open
outside the panel, where its click went to the scene. A save makes the document folder, because the default folder
of a new account does not exist.

**Weakest.** The visual check used HTTP and not the buttons; a test of the panel presses them. An open applies each
command two times. The folder of unsaved documents grows, because no code removes a file in it. No test runs the
restart of the program; the visual check ran it one time.

## Notes for the implementer

- **The tests of the desktop surface.** With no `--file` the desktop writes into the folder of unsaved documents, so
  each test that starts the desktop surface gives `--Desktop:UnsavedFolder` with a scratch folder.
- **The size.** About 2 to 3 evenings.

## Progress

- 2026-10-28: opened after TASK-0091 (v0.91). The first commit holds ADR-0030, accepted by the owner.
- 2026-10-28: the code (ADR-0030 items 1 to 5). `DocumentFolder` resolves a plain file name in one folder;
  `GET /document`, `POST /document/save` and `POST /document/open` (501 with `E-API-NOT-SUPPORTED` on a host that
  cannot start again); `EngineHost.FileName` is null for an unsaved document; `DocumentFile.Check` opens a file in a
  scratch session with no write and no lock, and the reader of a file shares the write access, so a check reads a
  file that the session holds. The desktop reads `--file`, `Desktop:UnsavedFolder` and `Desktop:DocumentFolder`,
  stops with exit code 6, and `DesktopDocument` checks a file, ends the window loop, and starts the program again
  with its arguments and the new file. The panel has a file line, "File name", Save as and Open; the buttons have
  their own line, because the first layout put Open outside the panel and its click went to the scene. 26 new
  tests: 18 in `Engine.Tests` and 8 in `3DEngine.ImageTests`, among them a process test of the desktop program.
  Nine injections each failed a test; one first passed (a default folder that can start again), because each test
  registered its own folder, so a test now starts a surface with no folder of its own.
- 2026-10-28: a save makes the document folder when it does not exist, because the default folder of a new account
  does not exist before the first save; the save test starts with no folder, and an injection that makes no folder
  failed it. `Engine.Core/Persistence/**` replaces `DocumentFile.cs` in the write set. Correction of the line before:
  it gave nine injections when there were eight; this one is the ninth.
- 2026-10-28: the owner accepted the two pictures and the log on 2026-10-28. Closed after a clean build and a full
  local run of 719 tests (582 in `Engine.Tests`, 89 in `3DEngine.Tests`, 48 in `3DEngine.ImageTests`).
- 2026-10-28: run 40682786957 of the close failed on Windows only: the assertions of the process test of a server
  with a file (TASK-0091) passed, and its cleanup failed, because Windows still held `plate.3de` after the kill.
  The cleanup of the two file process tests now tries for 5 s, then leaves the scratch folder, as the other process
  test of the class does. The task is Active for this change and Done again after it.
- 2026-10-28: Done again after the change of the cleanup.
