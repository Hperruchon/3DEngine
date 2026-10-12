using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// One mesh on the GPU (TASK-0070): its vertices in MeshVertex form and its 32-bit
// indices, each in its own buffer.
public sealed class GpuMesh : IDisposable
{
    private readonly GpuBuffer _vertices;
    private readonly GpuBuffer _indices;

    public GpuMesh(GraphicsDevice device, float[] positions, uint[] indices)
    {
        _vertices = new GpuBuffer(device, VkBufferUsageFlags.VertexBuffer, MemoryMarshal.AsBytes<float>(positions));
        _indices = new GpuBuffer(device, VkBufferUsageFlags.IndexBuffer, MemoryMarshal.AsBytes<uint>(indices));
        IndexCount = (uint)indices.Length;
    }

    public uint IndexCount { get; }

    public VkBuffer Vertices => _vertices.Handle;

    public VkBuffer Indices => _indices.Handle;

    // The caller waits until the device is idle before it disposes a mesh.
    public void Dispose()
    {
        _indices.Dispose();
        _vertices.Dispose();
    }
}
