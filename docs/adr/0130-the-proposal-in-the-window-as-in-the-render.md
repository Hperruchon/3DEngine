---
id: 0130
title: A proposal shows its values in the rows, a pending node on the history arc, a glow on the edges and Undo after an accept
status: Accepted
topic: Clients, rendering
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0092', '0104', '0110']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.App/Ui/DesktopUi.Rows.cs
  - 3DEngine.App/Ui/DesktopUi.History.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.ImageTests/ProposalWindowTests.cs
enforced-by: 3DEngine.ImageTests/ProposalWindowTests.cs
---

# ADR-0130 — A proposal shows its values in the rows, a pending node on the history arc, a glow on the edges and Undo after an accept

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Slice S9 of the study of the gaps to the render (`docs/proposals/2026-11-10-gaps-to-the-render.md`, gap G11) is roadmap row
R18i. The render shows a proposal of an agent with the old value crossed out and the new value in amber, in the rows and on
the edge; a pending node "C3 to 75 ?" on the history arc; a glow on the edge of the screen; and after Accept the line
"Applied · Bracket rebuilt · Undo". The window had the arc of the proposal with Reject and Accept, the lines of its commands,
"AGENT IS WAITING", and each chip of a proposed value as "60.000 → 75.000 mm" (ADR-0110 item 2). After an accept the status
line said "Proposal accepted: 1 command applied, 1 step of undo" (ADR-0092 item 6).

The study keeps the preview of the new shape (S11) and the author of a step (S12) for later. On 2026-11-14 the owner ordered
work alone until Sunday, with the recommended option for each question.

## Decision

1. **The rows (amends ADR-0110 item 2).** While the window shows a proposal, each row of a value of the selected body whose
   key is the key of a proposed value gives the value now in the quiet colour, crossed out by a line, and at its right the
   proposed value in amber, with three decimals and the unit ("60.000 mm 75.000 mm"). A row that the proposal does not change
   stays as it is. The chip keeps its form of ADR-0110 item 2.
2. **The pending node (amends ADR-0104).** While the window shows a proposal with a named value, the history arc has a node
   one step above its newest node: a ring of amber with a glow, and at its left the text "C1 to 75 ?" in amber, from the
   first named value of the proposal. It takes no tap; Accept and Reject stay on the arc of the proposal. A proposal with no
   named value has no pending node.
3. **The glow (amends ADR-0092 item 7).** While a proposal waits, four frames of amber go along the edges of the display,
   inside the safe area, from 2 pixels at an alpha of 0.9 at the edge to 20 pixels at an alpha of 0.06 inside. They take no
   tap, on a landscape and on a portrait display.
4. **After an accept (amends ADR-0092 item 6).** The line over the status line says "Applied · Bracket rebuilt" with the name
   of the body that was selected at the accept, or "Applied · 2 commands" with no selected body, and then the button "Undo"
   in the accent, as high as a target of 44 pixels. A tap on it gives the Undo of the anchors, one step, and the line goes.
   A reject and a failed accept keep their texts of ADR-0092 item 6, with no button.
5. **No contract change, no engine change.** Each part is presentation (ADR-0007) from the card of the proposal that the
   host reads from the queue, with no read of the session.

## Consequences

**Good:** the person sees which value changes, where the step goes in the history, and that the window waits, before the
tap on Accept; after it, one tap undoes the change.

**Bad:** "Undo" undoes one step; a proposal of several commands needs one tap for each. "rebuilt" names the selected body,
also when the proposal changed another body. The glow is drawn in each frame while a proposal waits. No person used the
change in the window.

## Alternatives considered

- **Undo of the whole proposal in one tap.** Refused for now: ADR-0092 Q4 A gives one step of undo for each command, and an
  undo of a group of steps is a change of the history of the engine.
- **The pending node as a target of Accept.** Refused: the arc of the proposal holds Accept and Reject, and a second target
  of Accept is a second place to learn.

## History

- 2026-11-14 · TASK-0358 · The record, from slice S9 of the study of the gaps to the render, with the recommended option
  under the order of the owner of 2026-11-14.
- 2026-11-14 · TASK-0358 · Shipped at v0.343.
