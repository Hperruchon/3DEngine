---
id: 0163
title: The desktop window resizes and maximizes, down to a minimum size, and the layout follows each step of a drag
status: Amended
topic: Clients, layout
date: 2026-11-18
supersedes: []
superseded-by: []
amends: ['0113']
amended-by: ['0172']
affects:
  - 3DEngine/DesktopEngineHost.cs
  - 3DEngine.Vulkan/Window.cs
  - 3DEngine.Vulkan/Presenter.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Image.cs
  - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
enforced-by: 3DEngine.ImageTests/ResponsiveLayoutTests.cs
---

# ADR-0163 — The desktop window resizes and maximizes, down to a minimum size, and the layout follows each step of a drag

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-18 the owner asked why the desktop window has a fixed size, when the responsive work (R19, ADR-0113) was done
to support each size. The host made the window with `new Window("3DEngine Desktop", 1280, 720)` and no flags, and
`Window` adds `SDL_WINDOW_RESIZABLE` only for `WindowFlags.Resizable`. So the window had no resize border and no maximize
button. ADR-0113 tested the layout at thirteen fixed sizes, not during a resize.

A probe of TASK-0404 found four more facts:

1. A grid of sizes from 360 by 360 found faults of the layout in small windows that are almost square: each width less
   than 750 gives faults at some height less than 570. For example, at 360 by 360 the path line is under the command line.
   From a height of 570, each width from 360 passes; from a width of 750, each height from 360 passes.
2. At each width, some heights gave one fault: the bottom of the panel cut a row in two, and the middle of the row was
   under Undo and Redo. The panel clips that row, so a person sees its upper half, and a tap on that half reaches it.
   The test counted only a row with its top below the panel as "scrolled out of view".
3. On Windows a drag of the border runs a modal loop of the system, and `SDL_PollEvent` does not return until the drag
   ends. The window showed its last frame, cut at the new edge, until the person released the button.
4. `NativeThreeDEngine.Render` read `Swapchain.Extent` for the aspect of the camera and the scale of the window before
   `Presenter.RenderFrame` made the swapchain again. So the first frame after each change of size used the old aspect.
   The window of the images kept its first place (`ImGuiCond.FirstUseEver`), so a smaller window cut it.

## Decision

1. **Resize and maximize.** The desktop host makes its window with `WindowFlags.Resizable`. A person can drag each
   border, maximize, minimize and restore. The window starts at 1280 by 720, centred, as before.
2. **The minimum size.** The window is at least 360 by 560 in the units of the window (`SDL_SetWindowMinimumSize`).
   `DesktopUi.MinimumWindowWidth` and `DesktopUi.MinimumWindowHeight` hold the two numbers, and the host and the test
   read them. The width is the smallest width of ADR-0113 item 1, so a narrow window beside a terminal stays possible.
   Until TASK-0418 the height was 600: the edge of fact 1 (570) and a margin of 30 pixels. ADR-0172 fixed the faults
   below 570 in each state, and the owner chose 560 (ADR-0172 item 5). The other choice was 750 by 360, which keeps a
   short wide window and refuses a narrow one.
3. **Each step of a drag draws a frame.** The host gives its frame to `Window.FollowLiveResize`, which adds an event
   watch of SDL. SDL sends `SDL_EVENT_WINDOW_EXPOSED` with `data1 = 1` (a live resize) from the modal loop, and the watch
   runs the same frame as the loop of the host: Update, then Render. A failure in that frame stays for the loop, which
   throws it, because an exception must not leave a callback of SDL. Before it reads `Swapchain.Extent`, each frame
   calls `Presenter.Follow`, which makes the swapchain again when the surface has a new extent.
4. **A test of each step of a resize (amends ADR-0113 item 1).** The sizes of ADR-0113 item 1 include the minimum
   window. A second test applies the rule at each step of 8 pixels on three lines of a resize: the minimum height from
   the minimum width to 2560, the minimum width up to a height of 1440, and the width 1920 up to 1440. A control with its
   top in its panel and its middle at or below the bottom of the panel is scrolled out of view (fact 2).
5. **The images stay inside the display.** The images are a fixed part of the layout, in the place of the panel or of
   the rows (ADR-0172 item 4), so they stay inside the display at each size. Until TASK-0418 the person placed the
   window, and a smaller display moved it back to 16 pixels inside its edge.

## Consequences

**Good:** the window resizes, maximizes and follows a drag with a correct layout at each step. A run of TASK-0404 on
Windows dragged the corner in 40 steps from 1280 by 720: the swapchain was made again 40 times, and a picture with the
button still down showed the layout of 980 by 620. The test covers 486 sizes in four states in about 8 seconds.

**Bad:** a window less than 750 wide cannot be less than 600 high, so a landscape phone size such as 640 by 360 is not
a size of the desktop window. The tests still hold each phone size. On a narrow display, the window of the images can
cover the right part of the first rows of the panel; until TASK-0404 it was outside the display there. The
test of item 4 has no window of the images. A frame during a drag runs inside `SDL_PollEvent`, so it sees the input
events of the queue only after the drag.

## History

- 2026-11-18 · TASK-0404 · The record, from the question of the owner of 2026-11-18 and the probe of TASK-0404.
- 2026-11-18 · TASK-0418 · ADR-0172 items 4 and 5 amend items 2 and 5: the minimum size is 360 by 560, and the images are
  a fixed part of the layout. The title no longer gives the number.
- 2026-11-21 · TASK-0441 · Item 4: the test of the resize is one test for each state. The states of ADR-0172 to ADR-0178
  made one test of 496 sizes in 15 states, about 41 seconds on the computer of the owner and more than the hang limit of 2
  minutes on windows-latest (runs 44758589835 and 44908442870). One test checks at most 1,000 layouts (ADR-0173 item 4).
