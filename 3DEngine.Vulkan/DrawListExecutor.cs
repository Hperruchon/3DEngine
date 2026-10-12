using System.Numerics;
using ThreeDEngine.Core.Rendering;
using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// Executes a draw list with one direct indexed draw for each record (ADR-0023 item 4,
// TASK-0070). A later executor can draw the same list with indirect draws.
public sealed unsafe class DrawListExecutor
{
    private readonly GraphicsDevice _device;
    private readonly RenderPipeline _pipeline;

    public DrawListExecutor(GraphicsDevice device, RenderPipeline pipeline)
    {
        _device = device;
        _pipeline = pipeline;
    }

    // Draws each record whose mesh is in the table. A record of an unknown mesh is a
    // defect of the host, so it throws.
    public void Execute(
        VkCommandBuffer commandBuffer,
        VkExtent2D extent,
        IReadOnlyList<DrawRecord> records,
        IReadOnlyDictionary<string, GpuMesh> meshes,
        Matrix4x4 projection)
    {
        _pipeline.Bind(commandBuffer, extent);

        foreach (var record in records)
        {
            if (!meshes.TryGetValue(record.MeshId, out var mesh))
                throw new InvalidOperationException($"The draw list names the mesh '{record.MeshId}', which the GPU does not hold.");

            _pipeline.Push(commandBuffer, record.ModelView, projection);
            VkBuffer vertexBuffer = mesh.Vertices;
            ulong offset = 0;
            _device.DeviceApi.vkCmdBindVertexBuffers(commandBuffer, 0, 1, &vertexBuffer, &offset);
            _device.DeviceApi.vkCmdBindIndexBuffer(commandBuffer, mesh.Indices, 0, VkIndexType.Uint32);
            _device.DeviceApi.vkCmdDrawIndexed(commandBuffer, mesh.IndexCount, 1, 0, 0, 0);
        }
    }
}
