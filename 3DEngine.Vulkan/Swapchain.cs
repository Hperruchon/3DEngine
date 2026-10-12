// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See THIRD-PARTY-NOTICES.md, section 4.
// This file comes from the Vortice.Vulkan sample framework. TASK-0027 moved it
// into this project and changed it. ADR-0017 gives the rules for this project.

using System; 
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace ThreeDEngine.Vulkan;

public sealed unsafe class Swapchain : IDisposable
{
    public readonly GraphicsDevice Device;
    public readonly Window Window;
    private readonly VkImageView[] _swapChainImageViews;
    private readonly VkSurfaceKHR _surface;

    public VkSwapchainKHR Handle;
    public int ImageCount => _swapChainImageViews.Length;
    public VkRenderPass RenderPass;
    public VkExtent2D Extent { get; }
    public VkFramebuffer[] Framebuffers { get; }

    // The depth image of the render pass (ADR-0023 item 1, TASK-0070).
    public DepthBuffer Depth { get; }

    public Swapchain(GraphicsDevice device, VkSurfaceKHR surface, Window window)
    {
        Device = device;
        _surface = surface;
        Window = window;

        SwapChainSupportDetails swapChainSupport = Utils.QuerySwapChainSupport(device.InstanceApi, device.PhysicalDevice, surface);

        VkSurfaceFormatKHR surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        VkPresentModeKHR presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
        Extent = ChooseSwapExtent(swapChainSupport.Capabilities);
        Depth = new DepthBuffer(device, Extent);

        CreateRenderPass(surfaceFormat.format);

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
            oldSwapchain = VkSwapchainKHR.Null
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

        Device.DeviceApi.vkDestroyRenderPass(RenderPass);
        Depth.Dispose();

        if (Handle != VkSwapchainKHR.Null)
        {
            Device.DeviceApi.vkDestroySwapchainKHR(Handle);
        }

        if (_surface != VkSurfaceKHR.Null)
        {
            Device.InstanceApi.vkDestroySurfaceKHR(_surface);
        }
    }

    private void CreateRenderPass(VkFormat colorFormat)
    {
        VkAttachmentDescription attachment = new VkAttachmentDescription(
            colorFormat,
            VkSampleCountFlags.Count1,
            VkAttachmentLoadOp.Clear, VkAttachmentStoreOp.Store,
            VkAttachmentLoadOp.DontCare, VkAttachmentStoreOp.DontCare,
            VkImageLayout.Undefined, VkImageLayout.PresentSrcKHR
        );

        // The depth attachment (ADR-0023 item 1): cleared at the start of the pass, and
        // not stored, because no later pass reads it.
        VkAttachmentDescription* attachments = stackalloc VkAttachmentDescription[2];
        attachments[0] = attachment;
        attachments[1] = new VkAttachmentDescription(
            DepthBuffer.Format,
            VkSampleCountFlags.Count1,
            VkAttachmentLoadOp.Clear, VkAttachmentStoreOp.DontCare,
            VkAttachmentLoadOp.DontCare, VkAttachmentStoreOp.DontCare,
            VkImageLayout.Undefined, VkImageLayout.DepthStencilAttachmentOptimal
        );

        VkAttachmentReference colorAttachmentRef = new VkAttachmentReference(0, VkImageLayout.ColorAttachmentOptimal);
        VkAttachmentReference depthAttachmentRef = new VkAttachmentReference(1, VkImageLayout.DepthStencilAttachmentOptimal);

        VkSubpassDescription subpass = new VkSubpassDescription
        {
            pipelineBindPoint = VkPipelineBindPoint.Graphics,
            colorAttachmentCount = 1,
            pColorAttachments = &colorAttachmentRef,
            pDepthStencilAttachment = &depthAttachmentRef
        };

        VkSubpassDependency[] dependencies = new VkSubpassDependency[2];

        dependencies[0] = new VkSubpassDependency
        {
            srcSubpass = VK_SUBPASS_EXTERNAL,
            dstSubpass = 0,
            srcStageMask = VkPipelineStageFlags.BottomOfPipe | VkPipelineStageFlags.LateFragmentTests,
            dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput | VkPipelineStageFlags.EarlyFragmentTests,
            srcAccessMask = VkAccessFlags.MemoryRead | VkAccessFlags.DepthStencilAttachmentWrite,
            dstAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite | VkAccessFlags.DepthStencilAttachmentWrite,
            dependencyFlags = VkDependencyFlags.ByRegion
        };

        dependencies[1] = new VkSubpassDependency
        {
            srcSubpass = 0,
            dstSubpass = VK_SUBPASS_EXTERNAL,
            srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
            dstStageMask = VkPipelineStageFlags.BottomOfPipe,
            srcAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite,
            dstAccessMask = VkAccessFlags.MemoryRead,
            dependencyFlags = VkDependencyFlags.ByRegion
        };

        fixed (VkSubpassDependency* dependenciesPtr = &dependencies[0])
        {
            VkRenderPassCreateInfo createInfo = new()
            {
                attachmentCount = 2,
                pAttachments = attachments,
                subpassCount = 1,
                pSubpasses = &subpass,
                dependencyCount = 2,
                pDependencies = dependenciesPtr
            };

            Device.DeviceApi.vkCreateRenderPass(&createInfo, null, out RenderPass).CheckResult();
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

    private static VkSurfaceFormatKHR ChooseSwapSurfaceFormat(ReadOnlySpan<VkSurfaceFormatKHR> availableFormats)
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
