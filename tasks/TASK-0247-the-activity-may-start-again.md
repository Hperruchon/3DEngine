---
id: 0247
title: The activity of the phone may start again in its process
status: Done
phase: P0.99
opened: 2026-11-06
depends-on: [0243]
governed-by: [0061, 0073, 0074, 0079]
writes:
  create:
    - tasks/TASK-0247-the-activity-may-start-again.md
    - docs/adr/0079-the-activity-may-start-again.md
  modify:
    - 3DEngine.Android/MainActivity.cs
    - 3DEngine.Android/PhoneHost.cs
    - eng/android/smoke-emulator.sh
    - docs/adr/0073-the-phone-cuts-and-keeps-its-document.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0247 — The activity of the phone may start again in its process

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The emulator job failed in about a third of its runs with no line of the program (finding V105). The lines of P0.98 gave
the cause in run 42289169473: Android created the activity, destroyed it, and created it again in the same process, and
SDL ended the process with the code 0. A phone can start an activity again in the same way, for example for a new
overlay of the system, so the program closes with no message.

## Goal

A new start of the activity in its process does not end the program.

## Scope (in)

1. ADR-0079, Accepted under the standing order of 2026-11-05; it amends ADR-0073 item 3.
2. The hint `SDL_ANDROID_ALLOW_RECREATE_ACTIVITY` in `MainActivity.OnCreate`, after SDL is loaded, with one line.
3. `ConfigurationChanges` names `FontWeightAdjustment` and `GrammaticalGender`.
4. The smoke test fails when the hint is refused.

## Scope (out)

- A file for the document of the phone, so that a new start opens it again.

## Acceptance criteria

- [x] The APK builds with zero warnings.
- [x] The emulator job writes the line of the hint, and a run with a new start of the activity goes on to the first frame.

## Outcome

Status: Done · commit (the merge of the branch p099-activity-new-start-b) · v0.231

## Method

Mechanical: one hint and two flags.
Judgement:  the hint after `base.OnCreate`, because SDL is loaded only then, and a new start comes after the first create.
Weakest:    no run of this branch had a new start of the activity, so the hint is proven to be set, not yet to keep a
            new start on the screen; the next failures of that form, or their absence, show it.

## Progress

- 2026-11-06: ADR-0079, the hint, the two flags and the check of the smoke test.
- 2026-11-06: runs 42292342902 and 42293786790 passed; the program wrote "a new start of the activity goes on". Closed at v0.231.
