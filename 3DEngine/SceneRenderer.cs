using Engine.Api.Http;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Core.Rendering;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.Desktop;

// The draw of the scene of a session into one target (roadmap phases R3 to R6). Update
// brings the live bodies of the session into the scene (SessionBodies, ADR-0024), and
// frames them with the camera when the set of visible bodies goes from empty to not empty
// (ADR-0025 item 6, ADR-0029 item 8). Record walks the scene
// into a draw list relative to the eye, and the Vulkan layer executes the list
// (ADR-0023). The window and the image test use this one renderer, so the test draws
// what the person sees (TASK-0076).
internal sealed unsafe class SceneRenderer : IDisposable
{
    // The colour of the clear: red, green and blue from 0 to 1.
    public static readonly (float R, float G, float B) Background = (0.0f, 1.0f, 0.0f);

    private readonly GraphicsDevice _device;
    private readonly VkRenderPass _renderPass;
    private readonly SessionBodies _bodies;
    private readonly DrawList _drawList = new();
    private readonly Dictionary<string, GpuMesh> _meshes = new(StringComparer.Ordinal);
    private readonly RenderPipeline _pipeline;
    private readonly DrawListExecutor _executor;

    // The highlight of the selected body (ADR-0029 item 2).
    private readonly RenderPipeline _highlight;
    private readonly DrawListExecutor _highlighter;
    private readonly List<DrawRecord> _selected = new();

    public SceneRenderer(GraphicsDevice device, VkRenderPass renderPass, EngineHost host)
    {
        _device = device;
        _renderPass = renderPass;
        _pipeline = new RenderPipeline(device, renderPass, "mesh.vert", "mesh.frag");
        _executor = new DrawListExecutor(device, _pipeline);
        _highlight = new RenderPipeline(device, renderPass, "mesh.vert", "highlight.frag", highlight: true);
        _highlighter = new DrawListExecutor(device, _highlight);
        _bodies = new SessionBodies(host);
    }

    // Each live body with its kind, its name and its visibility, for the panel of the user
    // interface (ADR-0027 item 7, ADR-0029 item 7).
    public IReadOnlyList<LiveBody> LiveBodies => _bodies.Live;

    // The triangles of each live body for a pick (ADR-0029 item 1).
    public IEnumerable<PickBody> PickBodies => _bodies.PickBodies;

    // ADR-0029 item 8 (decision KD6 of the owner): the camera frames the visible bodies only
    // when their set goes from empty to not empty, so a later change of a body, a rename or a
    // hide, does not move the camera. Until TASK-0086 the camera framed each change.
    public static bool ShouldFrame(int visibleBefore, int visibleAfter) => visibleBefore == 0 && visibleAfter > 0;

    // The box around the live bodies that the scene holds, or null.
    public ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? Bounds => _bodies.Bounds;

    // Brings each change of the live bodies into the scene. When the first visible body
    // comes, the camera frames the visible bodies for the aspect of the target.
    public void Update(Scene scene, Camera camera, float aspect)
    {
        var before = _bodies.VisibleCount;
        if (_bodies.Apply(scene, _meshes, _device) && ShouldFrame(before, _bodies.VisibleCount) && _bodies.Bounds is { } box
            && !camera.Frame(box.Min, box.Max, aspect))
        {
            Console.WriteLine($"[WARN] The bodies do not fit between the near plane ({camera.NearPlane}) and the far plane ({camera.FarPlane}) of the camera, so a part of them is not drawn.");
        }
    }

    // Records the render pass of one frame: the clear, then each draw of the scene, then the
    // overlay, for example the user interface (ADR-0027 item 3), in the same render pass.
    public void Record(VkCommandBuffer commandBuffer, VkFramebuffer framebuffer, VkExtent2D extent, Scene scene, Camera camera, Action<VkCommandBuffer>? overlay = null, Guid? selected = null)
    {
        _drawList.Clear();
        SceneWalker.Fill(scene, camera, _drawList);

        // The colour, and the depth of the far plane of the reversed projection.
        VkClearValue* clearValues = stackalloc VkClearValue[2];
        clearValues[0] = new VkClearValue { color = new VkClearColorValue(Background.R, Background.G, Background.B, 1.0f) };
        clearValues[1] = new VkClearValue { depthStencil = new VkClearDepthStencilValue(DepthBuffer.ClearDepth, 0) };

        VkRenderPassBeginInfo renderPassInfo = new()
        {
            renderPass = _renderPass,
            framebuffer = framebuffer,
            renderArea = new VkRect2D
            {
                offset = new VkOffset2D(0, 0),
                extent = extent
            },
            clearValueCount = 2,
            pClearValues = clearValues
        };

        _device.DeviceApi.vkCmdBeginRenderPass(commandBuffer, &renderPassInfo, VkSubpassContents.Inline);
        var projection = camera.Projection((float)extent.width / extent.height);
        _executor.Execute(commandBuffer, extent, _drawList.Records, _meshes, projection);

        // The selected body again, through the highlight pipeline, after each body.
        _selected.Clear();
        if (selected is { } body)
            _selected.AddRange(_drawList.Records.Where(record => record.MeshId == body.ToString()));
        if (_selected.Count > 0)
            _highlighter.Execute(commandBuffer, extent, _selected, _meshes, projection);

        overlay?.Invoke(commandBuffer);
        _device.DeviceApi.vkCmdEndRenderPass(commandBuffer);
    }

    // Before TASK-0027 the host released nothing, and the process exit freed each
    // Vulkan object. The validation layer now also checks the teardown.
    public void Dispose()
    {
        // The GPU must be idle before a pipeline or a buffer goes.
        _bodies.Dispose();
        _device.WaitIdle();
        foreach (var mesh in _meshes.Values)
            mesh.Dispose();
        _meshes.Clear();
        _pipeline.Dispose();
        _highlight.Dispose();
    }
}
