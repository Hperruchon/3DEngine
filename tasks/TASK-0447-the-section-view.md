---
id: 0447
title: The section view of the window, a cut of each body by a plane with a cap
status: Done
phase: R26
opened: 2026-11-21
depends-on: [0446]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0031, 0032, 0034, 0043, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0088, 0089, 0091, 0092, 0093, 0095, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0109, 0110, 0111, 0113, 0114, 0116, 0117, 0119, 0120, 0121, 0122, 0123, 0124, 0125, 0127, 0128, 0129, 0130, 0131, 0132, 0137, 0138, 0139, 0140, 0152, 0163, 0166, 0167, 0168, 0170, 0171, 0172, 0174, 0176, 0177, 0178, 0184]
writes:
  create:
    - tasks/TASK-0447-the-section-view.md
    - docs/adr/0184-the-section-view.md
    - 3DEngine.App/SectionMesh.cs
    - 3DEngine.App/SectionView.cs
    - 3DEngine.App/Ui/DesktopUi.Section.cs
    - 3DEngine.ImageTests/SectionViewTests.cs
  modify:
    - 3DEngine.App/SceneRenderer.cs
    - 3DEngine.App/SessionBodies.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Part.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Orbit.cs
    - 3DEngine.App/Ui/DesktopUi.Pad.cs
    - 3DEngine.ImageTests/PartRingTests.cs
    - 3DEngine.ImageTests/ShowHiddenTests.cs
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
    - docs/adr/0174-the-tools-of-the-part-on-the-ring-and-a-structure-view.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
    - 3DEngine.Vulkan/Shaders/**
---

# TASK-0447 — The section view of the window, a cut of each body by a plane with a cap

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R26 of the roadmap: "The section view: a cut plane on an origin plane or on a datum, a pad for its offset, and a cap on the
cut faces; presentation only (kernel boundary: section by a plane). After K10." ADR-0157 item 15 and card N7 of
`docs/proposals/2026-11-15-assembly-and-agent-design.md` give it. K10a is done (TASK-0429). P0.178 (TASK-0445) gave the cut of
a solid of the owned kernel by a plane with a cap (ADR-0057 item 10) for the section of a sheet. The branch `r26-section-view`
starts from `13965c54` (P0.179, TASK-0446, v0.430, not merged yet).

## Goal

A person starts a section of the part from the ring of the part, chooses its plane, its offset and its side, and sees each body
cut with a cap, with no change of the engine.

## Scope (in)

1. "Section" on the ring of the part, and the tools "Plane", "Offset" and "Flip" while a section lasts (ADR-0184 items 1 to 4).
2. The planes: XY, XZ and YZ, and each named datum with no fault, read off the thread of the frame.
3. The cut of each body by the owned kernel on the thread pool, the cap with its hatch, a mesh body clipped with no cap, and the
   draw of the kept parts and the caps by the renderer (items 5 to 7).
4. The end of a section (item 8).
5. ADR-0184 for the choices that the row leaves open, and the amendment of ADR-0174 item 1.
6. The tests, the pictures of the bookcase, the layout test of ADR-0172, the roadmap, the ledger and the counts of the tests.

## Scope (out)

- A change of `Engine.Contracts`, of a command, of an event, of a query or of the Document; no new diagnostic code.
- A change of a shader.
- A drag of the plane, a pick that stops at the plane, a second plane, and a section of the lines of a flat view or of a sheet.

## Acceptance criteria

- [x] The ring of the part has "Section" while the part has a live body; a tap starts a section on XZ through the middle of the
  visible bodies, and the ring then holds "Section", "Plane", "Offset" and "Flip" (`SectionViewTests`).
- [x] The pad sets the offset, "Plane" goes through the origin planes and the named datums, "Flip" turns the side, and Escape at
  the level of the document or a second tap on "Section" ends the section. No tool gives an action.
- [x] A box cut at its middle keeps half its volume and has one cap of the right area; a plate with a hole gives a cap with a
  hole; the five panels of the bookcase keep their front part with a cap, and the back is on the side that goes.
- [x] A mesh body is clipped with no cap, and the hatch is lines at 45 degrees inside the cap.
- [x] The layout test of ADR-0172 holds the state Section at each size.
- [x] The pictures of the bookcase with a section through its middle at 1280 by 720 and at 390 by 844 (local, not committed).
- [x] An injection in each part makes a named test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

ADR-0184 holds the decisions and the alternatives. In short:

- "Section" is a segment of the ring of the part after "Bodies" (amends ADR-0174 item 1), with the state of a tool in use while a
  section lasts.
- The planes are XY, XZ and YZ, then each datum of the Document with a name and no fault; a new section is on XZ through the middle
  of the visible bodies, rounded to a whole unit, and a datum starts at its own place.
- The offset is a value pad in the unit of the Document; its apply gives no action.
- With no flip, the side of the direction of the plane goes: +z, +y and +x, the side of the start eye. "Flip" turns it.
- The renderer cuts the tessellation of each body by `PlanarBoolean.Cut` on the thread pool, with a real cap; no clip plane of the
  GPU and no shader change. The hatch is lines at 45 degrees in the accent colour, 1/40 of the diagonal of the box of the visible
  bodies apart.
- A body that the plane misses shows whole; a body on the side that goes does not show; a mesh body is clipped with no cap.
- Escape at the level of the document, or a second tap on "Section", ends the section; so do no live body and the home screen.

## Outcome

Status: Done · commit (the merge of the branch r26-section-view) · v0.431

## Method

Mechanical: the segments of the ring, the pad, the icons, the History line of ADR-0174, the roadmap rows, the counts of
`eng/test-floor.txt`.

Judgement: the place of "Section", the cut on the client from its own tessellation with the cut of the kernel, the default plane
and side, the hatch, and the rule for the bodies that the plane misses and for mesh bodies (ADR-0184, "Alternatives considered").

Weakest: the time of the cut of a curved body with many triangles was not measured in the window; the old cut stays on the GPU
until the new cut is complete, so a slow cut shows late and does not stop the frame. The desktop was not run.

The new tests do not compile with the code of `13965c54`: `SectionMesh`, `SectionView`, the labels of the section and
`StartSection` do not exist there.

Injections (`inject.js` in `%TEMP%\r26`), each restored byte for byte (sha256), each test run of `dotnet test` with
`shell: false`. Each of the twenty made each named test fail:

1. the kernel cut keeps the side that goes (`3DEngine.App/SectionMesh.cs`): `A_Box_Cut_At_Its_Middle_Keeps_Half_Its_Volume_And_One_Cap`.
2. no cap (`3DEngine.App/SectionMesh.cs`): `A_Box_Cut_At_Its_Middle_Keeps_Half_Its_Volume_And_One_Cap`, `A_Plate_With_A_Hole_Gives_A_Cap_With_A_Hole`.
3. a mesh body keeps the side that goes (`3DEngine.App/SectionMesh.cs`): `A_Mesh_Body_Is_Clipped_With_No_Cap`.
4. a body on the side that goes shows whole (`3DEngine.App/SectionMesh.cs`): `The_Panels_Of_The_Bookcase_Keep_The_Front_Part_With_A_Cap`.
5. the hatch is along an axis (`3DEngine.App/SectionMesh.cs`): `The_Hatch_Of_A_Cap_Is_Lines_At_45_Degrees_Inside_The_Cap`.
6. no Section on the ring of the part (`3DEngine.App/Ui/DesktopUi.Part.cs`): `A_Tap_On_Section_Starts_A_Section_On_XZ_Through_The_Middle_Of_The_Bodies`, `With_No_Selection_The_Ring_Holds_The_Tools_Of_The_Part`, `The_Ring_Of_The_Part_Has_Show_Hidden_Only_While_A_Body_Is_Hidden`.
7. a new section on XY (`3DEngine.App/Ui/DesktopUi.Section.cs`): `A_Tap_On_Section_Starts_A_Section_On_XZ_Through_The_Middle_Of_The_Bodies`.
8. a new section at the origin (`3DEngine.App/Ui/DesktopUi.Section.cs`): `A_Tap_On_Section_Starts_A_Section_On_XZ_Through_The_Middle_Of_The_Bodies`, `Plane_Goes_Through_The_Origin_Planes_And_The_Named_Datums`.
9. the ring of the section has no Flip (`3DEngine.App/Ui/DesktopUi.Section.cs`): `A_Tap_On_Section_Starts_A_Section_On_XZ_Through_The_Middle_Of_The_Bodies`, `Flip_Turns_The_Side_That_Goes`.
10. the pad sets no offset (`3DEngine.App/Ui/DesktopUi.Section.cs`): `The_Pad_Sets_The_Offset`.
11. the pad of the section gives an action (`3DEngine.App/Ui/DesktopUi.Pad.cs`): `The_Pad_Sets_The_Offset`.
12. Plane skips the datums (`3DEngine.App/Ui/DesktopUi.Section.cs`): `Plane_Goes_Through_The_Origin_Planes_And_The_Named_Datums`.
13. the planes of the datums keep a datum with no name (`3DEngine.App/SectionView.cs`): `The_Planes_Of_The_Datums_Are_The_Named_Datums_With_No_Fault`.
14. Flip does not turn the side (`3DEngine.App/Ui/DesktopUi.Section.cs`): `Flip_Turns_The_Side_That_Goes`, `Plane_Goes_Through_The_Origin_Planes_And_The_Named_Datums`.
15. Escape ends the section with a selected body (`3DEngine.App/Ui/DesktopUi.Section.cs`): `Escape_At_The_Document_Or_A_Second_Tap_On_Section_Ends_The_Section`.
16. no live body keeps the section (`3DEngine.App/Ui/DesktopUi.Section.cs`): `A_Tap_On_Section_Starts_A_Section_On_XZ_Through_The_Middle_Of_The_Bodies`.
17. each body draws whole (`3DEngine.App/SectionView.cs`): `The_Pictures_Show_The_Bookcase_With_A_Section_Through_Its_Middle`.
18. no hatch on the caps (`3DEngine.App/SceneRenderer.cs`): `The_Pictures_Show_The_Bookcase_With_A_Section_Through_Its_Middle`.
19. each frame starts a new cut (`3DEngine.App/SectionView.cs`): `The_Pictures_Show_The_Bookcase_With_A_Section_Through_Its_Middle`.
20. the picture test gives no box of the bodies, as a host that sets no `SectionBox` (`inject-box.js`):
    `The_Pictures_Show_The_Bookcase_With_A_Section_Through_Its_Middle`.

The pictures (`%TEMP%\r26\pictures`, not committed): `section-bookcase-1280x720.png` and `section-bookcase-390x844.png`
with the window, `section-bookcase-scene-*.png` with the scene only, and `section-bookcase-whole-*.png` before the section.

## Progress

- 2026-11-21: opened after P0.179 (TASK-0446, v0.430, not merged yet), branch `r26-section-view` from `13965c54`.
- 2026-11-21: ADR-0184, the section on the ring of the part, the cut on the thread pool, 17 new tests, the pictures of the
  bookcase, the roadmap rows and the ledger entry v0.431. The first spacing of the hatch, 1/120 of the diagonal, filled the
  caps of the bookcase in the picture; it is 1/40. Local: a clean build with 0 warnings, the phone project with 0 warnings,
  `dotnet test` 3174 passed (1834, 390, 947, 3), the write-set check and `eng/repo-gates` pass.
- 2026-11-21: a question of the review: the pictures show "0 mm" for XZ. The bookcase is 400 mm deep from y = -200 to
  200 (`BookcaseCase.cs`), so 0 is the middle. The picture test now holds the box of the bodies that the window gets, and an
  injection of no box makes it fail. The host sets `SectionBox` before each frame (`NativeThreeDEngine`). No code changed.
- 2026-11-22: run 44943334498 of `2aed6d7e` passed on each job. Closed at v0.431.
