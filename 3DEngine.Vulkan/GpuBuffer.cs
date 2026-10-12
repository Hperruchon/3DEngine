using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// A buffer that the CPU writes one time and the GPU reads, for the vertices and
// the indices of a mesh (TASK-0069). Its memory is visible to the host and
// coherent, so the copy needs no staging buffer and no one-time command buffer.
public sealed unsafe class GpuBuffer : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly VkDeviceMemory _memory;

    public GpuBuffer(GraphicsDevice device, VkBufferUsageFlags usage, ReadOnlySpan<byte> data)
    {
        _device = device;
        Size = (ulong)data.Length;

        VkBufferCreateInfo bufferInfo = new()
        {
            size = Size,
            usage = usage,
            sharingMode = VkSharingMode.Exclusive,
        };
        device.DeviceApi.vkCreateBuffer(&bufferInfo, out VkBuffer buffer).CheckResult();
        Handle = buffer;

        device.DeviceApi.vkGetBufferMemoryRequirements(Handle, out VkMemoryRequirements requirements);
        VkMemoryAllocateInfo allocateInfo = new()
        {
            allocationSize = requirements.size,
            memoryTypeIndex = device.FindMemoryType(
                requirements.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent),
        };
        device.DeviceApi.vkAllocateMemory(&allocateInfo, out _memory).CheckResult();
        device.DeviceApi.vkBindBufferMemory(Handle, _memory, 0).CheckResult();

        void* mapped;
        device.DeviceApi.vkMapMemory(_memory, 0, Size, 0, &mapped).CheckResult();
        data.CopyTo(new Span<byte>(mapped, data.Length));
        device.DeviceApi.vkUnmapMemory(_memory);
    }

    public VkBuffer Handle { get; }

    public ulong Size { get; }

    // The caller waits until the device is idle before it disposes a buffer.
    public void Dispose()
    {
        _device.DeviceApi.vkDestroyBuffer(Handle);
        _device.DeviceApi.vkFreeMemory(_memory);
    }
}
