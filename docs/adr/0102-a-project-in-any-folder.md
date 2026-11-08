---
id: 0102
title: The window opens and saves a project in any folder, through the dialogs of the system
status: Accepted
topic: Persistence, clients
date: 2026-11-08
supersedes: []
superseded-by: []
amends: ['0030', '0083']
amended-by: []
affects:
  - 3DEngine.App/ProjectFiles.cs
  - 3DEngine.App/ISessionHost.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/PhoneFiles.cs
  - 3DEngine.App/Ui/PanelAction.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.Home.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.Vulkan/Window.FileDialog.cs
  - 3DEngine/DesktopDocument.cs
  - 3DEngine/DesktopSession.cs
  - 3DEngine/DesktopSurface.cs
  - 3DEngine/RecentProjects.cs
  - 3DEngine.ImageTests/ProjectPathTests.cs
  - 3DEngine.ImageTests/DesktopDocumentTests.cs
  - 3DEngine.ImageTests/FileDialogStateTests.cs
  - 3DEngine.ImageTests/NumericPadUiTests.cs
  - 3DEngine.ImageTests/StlNameTests.cs
  - 3DEngine.ImageTests/StlOpenTests.cs
enforced-by: 3DEngine.ImageTests/ProjectPathTests.cs
---

# ADR-0102 — The window opens and saves a project in any folder, through the dialogs of the system

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-08 the owner asked to use the desktop for a personal project, the entry hall shelf, with no command line and no
agent (phase P9g, TASK-0316). At v0.298:

- "Open file" on the home screen opened the dialog of the system for an STL file only, and each file of the dialog went to
  the STL import (ADR-0083 D1). No path of the window opened a `.3de` file from a dialog.
- A save and an open by name used one folder, `<documents of the account>/3DEngine` (ADR-0030 items 2 and 3), and the home
  screen gave the five newest files of that folder (ADR-0083 D6). The shelf is in another folder.
- "Save as" and "Export STL" took the name of a field of the panel and wrote into that one folder.
- A start with `--file` (P9f) opened a file in each folder, but it needs a terminal.

The owner answered the four questions of the task on 2026-11-08:

- **Q1 A.** The folder of a save: the save dialog of the system, in each folder.
- **Q2 A.** The recent rows: a list of the last eight paths that this desktop opened or saved, in a file of the local
  application data, not in a Document. A path that no longer exists leaves the list.
- **Q3 A.** A change of project while the document has no file and holds at least one command: a question with "Save
  as...", "Discard" and "Cancel". "Discard" leaves the journal in the folder of unsaved documents (ADR-0030 item 6).
- **Q4 A.** The phone is out of scope.

## Decision

1. **Two actions (amends ADR-0030 item 3 and ADR-0083 D1).** The home screen, the panel and the command line give "Open
   project..." and "Import STL..." as two actions. "Open project..." opens the dialog of the system with the filter
   "3DEngine project" and `3de`, in each folder; the path goes through the check of ADR-0030 item 5 and the next session
   of R14b. "Import STL..." keeps the behaviour of P9c. The panel has no name field: its file controls are "Open
   project...", "Save as...", "Export STL..." and "Import STL...". Each label ends with three dots, because it opens a
   dialog.
2. **The purpose of a dialog.** Each dialog of the system keeps the purpose that the window gives it (import, open of a
   project, save, export), and its result gives that purpose back. A late result of one purpose goes to that purpose only.
3. **Save as (Q1 A, amends ADR-0030 item 3).** "Save as..." opens the save dialog of the system with the filter `3de`, at
   the file of the project, or where the system chooses for a document with no file. The chosen path gets `.3de` when it
   has none. `DocumentSession.SaveAs` moves the journal there (ADR-0015 item 9): for a document with a file, the old file
   goes, as before. A target that exists is refused with `E-IO-SAVE-FAILED`, because the session does not replace a file.
4. **Export STL.** "Export STL..." opens the save dialog of the system with the filter `stl`, at the name of the project
   with `.stl`. The chosen path gets `.stl` when it has none. The line says "Exported", and the file of the project does not
   change.
5. **The question (Q3 A).** Before a change of project (an open, a recent row, a new part or an import) while the document
   has no file and holds at least one command, a window asks "Save as...", "Discard" or "Cancel". "Save as..." opens the
   save dialog, and the change follows a save; a closed dialog or a failed save drops it. "Discard" makes the change, and
   the journal stays in the folder of unsaved documents. "Cancel" drops the change.
6. **The recent projects (Q2 A, amends ADR-0083 D6).** The home screen gives the last eight projects that this desktop
   opened or saved, in each folder, with the name and the folder in the quiet colour; a tap opens the path. The list is the
   file `recent-projects.txt` beside the folder of unsaved documents, or the file of the setting `Desktop:RecentFile`. Each
   session that holds a file of a project adds it, from the window, an agent or the setting `file`. A failed read or write
   of the list stops nothing.
7. **The top bar.** The folder of the file of a project follows its name, in the quiet colour, cut at its start, where at
   least 48 pixels are left of the command line.
8. **The surface of an agent does not change.** `POST /document/save` and `POST /document/open` keep their names in the
   document folder of ADR-0030 item 2, and `DocumentFolder.Resolve` keeps its rules.
9. **The phone (Q4 A).** A host with no files shows "Import STL..." and no project. The register was full (15 of 15
   entries) on 2026-11-08, so this record holds the phone until the owner chooses an entry.
10. **No contract change.** No command, no event and no code is new.

## Consequences

**Good:** a person makes, saves, opens, changes and exports a project in each folder from the window only, and cannot take
the export for the save of the project.

**Bad:** "Save as..." on a document with a file moves that file, as before; a person who wants a copy must copy the file.
A save over a file that exists is refused, although the dialog of the system asked whether to replace it. The pictures of
the dialogs of the system need a person: the tests give their results as samples.

## History

- 2026-11-08 · TASK-0316 · The record, with the answers of the owner.
- 2026-11-09 · TASK-0316 · Shipped at v0.299; the row waits for the run of the owner.
