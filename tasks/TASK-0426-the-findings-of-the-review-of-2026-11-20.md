---
id: 0426
title: The medium and low findings of the codebase review of 2026-11-20
status: Done
phase: P0.171
opened: 2026-11-20
depends-on: [0425]
governed-by: [0004, 0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0088, 0089, 0091, 0092, 0093, 0095, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0107, 0108, 0110, 0111, 0113, 0114, 0115, 0116, 0117, 0119, 0120, 0122, 0123, 0124, 0125, 0127, 0128, 0129, 0130, 0131, 0132, 0133, 0134, 0135, 0136, 0138, 0139, 0140, 0141, 0142, 0143, 0144, 0145, 0147, 0152, 0163, 0164, 0165, 0166, 0167, 0168, 0169, 0170, 0171, 0172, 0173, 0174, 0175, 0176, 0177, 0178]
writes:
  create:
    - tasks/TASK-0426-the-findings-of-the-review-of-2026-11-20.md
    - Engine.Tests/Pipeline/TestStepTests.cs
  modify:
    - Engine.Core/Drawing/SectionCut.cs
    - Engine.Core/Drawing/ViewEdges.cs
    - 3DEngine.App/SheetActions.cs
    - 3DEngine.App/SheetExports.cs
    - 3DEngine.App/SheetPage.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Pad.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.Part.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.SheetPage.cs
    - Engine.Tests/Drawing/SheetSectionTests.cs
    - Engine.Tests/Http/HttpConcurrencyTests.cs
    - 3DEngine.ImageTests/BodyRowsTests.cs
    - 3DEngine.ImageTests/PartRingTests.cs
    - 3DEngine.ImageTests/ProposedSheetTests.cs
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
    - 3DEngine.ImageTests/RowDensityTests.cs
    - 3DEngine.ImageTests/SheetExportTests.cs
    - 3DEngine.ImageTests/SheetPageTests.cs
    - 3DEngine.ImageTests/SheetValueTests.cs
    - .github/workflows/ci.yml
    - docs/adr/0173-a-lost-runner-gets-one-automatic-re-run.md
    - docs/adr/0177-the-proposal-of-the-agent-on-the-sheet-page.md
    - docs/adr/0178-show-hidden-on-the-ring-of-the-part-and-the-path-line.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0426 — The medium and low findings of the codebase review of 2026-11-20

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-20 (`docs/reviews/2026-11-20-codebase-review.md`, TASK-0425) found six medium and ten low
findings and no critical or high one. By `docs/templates.md`, section 7, they come after the next product phase, R20d.

## Goal

Each finding has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: T183 (the concurrency test of the HTTP host decides its result with a time limit), V210 (a race of one frame in the
   host brings back V206), V211 (the file menu can open behind the structure view), V212 (a pad of a sketch on a phone measures
   its room at one top and opens at another), V221 (on the sheet page the paper covers the arc of a proposal, and a tap on
   Accept over the paper also acts on the paper), V222 (on the sheet page the arc hides the line of each command).
2. Low: E226, T184, V213 to V220.
3. The answers Q1, Q2, Q3 and Q8 of the review, each option A under the order of the owner of 2026-11-20.

## Scope (out)

- E222 beyond the cache: the cut of each polygon by the plane needs a task of the kernel.
- The write set holds only this file now. The task adds the files of each correction, and the ADRs that govern them, when it
  starts (`docs/templates.md`, section 2).

## Decisions

The owner gave a standing order on 2026-11-20: take the recommended option A of each question of the review. This task records
the answers to the four questions of its findings.

- Q1 (V218) A: "Show hidden" on the ring of the part sends one command for each hidden body, as the button "Show all" does.
  ADR-0178 item 1 says so. No engine change follows.
- Q2 (V219) A: on the path line of a phone the part is "N hidden", and the file name is cut but keeps its chevron. A tap on the
  part opens the list under the path line. ADR-0178 item 2 says so, and its `affects` field names `DesktopUi.Part.cs`.
- Q3 (V222) A: on the sheet page the arc keeps the line of each command that the page does not show. ADR-0177 item 5 says so.
- Q8 (T183) A: the concurrency test finds a deadlock when no operation ends for 60 seconds; the time of the work is not a
  result, as ADR-0173 item 4 says for a time limit. ADR-0173 item 4 says so for `HttpConcurrencyTests`.

ADR-0178 has no code yet; R20e (TASK-0424) implements it. This task changes the text of items 1 and 2 only.

## Acceptance criteria

