---
id: 0098
title: The command line at the upper right runs the commands of the state with Control and K
status: Accepted
topic: Clients, rendering
date: 2026-11-08
supersedes: []
superseded-by: []
amends: ['0032', '0084']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Rows.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.Core/Input/InputSamples.cs
  - 3DEngine.Vulkan/SdlInput.cs
  - 3DEngine.Tests/SdlInputTests.cs
  - 3DEngine.ImageTests/CommandLineTests.cs
  - 3DEngine.ImageTests/BodyRowsTests.cs
enforced-by: 3DEngine.ImageTests/CommandLineTests.cs
---

# ADR-0098 — The command line at the upper right runs the commands of the state with Control and K

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The fifth slice that the owner chose on 2026-11-08 is the command line. In the render "mix", a field "Type a command or a
value" is at the upper right, and the brief says: "Only a few controls stay in fixed places: the name, Undo, Redo and the
command line (Ctrl K or Cmd K). The command line gives a keyboard path to each action." The interaction model of the brief
gives "Ctrl K or Cmd K search" as the command menu of the keyboard. ADR-0032 says that each number of the model goes
through the pad and its evaluator.

### Q1. What the command line runs

- **A.** The commands of the state: the tools of the active sketch, or the list "New", the sketch on a plane, the tools of
  the selected body, Show all, and Undo, Redo and Frame. A command that needs a number opens its pad.
- **B.** A value too: a number typed in the field sets the selected value.

B puts a second path for a number beside the pad, which ADR-0032 does not permit with no new decision. **Recommended: A.**
The hint says "Type a command".

### Q2. Where it shows

- **A.** At the upper right of a landscape display only.
- **B.** On each display.

A phone has no keyboard of its own, and a field of 300 pixels covers the top bar at 390. **Recommended: A.**

### Q3. Command and K on macOS

- **A.** Control and K only in this slice. The input of the host gives no Command key today.
- **B.** A new modifier for the Command key.

**Recommended: A.**

## Decision

The owner chose the slice; the recommendations answer Q1 to Q3.

1. **The key (Q3 A).** `InputKey.K` follows the letter of the layout, as Z and Y do (ADR-0032 item 9). Control and K, down,
   give the field the keys, except while a pad is open.
2. **The field (Q2 A).** On a landscape display, a field 300 pixels wide is 16 pixels inside the upper right edge, with the
   hint "Type a command". Escape empties it and gives the keys back to the scene.
3. **The list (Q1 A).** A text lists at most eight commands of the state whose name holds it, with no regard to the case; a
   name that starts with the text comes first. The arrows choose; Enter, or a tap on a listed command, runs it as its
   button or its segment does. A command that needs a number opens its pad, so the pad and its evaluator stay the only path
   of a number (ADR-0032).
4. **The rows (amends ADR-0084 item 1).** The rows of a selected body start 8 pixels under the field.
5. **The release of a key (amends ADR-0032 item 6).** While the pad is open it takes each key and each text, and the
   release of a key also reaches the library. A command opens its pad in the frame of the press of Enter, so without this
   the library held Enter as down and the next Enter of a field gave no press.

## Consequences

**Good:** each command of the state has a keyboard path, as the brief asks, and the field is where the render puts it.

**Bad:** no value can be typed in the field, and a Mac has no Command and K in this slice. A phone has no command line.

## History

- 2026-11-08 · TASK-0312 · The record, with the slice.
- 2026-11-08 · TASK-0312 · Shipped at v0.297.
