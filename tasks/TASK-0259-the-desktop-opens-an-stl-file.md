---
id: 0259
title: The desktop opens an STL file from its home screen
status: Done
phase: P9c
opened: 2026-11-07
depends-on: [0265]
governed-by: [0007, 0017, 0019, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0061, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083]
writes:
  create:
    - tasks/TASK-0259-the-desktop-opens-an-stl-file.md
    - 3DEngine.Vulkan/Window.FileDialog.cs
    - 3DEngine.App/StlOpening.cs
    - 3DEngine.App/Ui/DesktopUi.Home.cs
    - 3DEngine.ImageTests/StlOpenTests.cs
    - 3DEngine.ImageTests/HomeImageTests.cs
  modify:
    - 3DEngine.Vulkan/Window.cs
    - 3DEngine.App/ISessionHost.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - 3DEngine/DesktopDocument.cs
    - 3DEngine/DesktopSession.cs
    - 3DEngine/DesktopEngineHost.cs
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - docs/adr/0080-the-fixed-controls-of-the-redesign.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0259 — The desktop opens an STL file from its home screen

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The demonstration "Open the same STL on desktop and Android" (ADR-0083, the owner, 2026-11-07) needs on the desktop: a
home screen, the file dialog of Windows with a filter for `.stl`, the frame after an open, a control "Frame", and a
message with "Choose another file". TASK-0258 gives the engine part.

## Goal

A person opens `bracket.stl` from the home screen through the file dialog of the system, sees it framed with its name
and its size in "mm (assumed)", frames it again with "Frame", and a damaged file gives a message with its code and
"Choose another file".

## Scope (in)

1. The file dialog of the system through SDL (`Window.OpenFile`, `Window.LoadFile`), which serves the phone too.
2. The check of a chosen file with the reader of the engine, its bytes in the store, and a new part document whose
   session applies the one import first.
3. The home screen, "Frame" and "Home", and the message.

## Scope (out)

- The phone (P9d) and the test sheet (P9e). The row closes after the run of the owner.

## Acceptance criteria

- [x] The home screen gives "Open file", "New part" and a recent file; "Frame" and "Home" give their actions.
- [x] A damaged file gives E-STL-TRUNCATED and no new document; a good file gives a new part with a mesh body.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.
- [ ] The run of the owner on the desktop passes the criteria of the page of the demonstration. This box waits for the
      owner: the row P9c of the roadmap stays open until it, as R14e does (`docs/demo-stl-test-sheet.md`).

## Outcome

Status: Done · commit (the merge of the branch p9c-desktop-open-b) · v0.247

## Method

Mechanical: the actions of the home screen, of the row of the anchors and of the message; the check, the store and the
            import of a chosen file; the next session of a new part.
Judgement:  the dialog of SDL for both devices; the client checks the file before a new document, and the engine stays
            the authority at the import; "Frame" and "Home" on the row of the anchors, because the upper right covered
            the panel on a small display; the import and the home screen belong to the process, because each session
            makes its own DesktopDocument.
Weakest:    no person opened a file through the dialog of Windows yet; the tests reach the dialog only through
            `TakeOpenedFile`, so the filter, the folder where the dialog opens and the time to the frame are not
            measured. The run of the owner measures them.

## Progress

- 2026-11-07: the dialog, the check, the new part with its import, the home screen, "Frame", "Home" and the message; five
  tests with no GPU. The first picture of the panel showed "Frame" and "Home" over the panel on a display of 640 by 480,
  so they moved to the row of the anchors. Injections of a new part that does not end the session and of a home screen
  beside the panel fail two tests.
- 2026-11-07: on v0.246, after the corrections of the review of 2026-11-07 (E167, V124); a picture of the home screen on
  the GPU, kept for the owner.
- 2026-11-07: run 42355699358 passed. Closed at v0.247; the row waits for the run of the owner.
