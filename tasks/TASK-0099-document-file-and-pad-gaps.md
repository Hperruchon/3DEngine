---
id: 0099
title: The document file keeps each complete line and each held file, and the pad applies to the current selection
status: Active
phase: P0.40
opened: 2026-10-28
depends-on: [0101]
governed-by: [0004, 0007, 0009, 0013, 0015, 0016, 0017, 0019, 0022, 0023, 0024, 0025, 0026, 0027, 0029, 0030, 0031, 0032, 0034]
writes:
  create:
    - tasks/TASK-0099-document-file-and-pad-gaps.md
  modify:
    - Engine.Core/Persistence/**
    - Engine.Core/DocumentSession.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/DocumentFolder.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/Endpoints/DocumentEndpoint.cs
    - Engine.Api.Http/Endpoints/CommandsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
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
- 2026-10-28: the owner decided to do P0.40 now, after K2b and before K3a and P0.39. `depends-on` names TASK-0101
  (K2b, Done) and not TASK-0098 (P0.39), and the status is Active. `roadmap.md`, "Order", rule 1, says so.
- 2026-10-28: seven questions went to the owner (E76, E77, E81, E85, V31 to V34, V37): each one changes a Decision
  section of ADR-0015, ADR-0030 or ADR-0032.
- 2026-10-28: E78, E79, E80, E82, E83 and E84 are corrected, with 10 new test cases. Nine injections each failed a
  test. The test of E83 asserts on Windows only, because .NET takes no lock on Linux and macOS for a write with the
  share of a read.
- 2026-10-28: V35, V36, V38 and V39 are corrected, with 10 new test cases. Five injections each failed a test. V39 is
  confirmed by reading only; its test holds the release of each object that exists.
- 2026-10-29: the owner answered Q1 to Q7 with A. ADR-0015 items 8 and 10, ADR-0030 items 2, 4 and 5, and ADR-0032
  items 4 and 6 record the answers. Q5 A adds the code `E-API-OPEN-PENDING`, which the commit of V37 registers.
- 2026-10-29: E76, E77, E81 and E85 are corrected, with 26 new test cases and one case changed: a complete last line
  that is JSON is no longer a torn line. Twelve injections each failed a test. One injection showed that a check of
  a final space or dot was redundant, and the check went out.
- 2026-10-29: V31 and V37 are corrected. The check holds the file as a journal does, except the file that the desktop
  holds. After an accepted open, the surface refuses each command, save and open with HTTP 503 and the new code
  `E-API-OPEN-PENDING`, which `docs/diagnostics.md` registers. The write set gains `Engine.Core/DiagnosticCodes.cs`
  `Engine.Api.Http/Endpoints/CommandsEndpoint.cs` and `SchemaDiagnosticsEndpoint.cs`, so `governed-by` gains ADR-0013. One new test case and two changed; four injections each
  failed a test.
- 2026-10-29: V32, V33 and V34 are corrected, with 4 new test cases. A pad that names a selection closes when the
  selection changes, the text fields are disabled while the pad is open, and a second apply waits for the end of the
  pad. Six injections each failed a test.
- 2026-10-29: `eng/test-floor.txt` holds the new counts: 689, 190, and 95 with 100 on Linux.
