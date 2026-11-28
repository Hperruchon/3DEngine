---
id: 0486
title: The comment of DatumReference names each feature that a datum resolves on
status: Done
phase: P0.200
opened: 2026-11-28
depends-on: [0485]
governed-by: [0008, 0033, 0051]
writes:
  create:
    - tasks/TASK-0486-the-features-of-a-datum-reference.md
  modify:
    - Engine.Contracts/Sketch/DatumRecord.cs
    - docs/adr/0051-references-and-datum-on-a-face.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
---

# TASK-0486 — The comment of DatumReference names each feature that a datum resolves on

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Finding E160 of the codebase review of 2026-11-06 (d) said that the texts of `DatumResolver` and the comment of
`Engine.Contracts/Sketch/DatumRecord.cs` name only some of the features. The reasons of `DatumResolver` now name each
feature. The review of 2026-11-28 gives E160 "fixed in part": the comment names a box, a cylinder and an extrude, but
`DatumResolver.Form` also accepts a cone (ADR-0089), and it gives a reason for a sphere and a torus.

## Goal

The comment names each feature that a datum resolves on, and the two features that give a reason.

## Scope (in)

1. The comment of `DatumReference`, and a History line in ADR-0051. A comment does not change the public shape of
   `Engine.Contracts`.

## Scope (out)

- Each other open finding of the review of 2026-11-28.

## Acceptance criteria

- [x] The comment agrees with `DatumResolver.Form` (`Engine.Core/Sketch/DatumResolver.cs`).
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Done · commit (the merge of the branch p0200-datum-comment) · v0.469

## Method

Mechanical: the cases of `DatumResolver.Form` and the text of the comment.
Judgement:  none.
Weakest:    no test reads a comment, so a later primitive can make it false again.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after P0.199. Ledger v0.469.
- 2026-11-28: run 46141631186 of `516d0424` passed on each job. Closed at v0.469.
