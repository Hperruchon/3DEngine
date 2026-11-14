---
id: 0127
title: The row of the anchors holds Undo, Redo and the step of Undo, and the status gives the log, the agent and the save
status: Amended
topic: Clients, rendering
date: 2026-11-12
supersedes: []
superseded-by: []
amends: ['0083', '0093', '0098']
amended-by: ['0129']
affects:
  - 3DEngine.App/StepTexts.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/ISessionHost.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.Home.cs
  - 3DEngine/HostedSession.cs
  - 3DEngine.Core/Input/InputSamples.cs
  - 3DEngine.Vulkan/SdlInput.cs
  - Engine.Api.Http/EngineHost.cs
  - 3DEngine.ImageTests/FixedStatusTests.cs
  - 3DEngine.ImageTests/FixedControlsTests.cs
  - 3DEngine.ImageTests/HiddenBodiesTests.cs
  - 3DEngine.ImageTests/StlOpenTests.cs
  - 3DEngine.Tests/SdlInputTests.cs
  - Engine.Tests/Http/EngineHostListenersTests.cs
enforced-by: 3DEngine.ImageTests/FixedStatusTests.cs
---

# ADR-0127 — The row of the anchors holds Undo, Redo and the step of Undo, and the status gives the log, the agent and the save

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The study of the gaps to the render (`docs/proposals/2026-11-10-gaps-to-the-render.md`, gap G9, slice S7) compared the
window with the render "Mix 1". The render has only Undo and Redo at the lower left, and "Undo: C3 = 75" beside them after
an apply. Its status line is "log 7 · all correct · agent connected · 127.0.0.1:5180", and its top bar ends with "mm ·
saved" or "mm · not saved". The window had "Frame" and "Home" beside Undo and Redo (ADR-0083, TASK-0259), the status line
"1 body · all correct · address" (ADR-0093 item 3), and "mm" in the top bar (ADR-0093 item 1). The owner took Q3 A on
2026-11-10: Frame and Home leave the row, and the command line and the keys keep them (roadmap row R18g).

The engine gives the name of each applied command in the event `command.applied`, and no text of a step and no undo
history to a client. The owner answered four questions on 2026-11-12, each with the recommended option:

- The window keeps the text of the steps that it knows; a command of another client has its name.
- "agent connected" shows while a client listens to the event stream of the surface.
- "saved" means that the document has its own file.
- Frame and Home leave the row on a landscape display only.

## Decision

1. **The count of the log (amends ADR-0093 item 3).** The status line starts with "log N", the count of the entries of
   the log of the Document, in place of the count of the live bodies, which the rows show. The host reads the log of
   the Document and subscribes to the events of the session in one read of the session (`EngineHost.Subscribe`), so no
   event is lost and no event counts two times. Each later `command.applied` adds one. An Undo and a Redo are entries of
   the log too (ADR-0031 item 1). A document that the host opens starts a new session, and its log holds the entries of
   its file.
2. **The step of Undo.** Right of Redo, in the middle of the height of the anchors, the window shows "Undo: " and the
   text of the step that the next Undo reverses. A text longer than a quarter of the width of the display keeps its
   start and ends with "...". With no step to undo, nothing shows. The window keeps the steps as the command bus keeps
   its history (ADR-0031 item 2): each applied command other than Undo and Redo is a step and clears the undone steps,
   an Undo moves the last step to the undone steps, and a Redo moves it back. The text of a step:
   - a value of a constraint from a pad of the window: its label in its sketch and its value, "C3 = 75", and "C1 = 45
     deg" for an angle;
   - a distance of an extrude from its pad: "distance = 30";
   - each named value of an accepted proposal of an agent, as its card names it (ADR-0110 item 3): "C3 = 75";
   - each other command, and each command of another client: the name of the command in words, "Create box".
   The host gives the window the text before it sends the command, and the next applied command with that name takes it.
   A rejected or cancelled command with that name removes it, and the host forgets each text that no command took after
   the send.
3. **The agent (amends ADR-0093 item 3).** While at least one client listens to the event stream of the surface, the
   status line has "agent connected" after the state. `EngineHost.Listeners` gives the count of those clients; a
   subscription of the host to its own session is not a client, so the window does not count itself. A host with no
   surface, as the phone, has none. A narrow room drops the address first, then "agent connected", then the line.
   "AGENT IS WAITING" beside the command line stays as it is.
4. **Frame and Home (amends ADR-0083, the desktop part of TASK-0259, and ADR-0098 item 3).** On a landscape display,
   Frame and Home leave the row of the anchors. The command line holds Frame and a new command Home. The key F, down and
   with no Control, frames, while no field, no pad and no home screen takes the keys; it follows the letter of the
   layout, as Z, Y and K do (ADR-0032 item 9, ADR-0098 item 1). F is not an intent of the intent machine, because a touch
   has no gesture for it (objective 9, `IntentParityTests`). A portrait display has no command line, so it keeps the two
   buttons on the row, and the step of Undo does not show there.
5. **The save (amends ADR-0093 item 1).** The top bar ends with "mm · saved" when the document has its own project file,
   and "mm · not saved" while it is in the folder of the unsaved documents (ADR-0030 item 4). A host with no folder of
   documents, as the phone, shows the unit alone.
6. **No contract change, no engine change.** The texts are presentation (ADR-0007): they send nothing. The surface
   library gets one read-only member, `Listeners`. The angle of a step uses `Math.Atan2` in the window only; nothing of
   it enters the log (determinism rule 1).

## Consequences

**Good:** the row of the anchors and the status line read as in the render. A person sees what Undo reverses before the
tap, how long the log is, whether an agent listens, and whether the document has its own file.

**Bad:** the window knows the text of its own value edits and of the accepted proposals only. A box from the ring, a cut
or a command of an agent with no proposal shows the name of its command. A step of the log of a file that the host opens
shows its name, not its value. A client that listens is not always an agent: a second window of a tool on the event
stream also gives "agent connected". The count of the bodies leaves the status line. A person with a mouse on a
landscape display no longer sees Frame and Home as buttons; the command line and the key F hold them. No person used the
change in the window.

## Alternatives considered

- **The text of a step in the log.** Refused: it is a change of the contract and of each saved log (Q5 of the study of
  the gaps, the author of a step, waits for S12).
- **"agent connected" only while a proposal waits.** Refused by the owner: the render shows it as a state of the session,
  and "AGENT IS WAITING" already shows a proposal.
- **"saved" after each write of the journal.** Refused: the journal writes each command at its commit, so a named file is
  always saved, and the difference that a person needs is a file of its own or not.
- **Frame and Home out of the row on each display.** Refused by the owner: a portrait display has no command line, so a
  touch person would lose them.

## History

- 2026-11-12 · TASK-0354 · The record, from slice S7 of the study of the gaps to the render and the answers of the owner of
  2026-11-12.
- 2026-11-12 · TASK-0354 · Shipped at v0.339.
- 2026-11-14 · TASK-0356 · ADR-0129 item 3 amends item 2: the window expects the text of a step for the identifier of its
  command, not for its name (finding V177 of the codebase review of 2026-11-14).
