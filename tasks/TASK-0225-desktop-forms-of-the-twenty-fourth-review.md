---
id: 0225
title: The start again keeps an unsaved document, a window that does not open gives a code, and the flush holds its place
status: Done
phase: P0.90
opened: 2026-11-06
depends-on: [0224]
governed-by: [0007, 0015, 0017, 0019, 0022, 0023, 0024, 0025, 0029, 0030, 0061]
writes:
  create:
    - tasks/TASK-0225-desktop-forms-of-the-twenty-fourth-review.md
  modify:
    - 3DEngine/DesktopEngineHost.cs
    - 3DEngine/DesktopSurface.cs
    - Engine.Api.Http/EngineHost.cs
    - 3DEngine.Vulkan/Window.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - 3DEngine.ImageTests/DesktopSessionTests.cs
    - docs/adr/0072-the-drawing-code-is-a-library.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0225 — The start again keeps an unsaved document, a window that does not open gives a code, and the flush holds its place

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-06 (b) found V91 (medium) and V92 to V96 (low). By `docs/templates.md`, section 7, they come
after the next product phase, and after P0.89. The standing order of 2026-11-05 answers Q2 of the review with A. The
first commit of this task corrects the write set and the field `governed-by`.

## Goal

A start again after a next session that does not start keeps a document of the unsaved folder, a window that does not
open gives an exit code, the flush of V90 comes after the new device and has a test, and ADR-0072 is true.

## Scope (in)

1. V91 (Q2 A): the host keeps the kind of the previous document.
2. V92: the window in a `try`, with an exit code and a message.
3. V93 and V94: the flush after `Initialize` and `LoadScene`, and a test with no window.
4. V95: ADR-0072 item 3 names the placement of an image and the export.
5. V96: the count of ADR-0072.

## Scope (out)

- E152 to E155, T145 and T146 (TASK-0224).

## Acceptance criteria

- [x] Each finding has a test or an injection that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p090-review-forms) · v0.211

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  the unsaved folder of `EngineHost` also with a path, so that a journal of that folder opened by its path
            has no name; the exit code 7 for a window that does not open.
Weakest:    no test holds the order of V93, and nobody started the desktop to see a session change in a window.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06 (b).
- 2026-11-06: the write set takes `Engine.Api.Http/EngineHost.cs` and leaves `NativeThreeDEngine.cs`, which needs no
  change. V91 (Q2 A): the start again keeps the kind of the previous document; `EngineHost` reads the unsaved folder
  also with a path. V92: the window is made in a `try`, a show that fails gives `ExitWindowFailed` (7), and the error goes
  to the standard error. V93: the flush runs after `Initialize` and `LoadScene`. V94: a test with the queue of SDL and
  no window. V95 and V96: ADR-0072. Three injections each fail a test: the start again with the path only, a show that
  fails with the code 0, and a flush that removes nothing. No test holds the order of V93.
- 2026-11-06: closed at v0.211.
