---
id: 0091
title: The interface draws with Instrument Sans, and a value on an edge with JetBrains Mono
status: Accepted
topic: Clients, rendering
date: 2026-11-08
supersedes: []
superseded-by: []
amends: ['0027', '0078']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Values.cs
  - 3DEngine.App/3DEngine.App.csproj
enforced-by: 3DEngine.ImageTests/FontTests.cs
---

# ADR-0091 — The interface draws with Instrument Sans, and a value on an edge with JetBrains Mono

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The first slice of the redesign (the owner, 2026-11-06, evening) names "the theme Proposed (harmonized) and the fonts
Instrument Sans and JetBrains Mono (OFL), with the amendment of ADR-0027 item 6". ADR-0078 shipped the theme and put the
fonts in a next slice, after the owner allowed the download of their files (ADR-0078 item 3). ADR-0027 item 6 says that the
font is the default bitmap font of Dear ImGui at a larger size, and that no other font file enters the repository.

On 2026-11-08 the owner allowed the download. The files come from `github.com/google/fonts`: `InstrumentSans[wdth,wght].ttf`
(commit `2fc9491`) and `JetBrainsMono[wght].ttf` (commit `2e05c1c`), each with its `OFL.txt`. Both are fonts with
variations; Dear ImGui 1.91.6 draws the default instance of each, the weight 400.

### Q1. Where each font goes

- **A.** Instrument Sans for each text of the interface; JetBrains Mono for the values on the edges (ADR-0082), which are
  numbers that a person compares. The rows and the windows keep Instrument Sans.
- **B.** JetBrains Mono for each number of the interface, in the rows and the windows too.

A changes one place of the drawing for the second font. **Recommended: A.**

## Decision

The owner allowed the download; the standing order of 2026-11-05 gives A for Q1.

1. **The files (amends ADR-0027 item 6).** `3DEngine.App/Assets/Fonts` holds `InstrumentSans.ttf` and `JetBrainsMono.ttf`,
   with their licence texts, and the assembly embeds the two fonts, so the desktop and the phone draw with the same fonts.
   `THIRD-PARTY-NOTICES.md`, section 6, gives the sources and the checksums.
2. **The atlas.** The first font of the atlas is Instrument Sans at `DesktopUi.FontSize`; the second is JetBrains Mono at
   the same size. The atlas does not own the bytes, which the interface frees after its context.
3. **The value on an edge (Q1 A, amends ADR-0078 item 3).** A chip of ADR-0082 measures and draws its text with JetBrains
   Mono. Each other text uses Instrument Sans.
4. **No change of the sizes.** Each control keeps its size of ADR-0027 item 6; only the widths of the texts change.

## Consequences

**Good:** the text is a vector font at each size, as the brief draws it, and a value has one width for each digit.

**Bad:** the program is about 380 KB larger. The fonts draw at the weight 400 only, because the binding does not choose an
instance of a variable font.

## History

- 2026-11-08 · TASK-0292 · The record, with the download that the owner allowed on 2026-11-08 and Q1 A under the standing
  order.
- 2026-11-08 · TASK-0292 · Shipped at v0.278.
