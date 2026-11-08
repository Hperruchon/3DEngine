---
id: 0292
title: The interface draws with Instrument Sans, and a value on an edge with JetBrains Mono
status: Done
phase: R16i
opened: 2026-11-08
depends-on: [0294]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0091]
writes:
  create:
    - tasks/TASK-0292-the-fonts-of-the-redesign.md
    - docs/adr/0091-the-fonts-of-the-redesign.md
    - 3DEngine.App/Assets/Fonts/InstrumentSans.ttf
    - 3DEngine.App/Assets/Fonts/JetBrainsMono.ttf
    - 3DEngine.App/Assets/Fonts/InstrumentSans-OFL.txt
    - 3DEngine.App/Assets/Fonts/JetBrainsMono-OFL.txt
    - 3DEngine.ImageTests/FontTests.cs
  modify:
    - 3DEngine.App/3DEngine.App.csproj
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.ImageTests/NumericPadUiTests.cs
    - 3DEngine.ImageTests/FileDialogStateTests.cs
    - 3DEngine.ImageTests/WindowLifecycleTests.cs
    - THIRD-PARTY-NOTICES.md
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - docs/adr/0027-text-and-panels.md
    - docs/adr/0078-the-theme-of-the-redesign.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0292 — The interface draws with Instrument Sans, and a value on an edge with JetBrains Mono

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The first slice of the redesign named the fonts Instrument Sans and JetBrains Mono, and ADR-0078 item 3 put them after the
owner allowed the download of their files. On 2026-11-08 the owner allowed it.

## Goal

The desktop and the phone draw their text with the fonts of the redesign.

## Scope (in)

1. The two fonts and their licence texts in the repository, and the notices.
2. The atlas with the two fonts; the chips of the values with JetBrains Mono.
3. ADR-0091.

## Scope (out)

- Weights other than 400, and the fonts of the rows and the windows for the numbers.

## Acceptance criteria

- [x] The atlas holds the two fonts, and JetBrains Mono gives each character one width.
- [x] The image tests pass, and the pictures on the GPU show the fonts.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r16i-fonts-b) · v0.278

## Method

Mechanical: the test of the fonts and an injection of the wrong font of a value; the pictures on the GPU.
Judgement:  JetBrains Mono for the values on the edges only (Q1 A).
Weakest:    the fonts draw at the weight 400 only; no run on the S23 showed them yet.

## Progress

- 2026-11-08: the fonts, the atlas, the chips and ADR-0091. The test of the fonts fails with Instrument Sans as the font of
  a value. With the fonts, two tests needed a fix that is not of the fonts: the test of the pad tapped the list in its
  second frame, before the popup had its size; and the test of the file dialog of TASK-0282 ran at the same time as the
  test of the windows, whose new window clears the state of the dialog, so the two classes now run in one collection.
- 2026-11-08: the gate of the control bytes read a font file as text; `.ttf` joins its list of binary extensions, and an
  injection of a zero byte in a Markdown file still fails it. The test of the fonts reads the widths through `DesktopUi`,
  because only the files in `Ui/` name the library of the interface.
- 2026-11-08: run 42438126203 passed. Closed at v0.278.
