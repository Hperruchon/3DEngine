---
id: 0332
title: The look of the render, a dark ground and outlined controls
status: Ready
phase: R18a
opened: 2026-11-10
depends-on: [0330]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0088, 0089, 0091, 0092, 0093, 0095, 0097, 0098, 0099, 0102, 0104, 0105, 0107, 0110]
writes:
  create:
    - tasks/TASK-0332-the-look-of-the-render.md
    - docs/adr/0108-the-look-of-the-render.md
    - docs/proposals/2026-11-10-gaps-to-the-render.md
  modify:
    - 3DEngine.App/Ui/Theme.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Orbit.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.ImageTests/ThemeTests.cs
    - 3DEngine.ImageTests/MixImageTests.cs
    - 3DEngine.ImageTests/BodyRowsImageTests.cs
    - 3DEngine.ImageTests/EdgeValueImageTests.cs
    - 3DEngine.ImageTests/EdgeValueTests.cs
    - 3DEngine.ImageTests/FirstDemonstrationImageTests.cs
    - 3DEngine.ImageTests/HomeImageTests.cs
    - 3DEngine.ImageTests/ImageQuadImageTests.cs
    - 3DEngine.ImageTests/NumericPadUiTests.cs
    - 3DEngine.ImageTests/PlacedImageImageTests.cs
    - 3DEngine.ImageTests/RingImageTests.cs
    - 3DEngine.ImageTests/SketchLiftImageTests.cs
    - 3DEngine.ImageTests/SketchValueTests.cs
    - 3DEngine.ImageTests/SketchWindowUiTests.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - 3DEngine.ImageTests/CommandLineTests.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - docs/adr/0078-the-theme-of-the-redesign.md
    - docs/adr/0081-the-tool-ring-of-the-redesign.md
    - docs/adr/0095-the-look-of-the-orbit-ring.md
    - docs/adr/0098-the-command-line.md
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0332 — The look of the render, a dark ground and outlined controls

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-10 the owner asked if the window is close to the render "mix" of the designer brief. The proposal
`docs/proposals/2026-11-10-gaps-to-the-render.md` compares the renders Mix 1 to Mix 4 with the window at v0.311: the
layout is as in the render, and the look is not. The owner accepted each recommended answer on 2026-11-10. This task is
slice S1 (gap G1, part of G9, the look of G10). Q2 A: the ground of the render. R18b (TASK-0333, the edges of the body) can
run at the same time: it changes the renderer, and this task changes the window.

## Goal

The window has the ground and the controls of the render: an almost black ground, and thin outlined controls with no fill.

## Scope (in)

1. ADR-0108, from slice S1 and the answer Q2 A. It amends the theme (ADR-0078) and the forms of the ring (ADR-0081,
   ADR-0095), of the command line (ADR-0098) and of the message of an agent (ADR-0092).
2. **The ground.** The clear colour of the render, with a soft light at the middle of the scene if the cost is low. Each
   colour of the theme keeps a contrast of at least 4.5:1 for text and 3:1 for large text and marks on the new ground.
3. **The controls.** The segments of the ring, the chips of the values, the command line, the anchors and the buttons
   have an outline and no fill; the hover and the press give a light fill. The selected tool keeps a clear mark.
4. **The command line.** An outlined field with a search mark, the text "Type a command or a value", and "Ctrl K" at its
   right ("Cmd K" on macOS). The typed value itself is slice S8.
5. **The states.** "correct" in green and "failed" with its code in red, in the rows and the status line, as the render.
6. **The agent.** "AGENT IS WAITING", with its dot, at the left of the command line, and not in the top bar.
7. Tests and pictures: each image test that reads the clear colour reads the new one; the three pictures of
   `MixImageTests` at 1440 by 900 beside the renders Mix 1 to Mix 3.

## Scope (out)

- The edges and the fill of the body (R18b), the values and their dimension lines (R18c), the rows (R18d), the ring
  segments Edit sketch and Rename and the history arc (R18e).
- Frame and Home out of the row of the anchors (R18g).
- Any change of the engine, of the contract or of the log.

## Acceptance criteria

- [ ] ADR-0108 exists and records the answer Q2 A of 2026-11-10. The amended ADRs show the amendment.
- [ ] A test holds the contrast of each colour of the theme on the new ground.
- [ ] The pictures at 1440 by 900 show the dark ground and the outlined controls; the owner gets them beside the render.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge (Q7 A: merge after green CI, with the pictures).

## Notes for the implementer

- `3DEngine.App/Ui/Theme.cs` holds the clear colour and the colours of the style. About thirteen image tests read the
  clear; the write set names them.
- The renders are in the designer brief (`https://claude.ai/artifact/4dPFQPdAPF39BZQSqbgYFD`, section "UX examples").
- Check the numbers at commit: TASK-0332 and ADR-0108 were free on each branch on 2026-11-10 at `main` 0125894.
- `governed-by` must hold each ADR in force whose `affects` meets the write set. `TaskGovernanceGateTests` gives the
  list. Add a file to the write set when the work needs it, and say why in the progress.

## Method

Mechanical: the colours from the render and the design system; the outlines from the draw list.
Judgement:  the contrast of each colour on the new ground; the mark of the selected tool with no fill.
Weakest:    the render is a picture; its exact colours come from a look, not from tokens.

## Progress

- 2026-11-10: opened from the proposal, with the answers of the owner.
