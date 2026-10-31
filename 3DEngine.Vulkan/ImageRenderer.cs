using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// The images of the scene (ADR-0043 items 3 and 4, phase V1a, TASK-0146): each one on a quad, in
// subpass 0 of the render pass of its target, after the bodies. The pipeline tests the depth with
// Greater and writes it, as the mesh pipeline does, culls no face, and blends nothing. One vertex
// buffer holds the two triangles of the unit square; the push constant of each draw takes the
// square to its quad. A test reads the fixed state below with no GPU.
public sealed unsafe class ImageRenderer : IDisposable
{
    public const VkCullModeFlags CullMode = VkCullModeFlags.None;
    public const VkCompareOp DepthCompare = VkCompareOp.Greater;
    public const uint PushConstantSize = 2 * 16 * sizeof(float);
    public const int MaximumImages = 16;

    // The two counter-clockwise triangles of the unit square, as (x, y) pairs.
    public static readonly float[] UnitSquare = [0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1];

    private readonly GraphicsDevice _device;
    private readonly VkDescriptorSetLayout _setLayout;
    private readonly VkDescriptorPool _pool;
    private readonly VkSampler _sampler;
    private readonly GpuBuffer _square;
    private readonly Dictionary<string, (UiTexture Texture, VkDescriptorSet Set)> _images = new(StringComparer.Ordinal);

