// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See THIRD-PARTY-NOTICES.md, section 4.
// This file comes from the Vortice.Vulkan sample framework. TASK-0027 moved it
// into this project and changed it. ADR-0017 gives the rules for this project.

using System;
using SDL3;
using Vortice.Vulkan;
using static SDL3.SDL3;

namespace ThreeDEngine.Vulkan;

[Flags]
public enum WindowFlags
{
    None = 0,
    Fullscreen = 1 << 0,
    Borderless = 1 << 1,
    Resizable = 1 << 2,
    Minimized = 1 << 3,
    Maximized = 1 << 4,
}

public sealed unsafe class Window
{
    private readonly SDL_Window _window;

    public unsafe Window(string title, int width, int height, WindowFlags flags = WindowFlags.None)
    {
        Title = title;

        SDL_WindowFlags sdl_flags = SDL_WindowFlags.HighPixelDensity | SDL_WindowFlags.Vulkan | SDL_WindowFlags.Hidden;
        if ((flags & WindowFlags.Fullscreen) != WindowFlags.None)
        {
            sdl_flags |= SDL_WindowFlags.Fullscreen;
        }
        else
        {
            if ((flags & WindowFlags.Borderless) != WindowFlags.None)
            {
                sdl_flags |= SDL_WindowFlags.Borderless;
            }

            if ((flags & WindowFlags.Resizable) != WindowFlags.None)
            {
                sdl_flags |= SDL_WindowFlags.Resizable;
            }

            if ((flags & WindowFlags.Minimized) != WindowFlags.None)
            {
                sdl_flags |= SDL_WindowFlags.Minimized;
            }

            if ((flags & WindowFlags.Maximized) != WindowFlags.None)
            {
                sdl_flags |= SDL_WindowFlags.Maximized;
            }
        }

        // A touch gives touch samples only: SDL makes no mouse event from it (ADR-0026 item 4,
        // decision D8). SdlInput also drops such an event, so neither line alone holds the rule.
        fixed (byte* name = SDL_HINT_TOUCH_MOUSE_EVENTS)
        fixed (byte* off = "0"u8)
        {
            _ = SDL_SetHint(name, off);
        }

        _window = SDL_CreateWindow(title, width, height, sdl_flags);
        if (_window.IsNull)
        {
            throw new Exception("SDL: failed to create window");
        }

        _ = SDL_SetWindowPosition(_window, SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED);
        Id = SDL_GetWindowID(_window);
    }

    public string Title { get; }

    public SDL_WindowID Id { get; }
    public VkExtent2D Extent
    {
        get
        {
            SDL_GetWindowSize(_window, out int width, out int height);
            return new(width, height);
        }
    }

    public void Show()
    {
        _ = SDL_ShowWindow(_window);
    }

    // True while the window is minimised or has no area, so that a frame draws nothing
    // (finding V23 of the codebase review of 2026-10-17, TASK-0079).
    public bool HasNoArea
    {
        get
        {
            if ((SDL_GetWindowFlags(_window) & SDL_WindowFlags.Minimized) != 0)
                return true;

            var extent = Extent;
            return extent.width == 0 || extent.height == 0;
        }
    }

    // Waits until an event arrives or the limit passes. The event stays in the queue for
    // the next poll.
    public static void WaitForEvent(int limitMilliseconds)
    {
        _ = SDL_WaitEventTimeout(null, limitMilliseconds);
    }

    public VkSurfaceKHR CreateSurface(VkInstance instance)
    {
        VkSurfaceKHR surface;
        if (!SDL_Vulkan_CreateSurface(_window, instance, 0, (ulong**)&surface))
        {
            throw new Exception("SDL: failed to create vulkan surface");
        }

        return surface;
    }
}
