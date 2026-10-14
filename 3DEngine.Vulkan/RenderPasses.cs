// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See THIRD-PARTY-NOTICES.md, section 4.
// This file comes from the Vortice.Vulkan sample framework. TASK-0027 moved it
// into this project and changed it. ADR-0017 gives the rules for this project.

using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace ThreeDEngine.Vulkan;

// The render pass of a target (ADR-0022 item 2, ADR-0025 item 2): one colour
// attachment, cleared and stored, and the depth attachment of ADR-0023 item 1. The
// swapchain gives its colour to the present, and the offscreen target gives it to a
// copy; the last layout and the dependency after the pass follow that use.
// TASK-0076 moved this method out of Swapchain.
internal static unsafe class RenderPasses
{
    public static VkRenderPass Create(GraphicsDevice device, VkFormat colorFormat, VkImageLayout finalColorLayout)
    {
        VkAttachmentDescription attachment = new VkAttachmentDescription(
            colorFormat,
            VkSampleCountFlags.Count1,
            VkAttachmentLoadOp.Clear, VkAttachmentStoreOp.Store,
            VkAttachmentLoadOp.DontCare, VkAttachmentStoreOp.DontCare,
            VkImageLayout.Undefined, finalColorLayout
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

        // A copy reads the colour after the pass; a present reads it at the bottom of
        // the pipeline, after the semaphore of the submit.
        var toCopy = finalColorLayout == VkImageLayout.TransferSrcOptimal;

        VkSubpassDependency* dependencies = stackalloc VkSubpassDependency[2];

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
            dstStageMask = toCopy ? VkPipelineStageFlags.Transfer : VkPipelineStageFlags.BottomOfPipe,
            srcAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite,
            dstAccessMask = toCopy ? VkAccessFlags.TransferRead : VkAccessFlags.MemoryRead,
            dependencyFlags = VkDependencyFlags.ByRegion
        };

        VkRenderPassCreateInfo createInfo = new()
        {
            attachmentCount = 2,
            pAttachments = attachments,
            subpassCount = 1,
            pSubpasses = &subpass,
            dependencyCount = 2,
            pDependencies = dependencies
        };

        device.DeviceApi.vkCreateRenderPass(&createInfo, null, out VkRenderPass renderPass).CheckResult();
        return renderPass;
    }
}
