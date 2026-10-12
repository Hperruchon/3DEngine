using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// A graphics pipeline in subpass 0 of the render pass of the swapchain (ADR-0022
// item 2, roadmap phase R3). The fixed state below is the same for each pipeline of
// R3, and a test reads it with no GPU.
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

    private readonly GraphicsDevice _device;

    public RenderPipeline(GraphicsDevice device, string vertexShader, string fragmentShader)
    {
        _device = device;

        VkShaderModule vertex = device.CreateShaderModule(ShaderLibrary.Load(vertexShader));
        VkShaderModule fragment = device.CreateShaderModule(ShaderLibrary.Load(fragmentShader));
        try
        {
            VkPipelineLayoutCreateInfo layoutInfo = new();
            device.DeviceApi.vkCreatePipelineLayout(&layoutInfo, null, out VkPipelineLayout layout).CheckResult();
            Layout = layout;

            fixed (byte* main = "main"u8)
            {
                VkPipelineShaderStageCreateInfo* stages = stackalloc VkPipelineShaderStageCreateInfo[2];
                stages[0] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlags.Vertex, module = vertex, pName = main };
                stages[1] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlags.Fragment, module = fragment, pName = main };

                VkPipelineVertexInputStateCreateInfo vertexInput = new();
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
                    pColorBlendState = &colorBlend,
                    pDynamicState = &dynamicState,
                    layout = Layout,
                    renderPass = device.Swapchain.RenderPass,
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

    // Binds the pipeline and sets the viewport and the scissor of the extent.
    public void Bind(VkCommandBuffer commandBuffer, VkExtent2D extent)
    {
        _device.DeviceApi.vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, Handle);
        _device.DeviceApi.vkCmdSetViewport(commandBuffer, 0, Viewport(extent));
        _device.DeviceApi.vkCmdSetScissor(commandBuffer, 0, new VkRect2D(0, 0, extent.width, extent.height));
    }

    // The caller waits until the device is idle before it disposes a pipeline.
    public void Dispose()
    {
        _device.DeviceApi.vkDestroyPipeline(Handle);
        _device.DeviceApi.vkDestroyPipelineLayout(Layout);
    }
}
