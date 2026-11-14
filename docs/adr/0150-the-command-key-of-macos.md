---
id: 0150
title: On macOS the Command key gives the modifier of the shortcuts, and the command line shows "Cmd K"
status: Accepted
topic: Clients, input
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0108']
amended-by: []
affects:
  - 3DEngine.Vulkan/SdlInput.cs
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.Tests/SdlInputTests.cs
  - 3DEngine.ImageTests/CommandKeyTests.cs
enforced-by: 3DEngine.ImageTests/CommandKeyTests.cs
---

# ADR-0150 — On macOS the Command key gives the modifier of the shortcuts, and the command line shows "Cmd K"

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0108 item 4 gives the command line the keys "Ctrl K or Cmd K", as the render and the designer brief do. The input of the
window has no modifier for the Command key of macOS: `KeyModifiers` holds Shift, Control and Alt. So the field opens with
Control and K on each platform and shows "Ctrl K" (register entry R-0060). On macOS the shortcuts of an application use the
Command key.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The Command key (amends ADR-0108 item 4).** On macOS the Command key gives the modifier `Control` to the keys after it,
   as the key Control does on the other platforms. So Command and K open the command line, and each other shortcut of the
   window that reads Control reads the Command key too. The key Control of macOS keeps its effect. The other platforms leave
   the key of the system out, as before. `KeyModifiers` gets no new value: the modifier is the one of the shortcuts.
2. **The text.** The command line shows "Cmd K" on macOS and "Ctrl K" on the other platforms.
3. **No contract change.** The input is presentation of the window.

## Consequences

**Good:** a person on macOS uses the key of the platform, and the window says so.

**Bad:** on macOS a shortcut cannot tell the Command key from the key Control.

## Alternatives considered

- **A new value `Command` of `KeyModifiers`.** Refused for now: each reader of Control would read it too, and no shortcut
  needs to tell them apart.

## History

- 2026-11-14 · TASK-0385 · The record, from register entry R-0060, under the order of the owner of 2026-11-14.
