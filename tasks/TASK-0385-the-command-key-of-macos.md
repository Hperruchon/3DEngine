---
id: 0385
title: On macOS the Command key gives the modifier of the shortcuts, and the command line shows "Cmd K"
status: Done
phase: P0.154
opened: 2026-11-14
depends-on: [0384]
governed-by: [0007, 0017, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0098, 0099, 0101, 0102, 0108, 0113, 0119, 0120, 0127, 0128, 0137, 0138, 0150]
writes:
  create:
    - tasks/TASK-0385-the-command-key-of-macos.md
    - docs/adr/0150-the-command-key-of-macos.md
    - 3DEngine.ImageTests/CommandKeyTests.cs
  modify:
    - 3DEngine.Vulkan/SdlInput.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - docs/adr/0108-the-look-of-the-render.md
    - docs/adr/README.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0385 — On macOS the Command key gives the modifier of the shortcuts, and the command line shows "Cmd K"

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0060 (due 2027-05-09): the command line shows "Ctrl K" on macOS, where the render gives "Cmd K", because the
input has no Command key. After R22c each product row waits for P10, which needs the owner. On 2026-11-14 the owner ordered
work alone until Sunday, with the recommended option for each question.

## Goal

On macOS Command and K open the command line, and the field shows "Cmd K".

## Scope (in)

1. ADR-0150, which amends ADR-0108 item 4.
2. The Command key of macOS in `SdlInput`, and the text of the keys in the command line.
3. Tests, and R-0060 closed by its exit.

## Scope (out)

- A new value of `KeyModifiers`.

## Acceptance criteria

- [x] ADR-0150 exists, and ADR-0108 shows the amendment.
- [x] Tests prove the Command key on macOS and on the other platforms, and the text of each platform.
- [x] R-0060 is closed with its exit.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0154-command-key) · v0.370

## Method

Mechanical: the key of the system on macOS; the text.
Judgement:  the modifier of the shortcuts in place of a new modifier.
Weakest:    no person used it on a Mac; the runner of macOS runs the tests with the text of its platform.

## Progress

- 2026-11-14: opened after R22c, branch p0154-command-key. TASK-0385 and ADR-0150 are free. Three injections each failed a
  named test; each file came back byte for byte.
- 2026-11-14: run 43580359832 passed on the work commit, with the tests on the runner of macOS. Closed at v0.370.
