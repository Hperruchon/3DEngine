---
id: 0027
title: The Vulkan code is first-party and uses Vortice.Vulkan 3.2.3
status: Done
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
    - docs/register.md
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

- [x] `3DEngine.Vulkan/3DEngine.Vulkan.csproj` is the only project file that names `Vortice.Vulkan`,
      and it names 3.2.3.
- [x] No project named `Vortice.Vulkan.Sample` or `Vortice.Vulkan.SampleFramework` exists.
- [x] `dotnet build` gives zero errors and zero warnings.
- [x] `dotnet test` passes, and each new gate check failed once on an injected violation.
- [x] On this computer the host shows a green window with the validation layer active, and the
      console gives no validation message.
- [x] When the window closes, the host releases each Vulkan object, the console gives no validation
      message, and the exit code is 0.
- [x] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.
      Run `35791921631` on commit `90985e5`: build, test and both smoke tests pass on each runner,
      and the write-set job passes each commit.

**A correction.** Commit `885ba75` ticked "zero warnings" from an incremental build. A clean build
of that commit gives one warning: CS9191 at `3DEngine.Vulkan/Swapchain.cs:85`. The 3.x parameter for
the extent is `in` and not `ref`, therefore the port passed a local by `ref` with no need. The next
commit passes the property directly.

The cause of the false tick: the build that compiled the port ran behind a `sed` filter that failed,
so its output was lost. The next build found the project up to date, compiled nothing, and reported
zero warnings. A clean build (`--no-incremental`) of each commit is now the method for a warning
count.

## Notes for the implementer

Three commits, in this order, so that each diff has one topic:

1. The move. The pin stays at 1.9.8, therefore the host behaves as before.
2. The upgrade. The diff shows each change that 3.x needs, and nothing else.
3. The teardown, which is a change of behaviour, and the close of this task.

The branch has four commits. The correction above needed its own commit between 2 and 3.

## Outcome

Status: Done · v0.28 · commits `65ab50a`, `885ba75`, `f602022`, and the commit that carries this
block.

**One first-party project.** `3DEngine.Vulkan` holds `GraphicsDevice`, `Swapchain`, `Window`, `Log`
and `Utils`. `git mv` kept the history of each file. `Vortice.Vulkan.Sample` and
`Vortice.Vulkan.SampleFramework` are gone. Five members and one block did not move, because nothing
called them or they never compiled: `GetMemoryTypeIndex`, `GetCommandBuffer`, `FlushCommandBuffer`,
`CreateShaderModule`, `CheckDeviceExtensionSupport`, and the block `#if TODO`. The implicit
conversion to `VkDevice` moved, because `Swapchain` called it. The port removed its last caller, and
the conversion went with it. The types `Application` and `VertexPositionColor` had no caller in the
host.

**The binding is 3.2.3, with one pin.** The host names no Vulkan package and no SDL package. The
build output holds `Vortice.Vulkan.dll` with the product version `3.2.3+ef01519`, which is the commit
that the nuspec names.

**The host releases each Vulkan object at exit.** The window closes with the exit code 0 and no
validation message.

**The validation layer can speak.** A silent layer proves nothing until it reports a defect, therefore
two defects were injected and removed:

- A render pass that does not end gives `VUID-vkEndCommandBuffer-commandBuffer-00060`.
- A device that is not destroyed gives `VUID-vkDestroyInstance-instance-00629` at the close.

**The gates.** The dependency direction gate reads the host now. It adds three checks: the Vulkan
layer references at most the render kernel, only a host that draws references the Vulkan layer, and
each binding has one pin. A headless client can no longer reference the render kernel, which ADR-0009
section 2 already forbade. The marker gate reads `3DEngine.Core`, `3DEngine.Vulkan` and `3DEngine`.
Seven violations were injected, and each one failed with a message that names the project and the
rule: the host to the sample framework, the command line to the render kernel, the Vulkan layer to
`Engine.Contracts`, the web client to the Vulkan layer, a second pin in the host, a `TODO` in the
Vulkan layer, and the word "temporary" in the host.

**The licence.** `THIRD-PARTY-NOTICES.md`, section 4, gives the MIT licence of the sample author, and
each derived file points to it.

**Found in passing.** Six statements in five documents describe a repository state that no longer
exists. Working agreement rule 2.2 forbids the correction of an adjacent file under this task,
therefore register entry R-0019 holds them, with a limit of 30 days.

## Method

**Mechanical.** The move with `git mv`. The port of each call to `VkInstanceApi` or `VkDeviceApi`,
from a reflection listing of each overload in the 3.2.3 assembly, and not from memory. The two-call
pattern for each function that returned a span in 1.9.8.

**Judgement.** Remove the sample projects instead of keeping them on the old binding: two pins of one
binding give two answers to one question. Move only what has a caller, because nothing can show that
a method with no caller is correct. Mark ADR-0017 `Accepted`, as ADR-0016 was, because the code and
the gate land with it; the owner can change the status. Keep `BlazorApp` out of the role table of the
gate, because `BlazorApp` references `BlazorApp.Client` and a rule for the web shell needs a decision
that R1 does not own.

**Weakest.** The port runs on one computer with one GPU. The runners build the code on Linux and
macOS, but no runner has a GPU, so no runner creates a device. macOS needs MoltenVK, and nothing here
tests it. The resize path is still absent: `RenderFrame` does not create the swapchain again after
`ErrorOutOfDateKHR`, and the sample did not either. The window is not resizable, so a resize cannot
cause it. A minimized window can, and this task did not test that. Phase R3 or R4 must add the
recreation of the swapchain before it makes the window resizable.
