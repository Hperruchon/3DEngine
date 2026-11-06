---
id: 0220
title: An export of the command line refuses a file that is its own document file
status: Active
phase: P0.87
opened: 2026-11-06
depends-on: [0199]
governed-by: [0002, 0016, 0018, 0019]
writes:
  create:
    - tasks/TASK-0220-an-export-keeps-its-document.md
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

# TASK-0220 — An export of the command line refuses a file that is its own document file

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of Codex of the merge of v0.198 gives a high finding: `engine export step part.3de part.3de` writes the STEP
text over the document file, and the document no longer opens. The order 2 of the owner of 2026-11-05 says that a
confirmed high finding of Codex becomes a correction task before the next product phase. The reading of `Engine.Cli/Cli.cs`
confirms it, and the STL export of P8g has the same defect: `DocumentFile.Read` holds no journal, and the export writes
the file with no compare of the two paths.

## Goal

An export onto its own document file, by the same path or by another spelling of it, gives `E-EXPORT-WRITE-FAILED` and
leaves each byte of the document.

## Scope (in)

1. A compare of the two paths in `Cli.Export`: the full paths, after a symbolic link, with no case on Windows and macOS.
2. A test for STL and STEP, with the same path and with another spelling.

## Scope (out)

- A hard link to the document, which is a second name that a compare of paths does not see.

## Acceptance criteria

- [ ] The test fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the compare of the paths, as `DesktopDocument.SameFile` does.
Judgement:  the existing code `E-EXPORT-WRITE-FAILED`, and no new code.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: opened from the review of Codex of v0.198; the test failed for STL and STEP before the change, and an
  injection that compares the raw strings fails it again.
