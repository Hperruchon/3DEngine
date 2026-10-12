using System.Runtime.InteropServices;
using ThreeDEngine.Core.Abstractions;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.Desktop;

public sealed unsafe class NativeThreeDEngine : IThreeDEngine, IDisposable
{
    private readonly Window _window;
    private GraphicsDevice? _graphicsDevice;
    private RenderPipeline? _meshPipeline;
    private GpuBuffer? _vertices;
    private GpuBuffer? _indices;

    public NativeThreeDEngine(Window window)
    {
        _window = window;
    }

    public string Name => "3DEngine Desktop Runtime";

    public Scene? CurrentScene { get; private set; }

    public void Initialize()
    {
        _graphicsDevice = new GraphicsDevice(Name, enableValidation: true, _window);

        // The indexed mesh of roadmap phase R3 (TASK-0069): the cut body of the
        // first demonstration, narrowed to float around the centre of its box. The
        // owner looks at it in the window (ADR-0022 item 4). The first draw of R3,
        // a triangle with no vertex input, is in the history of this file.
        var (origin, scale) = DemonstrationMesh.Frame();
        float[] positions = MeshVertex.Narrow(DemonstrationMesh.Positions, origin, scale);
        _meshPipeline = new RenderPipeline(_graphicsDevice, "mesh.vert", "mesh.frag");
        _vertices = new GpuBuffer(_graphicsDevice, VkBufferUsageFlags.VertexBuffer, MemoryMarshal.AsBytes<float>(positions));
        _indices = new GpuBuffer(_graphicsDevice, VkBufferUsageFlags.IndexBuffer, MemoryMarshal.AsBytes<uint>(DemonstrationMesh.Indices));
    }

    public void Update(TimeSpan deltaTime)
    {
        _ = deltaTime;
    }

    public void Render()
    {
        if (_graphicsDevice is null || _meshPipeline is null || _vertices is null || _indices is null)
        {
            throw new InvalidOperationException("Graphics device is not initialized.");
        }

        _graphicsDevice.RenderFrame((commandBuffer, framebuffer, extent) =>
        {
            VkClearValue clearValue = new()
            {
                color = new VkClearColorValue(0.0f, 1.0f, 0.0f, 1.0f)
            };

            VkRenderPassBeginInfo renderPassInfo = new()
            {
                sType = VkStructureType.RenderPassBeginInfo,
                renderPass = _graphicsDevice.Swapchain.RenderPass,
                framebuffer = framebuffer,
                renderArea = new VkRect2D
                {
                    offset = new VkOffset2D(0, 0),
                    extent = extent
                },
                clearValueCount = 1,
                pClearValues = &clearValue
            };

            _graphicsDevice.DeviceApi.vkCmdBeginRenderPass(commandBuffer, &renderPassInfo, VkSubpassContents.Inline);
            _meshPipeline.Bind(commandBuffer, extent);
            VkBuffer vertexBuffer = _vertices.Handle;
            ulong vertexOffset = 0;
            _graphicsDevice.DeviceApi.vkCmdBindVertexBuffers(commandBuffer, 0, 1, &vertexBuffer, &vertexOffset);
            _graphicsDevice.DeviceApi.vkCmdBindIndexBuffer(commandBuffer, _indices.Handle, 0, VkIndexType.Uint32);
            _graphicsDevice.DeviceApi.vkCmdDrawIndexed(commandBuffer, (uint)DemonstrationMesh.Indices.Length, 1, 0, 0, 0);
            _graphicsDevice.DeviceApi.vkCmdEndRenderPass(commandBuffer);
        });
    }

    public void LoadScene(Scene scene)
    {
        CurrentScene = scene;
        Console.WriteLine($"Loaded scene '{scene.Name}' with {scene.Entities.Count} entities.");
    }

    // Before TASK-0027 the host released nothing, and the process exit freed
    // each Vulkan object. The validation layer now also checks the teardown.
    public void Dispose()
    {
        // The GPU must be idle before a pipeline goes.
        _graphicsDevice?.WaitIdle();
        _indices?.Dispose();
        _vertices?.Dispose();
        _meshPipeline?.Dispose();
        (_indices, _vertices, _meshPipeline) = (null, null, null);
        _graphicsDevice?.Dispose();
        _graphicsDevice = null;
    }
}
