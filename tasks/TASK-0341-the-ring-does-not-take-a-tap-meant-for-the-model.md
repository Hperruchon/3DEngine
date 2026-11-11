---
id: 0341
title: The ring does not take a tap meant for the model
status: Active
phase: R20a
opened: 2026-11-10
depends-on: [0339]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0088, 0089, 0091, 0092, 0093, 0095, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0113, 0114, 0116, 0117, 0119, 0120]
writes:
  create:
    - tasks/TASK-0341-the-ring-does-not-take-a-tap-meant-for-the-model.md
    - docs/proposals/2026-11-10-ux-test-of-a-multi-part-project.md
    - docs/adr/0116-the-ring-takes-a-tap-only-on-its-labels.md
    - 3DEngine.App/GestureFadeTimer.cs
    - 3DEngine.ImageTests/RingTapTests.cs
  modify:
    - docs/roadmap.md
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.History.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - docs/adr/0081-the-tool-ring-of-the-redesign.md
    - docs/adr/0104-the-history-arc.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0341 — The ring does not take a tap meant for the model

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-10 the owner asked for a test of the window on a real project of many pieces. The proposal
`docs/proposals/2026-11-10-ux-test-of-a-multi-part-project.md` gives the test, seven problems and three improvements; the
owner took the three on 2026-11-10. This task is improvement A, for problem 1: with "Fond" selected, a click on the
module to its right, under the band of the ring, opened the pad of Move of "Fond", and no click selected the module.
ADR-0081 says that the band of the ring is a part of the panels for a tap.

## Goal

A tap on a body under the band of the ring selects that body. Only the icon and the label of a segment take a tap.

## Scope (in)

1. An ADR (ADR-0116, reserved on 2026-11-10) that amends ADR-0081: the part of a segment that takes a tap, and the fade
   of the ring while the view turns or zooms.
2. **The tap.** The press and the release on the icon or the label of a segment give its tool, as today. A tap on the
   rest of the band goes to the scene: it selects the body under it, or ends the selection on no body.
3. **The fade.** While an orbit, a pan or a zoom runs, the ring and the history arc fade, so the model under them can be
   read; they come back at the end of the gesture.
4. **The phone.** The icon and the label of a segment stay at least 44 by 44 pixels for a finger (ADR-0113).
5. Tests that fail on the old tap: a tap under the band on another body selects it and opens no pad; a tap on the icon
   of Move still opens the pad of Move; the layout test still passes at each size.

## Scope (out)

- The segment Dimensions, the hidden bodies (R20b) and the labels of the dimensions (R20c).
- The segments Edit sketch and Rename (R18e).
- Any change of the engine, of the contract or of the log.

## Acceptance criteria

- [ ] The scenario of improvement A passes in the window at 1280 by 720 on a copy of `Bibliotheque120.3de`: select
      "Fond", zoom three steps, click the module to the right under the band; the module is selected and no pad opens.
- [x] Each new test fails on an injection of the old tap.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

- The write set holds the files of the opening only. The session that starts the task adds the files of the work:
  `3DEngine.App/Ui/DesktopUi.Ring.cs` (the tap of a segment, near `ring.At`), the input route of the scene, the tests
  of the ring (`3DEngine.ImageTests/RingImageTests.cs`, `3DEngine.ImageTests/ToolRingTests.cs`), the ADR files, the
  ledger and the floor. Say why in the progress.
- The ring is drawn on the background draw list, and the band is a part of the panels for a tap today
  (`DesktopUi.Ring.cs`, the comment of ADR-0081 at the top of the file).
- `governed-by` must hold each ADR in force whose `affects` meets the write set. `TaskGovernanceGateTests` gives the
  list.
- Check the numbers at commit: TASK-0341 and ADR-0116 were reserved with the session of K8 and R17b on 2026-11-10 at
  `main` 6e8d1a7.

## Method

Mechanical: the tap of a segment, from its icon and its label.
Judgement:  the size of the part of a segment that takes a tap on a phone; the fade during a gesture.
Weakest:    the test of the window is a simulated mouse; a finger on the phone is not tested.

## Progress

- 2026-11-10: opened from the UX test of 2026-11-10, with the answer "All three" of the owner. The commit of the
  opening also adds the proposal and the roadmap rows R20 to R20c.
- 2026-11-11: started on the order of the owner, after R18e merged at v0.329 (c11ff56); the session of R18f was told first,
  because R18f changes the ring of a sketch in `DesktopUi.Ring.cs`. The write set adds the files of the work, as the notes
  say: the ring, the frame and the history arc of `DesktopUi`, the host (`NativeThreeDEngine.cs`, which tells the window
  that the view moves), the new timer `GestureFadeTimer.cs`, the new tests `RingTapTests.cs`, ADR-0116 with ADR-0081,
  ADR-0104 and the index, the floor and the ledger. `RingImageTests.cs` and `ToolRingTests.cs` needed no change: their taps
  are at the middle of a segment, which is on its target.
- 2026-11-11: ADR-0116 items 1 to 3; 11 new tests in `RingTapTests.cs`. Six injections each failed a named test, and each
  file came back byte for byte: the old tap (3 tests), no fade (1), no hold of the gesture (1), no minimum of 44 pixels (5),
  a target on the ring of a sketch (1), and no fade of the history arc (1). The colour of the arc under the fade has no
  test of its own; the test reads the alpha of the style when the arc is drawn. Of the 46 ADRs of `governed-by`, the
  session read ADR-0081, ADR-0104 (its history) and ADR-0119 only. The status is Active: the control of the window of the
  branch was refused on 2026-11-11, so the scenario of the window waits for the owner.
- 2026-11-11: merged `main` at v0.330 (7369ca2, R18f). The floor of `3DEngine.ImageTests` had a conflict: R18f added 6
  tests and this task 11, so the floor is 535 (567 on Linux). Run 42976032870 passed on the branch before the merge. ADR-0120 of R18f affects the
  ring and the frame, so `governed-by` holds it; it does not change the tap of the band.
