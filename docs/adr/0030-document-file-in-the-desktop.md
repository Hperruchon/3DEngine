---
id: 0030
title: The document file in the desktop and for an agent
status: Accepted
topic: Persistence, deployment
date: 2026-10-28
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - 3DEngine/DesktopSurface.cs
  - 3DEngine/DesktopEngineHost.cs
  - 3DEngine/Ui/**
  - Engine.Api.Http/EngineSurface.cs
  - Engine.Api.Http/Endpoints/DocumentEndpoint.cs
enforced-by: Engine.Tests/Http/DocumentEndpointTests.cs (TASK-0092 creates it)
---

# ADR-0030 — The document file in the desktop and for an agent

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0015 gives the document file to the engine and to `engine-api-http` (P8a, TASK-0091). Phase P8f gives it to the
desktop and to an agent. On 2026-10-28 the owner answered the questions of the codebase review of that date:

- Q4 A: a document with no name writes into the folder of unsaved documents from its first command, and a save
  moves the file.
- Q5 A: the desktop opens the file that its command line names, and an open in the window starts the program
  again. A replacement of the session at run time costs about 2 more evenings.
- Q6 A: an agent saves and opens over HTTP on the loopback address, limited to one folder, because ADR-0002 requires
  that an agent can do each operation of a person.

An open that starts the program again ends the old process. If the new process cannot open the file, the person has
no window. The surface library serves two hosts (ADR-0019), and only the desktop can start itself again.

## Decision

1. **The desktop and its file.** `3DEngine --file <path>` opens the file, or makes it when it does not exist, before
   the window and before the surface serves a request, as `engine-api-http` does (ADR-0015 item 10). With no
   `--file`, the desktop writes into the folder of unsaved documents from its first command (Q4). The setting
   `Desktop:UnsavedFolder` changes that folder, and a test gives a scratch folder. A file that does not open stops
   the desktop before the window with exit code 6.
2. **The document folder.** A save and an open by name use one folder: `<documents of the account>/3DEngine`, which
   the setting `Desktop:DocumentFolder` or `Engine:DocumentFolder` changes. A name is a plain file name with no
   folder part, no `..`, and the extension `.3de`, which the host adds when it is absent. A path that leaves the folder
   is refused with `E-API-BAD-REQUEST`.
3. **The panel.** The panel shows the name of the file, or "unsaved". It has a name field with the buttons Save as and
   Open. Save as calls `DocumentSession.SaveAs` with the path in the document folder. Open checks the file (item 5),
   then starts the program again with `--file` and the same port, and closes the window.
4. **HTTP.** The surface gains three routes on the loopback address:
   - `GET /document` gives `{ "name": <file name or null>, "lengthUnit": "mm", "version": <document version> }`.
   - `POST /document/save { "name": "plate" }` saves the document as `plate.3de` in the document folder. A failure
     gives `E-IO-SAVE-FAILED`.
   - `POST /document/open { "name": "plate" }` exists only on a host that can start itself again. The desktop checks
     the file, replies `202 Accepted`, and starts again with the same port, so an agent connects again to the same
     address. `engine-api-http` refuses it with HTTP 501, because a script starts that server with `--file`.
5. **The check before an open.** Before it starts again, the old process opens the file in a scratch session with its
   own backend and drops the session. A file that does not open gives its code (`E-IO-LOAD-FAILED` or
   `E-IO-BACKEND-MISMATCH`) and the old process continues, so the person keeps the window.
6. **The unsaved folder is not a list.** A file in the folder of unsaved documents stays after a crash and after a
   normal stop. No code lists or removes those files in this phase.

## Consequences

**Good:** A person keeps the work of each command in the desktop, and an agent saves and opens the same files over
HTTP (ADR-0002). An open never leaves the person with no window, because the old process checks the file first.

**Bad:**
- An open starts a new process, so the window closes and opens again, and an agent loses its WebSocket and
  connects again.
- The check of item 5 applies each command of the file two times, one time in each process.
- The folder of unsaved documents grows, because no code removes a file in it.
- The surface has a route that only one host serves.

**Next:** TASK-0092 (P8f) applies this record. A list of unsaved documents for a recovery is a later phase.

## History

- 2026-10-28 · TASK-0092 · The record is written with the answers Q4 to Q6 of the owner of 2026-10-28, and the owner
  accepted it on 2026-10-28.
