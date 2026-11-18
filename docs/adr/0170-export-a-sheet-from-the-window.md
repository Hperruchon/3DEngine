---
id: 0170
title: The sheet page and the command line export a sheet as PDF, SVG or DXF through the save dialog of the system, off the frame thread
status: Accepted
topic: Drawing, window, export
date: 2026-11-18
supersedes: []
superseded-by: []
amends: ['0168']
amended-by: []
affects:
  - 3DEngine.App/SheetExports.cs
  - 3DEngine.App/ProjectFiles.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.SheetPage.cs
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.App/Ui/PanelAction.cs
  - 3DEngine.Vulkan/Window.FileDialog.cs
  - 3DEngine.ImageTests/SheetExportTests.cs
enforced-by: 3DEngine.ImageTests/SheetExportTests.cs
---

# ADR-0170 — The sheet page and the command line export a sheet as PDF, SVG or DXF through the save dialog of the system, off the frame thread

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 item 9 (the answer D9 A of the owner of 2026-11-18) decides one button "Export" on the sheet page, with PDF, SVG and
DXF, the save dialog of the desktop and the save or share of the phone, and the words "Export PDF", "Export SVG" and "Export
DXF" in the command line of the window. Row R30d holds this slice; the owner ordered "and r30d" on 2026-11-18. Until this slice
only the HTTP routes (`GET /export/{pdf,svg,dxf}`) and the command line of the engine (`engine export`) wrote the files of a
sheet (ADR-0159 to ADR-0161).

## Decision

1. **The controls (amends ADR-0168 items 1 and 6).** The header of the sheet page has "Export" after the name of the sheet. A
   tap opens a row of the formats, PDF first, then SVG and DXF, each 44 pixels high, in the place of the actions of a selected
   view and of the views to place; a second tap, a format, a tap on the paper or Escape closes it. On the page Escape closes
   that row first. The command line has "Export PDF", "Export SVG" and "Export DXF" while a live sheet exists; each exports the
   sheet of the page, which is the sheet that the person chose there or the first live sheet.
2. **The save dialog.** A format opens the save dialog of the system through SDL, with the filter of the format ("PDF file"
   and `pdf`, "SVG file" and `svg`, "DXF file" and `dxf`) and a purpose of its own (ADR-0102 item 2), so a late result of
   another dialog never writes a sheet. The dialog starts beside the file of the project at "<project> <sheet>.<format>", with
   "_" for a character that a file name does not permit, or where the system chooses for a document with no file. On the phone
   the same call of SDL asks the document picker of Android for a new document, so the person saves the file in a folder or
   a service that the picker offers. A share sheet of Android is not part of this slice.
3. **The file.** The window reads the file from the Document with the code of the routes and of the command line of the
   engine (`PdfExport.Write`, `SvgExport.Write`, `DxfExport.Write`), and writes its bytes through SDL, so a content URI of the
   picker of Android takes them as a path does. The read and the write run in the queue of the commands of the page, off the
   frame thread (ADR-0168 item 4). A path with no extension gets the extension of the format; a content URI keeps its name.
   The panel says "Exported Sheet 1 as PDF to plate Sheet 1.pdf, 2470 bytes.", with each body that gives no lines and its
   reason. A sheet that is not live gives the code of the engine, and a write that fails gives `E-EXPORT-WRITE-FAILED` with the
   reason of SDL. The file of the project does not change.
4. **No contract change.** The window calls the three writers of `Engine.Core/Export` as they are.

## Consequences

**Good:** a person exports the sheet that the page shows with two taps and a dialog, on the desktop and on the phone, and the
file is the file of the routes and of the command line of the engine.

**Bad:** the path of the phone is not tested on a phone: the pipeline builds the APK and starts it on an emulator, and no test
drives the picker of Android. If SDL gives no save dialog on Android, the panel says "The file dialog did not open" with the
reason of SDL, and the routes stay as the path of an agent. A share sheet needs code of Android and waits for a question to the
owner.

## Alternatives considered

- **Three buttons in the panel beside "Export STL..." (D9 B).** The owner chose the page.
- **A share sheet of Android (an intent with a provider of files).** It needs code of the activity and a provider in the
  manifest, and no test on a phone can check it in this slice.
- **The read and the write on the frame thread, as "Export STL..." does.** Finding V196 of the codebase review of 2026-11-18.

## History

- 2026-11-18 · TASK-0412 · The controls, the dialog and the file (phase R30d).
