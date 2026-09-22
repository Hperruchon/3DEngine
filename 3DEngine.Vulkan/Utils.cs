// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See THIRD-PARTY-NOTICES.md, section 4.
// This file comes from the Vortice.Vulkan sample framework. TASK-0027 moved it
// into this project and changed it. ADR-0017 gives the rules for this project.

using System;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace ThreeDEngine.Vulkan;

internal ref struct SwapChainSupportDetails
{
    public VkSurfaceCapabilitiesKHR Capabilities;
    public ReadOnlySpan<VkSurfaceFormatKHR> Formats;
    public ReadOnlySpan<VkPresentModeKHR> PresentModes;
}


internal static class Utils
{
    public static SwapChainSupportDetails QuerySwapChainSupport(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface)
    {
        SwapChainSupportDetails details = new SwapChainSupportDetails();
        vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physicalDevice, surface, out details.Capabilities).CheckResult();

        details.Formats = vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface);
        details.PresentModes = vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface);
        return details;
    }
}
