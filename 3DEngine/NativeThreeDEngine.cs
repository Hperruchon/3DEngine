using ThreeDEngine.Core.Abstractions;
using ThreeDEngine.Core.Models;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace ThreeDEngine.Desktop;

public sealed unsafe class NativeThreeDEngine : IThreeDEngine
{
    private readonly Window _window;
    private GraphicsDevice? _graphicsDevice;

    public NativeThreeDEngine(Window window)
    {
        _window = window;
    }

    public string Name => "3DEngine Desktop Runtime";

    public Scene? CurrentScene { get; private set; }

    public void Initialize()
    {
        _graphicsDevice = new GraphicsDevice(Name, enableValidation: true, _window);
    }

    public void Update(TimeSpan deltaTime)
    {
        _ = deltaTime;
    }

    public void Render()
    {
        if (_graphicsDevice is null)
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

            vkCmdBeginRenderPass(commandBuffer, &renderPassInfo, VkSubpassContents.Inline);
            vkCmdEndRenderPass(commandBuffer);
        });
    }

    public void LoadScene(Scene scene)
    {
        CurrentScene = scene;
        Console.WriteLine($"Loaded scene '{scene.Name}' with {scene.Entities.Count} entities.");
    }
}
