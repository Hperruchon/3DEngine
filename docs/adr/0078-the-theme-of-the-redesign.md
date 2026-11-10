---
id: 0078
title: The desktop and the phone take the theme "Proposed (harmonized)" of the redesign
status: Amended
topic: Clients, rendering
date: 2026-11-06
supersedes: []
superseded-by: []
amends: []
amended-by: ['0091', '0108']
affects:
  - 3DEngine.App/Ui/Theme.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Overlay.cs
  - 3DEngine.App/SceneRenderer.cs
enforced-by: 3DEngine.ImageTests/ThemeTests.cs
---

# ADR-0078 — The desktop and the phone take the theme "Proposed (harmonized)" of the redesign

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the redesign of the desktop on 2026-11-06 (evening), from the designer brief of 2026-11-05 (the "mix"
direction). Its first slice is "the theme Proposed (harmonized) and the fonts Instrument Sans and JetBrains Mono (OFL),
with the amendment of ADR-0027 item 6. No change of layout."

The design system of the brief (`tokens.json`, the theme `proposed`) gives the theme as a set of tokens. Each token that
the theme does not give takes the value of the first theme, the dark style of Dear ImGui that the code has today:

- the panels: `chrome-window` `#101419` at alpha 0.94, `chrome-popup` `#171b20`, a solid `chrome-border` `#6f7c8c`,
  `chrome-title` `#0a0e12`, `chrome-scrollbar-grab` `#464e58` and `chrome-text-disabled` `#8a939f`;
- the scene: `viewport-clear` `#3f4955`, a cool neutral, in place of the green key `#00ff00` of the first image tests;
- the marks of the viewport: `sketch-other` `#a3acb8`, `sketch-reference` `#b99056`, `sketch-snap` `#ed6cd5` (in place of
  cyan), and a `sketch-casing` `#0b1016` of `stroke-casing` 2 pixels around each mark, which the code did not draw.

The fonts are files that the repository does not hold. A download needs the permission of the owner, which the session
asked for on 2026-11-06; until it comes, this slice holds the colours only.

## Decision

1. **The colours.** `Theme` holds the tokens of the theme that differ from the dark style, and the panel applies them at
   its start. The clear of the scene is `viewport-clear`, as bytes, so a test compares the bytes of a picture. The marks
   of a sketch take `sketch-other`, `sketch-reference` and `sketch-snap`. No size and no place of a control changes.
2. **The casing.** Each line of a sketch, each point, each ring, the mark of a snap, the outline of a selected face, a
   selected edge and each line of the dimensions lie over a casing in `sketch-casing`, wider by `stroke-casing` on each
   side, so a mark holds its contrast on each face and on the clear. A label has no casing.
3. **The fonts.** Instrument Sans and JetBrains Mono, with the amendment of ADR-0027 item 6, come in the next slice,
   after the owner allows the download of their files.

## Consequences

**Good:** the desktop and the phone take the cool structure of the brief with no change of layout; a mark of a sketch
reads on each face.

**Bad:** the clear is close to the darkest face of a body (`body-shade-min` `#313a47`); a picture test that tells the
clear from a body compares exact bytes, so it still holds.

**Next:** the fonts; then the layout of the redesign, slice by slice.

## History

- 2026-11-06 · TASK-0246 · The record, with the colours and the casing; the fonts wait for the permission of the owner.
- 2026-11-06 · TASK-0246 · Shipped at v0.232, with the colours and the casing.
- 2026-11-08 · TASK-0292 · ADR-0091 amends item 3: the fonts came, after the owner allowed the download on 2026-11-08.
- 2026-11-10 · TASK-0332 · ADR-0108 item 1 amends item 1: the clear of the scene is `#070d14`, the almost black ground
  of the render (Q2 A), and the windows take `#0b1219`.
