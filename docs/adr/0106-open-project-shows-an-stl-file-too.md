---
id: 0106
title: "Open project..." shows an STL file too, and an STL file goes to the import
status: Accepted
topic: Persistence, clients
date: 2026-11-09
supersedes: []
superseded-by: []
amends: ['0102']
amended-by: []
affects:
  - 3DEngine.App/ProjectFiles.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.Vulkan/Window.FileDialog.cs
  - 3DEngine.ImageTests/ProjectPathTests.cs
  - 3DEngine.ImageTests/FileDialogStateTests.cs
enforced-by: 3DEngine.ImageTests/ProjectPathTests.cs
---

# ADR-0106 — "Open project..." shows an STL file too, and an STL file goes to the import

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0102 item 1 gives "Open project..." and "Import STL..." as two actions. "Open project..." opens the dialog of the
system with one filter, "3DEngine project" and `3de`. After P9g the owner opened "Open project..." to find an STL file and
saw nothing in the folder, because the filter hid each file that is not a project.

On 2026-11-09 the owner asked for a first filter "Project or STL" with the patterns `3de;stl`, and the two filters of one
kind after it. A `.stl` file chosen there goes to the STL import, with the question of a change of project when it applies;
a `.3de` file goes to the open of the project.

## Decision

1. **The filters (amends ADR-0102 item 1).** "Open project..." opens the dialog of the system with three filters, in this
   order: "Project or STL" with `3de;stl`, "3DEngine project" with `3de`, and "STL file" with `stl`. The system shows the
   first one first. "Import STL..." keeps its one filter, "STL file" with `stl`.
2. **The path of a chosen file.** A file whose name ends with `.stl`, with no regard to the case of the letters, goes from
   the dialog of "Open project..." to the import of ADR-0083 D1 and D3: the check of the size, the read, the check with the
   reader of the engine, the store and a new part document. Each other file goes to the open of a project of ADR-0102
   item 1. The result of the dialog keeps the purpose of ADR-0102 item 2; only the host changes the open of an STL file into
   an import.
3. **The question.** "Open project..." and "Import STL..." each are a change of project, so the question of ADR-0102 item 5
   comes before the dialog, as before. An STL file from the dialog of the open asks no second question.
4. **No contract change.** The filters and the path are in the client. The import is the command that exists.

## Consequences

**Good:** a person who looks for a file in the dialog of the open sees each file that the window can take, and an STL file
opens as from "Import STL...".

**Bad:** the dialog of the open has three filters to keep in agreement with "Import STL...". The step of the host that sends
the result of the dialog to the import has no test, because it needs a window; a test covers the choice of the path and the
import of a path.

## History

- 2026-11-09 · TASK-0328 · The record, with the order of the owner of 2026-11-09.
