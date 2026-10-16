// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See THIRD-PARTY-NOTICES.md, section 4.
// This file comes from the Vortice.Vulkan sample framework. TASK-0027 moved it
// into this project and changed it. ADR-0017 gives the rules for this project.

using System; 
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace ThreeDEngine.Vulkan;

// One swapchain of a surface, with its framebuffers and its depth image. The Presenter
// owns the surface and the render pass, and makes a new swapchain when the window
// changes (TASK-0074); a swapchain therefore destroys neither of the two.
public sealed unsafe class Swapchain : IDisposable
{
    public readonly GraphicsDevice Device;
    public readonly Window Window;
    private readonly VkImageView[] _swapChainImageViews;

    public VkSwapchainKHR Handle;
    public int ImageCount => _swapChainImageViews.Length;
    public readonly VkRenderPass RenderPass;
    public VkExtent2D Extent { get; }
    public VkFramebuffer[] Framebuffers { get; }

    // The depth image of the render pass (ADR-0023 item 1, TASK-0070).
    public DepthBuffer Depth { get; }

    // The old swapchain, when not null, goes to the driver so that it can reuse its
    // images; the caller disposes it after this one exists.
    public Swapchain(GraphicsDevice device, VkSurfaceKHR surface, Window window, VkRenderPass renderPass, VkSurfaceFormatKHR surfaceFormat, VkSwapchainKHR oldSwapchain)
    {
        Device = device;
        Window = window;
        RenderPass = renderPass;

        SwapChainSupportDetails swapChainSupport = Utils.QuerySwapChainSupport(device.InstanceApi, device.PhysicalDevice, surface);

        VkPresentModeKHR presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
        Extent = ChooseSwapExtent(swapChainSupport.Capabilities);
        Depth = new DepthBuffer(device, Extent);

        uint imageCount = swapChainSupport.Capabilities.minImageCount + 1;
        if (swapChainSupport.Capabilities.maxImageCount > 0 &&
            imageCount > swapChainSupport.Capabilities.maxImageCount)
        {
            imageCount = swapChainSupport.Capabilities.maxImageCount;
        }

        VkSwapchainCreateInfoKHR createInfo = new()
        {
            surface = surface,
            minImageCount = imageCount,
            imageFormat = surfaceFormat.format,
            imageColorSpace = surfaceFormat.colorSpace,
            imageExtent = Extent,
            imageArrayLayers = 1,
            imageUsage = VkImageUsageFlags.ColorAttachment,
            imageSharingMode = VkSharingMode.Exclusive,
            preTransform = swapChainSupport.Capabilities.currentTransform,
            compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque,
            presentMode = presentMode,
            clipped = true,
            oldSwapchain = oldSwapchain
        };

        device.DeviceApi.vkCreateSwapchainKHR(&createInfo, null, out Handle).CheckResult();
        // 3.x returns no span. Ask for the count, then fill an array of that size.
        device.DeviceApi.vkGetSwapchainImagesKHR(Handle, out uint swapChainImageCount).CheckResult();
        VkImage[] swapChainImages = new VkImage[swapChainImageCount];
        device.DeviceApi.vkGetSwapchainImagesKHR(Handle, swapChainImages).CheckResult();
        _swapChainImageViews = new VkImageView[swapChainImages.Length];
        Framebuffers = new VkFramebuffer[swapChainImages.Length];

        for (int i = 0; i < swapChainImages.Length; i++)
        {
            var viewCreateInfo = new VkImageViewCreateInfo(
                swapChainImages[i],
                VkImageViewType.Image2D,
                surfaceFormat.format,
                VkComponentMapping.Rgba,
                new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1)
                );

            Device.DeviceApi.vkCreateImageView(&viewCreateInfo, null, out _swapChainImageViews[i]).CheckResult();
            Device.DeviceApi.vkCreateFramebuffer(RenderPass, new[] { _swapChainImageViews[i], Depth.View }, Extent, 1u, out Framebuffers[i]);
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < _swapChainImageViews.Length; i++)
        {
            Device.DeviceApi.vkDestroyImageView(_swapChainImageViews[i]);
        }

        for (int i = 0; i < Framebuffers.Length; i++)
        {
            Device.DeviceApi.vkDestroyFramebuffer(Framebuffers[i]);
        }

        Depth.Dispose();

        if (Handle != VkSwapchainKHR.Null)
        {
            Device.DeviceApi.vkDestroySwapchainKHR(Handle);
        }
    }

    private VkExtent2D ChooseSwapExtent(VkSurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.currentExtent.width > 0)
        {
            return capabilities.currentExtent;
        }
        else
        {
            VkExtent2D actualExtent = Window.Extent;

            actualExtent = new VkExtent2D(
                Math.Max(capabilities.minImageExtent.width, Math.Min(capabilities.maxImageExtent.width, actualExtent.width)),
                Math.Max(capabilities.minImageExtent.height, Math.Min(capabilities.maxImageExtent.height, actualExtent.height))
                );

            return actualExtent;
        }
    }

    internal static VkSurfaceFormatKHR ChooseSwapSurfaceFormat(ReadOnlySpan<VkSurfaceFormatKHR> availableFormats)
    {
        // If the surface format list only includes one entry with VK_FORMAT_UNDEFINED,
        // there is no preferred format, so we assume VK_FORMAT_B8G8R8A8_UNORM
        if ((availableFormats.Length == 1) && (availableFormats[0].format == VkFormat.Undefined))
        {
            return new VkSurfaceFormatKHR(VkFormat.B8G8R8A8Unorm, availableFormats[0].colorSpace);
        }

        // iterate over the list of available surface format and
        // check for the presence of VK_FORMAT_B8G8R8A8_UNORM
        foreach (VkSurfaceFormatKHR availableFormat in availableFormats)
        {
            if (availableFormat.format == VkFormat.B8G8R8A8Unorm)
            {
                return availableFormat;
            }
        }

        return availableFormats[0];
    }

    private static VkPresentModeKHR ChooseSwapPresentMode(ReadOnlySpan<VkPresentModeKHR> availablePresentModes)
    {
        foreach (VkPresentModeKHR availablePresentMode in availablePresentModes)
        {
            if (availablePresentMode == VkPresentModeKHR.Mailbox)
            {
                return availablePresentMode;
            }
        }

        return VkPresentModeKHR.Fifo;
    }
}
