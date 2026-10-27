---
id: 0027
title: Text and panels through Dear ImGui, drawn by a first-party renderer
status: Accepted
topic: Rendering, clients
date: 2026-10-27
supersedes: []
superseded-by: []
amends: ['0026']
amended-by: []
affects:
  - 3DEngine/Ui/**
  - 3DEngine/3DEngine.csproj
  - 3DEngine.Vulkan/Ui/**
enforced-by: Engine.Tests/Governance/UiLibraryGateTests.cs (TASK-0084 creates it)
---

# ADR-0027 — Text and panels through Dear ImGui, drawn by a first-party renderer

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The application draws no text. No file holds a font, a glyph or a library for a user interface. Register
entry R-0021 asked how to show the application, and phase R8 needs a text field for the name of a body.
On 2026-10-27 the owner decided KD1 of the decision sheet of that date
(`docs/proposals/2026-10-27-path-to-cad-decision-sheet.md`): the core of Dear ImGui through a binding of
cimgui, with a first-party Vulkan renderer and the input of `SdlInput`.

Four facts limit the choice:

1. Objective 7 of `docs/CHARTER.md` makes Vulkan a learning objective, so the renderer of the panels is
   first-party. The library gives draw data only.
2. ADR-0017 item 4 permits one pin of the SDL binding. The backends of the library bring a second SDL
   binding, so this project uses none of them.
3. The owner wants no question about a licence. The binding Hexa.NET.ImGui adds a clause on its name to
   the MIT licence. ImGui.NET, cimgui and Dear ImGui are MIT with no clause.
4. A check on 2026-10-27, with no window and no GPU, ran ImGui.NET 1.91.6.1 (Dear ImGui 1.91.6) on
   .NET 10.0.11 on win-x64: the native cimgui loaded, the font atlas was 512 x 128 RGBA, and the second frame
   of a window with a text, a text field and a button gave 1 draw list, 162 vertices and 267 indices. The
   macOS library of the package holds x86-64 and arm64. The package has no library for linux-arm64; the SDL
   binding has none either (finding V25 of the review of 2026-10-17).

## Decision

1. **The binding.** ImGui.NET 1.91.6.1. `3DEngine/3DEngine.csproj` holds its only pin. Only the files in
   `3DEngine/Ui/` name the namespace `ImGuiNET`. `THIRD-PARTY-NOTICES.md` gives the licence texts of
   ImGui.NET, cimgui and Dear ImGui, with its default font.
2. **No backend of the library.** The library sees no SDL, no Vulkan and no window. The host gives it
   input events and the size of the window, and it gives draw data back.
3. **The renderer.** `3DEngine.Vulkan/Ui/` draws neutral 2D draw data: vertices with a position, a
   texture coordinate and a colour, 16-bit indices, and commands with a clip rectangle, a texture and a
   range of indices. It holds the first sampled texture with its upload, the first descriptor set and the
   first 2D pipeline: alpha blending, no depth test, no culling, and one scissor for each command. It draws
   in the render pass of the target, after the scene. It names no library of a user interface (ADR-0017
   item 2). `3DEngine/Ui/` turns the draw data of the library into this form.
4. **Input.** The host gives each sample of a frame to the user interface first. A pointer gesture that
   starts over a panel belongs to the panel until it ends; each other gesture goes to the `IntentMachine`
   (ADR-0026). While a text field is active, keys and text go to the panel. A touch gives the panel the same
   events as a mouse, with the source "touch screen" of the library.
5. **The amendment of ADR-0026 item 1.** A text sample records the text that the keyboard gives, and the
   keys of text editing join the keys of a key sample. The window turns on the text input of SDL.
6. **Size.** Each control is at least 44 pixels high at a display scale of 1, so that a finger can hit it.
   The font is the default font of Dear ImGui at a larger size; no other font file enters the repository.
7. **The first panel.** One panel shows the address of the document and the live bodies of the session
   with their kind and a short identifier, and a text field filters the list. The panel reads the bodies
   that the desktop already holds from the events (ADR-0024); it reads no state of the engine. R8b adds
   the rename and the visibility.
8. **Tests.** The tests of the panel with no GPU live in `3DEngine.ImageTests`, because only that verifier
   may reference the host (`DependencyDirectionGateTests`). They run on each runner. An image test on
   lavapipe draws the panel offscreen. `UiLibraryGateTests` refuses the namespace outside `3DEngine/Ui/` and
   the package outside `3DEngine/3DEngine.csproj`.
9. **The exit.** A newer Dear ImGui, or a correction of the binding, comes from a fork: the code generator
   of the ImGui.NET repository on a newer cimgui, and a native build of cimgui in this repository, as for
   Manifold (`eng/manifold-native/`). Only `3DEngine/Ui/` changes. Nobody patches the package in place.

This record amends ADR-0026 item 1 (item 5 above).

## Consequences

**Good:** The application gets text, panels and a text field for R8 with no work on glyphs. The Vulkan
part stays first-party, and the first texture, descriptor set and 2D pipeline serve the later overlays
(R9, R10). One folder holds the library, so a fork or a first-party user interface replaces one folder.
Each licence is MIT with no clause.

**Bad:** The binding is from 2025-01 and gives Dear ImGui 1.91.6, not 1.92. The default font is a bitmap
font, and a large size looks coarse. The package also holds a library for win-x86, which a build with no
runtime identifier copies into the output; determinism rule 5 forbids a shipped x86 runtime identifier, so
the package of the desktop must leave it out. A gesture that starts over a panel never reaches the camera.

**Next:** TASK-0084 applies this decision. Register entry R-0021 closes with it.

## History

- 2026-10-27 · TASK-0084 · The record is written with decision KD1 of the owner of 2026-10-27, and the
  binding ImGui.NET that the owner chose on the same date.
