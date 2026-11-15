---
id: 0383
title: A line that closes a chain of the design shows its reference in parentheses, and each value keeps its row in each view
status: Done
phase: R22b
opened: 2026-11-14
depends-on: [0381, 0382]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0082, 0084, 0091, 0098, 0099, 0105, 0108, 0110, 0115, 0117, 0119, 0128, 0130, 0131, 0135, 0141, 0146, 0148]
writes:
  create:
    - tasks/TASK-0383-the-reference-of-a-chain.md
    - docs/adr/0148-the-reference-of-a-chain.md
    - 3DEngine.ImageTests/ChainReferenceTests.cs
  modify:
    - 3DEngine.App/EdgeValues.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.ImageTests/FlatViewValuesTests.cs
    - docs/adr/0146-a-value-shows-where-its-length-is-true.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0383 — A line that closes a chain of the design shows its reference in parentheses, and each value keeps its row in each view

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R22b (ADR-0146 item 2). R22a (TASK-0381) removed a value whose length is not true from the list that the rows read too,
so in a flat view the rows lost it. On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for
each question.

## Goal

A flat view shows the reference of each chain that the design closes, and the rows give each value of the design in each view.

## Scope (in)

1. ADR-0148, which amends ADR-0146 item 1.
2. The flag of a value on the view, and the reference of a chain, in `EdgeValues.Of`.
3. A chip with no row and no pad for a reference; a row with no chip for a value that the view does not show true.
4. Tests.

## Scope (out)

- A value between two points that are not the ends of one line (a later slice).
- R22c and R22d.

## Acceptance criteria

- [x] ADR-0148 exists, and ADR-0146 shows the amendment.
- [x] Tests prove the references of an L profile from the top, from the front and with perspective, no reference where a
      line leaves a gap, each value in the list in each view, and a reference with a chip, no row and no pad.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r22b-chain-reference) · v0.368

## Method

Mechanical: the flag on the view; the rows and the chips.
Judgement:  the rule of a chain; the reference from the design values.
Weakest:    the chain of the Bracket waits for a value between two points; no person used it.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after P0.153, branch r22b-chain-reference. TASK-0383 and
  ADR-0148 are free. The study found that R22a removed values from the rows in a flat view; the flag `InView` corrects it. The
  first test of a tap counted no pad only, and a tap with no guard sends an edit of the distance, so the test asserts no
  action too. The tiling check had no test with the L alone, so a U with a gap holds it. Eight injections each failed a named
  test; each file came back byte for byte.
- 2026-11-14: run 43578376204 passed on the work commit. Closed at v0.368.
