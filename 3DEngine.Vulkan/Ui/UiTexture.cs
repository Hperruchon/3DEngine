using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// The first sampled texture of the renderer (ADR-0027 item 3, TASK-0084): one image of
// R8G8B8A8_UNORM in memory of the device, filled once from the CPU through a staging
// buffer, and left in the layout that a fragment shader reads. The font atlas of Dear
// ImGui 1.91 is built once, so the texture needs no partial update.
public sealed unsafe class UiTexture : IDisposable
{
    public const VkFormat Format = VkFormat.R8G8B8A8Unorm;

    private readonly GraphicsDevice _device;
    private readonly VkImage _image;
    private readonly VkDeviceMemory _memory;

    public UiTexture(GraphicsDevice device, uint width, uint height, ReadOnlySpan<byte> rgba)
    {
        if (width == 0 || height == 0)
            throw new ArgumentException("A texture has at least one pixel.");
        if ((ulong)rgba.Length != (ulong)width * height * 4)
            throw new ArgumentException($"A texture of {width} x {height} needs {(ulong)width * height * 4} bytes, not {rgba.Length}.", nameof(rgba));

        _device = device;
        Width = width;
        Height = height;
        var api = device.DeviceApi;

        VkImageCreateInfo imageInfo = new()
        {
            imageType = VkImageType.Image2D,
            format = Format,
            extent = new VkExtent3D(width, height, 1),
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.Count1,
            tiling = VkImageTiling.Optimal,
            usage = VkImageUsageFlags.Sampled | VkImageUsageFlags.TransferDst,
            sharingMode = VkSharingMode.Exclusive,
            initialLayout = VkImageLayout.Undefined,
        };
        api.vkCreateImage(&imageInfo, null, out _image).CheckResult();

        api.vkGetImageMemoryRequirements(_image, out VkMemoryRequirements requirements);
        VkMemoryAllocateInfo allocateInfo = new()
        {
            allocationSize = requirements.size,
            memoryTypeIndex = device.FindMemoryType(requirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal),
        };
        api.vkAllocateMemory(&allocateInfo, out _memory).CheckResult();
        api.vkBindImageMemory(_image, _memory, 0).CheckResult();

        VkImageViewCreateInfo viewInfo = new(
            _image,
            VkImageViewType.Image2D,
            Format,
            VkComponentMapping.Rgba,
            new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1));
        api.vkCreateImageView(&viewInfo, null, out VkImageView view).CheckResult();
        View = view;

        using var staging = new GpuBuffer(device, VkBufferUsageFlags.TransferSrc, rgba);
        var image = _image;
        device.Submit(cmd =>
        {
            // Undefined to the destination of a copy; the old content does not matter.
            Transition(api, cmd, image, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal,
                VkAccessFlags.None, VkAccessFlags.TransferWrite, VkPipelineStageFlags.TopOfPipe, VkPipelineStageFlags.Transfer);

            VkBufferImageCopy region = new()
            {
                bufferOffset = 0,
                bufferRowLength = 0,
                bufferImageHeight = 0,
                imageSubresource = new VkImageSubresourceLayers(VkImageAspectFlags.Color, 0, 0, 1),
                imageOffset = new VkOffset3D(0, 0, 0),
                imageExtent = new VkExtent3D(width, height, 1),
            };
            api.vkCmdCopyBufferToImage(cmd, staging.Handle, image, VkImageLayout.TransferDstOptimal, 1, &region);

            // The copy must end before a fragment shader reads the image.
            Transition(api, cmd, image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal,
                VkAccessFlags.TransferWrite, VkAccessFlags.ShaderRead, VkPipelineStageFlags.Transfer, VkPipelineStageFlags.FragmentShader);
        });
    }

    public uint Width { get; }

    public uint Height { get; }

    public VkImageView View { get; }

    private static void Transition(
        VkDeviceApi api,
        VkCommandBuffer cmd,
        VkImage image,
        VkImageLayout from,
        VkImageLayout to,
        VkAccessFlags sourceAccess,
        VkAccessFlags destinationAccess,
        VkPipelineStageFlags sourceStage,
        VkPipelineStageFlags destinationStage)
    {
        VkImageMemoryBarrier barrier = new()
        {
            srcAccessMask = sourceAccess,
            dstAccessMask = destinationAccess,
            oldLayout = from,
            newLayout = to,
            srcQueueFamilyIndex = Vortice.Vulkan.Vulkan.VK_QUEUE_FAMILY_IGNORED,
            dstQueueFamilyIndex = Vortice.Vulkan.Vulkan.VK_QUEUE_FAMILY_IGNORED,
            image = image,
            subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1),
        };
        api.vkCmdPipelineBarrier(cmd, sourceStage, destinationStage, VkDependencyFlags.None, 0, null, 0, null, 1, &barrier);
    }

    // The caller waits until the device is idle before it disposes a texture.
    public void Dispose()
    {
        _device.DeviceApi.vkDestroyImageView(View);
        _device.DeviceApi.vkDestroyImage(_image);
        _device.DeviceApi.vkFreeMemory(_memory);
    }
}
