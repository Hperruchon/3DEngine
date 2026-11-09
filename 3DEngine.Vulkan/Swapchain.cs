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
        Extent = ChooseSwapExtent(swapChainSupport.Capabilities, () => window.PixelExtent);
        Depth = new DepthBuffer(device, Extent);

        uint imageCount = swapChainSupport.Capabilities.minImageCount + 1;
        if (swapChainSupport.Capabilities.maxImageCount > 0 &&
            imageCount > swapChainSupport.Capabilities.maxImageCount)
        {
            imageCount = swapChainSupport.Capabilities.maxImageCount;
        }

        // Finding V7 of the codebase review of 2026-09-30, part 1 (R-0030, TASK-0319): when the graphics family and the
        // present family differ, the images of the swapchain are shared by both families; until TASK-0319 they were
        // exclusive to one family, which the specification does not permit with no transfer of ownership.
        var sharing = Sharing(device.GraphicsFamily, device.PresentFamily);
        uint* families = stackalloc uint[2] { device.GraphicsFamily, device.PresentFamily };
        VkSwapchainCreateInfoKHR createInfo = new()
        {
            surface = surface,
            minImageCount = imageCount,
            imageFormat = surfaceFormat.format,
            imageColorSpace = surfaceFormat.colorSpace,
            imageExtent = Extent,
            imageArrayLayers = 1,
            imageUsage = VkImageUsageFlags.ColorAttachment,
            imageSharingMode = sharing.Mode,
            queueFamilyIndexCount = sharing.Count,
            pQueueFamilyIndices = families,
            preTransform = swapChainSupport.Capabilities.currentTransform,
            compositeAlpha = ChooseCompositeAlpha(swapChainSupport.Capabilities.supportedCompositeAlpha),
            presentMode = presentMode,
            clipped = true,
            oldSwapchain = oldSwapchain
        };

        // The depth image exists before the swapchain, so a failed creation frees it
        // (finding V18 of the codebase review of 2026-10-28, TASK-0090). One try covers the
        // whole constructor: until TASK-0099 a throw after the swapchain existed leaked the
        // handle, the depth image and the views (finding V39 of the second codebase review
        // of 2026-10-28). The driver retired the old swapchain at the creation, so the retry
        // of the Presenter with no old swapchain stays correct.
        _swapChainImageViews = [];
        Framebuffers = [];
        try
        {
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
                Device.DeviceApi.vkCreateFramebuffer(RenderPass, new[] { _swapChainImageViews[i], Depth.View }, Extent, 1u, out Framebuffers[i]).CheckResult();
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
        => Release(
            Framebuffers,
            _swapChainImageViews,
            Handle,
            framebuffer => Device.DeviceApi.vkDestroyFramebuffer(framebuffer),
            view => Device.DeviceApi.vkDestroyImageView(view),
            Depth.Dispose,
            swapchain => Device.DeviceApi.vkDestroySwapchainKHR(swapchain));

    // Destroys each object that exists, so that a constructor that failed at any step frees
    // what it made: each framebuffer before its view, the depth image, then the swapchain.
    public static void Release(
        IReadOnlyList<VkFramebuffer> framebuffers,
        IReadOnlyList<VkImageView> views,
        VkSwapchainKHR handle,
        Action<VkFramebuffer> destroyFramebuffer,
        Action<VkImageView> destroyView,
        Action disposeDepth,
        Action<VkSwapchainKHR> destroySwapchain)
    {
        foreach (var framebuffer in framebuffers)
        {
            if (framebuffer != VkFramebuffer.Null)
                destroyFramebuffer(framebuffer);
        }

        foreach (var view in views)
        {
            if (view != VkImageView.Null)
                destroyView(view);
        }

        disposeDepth();

        if (handle != VkSwapchainKHR.Null)
            destroySwapchain(handle);
    }

    // Finding V7 part 1 (TASK-0319): exclusive images for one family, and images that two families share for two.
    public static (VkSharingMode Mode, uint Count) Sharing(uint graphicsFamily, uint presentFamily)
        => graphicsFamily == presentFamily || presentFamily == VK_QUEUE_FAMILY_IGNORED ? (VkSharingMode.Exclusive, 0u) : (VkSharingMode.Concurrent, 2u);

    // Finding V2 of the codebase review of 2026-09-23 (R-0022, TASK-0319): a surface that leaves the extent to the swapchain
    // gives 0xFFFFFFFF, and then the size of the window in pixels applies, inside the limits of the surface. Until TASK-0319
    // the check was "greater than 0", so 0xFFFFFFFF passed, and the fallback took the size in logical units.
    public static VkExtent2D ChooseSwapExtent(VkSurfaceCapabilitiesKHR capabilities, Func<VkExtent2D> pixels)
    {
        if (capabilities.currentExtent.width != uint.MaxValue)
        {
            return capabilities.currentExtent;
        }
        else
        {
            VkExtent2D actualExtent = pixels();

            actualExtent = new VkExtent2D(
                Math.Max(capabilities.minImageExtent.width, Math.Min(capabilities.maxImageExtent.width, actualExtent.width)),
                Math.Max(capabilities.minImageExtent.height, Math.Min(capabilities.maxImageExtent.height, actualExtent.height))
                );

            return actualExtent;
        }
    }

    public static VkSurfaceFormatKHR ChooseSwapSurfaceFormat(ReadOnlySpan<VkSurfaceFormatKHR> availableFormats)
    {
        // If the surface format list only includes one entry with VK_FORMAT_UNDEFINED,
        // there is no preferred format, so we assume VK_FORMAT_B8G8R8A8_UNORM
        if ((availableFormats.Length == 1) && (availableFormats[0].format == VkFormat.Undefined))
        {
            return new VkSurfaceFormatKHR(VkFormat.B8G8R8A8Unorm, availableFormats[0].colorSpace);
        }

        // iterate over the list of available surface format and
        // check for the presence of VK_FORMAT_B8G8R8A8_UNORM.
        // Finding V7 of the codebase review of 2026-09-30, part 2 (R-0030, TASK-0319): with the colour space of sRGB, because
        // the device enables the extension of the colour spaces, and a surface can give the format in another space too.
        foreach (VkSurfaceFormatKHR availableFormat in availableFormats)
        {
            if (availableFormat.format == VkFormat.B8G8R8A8Unorm && availableFormat.colorSpace == VkColorSpaceKHR.SrgbNonLinear)
            {
                return availableFormat;
            }
        }

        return availableFormats[0];
    }

    // The composite alpha of the swapchain: opaque when the surface gives it, else the
    // first mode that the surface gives in this order (finding V20 of the codebase review
    // of 2026-10-17, TASK-0079). Until TASK-0079 the swapchain asked for opaque and did
    // not read the modes of the surface, and some surfaces on Linux do not give opaque
    // (register entry R-0022).
    public static VkCompositeAlphaFlagsKHR ChooseCompositeAlpha(VkCompositeAlphaFlagsKHR supported)
    {
        VkCompositeAlphaFlagsKHR[] order =
        [
            VkCompositeAlphaFlagsKHR.Opaque,
            VkCompositeAlphaFlagsKHR.Inherit,
            VkCompositeAlphaFlagsKHR.PreMultiplied,
            VkCompositeAlphaFlagsKHR.PostMultiplied,
        ];

        foreach (var mode in order)
        {
            if ((supported & mode) != 0)
                return mode;
        }

        throw new InvalidOperationException($"The surface gives no composite alpha mode (supportedCompositeAlpha = {supported}).");
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
