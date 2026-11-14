---
id: 0387
title: A free size or a reference on the line of a value that the view shows does not show again
status: Active
phase: P0.156
opened: 2026-11-14
depends-on: [0386]
governed-by: [0007, 0019, 0025, 0029, 0072, 0082, 0084, 0115, 0117, 0146, 0148, 0149, 0151]
writes:
  create:
    - tasks/TASK-0387-a-size-on-the-line-of-a-shown-value.md
    - docs/adr/0151-a-size-on-the-line-of-a-shown-value.md
  modify:
    - 3DEngine.App/EdgeValues.cs
    - 3DEngine.ImageTests/FreeSizeTests.cs
    - docs/adr/0148-the-reference-of-a-chain.md
    - docs/adr/0149-a-size-that-no-constraint-fixes.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0387 — A free size or a reference on the line of a value that the view shows does not show again

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The picture `mix-front.png` of the Bracket after R22c (v0.369) showed "60.000 mm not fixed" inside the plate, on the line of
the value 60.000. On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Goal

A flat view gives one number for each line that it shows.

## Scope (in)

1. ADR-0151, which amends ADR-0148 item 2 and ADR-0149 item 2.
2. The rule of one line in `EdgeValues`.
3. Tests and the pictures of the Bracket from the front and from the top.

## Scope (out)

- R-0066 and the other entries of the register.

## Acceptance criteria

- [ ] ADR-0151 exists, and ADR-0148 and ADR-0149 show the amendment.
- [ ] Tests prove no free size on the line of a shown value from the front and from the back, two free sizes from the top,
      and one free size where two free sides are one line.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the test of two ends along the view.
Judgement:  the order of the lines decides which free size shows.
Weakest:    only the pictures of the Bracket were seen.

## Progress

- 2026-11-14: opened after P0.155, branch p0156-coincident-free, from the picture of the Bracket from the front. TASK-0387 and
  ADR-0151 are free. Three injections each failed a named test; each file came back byte for byte. One injection first did not
  compile, because an unused local function is an error of the build, so it keeps the call.
