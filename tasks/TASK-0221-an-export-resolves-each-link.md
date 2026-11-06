---
id: 0221
title: An export of the command line resolves a link of each folder of its paths
status: Done
phase: P0.88
opened: 2026-11-06
depends-on: [0220]
governed-by: [0002, 0016, 0018, 0019]
writes:
  create:
    - tasks/TASK-0221-an-export-resolves-each-link.md
  modify:
    - Engine.Cli/Cli.cs
    - Engine.Tests/Export/StlExportTests.cs
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - 3DEngine/**
---

# TASK-0221 — An export of the command line resolves a link of each folder of its paths

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of Codex of the merge of v0.200 gives two findings on the compare of P0.87 (TASK-0220). Finding 1, high:
`engine export step /models/part.3de /alias/part.3de`, where the folder `/alias` is a link to `/models`, writes over the
document, because the compare resolves a link of the last part of a path only. Finding 2, medium: a path of the export
with a NUL character throws `ArgumentException` out of `Cli.Run`, because the compare calls `Path.GetFullPath` before
the handler of the export. Before P0.87 the write gave `E-EXPORT-WRITE-FAILED` for that path. The reading of
`Engine.Cli/Cli.cs` confirms both. The order 2 of the owner says that a confirmed high finding becomes a correction task
before the next product phase, R14e. Finding 2 is in the same lines, so this task corrects it too.

## Goal

An export onto its own document file through a link of a folder gives `E-EXPORT-WRITE-FAILED` and leaves each byte of
the document, and a path of the export that is not valid gives `E-EXPORT-WRITE-FAILED` with exit code 1.

## Scope (in)

1. The compare resolves the link of each part of each path, from the root.
2. A path that `Path.GetFullPath` refuses gives `E-EXPORT-WRITE-FAILED`.
3. A test with a link of a folder (a junction on Windows, a symbolic link on Linux and macOS) and a test with a NUL
   character.

## Scope (out)

- A hard link to the document, which a compare of paths does not see.

## Acceptance criteria

- [x] Each test fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p088-export-links) · v0.205

## Method

Mechanical: the walk of the parts of a path, and the link of a folder as `WriteSetJudgeTests` makes it.
Judgement:  finding 2 in this task, because it is in the same lines as finding 1.
Weakest:    a hard link and a short name of Windows (8.3) are second names that the compare does not see.

## Progress

- 2026-11-06: opened from the review of Codex of v0.200.
- 2026-11-06: the compare resolves the link of each part of each path, and a path of the export that is not valid
  gives `E-EXPORT-WRITE-FAILED`. Before the change the test of a link of a folder wrote over the document for STL and
  STEP, and the test of a NUL character threw `ArgumentException`; after it each one gives the code and the bytes of the
  document stay.
- 2026-11-06: closed at v0.205.
