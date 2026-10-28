using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// The first-party renderer of the user interface (ADR-0027 item 3, TASK-0084). It draws
// the neutral draw data of UiDrawData in subpass 0 of the render pass of its target, after
// the scene: the first sampled texture, the first descriptor set and the first 2D pipeline
// of the project. The pipeline blends with the alpha of the fragment, tests no depth, culls
// nothing, and sets one scissor for each command.
//
// The vertices and the indices of a frame go into buffers that the CPU writes. A buffer is
// written again only when the frame that read it ended, so the renderer keeps one pair of
// buffers for each slot that the caller names: the swapchain image of the frame, whose
// fence the presenter waits for before it reuses the image, or slot 0 for a target that
// waits until each frame ends.
public sealed unsafe class UiRenderer : IDisposable
{
    public const uint PushConstantSize = 4 * sizeof(float);
    public const int MaximumTextures = 16;

    private readonly GraphicsDevice _device;
    private readonly VkDescriptorSetLayout _setLayout;
    private readonly VkDescriptorPool _pool;
    private readonly VkSampler _sampler;
    private readonly Dictionary<ulong, (UiTexture Texture, VkDescriptorSet Set)> _textures = new();
    private readonly Dictionary<int, (GpuBuffer Vertices, GpuBuffer Indices)> _slots = new();
    private ulong _nextTexture = 1;

    public UiRenderer(GraphicsDevice device, VkRenderPass renderPass)
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

        VkDescriptorPoolSize poolSize = new() { type = VkDescriptorType.CombinedImageSampler, descriptorCount = MaximumTextures };
        VkDescriptorPoolCreateInfo poolInfo = new() { maxSets = MaximumTextures, poolSizeCount = 1, pPoolSizes = &poolSize };
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

