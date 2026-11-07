---
id: 0260
title: The phone opens an STL file from its home screen
status: Active
phase: P9d
opened: 2026-11-07
depends-on: [0259]
governed-by: [0007, 0019, 0025, 0029, 0061, 0072, 0073, 0075, 0076, 0077, 0079, 0080, 0081, 0082, 0083]
writes:
  create:
    - tasks/TASK-0260-the-phone-opens-an-stl-file.md
    - 3DEngine.App/PhoneFiles.cs
    - 3DEngine.ImageTests/PhoneFilesTests.cs
  modify:
    - 3DEngine.App/ISessionHost.cs
    - 3DEngine.App/LocalSessionHost.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine/DesktopDocument.cs
    - 3DEngine.Android/PhoneHost.cs
    - eng/android/check-apk.sh
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - docs/android-test-sheet.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0260 — The phone opens an STL file from its home screen

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The demonstration "Open the same STL on desktop and Android" (ADR-0083, the owner, 2026-11-07) needs on the phone: the
document picker of Android with no permission of storage, the same home screen as the desktop, and the part kept through
the background. TASK-0259 gives the desktop part and the dialog of SDL, which on Android is the document picker.

## Goal

A person opens `bracket.stl` from the Download folder of the S23 through the document picker, with the same home screen,
messages and controls as the desktop, and the part stays through 10 seconds in the background.

## Scope (in)

1. `PhoneFiles`: the home screen, a new part and its import for the run of the program; no document folder yet.
2. The store of the phone, a new part as a new session in the same window, and the engine of each session with them.
3. The APK check of the pipeline: the manifest asks for no permission of storage.

## Scope (out)

- A document folder of the phone, Save and Open of a document file. The row closes after the run of the owner on the S23.

## Acceptance criteria

- [ ] `PhoneFiles` gives a new part with its import one time, no recent file and no folder.
- [ ] The APK asks for no permission of storage, and an injected permission fails the check.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.
- [ ] The run of the owner on the S23 passes the criteria of the page of the demonstration.

## Outcome

Status: Active

## Method

Mechanical: a test of `PhoneFiles`; the APK check, proved with an injected permission on an APK built here.
Judgement:  the dialog of SDL as the picker; a new part is a new LocalSessionHost in the same window, so the rules of
            ADR-0079 for the window hold; the phone shows no file controls, because it has no document folder yet.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: `PhoneFiles`, the store and the new session of the phone, `HasFolder` for the file controls, and the APK
  check. The APK built here passes the check, and the same APK with `READ_EXTERNAL_STORAGE` fails it.
