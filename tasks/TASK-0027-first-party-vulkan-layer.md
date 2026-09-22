---
id: 0027
title: The Vulkan code is first-party and uses Vortice.Vulkan 3.2.3
status: Active
phase: R1
opened: 2026-09-23
depends-on: [0026]
governed-by: [0007, 0017]
writes:
  create:
    - tasks/TASK-0027-first-party-vulkan-layer.md
    - docs/adr/0017-first-party-vulkan-layer.md
    - 3DEngine.Vulkan/3DEngine.Vulkan.csproj
    - 3DEngine.Vulkan/GraphicsDevice.cs
    - 3DEngine.Vulkan/Swapchain.cs
    - 3DEngine.Vulkan/Window.cs
    - 3DEngine.Vulkan/Log.cs
    - 3DEngine.Vulkan/Utils.cs
  modify:
    - Vortice.Vulkan.SampleFramework/**
    - Vortice.Vulkan.Sample/**
    - 3DEngine/3DEngine.csproj
    - 3DEngine/DesktopEngineHost.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/Documentation/Architecture.md
    - 3DEngine/Documentation/Running.md
    - 3DEngine.sln
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/MarkerGateTests.cs
    - THIRD-PARTY-NOTICES.md
    - CLAUDE.md
    - docs/INDEX.md
    - docs/glossary.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Core/**
    - BlazorApp/**
---

# TASK-0027 — The Vulkan code is first-party and uses Vortice.Vulkan 3.2.3

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R1 is the first of six phases that end at the first objective of the owner: create a
box, subtract a second box, and observe the cut (phase R6).

Two facts were verified on 2026-09-23 before this task was written:

1. **The pin.** Three project files pinned `Vortice.Vulkan` 1.9.8: `3DEngine/3DEngine.csproj`,
   `Vortice.Vulkan.SampleFramework/Vortice.Vulkan.SampleFramework.csproj` and
   `Vortice.Vulkan.Sample/Vortice.Vulkan.Sample.csproj`. The nuget.org feed lists 3.2.3 as the
   highest version. It is listed, it was published on 2026-05-25, and it targets `net9.0` and
   `net10.0` with no dependency. A build of a copy against 3.2.3 gave 59 errors, and each one is the
   same kind: a global `vk*` function does not exist. In 3.x an instance function lives on
   `VkInstanceApi` and a device function lives on `VkDeviceApi`.
2. **The host.** `3DEngine` builds and runs on this computer: Windows 11, an NVIDIA GeForce RTX 4070
   SUPER, Vulkan instance version 1.4.341, and the Vulkan SDK 1.3.290.0. The window is 1280 by 720,
   the centre pixel is (0, 255, 0), the layer `VK_LAYER_KHRONOS_validation` is active, and the
   console gives no validation message in eight seconds.

The host referenced `Vortice.Vulkan.SampleFramework`. `CLAUDE.md` does not permit that reference, and
the dependency direction gate did not read the host, therefore it gave no report. ADR-0017 gives the
decision and each rule.

## Goal

The desktop host draws through a first-party project that uses `Vortice.Vulkan` 3.2.3.

## Scope (in)

1. Create `3DEngine.Vulkan`. Move the five files that the host uses into it, with `git mv`, so that
   the history of each file stays.
2. Remove `Vortice.Vulkan.Sample` and `Vortice.Vulkan.SampleFramework`.
3. Move each type and each method that has a caller in the host. A method with no caller does not
   move. The block `#if TODO` never compiled, therefore it does not move.
4. Upgrade the one pin to 3.2.3 and port the code to `VkInstanceApi` and `VkDeviceApi`.
5. The host releases each Vulkan object when the window closes. Before this task the host released
   nothing, therefore the teardown code had no caller.
6. Extend the dependency direction gate and the marker gate. Verify each new check by injection.
7. Give the derived code a correct licence notice.

## Scope (out)

- Do not add a pipeline, a vertex type or a shader. Phase R3 does that.
- Do not make the window resizable. The swapchain has no recreation path today, and R3 or R4 adds it.
- Do not upgrade `Alimer.Bindings.SDL`. R1 names the Vulkan binding only, and 3.9.2 works with 3.2.3.
- Do not change `3DEngine.Core`. No render-kernel type changes.
- Do not change `BlazorApp`. The gate reads it and does not classify it.

## Acceptance criteria

- [ ] `3DEngine.Vulkan/3DEngine.Vulkan.csproj` is the only project file that names `Vortice.Vulkan`,
      and it names 3.2.3.
- [ ] No project named `Vortice.Vulkan.Sample` or `Vortice.Vulkan.SampleFramework` exists.
- [ ] `dotnet build` gives zero errors and zero warnings.
- [ ] `dotnet test` passes, and each new gate check failed once on an injected violation.
- [ ] On this computer the host shows a green window with the validation layer active, and the
      console gives no validation message.
- [ ] When the window closes, the host releases each Vulkan object, the console gives no validation
      message, and the exit code is 0.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

Three commits, in this order, so that each diff has one topic:

1. The move. The pin stays at 1.9.8, therefore the host behaves as before.
2. The upgrade. The diff shows each change that 3.x needs, and nothing else.
3. The teardown, which is a change of behaviour, and the close of this task.