    public ImageRenderer(GraphicsDevice device, VkRenderPass renderPass)
    {
        _device = device;
        var api = device.DeviceApi;

        VkDescriptorSetLayoutBinding binding = new()
        {
            binding = 0,
            descriptorType = VkDescriptorType.CombinedImageSampler,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.Fragment,
        };
        VkDescriptorSetLayoutCreateInfo setLayoutInfo = new() { bindingCount = 1, pBindings = &binding };
        api.vkCreateDescriptorSetLayout(&setLayoutInfo, null, out _setLayout).CheckResult();

        VkDescriptorPoolSize poolSize = new() { type = VkDescriptorType.CombinedImageSampler, descriptorCount = MaximumImages };
        VkDescriptorPoolCreateInfo poolInfo = new() { maxSets = MaximumImages, poolSizeCount = 1, pPoolSizes = &poolSize };
        api.vkCreateDescriptorPool(&poolInfo, null, out _pool).CheckResult();

        VkSamplerCreateInfo samplerInfo = new()
        {
            magFilter = VkFilter.Linear,
            minFilter = VkFilter.Linear,
            mipmapMode = VkSamplerMipmapMode.Linear,
            addressModeU = VkSamplerAddressMode.ClampToEdge,
            addressModeV = VkSamplerAddressMode.ClampToEdge,
            addressModeW = VkSamplerAddressMode.ClampToEdge,
            maxLod = 1,
        };
        api.vkCreateSampler(&samplerInfo, null, out _sampler).CheckResult();

        _square = new GpuBuffer(device, VkBufferUsageFlags.VertexBuffer, MemoryMarshal.AsBytes(UnitSquare.AsSpan()));

        VkShaderModule vertex = device.CreateShaderModule(ShaderLibrary.Load("image.vert"));
        VkShaderModule fragment = device.CreateShaderModule(ShaderLibrary.Load("image.frag"));
        try
        {
            var setLayout = _setLayout;
            VkPushConstantRange pushConstant = new() { stageFlags = VkShaderStageFlags.Vertex, offset = 0, size = PushConstantSize };
            VkPipelineLayoutCreateInfo layoutInfo = new()
            {
                setLayoutCount = 1,
                pSetLayouts = &setLayout,
                pushConstantRangeCount = 1,
                pPushConstantRanges = &pushConstant,
            };
            api.vkCreatePipelineLayout(&layoutInfo, null, out VkPipelineLayout layout).CheckResult();
            Layout = layout;

            fixed (byte* main = "main"u8)
            {
                VkPipelineShaderStageCreateInfo* stages = stackalloc VkPipelineShaderStageCreateInfo[2];
                stages[0] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlags.Vertex, module = vertex, pName = main };
                stages[1] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlags.Fragment, module = fragment, pName = main };

                VkVertexInputBindingDescription vertexBinding = new() { binding = 0, stride = 2 * sizeof(float), inputRate = VkVertexInputRate.Vertex };
                VkVertexInputAttributeDescription corner = new() { location = 0, binding = 0, format = VkFormat.R32G32Sfloat, offset = 0 };
                VkPipelineVertexInputStateCreateInfo vertexInput = new()
                {
                    vertexBindingDescriptionCount = 1,
                    pVertexBindingDescriptions = &vertexBinding,
                    vertexAttributeDescriptionCount = 1,
                    pVertexAttributeDescriptions = &corner,
                };
                VkPipelineInputAssemblyStateCreateInfo inputAssembly = new() { topology = VkPrimitiveTopology.TriangleList };
                VkPipelineViewportStateCreateInfo viewportState = new() { viewportCount = 1, scissorCount = 1 };
                VkPipelineRasterizationStateCreateInfo rasterization = new()
                {
                    polygonMode = VkPolygonMode.Fill,
                    cullMode = CullMode,
                    frontFace = VkFrontFace.CounterClockwise,
                    lineWidth = 1.0f,
                };
                VkPipelineMultisampleStateCreateInfo multisample = new() { rasterizationSamples = VkSampleCountFlags.Count1 };
                VkPipelineDepthStencilStateCreateInfo depthStencil = new() { depthTestEnable = true, depthWriteEnable = true, depthCompareOp = DepthCompare };
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

    public bool Holds(string imageId) => _images.ContainsKey(imageId);

    // ADR-0043 item 4: uploads an RGBA image of 8 bits for each channel under its identifier, one
    // time. An identifier names one content, so a second upload does nothing. The caller uploads
    // outside a frame.
    public void Upload(string imageId, uint width, uint height, ReadOnlySpan<byte> rgba)
    {
        ArgumentNullException.ThrowIfNull(imageId);
        if (_images.ContainsKey(imageId))
            return;
        if (_images.Count >= MaximumImages)
            throw new InvalidOperationException($"The renderer of the images holds at most {MaximumImages} images.");

        var texture = new UiTexture(_device, width, height, rgba);
        var setLayout = _setLayout;
        VkDescriptorSetAllocateInfo allocateInfo = new() { descriptorPool = _pool, descriptorSetCount = 1, pSetLayouts = &setLayout };
        VkDescriptorSet set;
        _device.DeviceApi.vkAllocateDescriptorSets(&allocateInfo, &set).CheckResult();

        VkDescriptorImageInfo imageInfo = new() { sampler = _sampler, imageView = texture.View, imageLayout = VkImageLayout.ShaderReadOnlyOptimal };
        VkWriteDescriptorSet write = new()
        {
            dstSet = set,
            dstBinding = 0,
            descriptorCount = 1,
            descriptorType = VkDescriptorType.CombinedImageSampler,
            pImageInfo = &imageInfo,
        };
        _device.DeviceApi.vkUpdateDescriptorSets(1, &write, 0, null);
        _images.Add(imageId, (texture, set));
    }

    // Draws each quad whose image the renderer holds; a quad of another image is not drawn
    // (ADR-0043 item 4).
    public void Record(VkCommandBuffer commandBuffer, VkExtent2D extent, IEnumerable<(string ImageId, Matrix4x4 ModelView)> quads, Matrix4x4 projection)
    {
        var api = _device.DeviceApi;
        var bound = false;
        byte* block = stackalloc byte[(int)PushConstantSize];
        foreach (var (imageId, modelView) in quads)
        {
            if (!_images.TryGetValue(imageId, out var image))
                continue;
            if (!bound)
            {
                api.vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, Pipeline);
                api.vkCmdSetViewport(commandBuffer, 0, RenderPipeline.Viewport(extent));
                api.vkCmdSetScissor(commandBuffer, 0, new VkRect2D(0, 0, extent.width, extent.height));
                VkBuffer vertexBuffer = _square.Handle;
                ulong offset = 0;
                api.vkCmdBindVertexBuffers(commandBuffer, 0, 1, &vertexBuffer, &offset);
                bound = true;
            }

            RenderPipeline.WritePushConstants(new Span<byte>(block, (int)PushConstantSize), modelView, projection);
            api.vkCmdPushConstants(commandBuffer, Layout, VkShaderStageFlags.Vertex, 0, PushConstantSize, block);
            var set = image.Set;
            api.vkCmdBindDescriptorSets(commandBuffer, VkPipelineBindPoint.Graphics, Layout, 0, 1, &set, 0, null);
            api.vkCmdDraw(commandBuffer, (uint)(UnitSquare.Length / 2), 1, 0, 0);
        }
    }

    // The caller waits until the device is idle before it disposes the renderer.
    public void Dispose()
    {
        var api = _device.DeviceApi;
        foreach (var (texture, _) in _images.Values)
            texture.Dispose();
        _images.Clear();
        _square.Dispose();
        api.vkDestroyPipeline(Pipeline);
        api.vkDestroyPipelineLayout(Layout);
        api.vkDestroySampler(_sampler);
        api.vkDestroyDescriptorPool(_pool);
        api.vkDestroyDescriptorSetLayout(_setLayout);
    }
}
