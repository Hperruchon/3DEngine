---
id: 0084
title: The desktop shows text and a panel, drawn by a first-party renderer
status: Active
phase: R7b
opened: 2026-10-27
depends-on: [0079, 0082]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023, 0024, 0025, 0026, 0027]
writes:
  create:
    - tasks/TASK-0084-text-and-panels.md
    - docs/adr/0027-text-and-panels.md
    - docs/proposals/2026-10-27-path-to-cad-decision-sheet.md
    - Engine.Tests/Governance/UiLibraryGateTests.cs
  modify:
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - docs/adr/0026-intent-layer.md
    - docs/adr/README.md
    - docs/templates.md
    - docs/INDEX.md
    - docs/glossary.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - THIRD-PARTY-NOTICES.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0084 — The desktop shows text and a panel, drawn by a first-party renderer

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The application draws no text, and phase R8 needs a text field for the name of a body. On 2026-10-27 the
owner decided the path to the milestone "CAD on one part" (decisions KD1 to KD6 of the decision sheet of
that date, now `docs/proposals/2026-10-27-path-to-cad-decision-sheet.md`):

1. (KD1) Text and panels come from the core of Dear ImGui through a binding of cimgui. A first-party Vulkan
   renderer draws its data, with the first texture, descriptor set and 2D pipeline. The input comes from
   `SdlInput` through the intent layer. No backend of the library: ADR-0017 item 4 permits one SDL pin. The
   code of the user interface lives in one host folder.
2. (KD2) The order: R7b (this task), R8a, R8b, the codebase review when its gate makes it due, P8a, P8c, R10,
   K2, K3a, K0a, K0b. R9 comes after the milestone. This task changes the roadmap in its first commit.
3. The owner chose the binding ImGui.NET on 2026-10-27, because it has no question about a licence. A check
   on that date ran it on .NET 10 with no window and no GPU (ADR-0027, context item 4).
4. The defaults of the sheet for R7b: one host folder that alone references the library; a first-party
   renderer; the input from `SdlInput` through the intent layer; touch targets of at least 44 points; a
   test of the state of the panel with no GPU, an image test on lavapipe, and a picture that the owner
   accepts.

## Goal

The desktop shows a panel with text and a text field over the scene, a finger and a mouse use it in the same
way, and a gesture outside the panel still moves the camera.

## Scope (in)

1. ADR-0027 records the decision. It amends ADR-0026 item 1. Register entry R-0021 closes. The roadmap gets
   the rows and the rule of KD2.
2. `3DEngine.Vulkan/Ui/`: the sampled texture with its upload and its partial update, the descriptor set,
   the 2D pipeline and its shaders, and a renderer of neutral 2D draw data.
3. `3DEngine.Core/Input/`: the text sample and the keys of text editing. `3DEngine.Vulkan/SdlInput.cs`
   translates the text input and those keys; the window turns on the text input of SDL.
4. `3DEngine/Ui/`: the context of the library, the routing of the samples between the panel and the
   `IntentMachine`, the conversion of the draw data, the font, the sizes, and the first panel (ADR-0027
   item 7). The host draws the panel after the scene.
5. Tests: the state of the panel with no GPU (a touch tap and a mouse click give the same action, typed text
   reaches the text field, a gesture over the panel does not reach the camera, a gesture outside it does),
   the conversion of the draw data, an image test of the panel on lavapipe, and `UiLibraryGateTests`.
6. `THIRD-PARTY-NOTICES.md` gives the licence texts of ImGui.NET, cimgui and Dear ImGui with its font.

## Scope (out)

- The rename and the visibility of a body: R8a and R8b.
- A second font file, a theme, or a layout that a person saves.
- The package of the desktop. When it exists, it leaves out the win-x86 library of the binding (ADR-0027,
  "Bad").

## Acceptance criteria

- [ ] Each new test fails before its correction, or an injection shows that it can fail.
- [ ] `UiLibraryGateTests` fails on an injected use of the namespace `ImGuiNET` outside `3DEngine/Ui/`, and on
      an injected pin of the package in another project.
- [ ] The image test of the panel passes on lavapipe with no validation message.
- [ ] A picture of the desktop with the panel, a typed filter, and the camera moved by a drag outside the
      panel. The owner accepts it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The protocol of the font.** Dear ImGui 1.91 builds the font atlas once: read it with
  `GetTexDataAsRGBA32`, upload it, and set its texture identifier.
- **Touch.** No computer of the project has a touch screen; the touch path of the panel is proven by
  synthetic samples only, as in R7.
- **The size.** About 4 to 6 evenings.

## Progress

- 2026-10-27: opened after TASK-0079 (v0.85). The first commit holds ADR-0027, the amendment of ADR-0026, the
  close of R-0021, the roadmap rows and the rule of KD2, and the copy of the decision sheet.
