using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// A target with no window (ADR-0025 item 2, TASK-0076): a colour image and a depth
// image of one extent, a render pass of its own, and a buffer in memory that the
// host reads. Render records the draw of a frame, copies the colour image into the
// buffer, waits until the device ends, and gives the pixels: four bytes for each
// pixel (red, green, blue, alpha), one row after the other, the top row first.
public sealed unsafe class OffscreenTarget : IDisposable
{
    public const VkFormat ColorFormat = VkFormat.R8G8B8A8Unorm;

    private readonly GraphicsDevice _device;
    private readonly VkImage _color;
    private readonly VkDeviceMemory _colorMemory;
    private readonly VkImageView _colorView;
    private readonly DepthBuffer _depth;
    private readonly VkFramebuffer _framebuffer;
    private readonly GpuBuffer _readback;

    public OffscreenTarget(GraphicsDevice device, VkExtent2D extent)
    {
        _device = device;
        Extent = extent;
        var api = device.DeviceApi;

        VkImageCreateInfo imageInfo = new()
        {
            imageType = VkImageType.Image2D,
            format = ColorFormat,
            extent = new VkExtent3D(extent.width, extent.height, 1),
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.Count1,
            tiling = VkImageTiling.Optimal,
            usage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferSrc,
            sharingMode = VkSharingMode.Exclusive,
            initialLayout = VkImageLayout.Undefined,
        };
        api.vkCreateImage(&imageInfo, null, out _color).CheckResult();

        api.vkGetImageMemoryRequirements(_color, out VkMemoryRequirements requirements);
        VkMemoryAllocateInfo allocateInfo = new()
        {
            allocationSize = requirements.size,
            memoryTypeIndex = device.FindMemoryType(requirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal),
        };
        api.vkAllocateMemory(&allocateInfo, out _colorMemory).CheckResult();
        api.vkBindImageMemory(_color, _colorMemory, 0).CheckResult();

        VkImageViewCreateInfo viewInfo = new(
            _color,
            VkImageViewType.Image2D,
            ColorFormat,
            VkComponentMapping.Rgba,
            new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1));
        api.vkCreateImageView(&viewInfo, null, out _colorView).CheckResult();

        _depth = new DepthBuffer(device, extent);
        RenderPass = RenderPasses.Create(device, ColorFormat, VkImageLayout.TransferSrcOptimal);
        api.vkCreateFramebuffer(RenderPass, new[] { _colorView, _depth.View }, extent, 1u, out _framebuffer).CheckResult();

        _readback = new GpuBuffer(device, VkBufferUsageFlags.TransferDst, new byte[ByteCount]);
    }

    public VkExtent2D Extent { get; }

    // The render pass of this target, for its pipeline.
    public VkRenderPass RenderPass { get; }

    private int ByteCount => checked((int)(Extent.width * Extent.height * 4));

    // The draw begins and ends the render pass, as for a frame of the window.
    public byte[] Render(Action<VkCommandBuffer, VkFramebuffer, VkExtent2D> draw)
    {
        var api = _device.DeviceApi;
        _device.Submit(cmd =>
        {
            draw(cmd, _framebuffer, Extent);

            // The render pass leaves the colour in TransferSrcOptimal, and its last
            // dependency makes the write visible to this copy.
            VkBufferImageCopy region = new()
            {
                bufferOffset = 0,
                bufferRowLength = 0,
                bufferImageHeight = 0,
                imageSubresource = new VkImageSubresourceLayers(VkImageAspectFlags.Color, 0, 0, 1),
                imageOffset = new VkOffset3D(0, 0, 0),
                imageExtent = new VkExtent3D(Extent.width, Extent.height, 1),
            };
            api.vkCmdCopyImageToBuffer(cmd, _color, VkImageLayout.TransferSrcOptimal, _readback.Handle, 1, &region);

            // The host reads the buffer after the fence; this barrier makes the copy
            // visible to that read.
            VkBufferMemoryBarrier barrier = new()
            {
                srcAccessMask = VkAccessFlags.TransferWrite,
                dstAccessMask = VkAccessFlags.HostRead,
                srcQueueFamilyIndex = Vortice.Vulkan.Vulkan.VK_QUEUE_FAMILY_IGNORED,
                dstQueueFamilyIndex = Vortice.Vulkan.Vulkan.VK_QUEUE_FAMILY_IGNORED,
                buffer = _readback.Handle,
                offset = 0,
                size = _readback.Size,
            };
            api.vkCmdPipelineBarrier(cmd, VkPipelineStageFlags.Transfer, VkPipelineStageFlags.Host, VkDependencyFlags.None, 0, null, 1, &barrier, 0, null);
        });

        var pixels = new byte[ByteCount];
        _readback.Read(pixels);
        return pixels;
    }

    // The caller waits until the device is idle.
    public void Dispose()
    {
        var api = _device.DeviceApi;
        _readback.Dispose();
        api.vkDestroyFramebuffer(_framebuffer);
        api.vkDestroyRenderPass(RenderPass);
        _depth.Dispose();
        api.vkDestroyImageView(_colorView);
        api.vkDestroyImage(_color);
        api.vkFreeMemory(_colorMemory);
    }
}