- [x] Each finding has a test that fails before its correction, or a register entry.
- [x] Each injection of a correction makes a named test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Done · commit (the merge of the branch p0171-review-findings) · v0.410

Fourteen findings have a correction and a test that failed on the code of v0.409. V218 and V219 are corrections of the text
of ADR-0178, which has no code yet; the test of that code is `ShowHiddenTests` of TASK-0424. No register entry was necessary.
The ledger entry v0.410 gives the change and the proof.

## Method

Mechanical: the tests of the probes of the review, the texts of ADR-0173, ADR-0177 and ADR-0178, the counts of
`eng/test-floor.txt`, the roadmap line and the ledger entry.

Judgement:

- E226: the read keeps one cut for each pair of a view and a body, and `SectionCut.Keep` drops the cut of each body that the
  last read of the view did not cut. The cuts of the last 64 views stay. A sheet has at most 23 sections with the default
  letters.
- T183: the rule of a deadlock is a function (`Progress.EndsWhileItMoves`) with a clock and a tick from the caller, so its test
  runs on a clock of the test in no time. The real test ticks every 100 ms. The old test had no such function, so the new test
  of the rule fails to compile on the old code; its first case is the run of the finding: progress for 100 s.
- T184: the step "Test" gives `--blame-crash --blame-crash-dump-type mini` on Windows only, as the review recommends. A local
  run of `dotnet test` with the options attached the dump tool and passed. A test reads the workflow as text, because the
  pipeline is the only place where the step runs.
- V210: the queue is busy in the frame of the Poll that ended a work, so the host, which reads `Busy`, gives a pending read in
  that frame. The host does not change.
- V211: the menu takes the focus in its first frame only. A first form took it in each frame; a probe found that the focus
  ended the press of each other window, so a second tap on the file name did not close the menu, and the injection of V213
  stayed green. The test of V211 taps the file name two times, so it holds that.
- V212: one function (`PadPlace`) gives the place of the pad for each state, and `PlacePad` decides from it. The pad reads the
  window of the sketch of the last frame, which grows in the first frames of the pad, so the place of a pad of a sketch holds
  from its third frame. The test runs five frames.
- V213: `Route` does not give the library the press and the release of a tap outside the open menu and the file name of the
  last frame, and the release closes the menu. A popup of Dear ImGui was the other way, with more change. The code and
  ADR-0174 item 4 disagreed; the record is correct, and the code now agrees with it.
- V215: the review gave two changes. A new press clears the mark of a lost drag. The second change, the end of the drag in a
  frame with no paper, repairs the same path, so an injection of one of the two alone made no test fail; only the first is in
  the code.
- V216: a content URI whose decoded identifier has no "/" and no extension names no file.
- V217: the text of "Put on sheet" is the number of the value with three decimals and the last word of the text of its chip,
  the unit. A radius loses its "R" in that text.
- V221: while the page shows, the arc draws on the foreground list, so it is over the paper and each other window. The paper
  takes no tap whose press or release is on Reject or Accept of the last frame. The probe of the review asked that Accept not
  meet the paper; the correction keeps the place of the arc, so the test reads the order of the draw and the tap. At each
  size of the list of ADR-0113 Accept lies above the paper, so the guard had no case there; the theory has the size 2560 by
  1440 of the resize of ADR-0163 item 4, where both ends lie on the paper.
- V222: the arc removes the line of each command whose sheet item is on the sheet of the page. The review also recommended an
  amber value with the proposed number; that changes ADR-0177 item 2, which no answer of the owner covers, so it is not done,
  and register entry R-0071 holds it.

Weakest: the place of a pad of a sketch in its first two frames (V212), and the foreground list of V221, which draws the arc
over a pad or a message window on the page too. `ResponsiveLayoutTests.Each_Key_Of_A_Pad_Of_A_Sketch_Shows_On_A_Phone` and
`ProposedSheetTests.On_The_Page_The_Arc_Is_Over_The_Paper_And_Accept_Takes_The_Tap` hold what is tested.

## Progress

- 2026-11-21: opened under the standing order of the owner of 2026-11-20, branch p0171-review-findings from `9cadf07` (R20d,
  v0.409). The answers Q1, Q2, Q3 and Q8 are in the section "Decisions".
- 2026-11-21: the work: fourteen corrections with 32 new test cases, the text of ADR-0173, ADR-0177 and ADR-0178, and 18
  injections. Ledger entry v0.410.
- 2026-11-21: run 44837066779 of `1045aae` passed on each job. Closed at v0.410.
