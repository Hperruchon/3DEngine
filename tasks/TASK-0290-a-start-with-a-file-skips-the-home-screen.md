---
id: 0290
title: A start of the desktop with a file opens it and shows no home screen
status: Active
phase: P9f
opened: 2026-11-08
depends-on: [0288]
governed-by: [0007, 0019, 0025, 0029, 0061, 0083]
writes:
  create:
    - tasks/TASK-0290-a-start-with-a-file-skips-the-home-screen.md
  modify:
    - 3DEngine/DesktopSession.cs
    - 3DEngine.ImageTests/StlOpenTests.cs
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0290 — A start of the desktop with a file opens it and shows no home screen

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0083 item 7 was silent on a start with the setting `file` (`--file`), and the first session showed the home screen over
the named file. On 2026-11-08 the owner answered the question in the form of decisions: a start with `--file` skips the
home screen.

## Goal

A start of the desktop with the setting `file` shows that document at once; a start with no file shows the home screen.

## Scope (in)

1. The session of the setting `file` ends the home screen before the first frame.
2. A test of a start with a file, and the test of V126 with a start with no file.
3. ADR-0083 item 7.

## Scope (out)

- The phone, which has no setting `file`.

## Acceptance criteria

- [ ] A start with a file shows no home screen; a start with no file shows it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the new test, and an injection of the old rule.
Judgement:  the rule reads the setting in the session of the first start only; a later session has the file of an open.
Weakest:    to be written at the close.

## Progress

- 2026-11-08: the rule, two tests and ADR-0083 item 7. An injection of the old rule fails the new test.
