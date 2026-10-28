---
id: 0099
title: The document file keeps each complete line and each held file, and the pad applies to the current selection
status: Ready
phase: P0.40
opened: 2026-10-28
depends-on: [0098]
governed-by: [0004, 0007, 0009, 0015, 0016, 0017, 0019, 0022, 0023, 0024, 0025, 0026, 0027, 0029, 0030, 0031, 0032]
writes:
  create:
    - tasks/TASK-0099-document-file-and-pad-gaps.md
  modify:
    - Engine.Core/Persistence/**
    - Engine.Core/DocumentSession.cs
    - Engine.Api.Http/DocumentFolder.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/Endpoints/DocumentEndpoint.cs
    - Engine.Api.Http.Server/Program.cs
    - Engine.Tests/Persistence/**
    - Engine.Tests/Http/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - docs/adr/0015-command-log-persistence.md
    - docs/adr/0030-document-file-in-the-desktop.md
    - docs/adr/0032-numeric-pad-and-evaluator.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0099 — The document file keeps each complete line and each held file, and the pad applies to the current selection

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second codebase review of 2026-10-28 found gaps in the document file, the hosts, the pad and the desktop. Each
finding gives its evidence in `docs/reviews/2026-10-28-b-codebase-review.md`.

- E76 (medium): a complete last line that does not bind is cut as a torn line, and the file on disk loses it.
- E77 (medium): a save on a host started with `--file` moves that file, and the next start with the same `--file`
  makes an empty document with no message.
- E78 to E85 (low): the reader accepts an infinity, a folder with a final separator refuses each name, a name with a
  lone surrogate gives HTTP 500, a reserved or invalid file name, the exit code of a `--file` that names a folder, a
  second open of a held file, two failure paths of the journal, and a save on `engine-api-http` with no file.
- V31 (medium): an open of a file that another desktop holds passes the check, and then the window is lost.
- V32 (medium): the Move pad applies to the body that was selected when it opened.
- V33 to V39 (low): the pad takes the text of a text field, two Enter presses make two boxes, the pad goes past the
  edge of a tablet display, a bounce of a finger gives Undo, a command after the reply of an open changes the old
  document, the yaw and the target of the camera accept NaN, and a late failure of the swapchain leaks.

`docs/templates.md`, section 7, puts a medium or low finding into a task after the next product phase, so this
task comes after K2.

## Goal

The document file never loses a complete line or a held file in silence, and the pad sends the command that the
person sees.

## Scope (in)

1. E76: only a tail with no line feed, or a line that is not JSON, is a torn line. Each other failure refuses the
   file with `E-IO-LOAD-FAILED` and does not cut it.
2. E77: a start with a `--file` that does not exist says that it makes a new file, or a save on such a host copies
   the file. The owner chooses in the first commit.
3. E78 to E85: each form of the review is a test, with the recommendation of the review.
4. V31: the check opens the file with the access and the share of the journal.
5. V32: the pad closes when the selection changes, or names its body; a test holds it.
6. V33 to V39: each form of the review is a test, with the recommendation of the review.

## Scope (out)

- The high findings (TASK-0096, TASK-0097) and the gates (TASK-0098).
- V29 and V36 on a real touch screen: the review carries V29.

## Acceptance criteria

- [ ] Each form of the review fails a test before its correction, or an injection shows that the test can fail.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The ADRs.** E76 and E77 change the text of ADR-0015 items 8 and 10, V31 and V37 the text of ADR-0030 items 4 and
  5, and V32 to V34 the text of ADR-0032 item 4. The public shape of `Engine.Contracts` does not change.
- **The governance.** `governed-by` must equal the ADRs whose `affects` intersect the write set; the gate
  `TaskGovernance` gives the list.
- **The size.** About 3 to 4 evenings. A split into the engine and the desktop is possible.

## Progress

- 2026-10-28: opened by the second codebase review of 2026-10-28 (TASK-0095).
