using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// The depth image of the swapchain (ADR-0023 item 1, TASK-0070): one image of
// D32_SFLOAT of the extent, in memory of the device. The render pass clears it to 0
// at each frame, and the pipeline keeps the greater depth, so a nearer fragment wins.
public sealed unsafe class DepthBuffer : IDisposable
{
    public const VkFormat Format = VkFormat.D32Sfloat;

    // The value of a clear: the far plane of a reversed projection.
    public const float ClearDepth = 0.0f;

    private readonly GraphicsDevice _device;
    private readonly VkImage _image;
    private readonly VkDeviceMemory _memory;

    public DepthBuffer(GraphicsDevice device, VkExtent2D extent)
    {
        _device = device;

        VkImageCreateInfo imageInfo = new()
        {
            imageType = VkImageType.Image2D,
            format = Format,
            extent = new VkExtent3D(extent.width, extent.height, 1),
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.Count1,
            tiling = VkImageTiling.Optimal,
            usage = VkImageUsageFlags.DepthStencilAttachment,
            sharingMode = VkSharingMode.Exclusive,
            initialLayout = VkImageLayout.Undefined,
        };
        device.DeviceApi.vkCreateImage(&imageInfo, null, out _image).CheckResult();

        device.DeviceApi.vkGetImageMemoryRequirements(_image, out VkMemoryRequirements requirements);
        VkMemoryAllocateInfo allocateInfo = new()
        {
            allocationSize = requirements.size,
            memoryTypeIndex = device.FindMemoryType(requirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal),
        };
        device.DeviceApi.vkAllocateMemory(&allocateInfo, out _memory).CheckResult();
        device.DeviceApi.vkBindImageMemory(_image, _memory, 0).CheckResult();

        VkImageViewCreateInfo viewInfo = new(
            _image,
            VkImageViewType.Image2D,
            Format,
            VkComponentMapping.Rgba,
            new VkImageSubresourceRange(VkImageAspectFlags.Depth, 0, 1, 0, 1));
        device.DeviceApi.vkCreateImageView(&viewInfo, null, out VkImageView view).CheckResult();
        View = view;
    }

    public VkImageView View { get; }

    // The caller waits until the device is idle before it disposes the buffer.
    public void Dispose()
    {
        _device.DeviceApi.vkDestroyImageView(View);
        _device.DeviceApi.vkDestroyImage(_image);
        _device.DeviceApi.vkFreeMemory(_memory);
    }
}
