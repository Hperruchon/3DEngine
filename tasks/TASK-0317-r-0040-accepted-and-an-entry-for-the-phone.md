---
id: 0317
title: The register accepts the image test on Ubuntu only, and an entry holds the project file of the phone
status: Active
phase: P0.131
opened: 2026-11-09
depends-on: [0316]
governed-by: []
writes:
  create:
    - tasks/TASK-0317-r-0040-accepted-and-an-entry-for-the-phone.md
  modify:
    - docs/register.md
    - docs/adr/0102-a-project-in-any-folder.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0317 — The register accepts the image test on Ubuntu only, and an entry holds the project file of the phone

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

P9g (TASK-0316, ADR-0102) put the phone out of scope (Q4 A of the owner), and the answer asked for a register entry. The
register held 15 of 15 open entries, so ADR-0102 item 9 held the phone. On 2026-11-09 the owner answered: "R-0040 accept:
Ubuntu is enough".

The facts on disk at v0.299: `3DEngine.ImageTests/VulkanImageFactAttribute.cs` skips the image test in continuous
integration on Windows and macOS and never on Linux, and `.github/workflows/ci.yml` installs lavapipe and the validation
layers on Ubuntu. `3DEngine.App/PhoneFiles.cs` refuses each save and each open with "The phone has no document folder yet."

## Goal

R-0040 is an accepted compromise with the reason of the owner, and a new open entry holds the project file of the phone.

## Scope (in)

1. R-0040 moves to "Accepted compromises", with the date, this task and the reason.
2. R-0056, a question: the phone opens and saves no project file.
3. ADR-0102 item 9 names R-0056 in its history.
4. R-0039 closes as resolved: since TASK-0194 a test starts the surface of the desktop.

## Scope (out)

- A change of the pipeline or of the image test. A project file on the phone.

## Acceptance criteria

- [ ] The register holds 14 open entries: R-0040 is in "Accepted compromises", R-0039 is closed, and R-0056 is open.
- [ ] `repo-gates` passes, and continuous integration passes on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the exit "Accept" of the register (rule 3) and the answer of the owner.
Judgement:  the class of R-0056 is "question" (90 days), because the owner decides later whether the phone gets a project
            file.
Weakest:    to be written at the close.

## Progress

- 2026-11-09: the register change. R-0039 closes too: the session found on disk that `DesktopSessionTests` starts the
  surface since TASK-0194, with the three conditions of the entry.
