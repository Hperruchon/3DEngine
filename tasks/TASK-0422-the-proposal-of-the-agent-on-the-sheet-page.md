---
id: 0422
title: The sheet page shows a proposal of the agent as amber boxes of views and amber values before the accept
status: Done
phase: R30f
opened: 2026-11-20
depends-on: [0413, 0416]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0085, 0086, 0092, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0113, 0114, 0115, 0116, 0120, 0124, 0127, 0129, 0130, 0131, 0133, 0134, 0135, 0136, 0137, 0138, 0139, 0140, 0141, 0152, 0163, 0166, 0167, 0168, 0170, 0171, 0174, 0176, 0177]
writes:
  create:
    - tasks/TASK-0422-the-proposal-of-the-agent-on-the-sheet-page.md
    - docs/adr/0177-the-proposal-of-the-agent-on-the-sheet-page.md
    - 3DEngine.App/ProposedSheet.cs
    - 3DEngine.ImageTests/ProposedSheetTests.cs
  modify:
    - 3DEngine/HostedSession.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/Ui/DesktopUi.Sheet.cs
    - 3DEngine.App/Ui/DesktopUi.SheetPage.cs
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/0168-the-sheet-page.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - 3DEngine.Android/**
---

# TASK-0422 — The sheet page shows a proposal of the agent as amber boxes of views and amber values before the accept

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 item 8 records the answer D8 A of the owner of 2026-11-18: on the sheet page, a proposed view is an amber box at its
place, and a proposed dimension an amber value. The arc of ADR-0092 keeps Accept and Reject, and no preview enters the log.
Row R30f holds the slice. On 2026-11-20 the owner ordered "Yes, start R30f".

Today the arc shows the text of each command of a proposal (ADR-0092 item 5). A proposal of CreateSheet, AddSheetView and
PlaceSheetDimension commands is valid, but the person reads only numbers, and cannot judge a place or a scale from them (gap G7
of `docs/proposals/2026-11-18-2d-view-workflow.md`).

The engine has each part that the preview needs: GetViewLines gives the lines of a frame (ADR-0166 item 1), GetBodyInfo gives
the distance of an extrude, and the sketches of the window give the value of a constraint. This slice is a change of the
window only.

## Goal

An agent posts a proposal with a new sheet, three views and four dimensions. Before the accept, the sheet page shows the paper
of the new sheet, an amber box with the name of each view at the place where GetSheet puts that view after the accept, and an
amber value at the place of each dimension.

## Scope (in)

1. The sheet items of the waiting proposal: CreateSheet, AddSheetView and PlaceSheetDimension (ADR-0177 item 1).
2. The read of the boxes and of the values, off the frame thread (item 2).
3. The sheet page: the sheet of the proposal, the amber boxes, names and values, and a proposed sheet that takes no tap (items 3
   to 5).

## Scope (out)

- A preview of each other sheet command (move, scale, remove, the lines of a view, a section).
- The lines of a proposed view; the box shows its place and its size only.
- The phone. It has no HTTP surface, so it shows no proposal (ADR-0092 item 8).
- A change of `Engine.Core/**`, `Engine.Api.Http/**` or `Engine.Contracts/**`.

## Acceptance criteria

- [x] Before the accept, each proposed view has an amber box that is the box of GetSheet for that view after the accept, within
      0.001 mm, and each proposed dimension has its amber value at its place.
- [x] A proposal for a new sheet shows its paper, and the page takes no tap on it; a proposal for a live sheet shows its items
      over that sheet.
- [x] Nothing enters the log before the accept.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch.

## Outcome

Status: Done · commit (the merge of the branch r30f-sheet-proposal) · v0.406

The sheet page shows the sheet items of the waiting proposal: the paper of a new sheet, an amber box with the name of each
proposed view at the place of GetSheet after the accept, and each proposed value in amber. Accept and Reject on the arc work
over the page. The ledger entry v0.406 gives the proof.

## Method

- Read ADR-0162 item 8, ADR-0092 items 5 to 7 and ADR-0168 item 1 before the code.
- Ran `ProposedSheetTests` after each change, then 17 injections with `inject0422.js` in the scratchpad; each file came back
  byte for byte (sha256).
- Checked the real window with a script that posts a proposal over HTTP, and took pictures before and after the accept.
- A clean build, the phone project, `dotnet test`, the write-set check and `eng/repo-gates` before the commit.

## Progress

- 2026-11-20: opened under the order of the owner "Yes, start R30f", on the branch r30f-sheet-proposal from the closed branch of
  TASK-0416 (v0.405), so the sheet page has the corrections of P0.167.
- 2026-11-20: the real window showed three faults: the command lines of the arc on the paper, no accept while the page was
  open, and a page that went to the first sheet after the accept. Each has a test now. `DesktopUi.Sheet.cs` joined the write
  set for the third.
- 2026-11-20: closed at v0.406. P0.167 reached `main` at `8dbaafb7` meanwhile, with the same commits as the base of this branch.
- 2026-11-20: run 44815031048 of 3267a150 on the branch passed each of its seven jobs at attempt 1.
