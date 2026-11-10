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
    // The colour of the clear: red, green and blue from 0 to 1. ADR-0078 item 1 (TASK-0246): the viewport-clear of the
    // theme of the redesign, in place of the green key of the first image tests, which read this field.
    public static readonly (float R, float G, float B) Background = (Ui.Theme.Clear.R / 255f, Ui.Theme.Clear.G / 255f, Ui.Theme.Clear.B / 255f);

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
    private readonly List<DrawRecord> _others = new();

    // ADR-0109 (phase R18b, TASK-0333): the depth of the selected body, its edge lines, and its hidden edge lines.
    private readonly RenderPipeline _depth;
    private readonly DrawListExecutor _depthWriter;
    private readonly EdgePipeline _edges;
    private readonly EdgePipeline _hiddenEdges;

    // ADR-0109 item 2: the colour and the half width in pixels of the edge lines of a body, and of the selected body; the
    // dash of a hidden edge of the selected body in pixels (item 3).
    internal static readonly System.Numerics.Vector4 EdgeColour = new(0.92f, 0.95f, 1.0f, 1.0f);
    internal static readonly System.Numerics.Vector4 SelectedEdgeColour = new(1.0f, 0.55f, 0.1f, 1.0f);
    internal const float EdgeHalfWidth = 0.75f;
    internal const float SelectedEdgeHalfWidth = 1.0f;
    internal const float HiddenDash = 6f;

    // The images of the scene, each on a quad (ADR-0043, phase V1a).
    private readonly ImageRenderer _images;

    public SceneRenderer(GraphicsDevice device, VkRenderPass renderPass, ISessionHost host)
    {
        _device = device;
        _renderPass = renderPass;
        _pipeline = new RenderPipeline(device, renderPass, "mesh.vert", "mesh.frag");
        _executor = new DrawListExecutor(device, _pipeline);
        _highlight = new RenderPipeline(device, renderPass, "mesh.vert", "highlight.frag", highlight: true);
        _highlighter = new DrawListExecutor(device, _highlight);
        _depth = new RenderPipeline(device, renderPass, "mesh.vert", "highlight.frag", depthOnly: true);
        _depthWriter = new DrawListExecutor(device, _depth);
        _edges = new EdgePipeline(device, renderPass, hidden: false);
        _hiddenEdges = new EdgePipeline(device, renderPass, hidden: true);
        _images = new ImageRenderer(device, renderPass);
        _bodies = new SessionBodies(host);
    }

    // Each live body with its kind, its name and its visibility, for the panel of the user
    // interface (ADR-0027 item 7, ADR-0029 item 7).
    public IReadOnlyList<LiveBody> LiveBodies => _bodies.Live;

    // The count of meshes that the scene uploaded (V48, TASK-0118).
    public int Meshes => _bodies.Meshes;

    // The triangles of each live body for a pick (ADR-0029 item 1). ADR-0101 item 2: while a focus lasts, a pick finds the
    // body of the focus only.
    public IEnumerable<PickBody> PickBodies
        => Focus is { } focus ? _bodies.PickBodies.Select(body => body.Id == focus ? body : body with { Visible = false }) : _bodies.PickBodies;

    // ADR-0101 item 2 (phase R17b, TASK-0315): the body of the focus, or null. This is the one place of the rule: while a focus
    // lasts, the renderer draws the body of the focus only, a pick finds it only, and the dimensions take its box only. The
    // visibility of each body, the box of the far plane and the camera do not change.
    public Guid? Focus { get; set; }

    // ADR-0101 item 2: a draw list that keeps the draws of the mesh of the focus only.
    private sealed class FocusList(DrawList list, string meshId) : IDrawList
    {
        public void Add(string mesh, System.Numerics.Matrix4x4 modelView)
        {
            if (mesh == meshId)
                list.Add(mesh, modelView);
        }
    }

    // ADR-0029 item 8 (decision KD6 of the owner): the camera frames the visible bodies only
    // when their set goes from empty to not empty, so a later change of a body, a rename or a
    // hide, does not move the camera. Until TASK-0086 the camera framed each change.
    public static bool ShouldFrame(int visibleBefore, int visibleAfter) => visibleBefore == 0 && visibleAfter > 0;

    // ADR-0038 items 2 and 7: the pivot of the gizmo of a body, and the preview of its drag.
    public (double X, double Y, double Z)? Pivot(Guid bodyId) => _bodies.Pivot(bodyId);

    public void Preview(Guid bodyId, System.Numerics.Vector3 offset) => _bodies.Preview(bodyId, offset);

    // The box around the live bodies that the scene holds, or null.
    public ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? Bounds => _bodies.Bounds;

    // ADR-0056 items 3 and 6 (phase R13): the boxes of the dimension overlay, kept until a change (V81).
    // ADR-0101 item 2: while a focus lasts, the box of the body of the focus only.
    public ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? Union(bool hiddenToo)
        => Focus is { } focus ? _bodies.BoxOf(focus) : _bodies.Union(hiddenToo);

    // The count of the unions that the bodies made, for a test of V81.
    internal int UnionsMade => _bodies.UnionsMade;

    public ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? BoxOf(Guid bodyId) => _bodies.BoxOf(bodyId);

    // V109 (TASK-0243): the geometry key of the mesh of a live body, or null.
    public Guid? KeyOf(Guid bodyId) => _bodies.KeyOf(bodyId);

    // V87 (TASK-0191): the box of a body with no offset of a preview.
    public ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? RestBoxOf(Guid bodyId) => _bodies.RestBoxOf(bodyId);

    // Brings each change of the live bodies into the scene. When the first visible body
    // comes, the camera frames the visible bodies for the aspect of the target. Register
    // entry R-0051 (TASK-0179): at each change the far plane follows the box of the visible
    // bodies, so a model larger than the default far plane shows whole.
    // Finding V78 of the codebase review of 2026-11-05 (TASK-0181): the far plane follows the eye too, so the host calls
    // this in each frame and the far plane is set in each frame, from the kept box (V81), also with no change of a body.
    // Finding V86 of the codebase review of 2026-11-05 (b) (TASK-0191): the box holds each live image too, so an image
    // larger than the default far plane, also with no body, shows whole.
    public void Update(Scene scene, Camera camera, float aspect, IReadOnlyList<ImageQuad>? images = null)
    {
        var before = _bodies.VisibleCount;
        var changed = _bodies.Apply(scene, _meshes, _device);
        if (SceneBox(_bodies.Bounds, images) is not { } box)
            return;

        // Frame keeps the distance at half the far plane, so the far plane of the box comes before the frame, and again
        // after it, from the new eye.
        camera.FarPlane = camera.FarPlaneFor(box.Min, box.Max);
        if (changed && ShouldFrame(before, _bodies.VisibleCount))
        {
            if (!camera.Frame(box.Min, box.Max, aspect))
                Console.WriteLine($"[WARN] The bodies do not fit between the near plane ({camera.NearPlane}) and the far plane ({camera.FarPlane}) of the camera, so a part of them is not drawn.");
            camera.FarPlane = camera.FarPlaneFor(box.Min, box.Max);
        }
    }

    // V86 (TASK-0191): the box of the bodies and of the four corners of each image quad, or null when both are absent.
    internal static ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? SceneBox(
        ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? bodies, IReadOnlyList<ImageQuad>? images)
    {
        var box = bodies;
        foreach (var quad in images ?? [])
        {
            var (o, r, u) = (quad.Origin, quad.Right, quad.Up);
            foreach (var corner in new (double X, double Y, double Z)[] { o, (o.X + r.X, o.Y + r.Y, o.Z + r.Z), (o.X + u.X, o.Y + u.Y, o.Z + u.Z), (o.X + r.X + u.X, o.Y + r.Y + u.Y, o.Z + r.Z + u.Z) })
                box = box is { } known
                    ? ((Math.Min(known.Min.X, corner.X), Math.Min(known.Min.Y, corner.Y), Math.Min(known.Min.Z, corner.Z)),
                       (Math.Max(known.Max.X, corner.X), Math.Max(known.Max.Y, corner.Y), Math.Max(known.Max.Z, corner.Z)))
                    : (corner, corner);
        }

        return box;
    }

    // ADR-0043 item 4: uploads an image under its identifier, one time. The caller uploads outside
    // a frame.
    public void Upload(string imageId, uint width, uint height, ReadOnlySpan<byte> rgba) => _images.Upload(imageId, width, height, rgba);

    // Records the render pass of one frame: the clear, then each draw of the scene, then each image
    // on its quad (ADR-0043 item 3), then the overlay, for example the user interface (ADR-0027
    // item 3), in the same render pass.
    // ADR-0109 items 2 and 3 (phase R18b, TASK-0333): each body but the selected one draws opaque, then the images, then
    // the edge lines of those bodies. The selected body draws last: its depth, its transparent fill (the highlight), its
    // hidden edges dashed, and its visible edges, each in the colour of the selection. So a body behind the selected body
    // shows through it.
    public void Record(VkCommandBuffer commandBuffer, VkFramebuffer framebuffer, VkExtent2D extent, Scene scene, Camera camera, Action<VkCommandBuffer>? overlay = null, Guid? selected = null, IReadOnlyList<ImageQuad>? images = null)
    {
        _drawList.Clear();
        SceneWalker.Fill(scene, camera, Focus is { } focus ? new FocusList(_drawList, focus.ToString()) : _drawList);

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
        _selected.Clear();
        _others.Clear();
        var selectedMesh = selected?.ToString();
        foreach (var record in _drawList.Records)
            (record.MeshId == selectedMesh ? _selected : _others).Add(record);

        _executor.Execute(commandBuffer, extent, _others, _meshes, projection);
        if (images is { Count: > 0 })
            _images.Record(commandBuffer, extent, images.Select(quad => (quad.ImageId, quad.ModelView(camera))), projection);
        _edges.Execute(commandBuffer, extent, _others, _meshes, projection, EdgeColour, EdgeHalfWidth, 0f);

        if (_selected.Count > 0)
        {
            _depthWriter.Execute(commandBuffer, extent, _selected, _meshes, projection);
            _highlighter.Execute(commandBuffer, extent, _selected, _meshes, projection);
            _hiddenEdges.Execute(commandBuffer, extent, _selected, _meshes, projection, SelectedEdgeColour, SelectedEdgeHalfWidth, HiddenDash);
            _edges.Execute(commandBuffer, extent, _selected, _meshes, projection, SelectedEdgeColour, SelectedEdgeHalfWidth, 0f);
        }

        overlay?.Invoke(commandBuffer);
        _device.DeviceApi.vkCmdEndRenderPass(commandBuffer);
    }

    // Before TASK-0027 the host released nothing, and the process exit freed each
    // Vulkan object. The validation layer now also checks the teardown.
    public void Dispose()
    {
        // The GPU must be idle before a pipeline or a buffer goes.
        _bodies.Dispose();
        // Finding V145 of the codebase review of 2026-11-08 (b) (TASK-0303): on a lost device the wait fails, and the objects
        // go in each case, as Vulkan permits after a loss (E192). Until TASK-0303 the wait threw, and the renderer of the
        // interface, the context of ImGui and the device stayed.
        try
        {
            _device.WaitIdle();
        }
        catch (VkException exception) when (exception.Result == VkResult.ErrorDeviceLost)
        {
        }
        foreach (var mesh in _meshes.Values)
            mesh.Dispose();
        _meshes.Clear();
        _pipeline.Dispose();
        _highlight.Dispose();
        _depth.Dispose();
        _edges.Dispose();
        _hiddenEdges.Dispose();
        _images.Dispose();
    }
}
