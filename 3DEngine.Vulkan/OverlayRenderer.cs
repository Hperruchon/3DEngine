using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// One draw of the overlay: a triangle list in window points, with y down, and one colour.
public sealed record OverlayDraw(Vector2[] Triangles, Vector4 Colour);

// The overlay of the scene: the translate gizmo (ADR-0038 item 9, TASK-0113). It draws after the
// scene and the highlight and before the user interface, in subpass 0 of the render pass of its
// target. The pipeline tests no depth, culls nothing and blends with the alpha of the colour, so
// the gizmo shows in front of each body. The push block gives the scale and the translation of
// ui.vert, and the colour of the draw.
//
// The vertices of a frame go into a buffer that the CPU writes, one for each slot that the caller
// names, as in UiRenderer.
public sealed unsafe class OverlayRenderer : IDisposable
{
    public const uint PushConstantSize = 8 * sizeof(float);
    public const uint VertexStride = 2 * sizeof(float);

    private readonly GraphicsDevice _device;
    private readonly Dictionary<int, GpuBuffer> _slots = new();

    public OverlayRenderer(GraphicsDevice device, VkRenderPass renderPass)
    {
        _device = device;
        var api = device.DeviceApi;

        VkShaderModule vertex = device.CreateShaderModule(ShaderLibrary.Load("overlay.vert"));
        VkShaderModule fragment = device.CreateShaderModule(ShaderLibrary.Load("overlay.frag"));
        try
        {
            VkPushConstantRange pushConstant = new() { stageFlags = VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment, offset = 0, size = PushConstantSize };
            VkPipelineLayoutCreateInfo layoutInfo = new() { pushConstantRangeCount = 1, pPushConstantRanges = &pushConstant };
            api.vkCreatePipelineLayout(&layoutInfo, null, out VkPipelineLayout layout).CheckResult();
            Layout = layout;

            fixed (byte* main = "main"u8)
            {
                VkPipelineShaderStageCreateInfo* stages = stackalloc VkPipelineShaderStageCreateInfo[2];
                stages[0] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlags.Vertex, module = vertex, pName = main };
                stages[1] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlags.Fragment, module = fragment, pName = main };

                VkVertexInputBindingDescription binding = new() { binding = 0, stride = VertexStride, inputRate = VkVertexInputRate.Vertex };
                VkVertexInputAttributeDescription position = new() { location = 0, binding = 0, format = VkFormat.R32G32Sfloat, offset = 0 };
                VkPipelineVertexInputStateCreateInfo vertexInput = new()
                {
                    vertexBindingDescriptionCount = 1,
                    pVertexBindingDescriptions = &binding,
                    vertexAttributeDescriptionCount = 1,
                    pVertexAttributeDescriptions = &position,
                };
                VkPipelineInputAssemblyStateCreateInfo inputAssembly = new() { topology = VkPrimitiveTopology.TriangleList };
                VkPipelineViewportStateCreateInfo viewportState = new() { viewportCount = 1, scissorCount = 1 };
                VkPipelineRasterizationStateCreateInfo rasterization = new()
                {
                    polygonMode = VkPolygonMode.Fill,
                    cullMode = VkCullModeFlags.None,
                    frontFace = VkFrontFace.CounterClockwise,
                    lineWidth = 1.0f,
                };
                VkPipelineMultisampleStateCreateInfo multisample = new() { rasterizationSamples = VkSampleCountFlags.Count1 };

                // The render pass has a depth image; the overlay neither tests it nor writes it.
                VkPipelineDepthStencilStateCreateInfo depthStencil = new() { depthTestEnable = false, depthWriteEnable = false };
                VkPipelineColorBlendAttachmentState blendAttachment = new()
                {
                    blendEnable = true,
                    srcColorBlendFactor = VkBlendFactor.SrcAlpha,
                    dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                    colorBlendOp = VkBlendOp.Add,
                    srcAlphaBlendFactor = VkBlendFactor.One,
                    dstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                    alphaBlendOp = VkBlendOp.Add,
                    colorWriteMask = VkColorComponentFlags.All,
                };
                VkPipelineColorBlendStateCreateInfo colorBlend = new() { attachmentCount = 1, pAttachments = &blendAttachment };
                VkDynamicState* dynamicStates = stackalloc VkDynamicState[] { VkDynamicState.Viewport, VkDynamicState.Scissor };
                VkPipelineDynamicStateCreateInfo dynamicState = new() { dynamicStateCount = 2, pDynamicStates = dynamicStates };

                VkGraphicsPipelineCreateInfo pipelineInfo = new()
                {
                    stageCount = 2,
                    pStages = stages,
                    pVertexInputState = &vertexInput,
                    pInputAssemblyState = &inputAssembly,
                    pViewportState = &viewportState,
                    pRasterizationState = &rasterization,
                    pMultisampleState = &multisample,
                    pDepthStencilState = &depthStencil,
                    pColorBlendState = &colorBlend,
                    pDynamicState = &dynamicState,
                    layout = Layout,
                    renderPass = renderPass,
                    subpass = 0,
                };

                VkPipeline pipeline;
                api.vkCreateGraphicsPipelines(VkPipelineCache.Null, 1, &pipelineInfo, &pipeline).CheckResult();
                Pipeline = pipeline;
            }
        }
        finally
        {
            api.vkDestroyShaderModule(vertex);
            api.vkDestroyShaderModule(fragment);
        }
    }

    public VkPipeline Pipeline { get; }

    public VkPipelineLayout Layout { get; }

    // Records the draws inside the render pass of the target. The display is the size of the
    // window in points; the extent is the size of the target in pixels. The slot is the
    // swapchain image of the frame, or 0 for a target that waits for each frame.
    public void Record(VkCommandBuffer commandBuffer, VkExtent2D extent, Vector2 display, IReadOnlyList<OverlayDraw> draws, int slot)
    {
        var count = draws.Sum(draw => draw.Triangles.Length);
        if (count == 0)
            return;

        var vertices = new Vector2[count];
        var at = 0;
        foreach (var draw in draws)
        {
            draw.Triangles.CopyTo(vertices, at);
            at += draw.Triangles.Length;
        }

        var api = _device.DeviceApi;
        var buffer = Buffer(slot, (ulong)count * VertexStride);
        buffer.Write(MemoryMarshal.AsBytes(vertices.AsSpan()));

        api.vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, Pipeline);
        api.vkCmdSetViewport(commandBuffer, 0, new VkViewport { x = 0, y = 0, width = extent.width, height = extent.height, minDepth = 0, maxDepth = 1 });
        api.vkCmdSetScissor(commandBuffer, 0, new VkRect2D(0, 0, extent.width, extent.height));
        VkBuffer vertexBuffer = buffer.Handle;
        ulong offset = 0;
        api.vkCmdBindVertexBuffers(commandBuffer, 0, 1, &vertexBuffer, &offset);

        var (scale, translation) = UiRenderer.Transform(Vector2.Zero, display);
        float* block = stackalloc float[8];
        var first = 0u;
        foreach (var draw in draws)
        {
            var length = (uint)draw.Triangles.Length;
            if (length > 0)
            {
                (block[0], block[1], block[2], block[3]) = (scale.X, scale.Y, translation.X, translation.Y);
                (block[4], block[5], block[6], block[7]) = (draw.Colour.X, draw.Colour.Y, draw.Colour.Z, draw.Colour.W);
                api.vkCmdPushConstants(commandBuffer, Layout, VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment, 0, PushConstantSize, block);
                api.vkCmdDraw(commandBuffer, length, 1, first, 0);
            }
            first += length;
        }
    }

    // The buffer of a slot, made again when a frame needs more room.
    private GpuBuffer Buffer(int slot, ulong bytes)
    {
        _slots.TryGetValue(slot, out var buffer);
        if (buffer is null || buffer.Size < bytes)
        {
            buffer?.Dispose();
            buffer = new GpuBuffer(_device, VkBufferUsageFlags.VertexBuffer, new byte[Math.Max(bytes * 2, 4 * 1024)]);
            _slots[slot] = buffer;
        }
        return buffer;
    }

    // The caller waits until the device is idle before it disposes the renderer.
    public void Dispose()
    {
        foreach (var buffer in _slots.Values)
            buffer.Dispose();
        _slots.Clear();
        _device.DeviceApi.vkDestroyPipeline(Pipeline);
        _device.DeviceApi.vkDestroyPipelineLayout(Layout);
    }
}
