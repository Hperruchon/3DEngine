---
id: 0328
title: "Open project..." shows an STL file too, and an STL file goes to the import
status: Active
phase: P0.137
opened: 2026-11-09
depends-on: [0316]
governed-by: [0007, 0017, 0019, 0023, 0025, 0029, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0085, 0086, 0092, 0102, 0104, 0105, 0106]
writes:
  create:
    - tasks/TASK-0328-open-project-shows-an-stl-file-too.md
    - docs/adr/0106-open-project-shows-an-stl-file-too.md
  modify:
    - 3DEngine.App/ProjectFiles.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.Vulkan/Window.FileDialog.cs
    - 3DEngine.ImageTests/ProjectPathTests.cs
    - 3DEngine.ImageTests/FileDialogStateTests.cs
    - eng/test-floor.txt
    - docs/adr/0102-a-project-in-any-folder.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - tasks/TASK-0314-a-path-line-from-the-document-to-the-selection.md
    - tasks/TASK-0315-a-focus-on-one-body.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0328 — "Open project..." shows an STL file too, and an STL file goes to the import

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

After P9g the owner opened "Open project..." to find an STL file and saw nothing: the dialog had the filter of a project
only (ADR-0102 item 1). On 2026-11-09 the owner asked for a first filter "Project or STL", and a picture of the dialog before
the merge.

## Goal

The dialog of "Open project..." shows a project and an STL file under its first filter; an STL file chosen there goes to the
STL import, and a project file to the open of the project.

## Scope (in)

1. ADR-0106: the three filters, the path of a chosen file and the question.
2. A dialog of the system with a list of filters, and its test.
3. The path of a chosen file by its extension, the import of a path apart from the window, and a test for each extension.
4. A picture of the dialog of the open in the real window, before the merge.

## Scope (out)

- A change of "Import STL...", of the save dialog or of the export dialog.

## Acceptance criteria

- [ ] The tests pass, and each new test fails on an injection of the old behaviour.
- [ ] A picture of the dialog shows the filter "Project or STL (*.3de;*.stl)" with a project and an STL file.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the filters and the extensions follow the order of the owner.
Judgement:  the host, not `ProjectFiles`, sends an STL file to the import, because the import needs the store of the
            assets and the read of SDL; the import of a path becomes a method apart from the window, so a test reaches it.
Weakest:    to be written at the close.

## Progress

- 2026-11-09: the filters, the path by the extension and ten tests. Three injections failed their tests, and each file came
  back byte for byte: an STL file that stays an open (three tests), the old single filter (two tests), and each filter
  written at the first place of the array (one test). A picture of the real dialog at 1200 by 720, opened by window messages
  on a scratch folder, shows "Project or STL (*.3de;*.stl)" with `bracket.stl` and `EntryHallShelf.3de`.
