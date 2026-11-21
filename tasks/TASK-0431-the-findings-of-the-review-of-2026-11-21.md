---
id: 0431
title: The medium and low findings of the codebase review of 2026-11-21
status: Active
phase: P0.172
opened: 2026-11-21
depends-on: [0430]
governed-by: [0004, 0007, 0012, 0016, 0019, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0037, 0048, 0060, 0072, 0080, 0083, 0093, 0099, 0100, 0101, 0102, 0104, 0108, 0113, 0116, 0119, 0123, 0124, 0125, 0127, 0129, 0130, 0138, 0140, 0145, 0160, 0164, 0165, 0166, 0167, 0168, 0169, 0170, 0171, 0172, 0173, 0174, 0175, 0177, 0178, 0179]
writes:
  create:
    - tasks/TASK-0431-the-findings-of-the-review-of-2026-11-21.md
    - eng/bookcase-compare/BookcaseCompare.csproj
    - eng/bookcase-compare/Program.cs
  modify:
    - Engine.Core/Commands/SetSheetTitleCommandHandler.cs
    - Engine.Core/Drawing/TitleBlock.cs
    - Engine.Core/Export/DxfExport.cs
    - 3DEngine.App/SessionSheets.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.History.cs
    - 3DEngine.App/Ui/DesktopUi.SheetPage.cs
    - 3DEngine.App/Ui/DesktopUi.Views.cs
    - Engine.Tests/Assembly/BookcaseCaseTests.cs
    - Engine.Tests/Drawing/SheetTitleTests.cs
    - Engine.Tests/Http/HttpConcurrencyTests.cs
    - Engine.Tests/EventSinkContractTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - 3DEngine.ImageTests/DimensionLabelsTests.cs
    - 3DEngine.ImageTests/PartRingTests.cs
    - 3DEngine.ImageTests/SheetAddTests.cs
    - 3DEngine.ImageTests/SheetTitlePageTests.cs
    - 3DEngine.ImageTests/ShowHiddenTests.cs
    - 3DEngine.sln
    - docs/adr/0173-a-lost-runner-gets-one-automatic-re-run.md
    - docs/adr/0178-show-hidden-on-the-ring-of-the-part-and-the-path-line.md
    - docs/adr/0179-the-title-block-of-a-sheet.md
    - docs/proposals/2026-11-21-the-bookcase-case-of-k10.md
    - docs/diagnostics.md
    - docs/INDEX.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Kernel/**
    - Engine.Contracts/**
---

# TASK-0431 — The medium and low findings of the codebase review of 2026-11-21

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-21 (`docs/reviews/2026-11-21-codebase-review.md`, TASK-0430) found one medium and ten low
findings and no critical or high one. By `docs/templates.md`, section 7, they come after the next product phase.

## Goal

Each finding has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: V225 (on a portrait phone with a live sheet, "N hidden" can lie under "Views").
2. Low: E227, E228, E229, T185, T186, V223, V224, V226, V227, V228.
3. The answers Q5, Q6 and Q7 of the review, each option A under the order of the owner of 2026-11-20.

## Scope (out)

- When the task was opened, the write set held only this file. The task added the files of each correction, and the ADRs that
  govern them, when it started (`docs/templates.md`, section 2).
- A portrait phone of 360 pixels with a sheet (V225): the rules of the path line cannot all hold there; register entry R-0072
  asks the owner.
- `Engine.Contracts/**` and `Engine.Kernel/**`: no correction needs them.

## Decisions

The owner gave a standing order on 2026-11-20: take the recommended option A of each question of a review. The three
questions of these findings are answered by the standing order of the owner of 2026-11-20 (Q5 to Q7 of the codebase review of
2026-11-21):

- Q5 (V226) A: "N hidden" shows on the path line only while no body is selected. ADR-0178 item 2 says so.
- Q6 (E227) A: a SetSheetTitle that changes nothing is refused, as MoveSheetViews refuses a move to the place of the view
  (E225). The refusal uses the code of the command, `E-SHEET-TITLE-INVALID`, as E225 used `E-SHEET-VIEW-INVALID`, so no code is
  new; `docs/diagnostics.md` gives the new case. ADR-0179 item 2 says so.
- Q7 (T186) A: the comparison of the builder of the bookcase with the file of the owner is the program `eng/bookcase-compare`,
  which the owner runs. The test suite keeps the builder tests that run in the pipeline.

## Acceptance criteria

- [x] Each finding has a test that fails before its correction, or a register entry.
- [x] Each injection of a correction makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · commit (the merge of the branch p0172-review-findings) · v0.416

The eleven findings have a correction and a test that failed on the code of v0.415; the test of T186 failed by injection,
because its correction is the new program. V225 is corrected at 390 and 412 pixels; at 360 pixels the rules of the path line
cannot all hold, and register entry R-0072 asks the owner. The ledger entry v0.416 gives the change and the proof.

## Method

Mechanical: the tests of the probes of the review, the texts of ADR-0173, ADR-0178 and ADR-0179, the case of
`E-SHEET-TITLE-INVALID` in `docs/diagnostics.md`, the counts of `eng/test-floor.txt`, the roadmap line, the row of
`docs/INDEX.md` and the ledger entry.

Judgement:

- E227: the refusal uses the code of the command, as E225 did, so no code is new. `SheetTitleBlock` holds texts only, so `==`
  compares the values; null and the empty block differ, so the first empty block of a sheet stays one step of undo.
- E228: of the two corrections of the review, the task took "%" as "%%%" and the DXF file in the "Bad" list of ADR-0179. The
  alignment "Fit" changes the width of each text that fits its cell too, and changes item 7. The rule applies to the texts of
  the block only: the "%%c" of a diameter (ADR-0165 item 4) is a code on purpose.
- E229: 1015 for each character that the table does not hold, and not only for "@", so a text of other characters stays in
  its cell too.
- T185: 90 s. A test reads the hang limit from `ci.yml`, so a change of either limit that breaks the order fails it.
- T186: a program in `eng/` that compiles `Engine.Tests/Assembly/BookcaseCase.cs`, so the tests and the program use one
  builder. It is in the solution, so the pipeline builds it on three systems. A new project needs a class of
  `DependencyDirectionGateTests` (a headless client) and an entry of `EventSinkContractTests`; both lists changed, and an
  injection of each shows the failure.
- V224: a clip of each cell, found from `TitleBlock.Cells` with the same sums as the layout. The test reads the clip of each
  draw of the page.
- V225: one width of the controls at the upper right (`UpperRightWidth`) for the top bar and the path line. At 360 pixels the
  part still goes under "Views"; the floor of the file name (ADR-0100 item 1), the gap of ADR-0138 and the part (ADR-0178
  item 2) cannot all hold, so the theory runs at 390 and 412 pixels, and R-0072 holds the question.
- V228: the arc first, then each ground, then each dot and text. The test compares the order of the vertices of the grounds
  with the ring of each node that is not selected.

The injections, each restored byte for byte (sha256), and the test that each one made fail:

| Injection | Failed test |
|---|---|
| E227: no refusal of the same block | `SheetTitleTests.A_SetSheetTitle_That_Changes_Nothing_Is_Refused` |
| E228: "%" stays "%" | `SheetTitleTests.A_Percent_Sign_Of_The_Block_Stays_A_Percent_Sign_In_The_Dxf_File` |
| E229: 1000 for a character with no width | `SheetTitleTests.A_Character_With_No_Width_In_The_Table_Is_As_Wide_As_The_Widest_Character` |
| T185: a request stops after 60 s | `HttpConcurrencyTests.A_Request_Stops_After_The_Stall_Limit_And_Before_The_Hang_Limit` |
| T186: a test reads the documents folder | `BookcaseCaseTests.The_Comparison_With_The_File_Of_The_Owner_Is_A_Program_And_No_Test` |
| T186: the program has no class | `DependencyDirectionGateTests.Each_Project_Is_In_One_Class` |
| T186: the program references `3DEngine.Core` | `DependencyDirectionGateTests.A_Client_References_Only_The_Permitted_Projects` |
| T186: no entry of the program in the sink test | `EventSinkContractTests.Each_Project_That_Can_Hold_A_Sink_Is_Read_Here_Or_By_A_Named_Test` |
| V223: the copy does not read the block | `SheetAddTests.The_Window_Keeps_The_Title_Block_Of_Each_Sheet` |
| V224: the clip is the whole block | `SheetTitlePageTests.Each_Text_Of_The_Block_Is_Clipped_To_Its_Cell` |
| V225: the path line keeps the room of "Views" only | `ShowHiddenTests.With_A_Sheet_The_Path_Line_And_The_Top_Bar_End_Left_Of_Views` |
| V225: the top bar keeps the room of "Views" only | `ShowHiddenTests.With_A_Sheet_The_Path_Line_And_The_Top_Bar_End_Left_Of_Views` |
| V226: the part shows with a selection | `ShowHiddenTests.With_A_Selected_Body_The_Path_Line_Does_Not_Count_The_Hidden_Bodies` |
| V227: the room of the folder keeps the chevron | `PartRingTests.A_Long_Folder_After_The_File_Name_Ends_Left_Of_The_Command_Line` |
| V228: the ground of a node after its dot | `DimensionLabelsTests.Each_Ground_Of_The_History_Arc_Comes_Before_Each_Dot` |

The first form of the test of V225 stayed green for the injection of the top bar, because the text "mm" was short; the test
sets "not saved" now, and the injection fails it.

Weakest: E228 in a real CAD program (no CAD program opened the file) and the font of the DXF file, which only the "Bad" list
holds; V225 at 360 pixels (R-0072); the pixel values of V224 and V227 come from the font of the tests, not from a picture of
the real window.

## Progress

- 2026-11-21: opened under the standing order of the owner of 2026-11-20, branch p0172-review-findings from `878f7a87` (K15a,
  v0.415). The answers Q5, Q6 and Q7 are in the section "Decisions".
- 2026-11-21: the work: eleven corrections with 18 new test cases, the program `eng/bookcase-compare`, the text of ADR-0173,
  ADR-0178 and ADR-0179, register entry R-0072, and 15 injections. Ledger entry v0.416.