        VkShaderModule vertex = device.CreateShaderModule(ShaderLibrary.Load("ui.vert"));
        VkShaderModule fragment = device.CreateShaderModule(ShaderLibrary.Load("ui.frag"));
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
            fixed (VkVertexInputAttributeDescription* attributes = UiVertex.Attributes)
            {
                VkPipelineShaderStageCreateInfo* stages = stackalloc VkPipelineShaderStageCreateInfo[2];
                stages[0] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlags.Vertex, module = vertex, pName = main };
                stages[1] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlags.Fragment, module = fragment, pName = main };

                VkVertexInputBindingDescription vertexBinding = UiVertex.Binding;
                VkPipelineVertexInputStateCreateInfo vertexInput = new()
                {
                    vertexBindingDescriptionCount = 1,
                    pVertexBindingDescriptions = &vertexBinding,
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

                // The render pass has a depth image; the user interface neither tests it nor writes it.
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

    // Uploads an RGBA image of 8 bits for each channel and gives the identifier that a
    // command of the draw data names. The caller creates each texture outside a frame.
    public ulong CreateTexture(uint width, uint height, ReadOnlySpan<byte> rgba)
    {
        if (_textures.Count >= MaximumTextures)
            throw new InvalidOperationException($"The renderer of the user interface holds at most {MaximumTextures} textures.");

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

        var id = _nextTexture++;
        _textures.Add(id, (texture, set));
        return id;
    }

    // Records the draws of the user interface inside the render pass of the target, after
    // the scene. The slot is the swapchain image of the frame, or 0 for a target that waits
    // for each frame.
    public void Record(VkCommandBuffer commandBuffer, VkExtent2D extent, UiDrawData data, int slot)
    {
        if (data.Commands.Length == 0 || data.Vertices.Length == 0 || data.Indices.Length == 0)
            return;

        var api = _device.DeviceApi;
        var (vertices, indices) = Buffers(slot, data);
        vertices.Write(MemoryMarshal.AsBytes(data.Vertices.AsSpan()));
        indices.Write(MemoryMarshal.AsBytes(data.Indices.AsSpan()));

        api.vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, Pipeline);
        api.vkCmdSetViewport(commandBuffer, 0, new VkViewport { x = 0, y = 0, width = extent.width, height = extent.height, minDepth = 0, maxDepth = 1 });

        var (scale, translation) = Transform(data.DisplayPosition, data.DisplaySize);
        float* block = stackalloc float[4] { scale.X, scale.Y, translation.X, translation.Y };
        api.vkCmdPushConstants(commandBuffer, Layout, VkShaderStageFlags.Vertex, 0, PushConstantSize, block);

        VkBuffer vertexBuffer = vertices.Handle;
        ulong offset = 0;
        api.vkCmdBindVertexBuffers(commandBuffer, 0, 1, &vertexBuffer, &offset);
        api.vkCmdBindIndexBuffer(commandBuffer, indices.Handle, 0, VkIndexType.Uint16);

        foreach (var command in data.Commands)
        {
            if (Scissor(command.ClipRectangle, data.DisplayPosition, data.FramebufferScale, extent) is not { } scissor)
                continue;
            if (!_textures.TryGetValue(command.Texture, out var texture))
                throw new InvalidOperationException($"A command of the user interface names the texture {command.Texture}, which the renderer does not hold.");

            var set = texture.Set;
            api.vkCmdBindDescriptorSets(commandBuffer, VkPipelineBindPoint.Graphics, Layout, 0, 1, &set, 0, null);
            api.vkCmdSetScissor(commandBuffer, 0, scissor);
            api.vkCmdDrawIndexed(commandBuffer, command.IndexCount, 1, command.FirstIndex, command.VertexOffset, 0);
        }
    }

    // The push constant: a position in pixels of the display times the scale, plus the
    // translation, is the position in clip space. The top-left corner of the display goes
    // to (-1, -1), and the viewport of the renderer has y down.
    public static (Vector2 Scale, Vector2 Translation) Transform(Vector2 displayPosition, Vector2 displaySize)
    {
        if (!(displaySize.X > 0 && displaySize.Y > 0))
            throw new ArgumentOutOfRangeException(nameof(displaySize), displaySize, "A display has a positive size.");

        var scale = new Vector2(2f / displaySize.X, 2f / displaySize.Y);
        return (scale, new Vector2(-1f - displayPosition.X * scale.X, -1f - displayPosition.Y * scale.Y));
    }

    // The scissor of one command in pixels of the target, inside the extent, or null when
    // nothing of the command is visible.
    public static VkRect2D? Scissor(Vector4 clip, Vector2 displayPosition, Vector2 framebufferScale, VkExtent2D extent)
    {
        var left = Math.Max(0f, (clip.X - displayPosition.X) * framebufferScale.X);
        var top = Math.Max(0f, (clip.Y - displayPosition.Y) * framebufferScale.Y);
        var right = Math.Min(extent.width, (clip.Z - displayPosition.X) * framebufferScale.X);
        var bottom = Math.Min(extent.height, (clip.W - displayPosition.Y) * framebufferScale.Y);
        if (!(right > left && bottom > top))
            return null;

        var x = (int)left;
        var y = (int)top;
        return new VkRect2D(x, y, (uint)Math.Ceiling(right) - (uint)x, (uint)Math.Ceiling(bottom) - (uint)y);
    }

    // The buffers of a slot, made again when a frame needs more room.
    private (GpuBuffer Vertices, GpuBuffer Indices) Buffers(int slot, UiDrawData data)
    {
        var vertexBytes = (ulong)data.Vertices.Length * UiVertex.Stride;
        var indexBytes = ((ulong)data.Indices.Length * sizeof(ushort) + 3) / 4 * 4;

        _slots.TryGetValue(slot, out var buffers);
        if (buffers.Vertices is null || buffers.Vertices.Size < vertexBytes)
        {
            buffers.Vertices?.Dispose();
            buffers.Vertices = new GpuBuffer(_device, VkBufferUsageFlags.VertexBuffer, new byte[Math.Max(vertexBytes * 2, 64 * 1024)]);
        }

        if (buffers.Indices is null || buffers.Indices.Size < indexBytes)
        {
            buffers.Indices?.Dispose();
            buffers.Indices = new GpuBuffer(_device, VkBufferUsageFlags.IndexBuffer, new byte[Math.Max(indexBytes * 2, 32 * 1024)]);
        }

        _slots[slot] = buffers;
        return buffers;
    }

    // The caller waits until the device is idle before it disposes the renderer.
    public void Dispose()
    {
        var api = _device.DeviceApi;
        foreach (var (vertices, indices) in _slots.Values)
        {
            vertices.Dispose();
            indices.Dispose();
        }
        _slots.Clear();

        foreach (var (texture, _) in _textures.Values)
            texture.Dispose();
        _textures.Clear();

        api.vkDestroyPipeline(Pipeline);
        api.vkDestroyPipelineLayout(Layout);
        api.vkDestroySampler(_sampler);
        api.vkDestroyDescriptorPool(_pool);
        api.vkDestroyDescriptorSetLayout(_setLayout);
    }
}
