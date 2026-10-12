using SDL3;
using ThreeDEngine.Core.Abstractions;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Rendering;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.Desktop;

// The render loop of the desktop (roadmap phases R3 and R4). At each frame it walks
// the scene into a draw list, relative to the eye of the camera, and the Vulkan layer
// executes the list (ADR-0023). A left drag of the mouse orbits the camera, and the
// wheel zooms it; R7 replaces this reading of the mouse with the intent layer.
public sealed unsafe class NativeThreeDEngine : IThreeDEngine, IDisposable
{
    // Degrees of orbit for each pixel of a drag, and the zoom of one step of the wheel.
    private const double OrbitDegreesPerPixel = 0.3;
    private const double ZoomPerStep = 0.9;

    private readonly Window _window;
    private readonly DrawList _drawList = new();
    private readonly Dictionary<string, GpuMesh> _meshes = new(StringComparer.Ordinal);
    private GraphicsDevice? _graphicsDevice;
    private RenderPipeline? _meshPipeline;
    private DrawListExecutor? _executor;

    public NativeThreeDEngine(Window window)
    {
        _window = window;
    }

    public string Name => "3DEngine Desktop Runtime";

    public Scene? CurrentScene { get; private set; }

    public Camera Camera { get; private set; } = new();

    public void Initialize()
    {
        _graphicsDevice = new GraphicsDevice(Name, enableValidation: true, _window);
        _meshPipeline = new RenderPipeline(_graphicsDevice, "mesh.vert", "mesh.frag");
        _executor = new DrawListExecutor(_graphicsDevice, _meshPipeline);

        // The cut body of the first demonstration (TASK-0069), relative to the centre
        // of its box; the entities of the scene place it.
        _meshes[DemonstrationMesh.MeshId] = new GpuMesh(
            _graphicsDevice,
            MeshVertex.Narrow(DemonstrationMesh.Positions, DemonstrationMesh.Centre(), 1.0),
            DemonstrationMesh.Indices);
    }

    public void Update(TimeSpan deltaTime)
    {
        _ = deltaTime;
    }

    // One event of the window: a left drag orbits the camera, and the wheel zooms it.
    public void HandleEvent(in SDL_Event sdlEvent)
    {
        if (sdlEvent.type == SDL_EventType.MouseMotion && (sdlEvent.motion.state & SDL_MouseButtonFlags.Left) != 0)
            Camera.Orbit(-sdlEvent.motion.xrel * OrbitDegreesPerPixel, sdlEvent.motion.yrel * OrbitDegreesPerPixel);
        else if (sdlEvent.type == SDL_EventType.MouseWheel)
            Camera.Zoom(Math.Pow(ZoomPerStep, sdlEvent.wheel.y));
    }

    public void Render()
    {
        if (_graphicsDevice is null || _executor is null || CurrentScene is null)
        {
            throw new InvalidOperationException("Graphics device is not initialized.");
        }

        _drawList.Clear();
        SceneWalker.Fill(CurrentScene, Camera, _drawList);

        _graphicsDevice.RenderFrame((commandBuffer, framebuffer, extent) =>
        {
            // The colour, and the depth of the far plane of the reversed projection.
            VkClearValue* clearValues = stackalloc VkClearValue[2];
            clearValues[0] = new VkClearValue { color = new VkClearColorValue(0.0f, 1.0f, 0.0f, 1.0f) };
            clearValues[1] = new VkClearValue { depthStencil = new VkClearDepthStencilValue(DepthBuffer.ClearDepth, 0) };

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
                clearValueCount = 2,
                pClearValues = clearValues
            };

            _graphicsDevice.DeviceApi.vkCmdBeginRenderPass(commandBuffer, &renderPassInfo, VkSubpassContents.Inline);
            _executor.Execute(commandBuffer, extent, _drawList.Records, _meshes, Camera.Projection((float)extent.width / extent.height));
            _graphicsDevice.DeviceApi.vkCmdEndRenderPass(commandBuffer);
        });
    }

    // The scene gives the camera when it holds one; else the camera keeps its state.
    public void LoadScene(Scene scene)
    {
        CurrentScene = scene;
        Camera = scene.Cameras.FirstOrDefault() ?? Camera;
        Console.WriteLine($"Loaded scene '{scene.Name}' with {scene.Entities.Count} entities.");
    }

    // Before TASK-0027 the host released nothing, and the process exit freed
    // each Vulkan object. The validation layer now also checks the teardown.
    public void Dispose()
    {
        // The GPU must be idle before a pipeline or a buffer goes.
        _graphicsDevice?.WaitIdle();
        foreach (var mesh in _meshes.Values)
            mesh.Dispose();
        _meshes.Clear();
        _meshPipeline?.Dispose();
        (_meshPipeline, _executor) = (null, null);
        _graphicsDevice?.Dispose();
        _graphicsDevice = null;
    }
}
