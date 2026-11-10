---
id: 0336
title: The layout keeps each control in the display and free for a tap at each phone size and in a small window
status: Active
phase: R19
opened: 2026-11-10
depends-on: [0314]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0088, 0089, 0091, 0092, 0093, 0095, 0097, 0098, 0099, 0100, 0102, 0104, 0105, 0107, 0110, 0111, 0113]
writes:
  create:
    - tasks/TASK-0336-the-layout-at-each-phone-size-and-small-window.md
    - docs/adr/0113-the-layout-at-each-phone-size-and-small-window.md
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - docs/adr/0081-the-tool-ring-of-the-redesign.md
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/0093-the-layout-of-the-mix-has-no-panel-chrome.md
    - docs/adr/0098-the-command-line.md
    - docs/adr/README.md
    - tasks/TASK-0315-a-focus-on-one-body.md
    - tasks/TASK-0332-the-look-of-the-render.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0336 — The layout keeps each control in the display and free for a tap at each phone size and in a small window

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-10 the owner asked for work on the responsive layout of the app: the phone sizes and the small windows. A
layout that works on the desktop is not a fault. The owner put this work after the close of R17a (TASK-0314, v0.316) and
before R18a and R18b.

A probe at v0.316 drew the window at eleven sizes (360 by 640 to 1440 by 900) in four states (nothing selected, a body
selected, a face selected, a proposal). It found these faults:

1. On a portrait display of 390 pixels or less, these rows of the panel go past the right edge of the panel or of the
   display: "Sketch on XY XZ YZ Face", the button "Clear" of the filter, the toggle "Hidden too" (at 360), and in the rows
   of a selected body the name with "Rename" and the face with "Sketch on face".
2. On a portrait display the path line of ADR-0100 covers "AGENT IS WAITING", which ADR-0092 item 7 puts under the top
   bar. R17a caused it.
3. At 640 by 360 the command line overlaps the right end of the top bar and the top of the panel, and the status line
   covers "Hide" of the ring.
4. At 640 by 360 and 844 by 390 the label "Sketch on face" of the ring goes past the lower edge of the display.
5. R-0058: its exit names each display from 360 by 640. At v0.316 the probe found no tool of the ring under a panel from
   360 by 640, after ADR-0100 item 5; no test holds it yet.

The sizes 768 by 1024, 1024 by 768, 1280 by 720 and 1440 by 900 gave no fault, except the sheet of a proposal over the
panel, which ADR-0092 item 7 permits.

## Goal

Each control of the window stays in the display and takes its tap at each size of the list of ADR-0113.

## Scope (in)

1. ADR-0113: the list of the sizes, the rule "each control stays in the display and no window covers a control of
   another window or a tool of the ring", and the change for each fault.
2. A test over each size and each state of the probe. It fails when a control goes out of the display, when a window
   covers a control of another window or a tool of the ring, or when two fixed windows overlap. A control that the panel
   scrolls out of its view is not a fault.
3. The changes for faults 1 to 4.
4. R-0058 closes when the test holds at 360 by 640.
5. Pictures at 360 by 780, 390 by 844, 412 by 915, 844 by 390 and 640 by 360, for the owner.

## Scope (out)

- The look of the render (R18a) and the edges of the body (R18b).
- A change of a layout that works at 768 by 1024 or larger.
- The home screen, an active sketch, the pad and the history arc. A later slice measures them if the owner asks.
- Any change of the engine, of the contract or of the log.

## Acceptance criteria

- [ ] ADR-0113 exists and gives the list of the sizes and the rule.
- [ ] The test fails for each of faults 1 to 4 when its change is removed.
- [ ] R-0058 moves to the closed entries, with the test as its proof.
- [ ] The pictures show each control in the display; the owner gets them.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

- The probe was a test file that read the private field `_items` of `DesktopUi` through reflection. The test of this
  task reads the items through a member of `DesktopUi` instead.
- The phone host gives the window the size of `SDL_GetWindowSize`. This task did not verify the unit of that size on the
  S23 Ultra (pixels or density-independent points). The test sizes are in the units of the window.
- `governed-by` must hold each ADR in force whose `affects` meets the write set. `TaskGovernanceGateTests` gives the
  list.

## Method

Mechanical: the test from the probe; the sizes from the phones of the market and the small windows.
Judgement:  where a control goes when it does not fit: a second line, a smaller ring, or a scroll.
Weakest:    no person used the window on the S23 Ultra at these sizes yet.

## Progress

- 2026-11-10: opened from the probe at v0.316, after the order of the owner of 2026-11-10.
- 2026-11-10: started at the close of R17a (9694a87, v0.316), branch responsive-layout. ADR-0113 amends ADR-0081, ADR-0092,
  ADR-0093 and ADR-0098, so the write set adds those four files; `DesktopUi.Rows.cs` needed no change and left the write
  set. `ResponsiveLayoutTests` reads 13 sizes in 4 states. Six injections, one for each change and one for ADR-0100 item 5
  (R-0058), each failed the test, and each file came back. At 1440 by 900 a first form of item 5 moved the ring up by 72
  pixels and failed two tests of the desktop; the final form limits only the radius, so a ring that fits keeps its place.
  R-0059 holds the question of the units of the phone. The write set adds the files of TASK-0315
  and TASK-0332, whose `governed-by` must name ADR-0113.
