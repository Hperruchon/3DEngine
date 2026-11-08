---
id: 0314
title: The desktop opens and saves a project in any folder, and a person completes the project path with no terminal
status: Active
phase: P9g
opened: 2026-11-08
depends-on: [0313]
governed-by: [0007, 0017, 0019, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0061, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0093, 0097, 0098, 0099, 0100]
writes:
  create:
    - tasks/TASK-0314-open-and-save-a-project-in-any-folder.md
    - docs/adr/0100-a-project-in-any-folder.md
    - 3DEngine.App/ProjectFiles.cs
    - 3DEngine/RecentProjects.cs
    - 3DEngine.ImageTests/ProjectPathTests.cs
  modify:
    - 3DEngine/DesktopDocument.cs
    - 3DEngine/DesktopSession.cs
    - 3DEngine/DesktopSurface.cs
    - 3DEngine/Documentation/Running.md
    - 3DEngine.App/ISessionHost.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/PhoneFiles.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.Home.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - 3DEngine.Vulkan/Window.FileDialog.cs
    - 3DEngine.ImageTests/DesktopDocumentTests.cs
    - 3DEngine.ImageTests/FileDialogStateTests.cs
    - 3DEngine.ImageTests/HomeImageTests.cs
    - 3DEngine.ImageTests/NumericPadUiTests.cs
    - 3DEngine.ImageTests/StlNameTests.cs
    - 3DEngine.ImageTests/StlOpenTests.cs
    - docs/adr/0030-document-file-in-the-desktop.md
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0314 — The desktop opens and saves a project in any folder, and a person completes the project path with no terminal

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

The advisor session wrote the draft as TASK-0313 with ADR-0099. The slice R16p took both numbers on 2026-11-08 (branch
`r16p-body-rows-left`), so this task is TASK-0314 and its record is ADR-0100. The write set was corrected on 2026-11-08 from
a search of the code at v0.297; the governed-by list is set by the gate at the start of the work.

## Context

The owner, 2026-11-08: the owner wants to use the desktop for a personal project, with no command line and no agent.
The project is the entry hall shelf, `C:\Users\Henri\Documents\3DProjects\EntryHallShelf\EntryHallShelf.3de`.

The facts at v0.297 (origin/main 18297b8), read on disk on 2026-11-08:

1. "Open file" on the home screen opens the dialog of the system with the filter "STL file" and `stl` only
   (`3DEngine.App/NativeThreeDEngine.cs:410`). The folder of the shelf seems empty, because it holds no `.stl` file.
2. Each file that the dialog gives goes to the STL import (`TakeOpenedFile`, `NativeThreeDEngine.cs:149`). No path of
   the window opens a `.3de` file from the dialog.
3. ADR-0030 item 2: a save and an open by name use one folder, `<documents of the account>/3DEngine`. A name has no
   folder part. The recent rows of the home screen are the five newest `.3de` files of that folder
   (`DesktopDocument.Recent`, `RecentCount = 5`), so the shelf is not in them.
4. "Save as" (`SaveDocument`, `NativeThreeDEngine.cs:603`) and "Export STL" (`ExportStl`, line 621) take the name of the
   panel field and write into that one folder. The person cannot choose a folder.
5. ADR-0015 item 9: a document with no name writes from its first command into the folder of unsaved documents, and
   `DocumentSession.SaveAs` moves that file. A named project writes each command into its file. The window shows no path
   back to an unsaved document.
6. P9f (v0.274, TASK-0290): a start with `--file` opens that file and shows no home screen. That path needs a terminal.
   (The draft said that Codex opened the shelf that way; the ledger does not say so, so this task does not state it.)
7. Since R16p (TASK-0313), on a landscape display the rows of a selected body take the place of the panel, so the file
   controls of the panel show only while no body is selected.

## Goal

