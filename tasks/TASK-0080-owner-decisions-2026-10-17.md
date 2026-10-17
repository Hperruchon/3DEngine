---
id: 0080
title: The decisions of the owner of 2026-10-17 are a record
status: Done
phase: governance
opened: 2026-10-17
depends-on: [0077]
governed-by: []
writes:
  create:
    - tasks/TASK-0080-owner-decisions-2026-10-17.md
    - tasks/TASK-0081-sdl-upgrade.md
    - tasks/TASK-0082-intent-layer.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - eng/**
    - CLAUDE.md
    - docs/adr/**
---

# TASK-0080 — The decisions of the owner of 2026-10-17 are a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-10-17 the owner answered the four questions of the codebase review of 2026-10-17 with "1a 2d1a d2b
d3a d4a but allow setting for the action d5a d6b d7b d8c 3a 4a":

1. Q1, option A: TASK-0078 (P0.30) comes before R7.
2. Q2, the decisions of R7:
   - D1, option A: synthetic scripts of samples; the touch path stays unverified until a check on a touch
     screen.
   - D2, option B: a pen takes the path of one finger; the record of a sample keeps the kind Pen.
   - D3, option A: the samples, the intent state machine and the intents in a folder of `3DEngine.Core`; the
     translation of SDL events in `3DEngine.Vulkan`.
   - D4, option A, with a change: the map of the review (one finger or a left drag orbits; two fingers, a
     middle drag, or Shift and a left drag pan; a pinch or the wheel zooms; a tap or a click selects) is the
     default, and the action of each gesture is a setting.
   - D5, option A: the same intents from a touch script and from a mouse script, and a check that fails when a
     kind of intent has no touch script.
   - D6, option B: the keyboard alone moves the camera.
   - D7, option B: a task before R7 moves to a binding with SDL 3.2 or later.
   - D8, option C: the hint `SDL_HINT_TOUCH_MOUSE_EVENTS` is off, and the translator also drops each mouse
     event with `which == SDL_TOUCH_MOUSEID`.
3. Q3, option A: the selection is a state of the client, as ADR-0007 says; the roadmap row of R8 changes.
4. Q4, option A: R7 needs 5 to 7 evenings, and the upgrade of SDL is its own task.

## Goal

Each decision is in a task or in the roadmap.

## Scope (in)

1. The roadmap puts P0.30 and the upgrade of SDL (P0.32, TASK-0081) before R7, names TASK-0082 for R7 with 5
   to 7 evenings, and changes the row of R8: the selection stays a state of the client.
2. TASK-0082 holds the decisions of Q2. Its first commit writes them as an ADR.
3. One ledger entry, v0.78.

## Scope (out)

- No code and no ADR.

## Outcome

Status: Done · v0.78 · the commit that carries this block.

## Method

**Judgement.** The setting of D4 is a user setting of the client: ADR-0007 permits a setting that the client
owns and that no other client needs, so it needs no command. The upgrade of D7 is its own task, so that R7
starts on the binding that it uses; the review saw that versions 3.9.3 to 3.9.8 of `Alimer.Bindings.SDL`
exist, and did not check which SDL each one ships.

## Progress

- 2026-10-17: opened and closed in one commit.
