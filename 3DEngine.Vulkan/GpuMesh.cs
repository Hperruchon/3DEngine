using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// One mesh on the GPU (TASK-0070): its vertices in MeshVertex form and its 32-bit
// indices, each in its own buffer. ADR-0109 item 2 (phase R18b, TASK-0333): the
// vertices of its edge lines (EdgeLines) in a third buffer, or none when the mesh
// has no edge.
public sealed class GpuMesh : IDisposable
{
    private readonly GpuBuffer _vertices;
    private readonly GpuBuffer _indices;
    private readonly GpuBuffer? _edges;

    public GpuMesh(GraphicsDevice device, float[] positions, uint[] indices, float[]? edges = null)
    {
        _vertices = new GpuBuffer(device, VkBufferUsageFlags.VertexBuffer, MemoryMarshal.AsBytes<float>(positions));
        _indices = new GpuBuffer(device, VkBufferUsageFlags.IndexBuffer, MemoryMarshal.AsBytes<uint>(indices));
        IndexCount = (uint)indices.Length;
        if (edges is { Length: > 0 })
        {
            _edges = new GpuBuffer(device, VkBufferUsageFlags.VertexBuffer, MemoryMarshal.AsBytes<float>(edges));
            EdgeVertexCount = (uint)(edges.Length / EdgeLines.FloatsPerVertex);
        }
    }

    public uint IndexCount { get; }

    public VkBuffer Vertices => _vertices.Handle;

    public VkBuffer Indices => _indices.Handle;

    // The buffer of the edge lines, or null.
    public VkBuffer? EdgeVertices => _edges?.Handle;

    public uint EdgeVertexCount { get; }

    // The caller waits until the device is idle before it disposes a mesh.
    public void Dispose()
    {
        _edges?.Dispose();
        _indices.Dispose();
        _vertices.Dispose();
    }
}
