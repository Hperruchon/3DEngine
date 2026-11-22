using System.Numerics;
using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// The pipeline of the ground of the scene (ADR-0186 item 2, phase R18p, TASK-0458), in subpass 0 of the render pass of its
// target. It draws one triangle that covers the target, with no vertex buffer, no depth test, no depth write and no blend,
// as the first draw after the clear. So each body, image, edge and overlay draws over it, and the depth of the clear stays.
// The push block holds the colour of the middle, the colour of a corner and the size of the target: 40 bytes.
public sealed unsafe class GroundPipeline : IDisposable
{
    public const uint PushConstantSize = (4 + 4 + 2) * sizeof(float);

    private readonly GraphicsDevice _device;

    public GroundPipeline(GraphicsDevice device, VkRenderPass renderPass)
    {
        _device = device;
        var api = device.DeviceApi;

        VkShaderModule vertex = device.CreateShaderModule(ShaderLibrary.Load("ground.vert"));
        VkShaderModule fragment = device.CreateShaderModule(ShaderLibrary.Load("ground.frag"));
        try
        {
            VkPushConstantRange pushConstant = new() { stageFlags = VkShaderStageFlags.Fragment, offset = 0, size = PushConstantSize };
            VkPipelineLayoutCreateInfo layoutInfo = new() { pushConstantRangeCount = 1, pPushConstantRanges = &pushConstant };
            api.vkCreatePipelineLayout(&layoutInfo, null, out VkPipelineLayout layout).CheckResult();
            Layout = layout;

            fixed (byte* main = "main"u8)
            {
                VkPipelineShaderStageCreateInfo* stages = stackalloc VkPipelineShaderStageCreateInfo[2];
                stages[0] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlags.Vertex, module = vertex, pName = main };
                stages[1] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlags.Fragment, module = fragment, pName = main };

                VkPipelineVertexInputStateCreateInfo vertexInput = new();
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
                VkPipelineDepthStencilStateCreateInfo depthStencil = new() { depthTestEnable = false, depthWriteEnable = false };
                VkPipelineColorBlendAttachmentState blendAttachment = new() { blendEnable = false, colorWriteMask = VkColorComponentFlags.All };
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

    public VkPipeline Handle { get; }

    public VkPipelineLayout Layout { get; }

    // Draws the ground over the whole target. Each colour is red, green and blue from 0 to 1. The viewport has y down, as the
    // target: the light is symmetric, so the direction of y does not change a pixel.
    public void Execute(VkCommandBuffer commandBuffer, VkExtent2D extent, Vector3 middle, Vector3 corner)
    {
        var api = _device.DeviceApi;
        api.vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, Handle);
        api.vkCmdSetViewport(commandBuffer, 0, new VkViewport { x = 0, y = 0, width = extent.width, height = extent.height, minDepth = 0, maxDepth = 1 });
        api.vkCmdSetScissor(commandBuffer, 0, new VkRect2D(0, 0, extent.width, extent.height));
        float* block = stackalloc float[(int)(PushConstantSize / sizeof(float))];
        (block[0], block[1], block[2], block[3]) = (middle.X, middle.Y, middle.Z, 1f);
        (block[4], block[5], block[6], block[7]) = (corner.X, corner.Y, corner.Z, 1f);
        (block[8], block[9]) = (extent.width, extent.height);
        api.vkCmdPushConstants(commandBuffer, Layout, VkShaderStageFlags.Fragment, 0, PushConstantSize, block);
        api.vkCmdDraw(commandBuffer, 3, 1, 0, 0);
    }

    // The caller waits until the device is idle before it disposes a pipeline.
    public void Dispose()
    {
        _device.DeviceApi.vkDestroyPipeline(Handle);
        _device.DeviceApi.vkDestroyPipelineLayout(Layout);
    }
}
