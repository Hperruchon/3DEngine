---
id: 0079
title: The activity of the phone may start again in its process
status: Accepted
topic: Clients, platform
date: 2026-11-06
supersedes: []
superseded-by: []
amends: ['0073']
amended-by: []
affects:
  - 3DEngine.Android/MainActivity.cs
  - 3DEngine.Android/PhoneHost.cs
enforced-by: eng/android/smoke-emulator.sh
---

# ADR-0079 — The activity of the phone may start again in its process

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The emulator job failed in about a third of its runs, with no line of the program (finding V105 of the codebase review of
2026-11-06 (d)). P0.98 made the activity write each create and destroy. Run 42289169473 then showed the cause: the
activity was created, destroyed 0.4 seconds later, and created again "with a saved state", and 7 milliseconds after that
the runtime ended the process with the code 0. No change of the configuration reached `OnConfigurationChanged`.

The class `SDLActivity` of the package holds the strings "activity re-created", "activity finished" and a call of
`exit`, and `libSDL3.so` holds the hint `SDL_ANDROID_ALLOW_RECREATE_ACTIVITY` (the review, V105). So when Android creates
the activity again in the same process, SDL ends the process unless that hint is on. Android does so for a change that an
application cannot declare, as a new overlay of the system, and for each change that `ConfigurationChanges` does not
name. Two changes that the phone can name are not in the list: the weight of the font and the grammatical gender. A
phone can do the same as the emulator, so the program closes with no message.

The source of SDL is not in this repository; the order above comes from the strings of the package and the run.

### Q1. A new start of the activity

- **A.** The program sets the hint `SDL_ANDROID_ALLOW_RECREATE_ACTIVITY` to 1 when SDL is loaded, in `OnCreate` of the
  first activity, so a later activity of the same process goes on.
- **B.** The program keeps the default, and the process ends at each new start.

A keeps the program on the screen. A session that runs when the activity starts again ends, and the phone has no file of
its document (ADR-0073 item 3), so its document is lost in both answers. **Recommended: A.**

### Q2. The changes of the configuration

- **A.** `ConfigurationChanges` names the weight of the font and the grammatical gender too.
- **B.** No change.

**Recommended: A:** two more changes keep the session and its document.

## Decision

The standing order of 2026-11-05 gives A for each question.

1. **The hint (Q1 A).** `MainActivity.OnCreate` sets `SDL_ANDROID_ALLOW_RECREATE_ACTIVITY` to 1 after `SDLActivity`
   loaded SDL, and writes one line. A later create in the same process finds the hint set, and SDL goes on.
2. **The changes (Q2 A).** `ConfigurationChanges` names `FontWeightAdjustment` and `GrammaticalGender` too.
3. **The amendment.** This record amends ADR-0073 item 3: the phone keeps its document through the background, and a new
   start of the activity starts the program again in its process; the document of a session that ran is lost.

## Consequences

**Good:** the program no longer closes when Android starts its activity again; the next runs of the emulator job show it.

**Bad:** the document of a running session is lost at such a start, as before; a file of the phone keeps it later.

**Next:** a file for the document of the phone, so that a new start opens it again.

## History

- 2026-11-06 · TASK-0247 · The record, Accepted under the standing order.
- 2026-11-06 · TASK-0247 · Shipped at v0.231.
