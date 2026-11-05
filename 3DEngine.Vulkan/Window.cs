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

        // Register entry R-0043 and finding V85 (TASK-0188, TASK-0191): the hook of the button messages, on the video
        // driver of Windows only. The process has one window, so the hook is static.
        if (OperatingSystem.IsWindows() && SDL_GetCurrentVideoDriver() == "windows")
        {
            _handle = SDL_GetPointerProperty(SDL_GetWindowProperties(_window), SDL_PROP_WINDOW_WIN32_HWND_POINTER, IntPtr.Zero);
            s_hooked = this;
            SDL_SetWindowsMessageHook(&Hook, IntPtr.Zero);
        }

        // The text input of SDL gives the text events of a text field (ADR-0027 item 5).
        _ = SDL_StartTextInput(_window);
    }

    private readonly IntPtr _handle;
    private static Window? s_hooked;

    // Register entry R-0043 and finding V85 of the codebase review of 2026-11-05 (b) (TASK-0188, TASK-0191). SDL 3.2 gives
    // a press and a release of a mouse button at the last place of a motion (SDL_mouse.c fills button.x from mouse->x),
    // and its Windows backend does not read the place of the button message. A cursor that moves with no motion message
    // before a press, as a synthetic click does, gives the press at an old place. TASK-0188 took the place of the cursor
    // at the poll, and a hand that moves on after a press then moved the click (V85). The hook sees each message before
    // SDL reads it. When the place of a button message of this window is not the place that SDL holds, it warps the mouse
    // of SDL to the place of the message, where the cursor already is. SDL then gives a motion and the press at the true
    // place. A hand gives a motion before each press, so SDL holds the place of the press and the hook does nothing.
    [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static SDLBool Hook(IntPtr userdata, tagMSG message)
    {
        var window = s_hooked;
        var msg = (Win32.Message*)message.Value;
        if (window is null || msg is null || msg->Window != window._handle || Win32.FromPenOrTouch(Win32.GetMessageExtraInfo()))
            return true;

        float x, y;
        _ = SDL_GetMouseState(&x, &y);
        if (Correction(msg->Kind, msg->LParam, x, y) is { } place)
        {
            Console.WriteLine($"[INFO] A button message at ({place.X}, {place.Y}) moves the mouse of SDL from ({x}, {y}) before SDL reads it (R-0043).");
            SDL_WarpMouseInWindow(window._window, place.X, place.Y);
        }
        return true;
    }

    // The place of a button message in the coordinates of the window, when it is not the place that SDL holds; null for
    // another message or the same place. The message gives its place in the low and the high word of lParam, signed.
    public static (float X, float Y)? Correction(uint message, nint lParam, float heldX, float heldY)
    {
        if (message is < Win32.FirstButtonMessage or > Win32.LastButtonMessage)
            return null;
        float x = (short)(lParam & 0xFFFF);
        float y = (short)((lParam >> 16) & 0xFFFF);
        return x == heldX && y == heldY ? null : (x, y);
    }

    private static class Win32
    {
        // WM_LBUTTONDOWN to WM_MBUTTONDBLCLK: the left, the right and the middle button, each with a press, a release and
        // a double click.
        internal const uint FirstButtonMessage = 0x0201;
        internal const uint LastButtonMessage = 0x0209;

        // The layout of MSG on x64 and arm64.
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct Message
        {
            public IntPtr Window;
            public uint Kind;
            public nint WParam;
            public nint LParam;
            public uint Time;
            public int X;
            public int Y;
            public uint Private;
        }

        // A message that Windows makes from a pen or a touch carries this mark; SDL leaves it to its pen and touch events.
        internal static bool FromPenOrTouch(nint extraInfo) => ((ulong)extraInfo & 0xFFFFFF00UL) == 0xFF515700UL;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern nint GetMessageExtraInfo();
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
