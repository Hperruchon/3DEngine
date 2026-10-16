using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// The graphics pipeline of a mesh, in subpass 0 of the render pass of its target:
// the swapchain or the offscreen target (ADR-0022 item 2, ADR-0025 item 2, roadmap
// phase R3). It reads one MeshVertex for each vertex, and
// one push constant for the vertex shader: the model-view matrix of the draw and the
// projection (TASK-0070). It tests the depth and keeps the greater depth, which the
// reversed projection gives to a nearer fragment (ADR-0023 item 1). A test reads the
// fixed state below with no GPU.
//
// The viewport has a negative height, so y points up in clip space, as in the
// space of the engine. A triangle that is counter-clockwise when seen from outside
// a body (ADR-0018 item 9) is then counter-clockwise on the screen, and it is the
// front face; the pipeline culls each back face. A negative height is core in
// Vulkan 1.1, and the instance asks for 1.3.
public sealed unsafe class RenderPipeline : IDisposable
{
    public const VkPrimitiveTopology Topology = VkPrimitiveTopology.TriangleList;
    public const VkCullModeFlags CullMode = VkCullModeFlags.Back;
    public const VkFrontFace FrontFace = VkFrontFace.CounterClockwise;
    public const VkCompareOp DepthCompare = VkCompareOp.Greater;
    public const uint PushConstantSize = 2 * 16 * sizeof(float);

    private readonly GraphicsDevice _device;

    public RenderPipeline(GraphicsDevice device, VkRenderPass renderPass, string vertexShader, string fragmentShader)
    {
        _device = device;

        VkShaderModule vertex = device.CreateShaderModule(ShaderLibrary.Load(vertexShader));
        VkShaderModule fragment = device.CreateShaderModule(ShaderLibrary.Load(fragmentShader));
        try
        {
            VkPushConstantRange pushConstant = new() { stageFlags = VkShaderStageFlags.Vertex, offset = 0, size = PushConstantSize };
            VkPipelineLayoutCreateInfo layoutInfo = new() { pushConstantRangeCount = 1, pPushConstantRanges = &pushConstant };
            device.DeviceApi.vkCreatePipelineLayout(&layoutInfo, null, out VkPipelineLayout layout).CheckResult();
            Layout = layout;

            fixed (byte* main = "main"u8)
            {
                VkPipelineShaderStageCreateInfo* stages = stackalloc VkPipelineShaderStageCreateInfo[2];
                stages[0] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlags.Vertex, module = vertex, pName = main };
                stages[1] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlags.Fragment, module = fragment, pName = main };

                VkVertexInputBindingDescription binding = MeshVertex.Binding;
                VkVertexInputAttributeDescription position = MeshVertex.Position;
                VkPipelineVertexInputStateCreateInfo vertexInput = new()
                {
                    vertexBindingDescriptionCount = 1,
                    pVertexBindingDescriptions = &binding,
                    vertexAttributeDescriptionCount = 1,
                    pVertexAttributeDescriptions = &position,
                };
                VkPipelineInputAssemblyStateCreateInfo inputAssembly = new() { topology = Topology };
                VkPipelineViewportStateCreateInfo viewportState = new() { viewportCount = 1, scissorCount = 1 };
                VkPipelineRasterizationStateCreateInfo rasterization = new()
                {
                    polygonMode = VkPolygonMode.Fill,
                    cullMode = CullMode,
                    frontFace = FrontFace,
                    lineWidth = 1.0f,
                };
                VkPipelineMultisampleStateCreateInfo multisample = new() { rasterizationSamples = VkSampleCountFlags.Count1 };
                VkPipelineDepthStencilStateCreateInfo depthStencil = new()
                {
                    depthTestEnable = true,
                    depthWriteEnable = true,
                    depthCompareOp = DepthCompare,
                };
                VkPipelineColorBlendAttachmentState blendAttachment = new() { colorWriteMask = VkColorComponentFlags.All };
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
                device.DeviceApi.vkCreateGraphicsPipelines(VkPipelineCache.Null, 1, &pipelineInfo, &pipeline).CheckResult();
                Handle = pipeline;
            }
        }
        finally
        {
            device.DeviceApi.vkDestroyShaderModule(vertex);
            device.DeviceApi.vkDestroyShaderModule(fragment);
        }
    }

    public VkPipeline Handle { get; }

    public VkPipelineLayout Layout { get; }

    // The viewport of the whole extent, with y up: it starts at the bottom edge and
    // has a negative height.
    public static VkViewport Viewport(VkExtent2D extent) => new()
    {
        x = 0,
        y = extent.height,
        width = extent.width,
        height = -(float)extent.height,
        minDepth = 0,
        maxDepth = 1,
    };

    // Binds the pipeline, and sets the viewport and the scissor of the extent.
    public void Bind(VkCommandBuffer commandBuffer, VkExtent2D extent)
    {
        _device.DeviceApi.vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, Handle);
        _device.DeviceApi.vkCmdSetViewport(commandBuffer, 0, Viewport(extent));
        _device.DeviceApi.vkCmdSetScissor(commandBuffer, 0, new VkRect2D(0, 0, extent.width, extent.height));
    }

    // Gives the matrices of one draw to the vertex shader. A matrix of System.Numerics
    // acts on row vectors; the shader reads its bytes as a column-major matrix, which
    // is the transpose, and that acts on column vectors in the same way.
    public void Push(VkCommandBuffer commandBuffer, Matrix4x4 modelView, Matrix4x4 projection)
    {
        byte* block = stackalloc byte[(int)PushConstantSize];
        WritePushConstants(new Span<byte>(block, (int)PushConstantSize), modelView, projection);
        _device.DeviceApi.vkCmdPushConstants(commandBuffer, Layout, VkShaderStageFlags.Vertex, 0, PushConstantSize, block);
    }

    // The bytes of the push constant block of one draw: the model-view matrix at offset
    // 0 and the projection at offset 64, as the block Draw of mesh.vert declares them. A
    // test reads the same block from the SPIR-V (finding V13 of the codebase review of
    // 2026-10-13, TASK-0074).
    public static void WritePushConstants(Span<byte> block, Matrix4x4 modelView, Matrix4x4 projection)
    {
        MemoryMarshal.Write(block[..64], in modelView);
        MemoryMarshal.Write(block[64..(int)PushConstantSize], in projection);
    }

    // The caller waits until the device is idle before it disposes a pipeline.
    public void Dispose()
    {
        _device.DeviceApi.vkDestroyPipeline(Handle);
        _device.DeviceApi.vkDestroyPipelineLayout(Layout);
    }
}