From the window only, a person makes a project, saves it in a folder that they choose, opens it again after a restart,
changes it, and exports an STL file. The person cannot confuse the export with the save of the project.

## Answers of the owner (2026-11-08)

Q1 A, Q2 A, Q3 A, Q4 A.

- **Q1 A.** The folder of a save: the save dialog of the system, in each folder.
- **Q2 A.** The recent rows: a list of the last eight paths that this desktop opened or saved, in a file of the local
  application data of the desktop, not in a Document. A path that no longer exists leaves the list.
- **Q3 A.** A change of project while the document has no name and has at least one command: a question with "Save as…",
  "Discard" and "Cancel". "Discard" leaves the journal in the folder of unsaved documents (ADR-0030 item 6).
- **Q4 A.** The phone is out of scope; a register entry holds it.

## Scope (in)

1. ADR-0100, which amends ADR-0030 items 2 and 3 and ADR-0083 D1:
   - The home screen and the panel have two separate actions: "Open project…" and "Import STL…".
   - "Open project…" opens the dialog of the system with the filter "3DEngine project" and `3de`, in each folder. It
     uses the check of ADR-0030 item 5 and the start again of R14b.
   - "Import STL…" keeps the behaviour of P9c: a new part with the mesh body.
   - "Save as…" on a document with no name opens the save dialog of the system with the filter `3de`. The person
     chooses the folder and the name. `DocumentSession.SaveAs` moves the journal there (ADR-0015 item 9).
   - "Export STL…" opens the save dialog of the system with the filter `stl`. Its label, its message and its default
     name say "export". It never changes the file of the project.
   - The window shows when the project is in its file: the name of the file and its folder in the top bar.
   - The routes `POST /document/save` and `POST /document/open` keep their names in the document folder. An agent
     keeps the same surface.
   - The command line also holds "Open project", "Import STL", "Save as" and "Export STL", because the rows of R16p hide
     the panel while a body is selected.
2. The recent rows of the home screen hold the projects that this desktop opened or saved, in each folder (Q2 A).
3. A change of project while the document has no name follows Q3 A.
4. Tests of the library with the dialog result as a sample, for each case of the acceptance criteria.
5. The visual check of the path, with one picture for each step (see "Visual check").
6. `3DEngine/Documentation/Running.md`: the project path from the window, with no `--file`.
7. The phone (Q4 A): ADR-0100 item 9 holds it, because the register is full (15 of 15 entries).

## Scope (out)

- The phone (Q4 A). The document picker of Android gives a content address, not a path.
- A change of `Engine.Contracts` or `Engine.Core`. The task changes the desktop host and the drawing library only.
- The script `entry-shelf.js` of the owner. The task does not run it and does not change it.
- A delete of a file in the folder of unsaved documents (ADR-0030 item 6).

## Acceptance criteria

The path (each step from the window, with no terminal and no agent):

- [ ] 1. "New part" makes a project with no name. The top bar says that it has no file.
- [ ] 2. "Save as…" opens the save dialog. The person chooses a folder and a name. The file `<name>.3de` exists there,
      and the top bar shows its name.
- [ ] 3. The person closes the window and starts the desktop again with no argument.
- [ ] 4. The home screen shows the project in its recent rows. "Open project…" in its folder opens it too.
- [ ] 5. The person changes a part, for example a move or a new box, and closes the window. The window gives no
      question, because the journal holds the change.
- [ ] 6. After a start again and an open, the change is there.
- [ ] 7. "Export STL…" writes `<name>.stl` in the folder that the person chooses. The `.3de` file has the same bytes
      before and after the export. The message says "Exported", not "Saved".

The other cases:

- [ ] 8. Cancel in each dialog changes nothing and shows no message.
- [ ] 9. A file that is not a project, for example a text file with the extension `.3de`, gives its code
      (`E-IO-LOAD-FAILED` or `E-IO-BACKEND-MISMATCH`). The window and the current document stay.
