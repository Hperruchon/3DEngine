---
id: 0419
title: The tools of the part on the ring, a file menu, and a structure view in the place of the panel
status: Done
phase: R31a
opened: 2026-11-19
depends-on: [0418]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0088, 0089, 0091, 0092, 0093, 0095, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0113, 0114, 0116, 0117, 0119, 0120, 0122, 0123, 0124, 0125, 0127, 0129, 0130, 0131, 0132, 0137, 0138, 0139, 0140, 0145, 0152, 0163, 0166, 0167, 0168, 0170, 0171, 0172, 0174]
writes:
  create:
    - tasks/TASK-0419-the-tools-of-the-part-on-the-ring-and-a-structure-view.md
    - docs/adr/0174-the-tools-of-the-part-on-the-ring-and-a-structure-view.md
    - 3DEngine.App/Ui/DesktopUi.Part.cs
    - 3DEngine.ImageTests/PartRingTests.cs
  modify:
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.Orbit.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Views.cs
    - 3DEngine.ImageTests/BodyRowsTests.cs
    - 3DEngine.ImageTests/CutUiTests.cs
    - 3DEngine.ImageTests/DesktopDocumentTests.cs
    - 3DEngine.ImageTests/DesktopFormsTests.cs
    - 3DEngine.ImageTests/DesktopUiTests.cs
    - 3DEngine.ImageTests/DimensionImageTests.cs
    - 3DEngine.ImageTests/DimensionOverlayTests.cs
    - 3DEngine.ImageTests/DocumentPathTests.cs
    - 3DEngine.ImageTests/FaceArmTests.cs
    - 3DEngine.ImageTests/FixedControlsTests.cs
    - 3DEngine.ImageTests/FlatViewLinesTests.cs
    - 3DEngine.ImageTests/FocusClickTests.cs
    - 3DEngine.ImageTests/NumericPadUiTests.cs
    - 3DEngine.ImageTests/PanelImageTests.cs
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
    - 3DEngine.ImageTests/SketchWindowUiTests.cs
    - docs/adr/0056-dimension-overlay.md
    - docs/adr/0076-the-cylinder.md
    - docs/adr/0093-the-layout-of-the-mix-has-no-panel-chrome.md
    - docs/adr/0100-a-path-line-from-the-document-to-the-selection.md
    - docs/adr/0102-a-project-in-any-folder.md
    - docs/adr/0081-the-tool-ring-of-the-redesign.md
    - docs/adr/0172-the-layout-of-small-windows-and-of-each-state.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.Core/**
    - 3DEngine.Android/**
---

# TASK-0419 — The tools of the part on the ring, a file menu, and a structure view in the place of the panel

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-18 the owner said that the panel at the left takes too much space. A study measured it at 29 % of a display of
1280 by 720 and 64 % of a phone, and gave three options. The owner chose "B plus C" (the tools of the part on the ring, and
a structure view in the place of the panel) and density B (rows of 30 pixels for a mouse or a pen). On 2026-11-19 the owner
gave the order "start the left panel task, add the roadmap row". This task is the first slice, R31a (ADR-0174).

## Goal

With no selection, the part has the display less one folded row, and each control of the panel is still one or two taps
away: on the ring of the part, in the file menu, in the net of the views or in the structure view.

## Scope (in)

1. The ring of the part and its second level (ADR-0174 items 1 and 2).
2. The structure view in the place of the panel (item 3).
3. The file menu on the file name (item 4).
4. "Dimensions" and "Hidden too" in the net of the views (item 5).
5. The tests of item 7.

## Scope (out)

- The rows of 30 pixels and the setting "Compact" or "For touch" (R31b, with its own record).
- The tabs of an assembly (joints, requirements, checks) in the structure view.

## Acceptance criteria

- [x] `PartRingTests` holds items 1 to 5 of ADR-0174, and an injection of each fails a test.
- [x] The layout test of ADR-0172 item 5 passes in its states, with the open structure view and the open file menu.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch.

## Outcome

Status: Done · commit (the merge of the branch left-panel) · v0.401

## Method

Mechanical: the controls of the panel move to the ring of the part, the file menu and the net of the views with the same
actions.
Judgement:  the centre of the ring of the part (the box of the visible bodies from the host); the file name as the opener of
            the menu only with no selection; the two switches on the row of "Lines" at a least scale of 0.6; the ground of
            the structure view in both states; no ring of the part on the paper of a flat view or under the open net.
Weakest:    the host centre of the ring of the part has no test with a real camera; the tests give the centre by hand, and
            the pictures of the real window showed the ring around the part.

## Progress

- 2026-11-19: opened after the merge of P0.169 (v0.400), on the order of the owner "start the left panel task, add the
  roadmap row". The roadmap has R31a and R31b; ADR-0174 records the slice.
- 2026-11-19: the code, `PartRingTests` and 16 older test files that used the controls of the panel. The ring of the part
  moved when the structure view opened, because its centre followed the free part; the host now gives the centre of the
  box of the visible bodies, and the fallback reads the fixed controls only. A picture of a flat view showed the ring on the
  paper, where its labels did not read, so the ring of the part does not show there. The test of the dashes of a hidden
  line counted the paper in a block of 3 by 3, which shortens each gap by 2 pixels; it now looks across the line only.
- 2026-11-19: 29 injections, each restored from its saved text; each failed a named test. A first form of the test of the
  switches tapped the net in its first frame, where Dear ImGui takes no tap, so the injection "Hidden too enabled" passed;
  the test now waits a frame. Pictures of the real window at 1280 by 720, 412 by 915 and 360 by 640 showed two faults that
  no test held: the file menu had no ground, so its entries lay on the rows of the structure view, and on a portrait
  display the open net of the views covered the structure view (also before R31a). The menu has an opaque ground now, with
  a picture test; the open net takes the place of the structure view and of the ring of the part, and the layout test has
  a state for it.
- 2026-11-20: run 44631948493 passed on the branch on each of its seven jobs (be9e222). A clean build gave 0 warnings, the
  phone project built, and dotnet test passed 2916 tests. Closed at v0.401; the merge waits for the owner.
