using ThreeDEngine.Core.Abstractions;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.Desktop;

public sealed unsafe class NativeThreeDEngine : IThreeDEngine, IDisposable
{
    private readonly Window _window;
    private GraphicsDevice? _graphicsDevice;
    private RenderPipeline? _trianglePipeline;

    public NativeThreeDEngine(Window window)
    {
        _window = window;
    }

    public string Name => "3DEngine Desktop Runtime";

    public Scene? CurrentScene { get; private set; }

    public void Initialize()
    {
        _graphicsDevice = new GraphicsDevice(Name, enableValidation: true, _window);

        // The first draw of roadmap phase R3 (TASK-0069): one triangle with no
        // vertex input. The owner looks at it in the window (ADR-0022 item 4).
        _trianglePipeline = new RenderPipeline(_graphicsDevice, "triangle.vert", "triangle.frag");
    }

    public void Update(TimeSpan deltaTime)
    {
        _ = deltaTime;
    }

    public void Render()
    {
        if (_graphicsDevice is null || _trianglePipeline is null)
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
            _trianglePipeline.Bind(commandBuffer, extent);
            _graphicsDevice.DeviceApi.vkCmdDraw(commandBuffer, 3, 1, 0, 0);
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
        _trianglePipeline?.Dispose();
        _trianglePipeline = null;
        _graphicsDevice?.Dispose();
        _graphicsDevice = null;
    }
}
