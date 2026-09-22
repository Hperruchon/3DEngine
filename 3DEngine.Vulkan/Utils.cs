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
    public static SwapChainSupportDetails QuerySwapChainSupport(VkInstanceApi api, VkPhysicalDevice physicalDevice, VkSurfaceKHR surface)
    {
        SwapChainSupportDetails details = new SwapChainSupportDetails();
        api.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physicalDevice, surface, out details.Capabilities).CheckResult();

        // 3.x returns no span. Ask for the count, then fill an array of that size.
        api.vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, out uint formatCount).CheckResult();
        VkSurfaceFormatKHR[] formats = new VkSurfaceFormatKHR[formatCount];
        api.vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, formats).CheckResult();
        details.Formats = formats;

        api.vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, out uint presentModeCount).CheckResult();
        VkPresentModeKHR[] presentModes = new VkPresentModeKHR[presentModeCount];
        api.vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, presentModes).CheckResult();
        details.PresentModes = presentModes;

        return details;
    }
}
