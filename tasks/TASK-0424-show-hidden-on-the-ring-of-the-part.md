---
id: 0424
title: "Show hidden" on the ring of the part, and the count of the hidden bodies on the path line where the status line has no room
status: Ready
phase: R20e
opened: 2026-11-20
depends-on: [0422]
governed-by: [0025, 0177]
writes:
  create:
    - 3DEngine.ImageTests/ShowHiddenTests.cs
  modify:
    - tasks/TASK-0424-show-hidden-on-the-ring-of-the-part.md
  forbid:
    - docs/CHARTER.md
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
---

# TASK-0424 — "Show hidden" on the ring of the part, and the count of the hidden bodies on the path line where the status line has no room

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-20 the owner chose the path line for register entry R-0064. ADR-0177 reads the answer with the ring of the part of
ADR-0174 item 1. TASK-0422 opened this task, because ADR-0177 names its test.

## Goal

A person finds "Show hidden" on the ring of the part, and a portrait phone shows the count of the hidden bodies.

## Scope (in)

1. ADR-0177 item 1: the segment "Show hidden" on the ring of the part while at least one live body is hidden.
2. ADR-0177 item 2: the part "N hidden · Show" on the path line where the status line has no room for it.
3. `ShowHiddenTests`, and a state of the layout test of ADR-0172 with hidden bodies on a portrait phone.

## Scope (out)

- The write set holds only the test file now. The task adds the files of the window, and the ADRs that govern them, when it
  starts (`docs/templates.md`, section 2).

- A change of the engine or of the contract (ADR-0177 item 3).

## Acceptance criteria

- [ ] `ShowHiddenTests` shows the segment with a hidden body and not without, and its tap sends one "Show all".
- [ ] At 390 by 844 with a hidden body the path line shows the part, and its tap opens the list of ADR-0124 item 3.
- [ ] An injection of each item makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
