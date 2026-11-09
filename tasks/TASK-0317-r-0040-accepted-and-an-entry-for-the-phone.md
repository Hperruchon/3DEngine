---
id: 0317
title: The register accepts the image test on Ubuntu only, and an entry holds the project file of the phone
status: Done
phase: P0.131
opened: 2026-11-09
depends-on: [0316]
governed-by: [0055]
writes:
  create:
    - tasks/TASK-0317-r-0040-accepted-and-an-entry-for-the-phone.md
  modify:
    - docs/register.md
    - docs/adr/0102-a-project-in-any-folder.md
    - docs/adr/0009-3dengine-core-peer-render-kernel.md
    - docs/adr/0071-container-model.md
    - docs/CHARTER.md
    - nuget.config
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
5. The answers of the owner of 2026-11-09: R-0007 and R-0054 are accepted; R-0024 (ADR-0009 kept) and R-0055 (a second
   Document allowed, so the charter loses that non-goal) are decided and closed; R-0053 records the answer "an ADR for
   units", which a task of its own writes.

## Scope (out)

- A change of the pipeline or of the image test. A project file on the phone.
- The ADR of units of R-0053, the design of a second Document, the vision library (R-0048, TASK-0318) and the old findings
  of the reviews (TASK-0319).

## Acceptance criteria

- [x] The register holds 10 open entries: R-0007, R-0040 and R-0054 are in "Accepted compromises", R-0024, R-0039 and
      R-0055 are closed, and R-0056 is open.
- [x] `repo-gates` passes, and continuous integration passes on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0131-register-r0040) · v0.300

## Method

Mechanical: the exit "Accept" of the register (rule 3) and the answer of the owner.
Judgement:  the class of R-0056 is "question" (90 days), because the owner decides later whether the phone gets a project
            file.
Weakest:    the closes of R-0024 and R-0039 rest on a reading of the code and the tests on 2026-11-09; no new test.

## Progress

- 2026-11-09: the register change. R-0039 closes too: the session found on disk that `DesktopSessionTests` starts the
  surface since TASK-0194, with the three conditions of the entry.
- 2026-11-09: the owner answered the form on the open entries: R-0007 A, R-0024 A, R-0054 A, R-0053 B, R-0055 B; R-0048
  and the old findings go to the next tasks. The comment of `nuget.config` named the folder an interim bootstrap and only
  the Manifold package; it now names both packages and the acceptance.
- 2026-11-09: run 42538301855 passed. Closed at v0.300.