- [ ] 10. An STL file renamed to `.3de` gives the same refusal. A `.3de` file in "Import STL…" is not offered by the
      filter.
- [ ] 11. A change of project while the document has no name follows Q3 A, with a test for each choice.
- [ ] 12. The shelf: "Open project…" opens `C:\Users\Henri\Documents\3DProjects\EntryHallShelf\EntryHallShelf.3de`
      and shows its bodies. The automated tests use a copy of that file in a scratch folder, never the file of the owner.

The gates:

- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Each new diagnostic code is in `docs/diagnostics.md`.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.
- [ ] The owner sees the pictures of the visual check before the merge.
- [ ] The row closes only after the run of the owner (as P9c): the owner does steps 1 to 12 on the desktop.

## Visual check

The implementer runs the desktop on a copy of the shelf and takes a picture of the window at each step:

1. the home screen with "Open project…" and "Import STL…";
2. the open dialog with the filter `3de`, in the folder of the copy;
3. the shelf open, with its name and folder in the top bar;
4. the save dialog of "Save as…";
5. the home screen after a start again, with the project in the recent rows;
6. the shelf after a change and a start again;
7. the save dialog of "Export STL…", and the message after the export;
8. the refusal of an invalid file;
9. the question of Q3 A.

The pictures go into the scratchpad of the session, and the owner sees them before the merge. A test that cannot
drive a dialog of the system uses the dialog result as a sample, and the progress says which steps had a real dialog.

## Notes for the implementer

- SDL 3 has `SDL_ShowSaveFileDialog`. `FileDialogState` holds one dialog at a time; a save dialog and an open dialog
  share that rule.
- Each dialog result says which action opened it, so that a late result of "Import STL…" never opens a project.
- ADR-0030 item 5 opens the file in a scratch session before the start again. Keep that check for a path in each folder.
- A named project writes each command into its file at once. Do not add a second save path. Show the state instead.
- `DocumentFolder.Resolve` refuses a path that leaves the folder. The new path of the window must not weaken that rule
  for the HTTP routes.
- The owner, 2026-11-08: no copy of the shelf goes into the repository. The session uses a copy of the shelf in a scratch
  folder for its own checks when it needs one, and the progress says what ran on it. The automated tests of the repository
  use a project that a test makes.

## Outcome

Status: Active

## Method

Mechanical: (to fill)
Judgement:  (to fill)
Weakest:    (to fill)

## Progress

- 2026-11-08: the owner asked for this task. The advisor session wrote the draft from the code at v0.297. This session
  checked each fact of the Context on disk, corrected fact 3 (five recent rows, not eight) and fact 6 (no source for
  the open by Codex), added fact 7, corrected the write set, renumbered the task and the ADR, and recorded the answers
  Q1 A to Q4 A.
- 2026-11-08: the slice. The write set at the start of the work: `3DEngine.App/ProjectFiles.cs`, `DesktopSession.cs`,
  `DesktopSurface.cs`, `FileDialogStateTests.cs`, `NumericPadUiTests.cs` and `StlNameTests.cs` were added;
  `Program.cs`, `FixedControlsTests.cs`, `PhoneFilesTests.cs`, `docs/diagnostics.md` and `docs/register.md` were removed,
  because the work did not change them. No new diagnostic code. The register holds 15 of 15 open entries, so the phone
  (Q4 A) is in ADR-0100 item 9 and not in a register entry; the owner decides which entry it may take.
- 2026-11-08: eleven injections each make a test fail. The visual check ran offscreen at 1440 x 900 on a scratch copy of the
  shelf (its hash stayed the same): the home screen, the shelf open, a part after Save as, the home screen after a start
  again, the shelf after a change and a start again, the line after an export, the refusal of a text file and the
  question. No step had a real dialog of the system: the tests and the pictures give the result of each dialog as a sample,
  because a session does not drive the mouse or a dialog of the system while the owner may use the computer.
