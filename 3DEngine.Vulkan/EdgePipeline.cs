using System.Numerics;
using ThreeDEngine.Core.Rendering;
using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// The pipeline of the edge lines of a body (ADR-0109 item 2, phase R18b, TASK-0333), in subpass 0 of the render pass of
// its target. It reads the vertices of EdgeLines, culls nothing, writes no depth and blends with the alpha of edge.frag.
// A visible pipeline keeps a line at or in front of the stored depth; a hidden pipeline keeps a line behind it, for the
// dashed hidden edges of the selected body (ADR-0109 item 3). The push block holds the model-view-projection matrix of the
// draw, the colour, and the size of the target, the half width and the dash in pixels: 96 bytes, inside the 128 bytes that
// each Vulkan device gives.
public sealed unsafe class EdgePipeline : IDisposable
{
    public const uint PushConstantSize = (16 + 4 + 4) * sizeof(float);

    private readonly GraphicsDevice _device;

    public EdgePipeline(GraphicsDevice device, VkRenderPass renderPass, bool hidden)
    {
        _device = device;
        Hidden = hidden;
        var api = device.DeviceApi;

        VkShaderModule vertex = device.CreateShaderModule(ShaderLibrary.Load("edge.vert"));
        VkShaderModule fragment = device.CreateShaderModule(ShaderLibrary.Load("edge.frag"));
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

                VkVertexInputBindingDescription binding = new() { binding = 0, stride = EdgeLines.VertexStride, inputRate = VkVertexInputRate.Vertex };
                VkVertexInputAttributeDescription* attributes = stackalloc VkVertexInputAttributeDescription[3];
                attributes[0] = new() { location = 0, binding = 0, format = VkFormat.R32G32B32Sfloat, offset = 0 };
                attributes[1] = new() { location = 1, binding = 0, format = VkFormat.R32G32B32Sfloat, offset = 3 * sizeof(float) };
                attributes[2] = new() { location = 2, binding = 0, format = VkFormat.R32G32Sfloat, offset = 6 * sizeof(float) };
                VkPipelineVertexInputStateCreateInfo vertexInput = new()
                {
                    vertexBindingDescriptionCount = 1,
                    pVertexBindingDescriptions = &binding,
                    vertexAttributeDescriptionCount = 3,
                    pVertexAttributeDescriptions = attributes,
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
                VkPipelineDepthStencilStateCreateInfo depthStencil = new()
                {
                    depthTestEnable = true,
                    depthWriteEnable = false,
                    depthCompareOp = hidden ? VkCompareOp.Less : VkCompareOp.GreaterOrEqual,
                };
                VkPipelineColorBlendAttachmentState blendAttachment = new()
                {
                    blendEnable = true,
                    srcColorBlendFactor = VkBlendFactor.SrcAlpha,
                    dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                    colorBlendOp = VkBlendOp.Add,
                    srcAlphaBlendFactor = VkBlendFactor.Zero,
                    dstAlphaBlendFactor = VkBlendFactor.One,
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
                Handle = pipeline;
            }
        }
        finally
        {
            api.vkDestroyShaderModule(vertex);
            api.vkDestroyShaderModule(fragment);
        }
    }

    public bool Hidden { get; }

    public VkPipeline Handle { get; }

    public VkPipelineLayout Layout { get; }

    // Draws the edges of each record whose mesh has edges, in one colour, with a half width and a dash in pixels (a dash
    // of 0 gives a solid line). The viewport is the one of the mesh pipeline, with y up.
    public void Execute(
        VkCommandBuffer commandBuffer,
        VkExtent2D extent,
        IEnumerable<DrawRecord> records,
        IReadOnlyDictionary<string, GpuMesh> meshes,
        Matrix4x4 projection,
        Vector4 colour,
        float halfWidth,
        float dash)
    {
        var api = _device.DeviceApi;
        var bound = false;
        float* block = stackalloc float[(int)(PushConstantSize / sizeof(float))];
        foreach (var record in records)
        {
            if (!meshes.TryGetValue(record.MeshId, out var mesh) || mesh.EdgeVertices is not { } edges)
                continue;
            if (!bound)
            {
                api.vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, Handle);
                api.vkCmdSetViewport(commandBuffer, 0, RenderPipeline.Viewport(extent));
                api.vkCmdSetScissor(commandBuffer, 0, new VkRect2D(0, 0, extent.width, extent.height));
                bound = true;
            }

            // A matrix of System.Numerics acts on row vectors, so the product of the model-view and the projection is in
            // that order; the shader reads its bytes as the transpose, as RenderPipeline.Push says.
            var modelViewProjection = record.ModelView * projection;
            new ReadOnlySpan<float>(&modelViewProjection, 16).CopyTo(new Span<float>(block, 16));
            (block[16], block[17], block[18], block[19]) = (colour.X, colour.Y, colour.Z, colour.W);
            (block[20], block[21], block[22], block[23]) = (extent.width, extent.height, halfWidth, dash);
            api.vkCmdPushConstants(commandBuffer, Layout, VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment, 0, PushConstantSize, block);
            VkBuffer vertexBuffer = edges;
            ulong offset = 0;
            api.vkCmdBindVertexBuffers(commandBuffer, 0, 1, &vertexBuffer, &offset);
            api.vkCmdDraw(commandBuffer, mesh.EdgeVertexCount, 1, 0, 0);
        }
    }

    // The caller waits until the device is idle before it disposes a pipeline.
    public void Dispose()
    {
        _device.DeviceApi.vkDestroyPipeline(Handle);
        _device.DeviceApi.vkDestroyPipelineLayout(Layout);
    }
}
