using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0113 (phase R19, TASK-0336): at each size of the list and in each state, each control stays in the display, no
// control goes past the right edge of its panel, no window covers a control of another window or a tool of the ring, and
// no two fixed windows overlap. A control that its panel scrolls out of view is not a fault. The sheet of a proposal may
// cover the panel (ADR-0092 item 7), but not a tool of the ring (ADR-0111 item 4). ADR-0113 names this file in its
// enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class ResponsiveLayoutTests
{
    private static readonly Guid A = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
    private static readonly Guid B = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000");
    private static readonly LiveBody Plate = new(A, "Extrude", "Plate", true);
    private static readonly LiveBody Rib = new(B, "Extrude", "Rib", true, new BodyFault("E-KERNEL-EXTRUDE-OPEN", "The profile is not closed."));
    private static readonly DocumentLine Shelf = new("entry-hall-shelf.3de", null, Kind: Document.Part);

    // The states of the probe of TASK-0336.
    public enum State
    {
        Nothing,
        Body,
        Face,
        Proposal,
    }

    // The sizes of ADR-0113 item 1: the phones in portrait and in landscape, a tablet in both, and two windows.
    public static TheoryData<int, int> Sizes => new()
    {
        { 360, 640 }, { 360, 780 }, { 384, 824 }, { 390, 844 }, { 393, 873 }, { 412, 915 },
        { 640, 360 }, { 844, 390 }, { 915, 412 },
        { 768, 1024 }, { 1024, 768 }, { 1280, 720 }, { 1440, 900 },
    };

    [Theory]
    [MemberData(nameof(Sizes))]
    public void Each_Control_Stays_In_The_Display_And_Free_For_A_Tap(int width, int height)
    {
        var faults = new List<string>();
        foreach (var state in Enum.GetValues<State>())
            faults.AddRange(Faults(width, height, state).Select(fault => $"{width}x{height} {state}: {fault}"));
        Assert.True(faults.Count == 0, string.Join(Environment.NewLine, faults));
    }

    // The faults of the layout at one size in one state, after three frames.
    internal static IReadOnlyList<string> Faults(int width, int height, State state)
    {
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        var display = new Vector2(width, height);
        Guid? selection = state == State.Nothing ? null : A;
        var face = state == State.Face ? new FaceLine("Face +z: 100 square mm", true, [], null, null, "Face +z", null) : null;
        IReadOnlyList<ProposalCard>? cards = state == State.Proposal
            ? [new ProposalCard(Guid.NewGuid(), "Widen the plate to 75 mm", ["SetExtrudeDistance 1", "SetExtrudeDistance 2"])]
            : null;
        for (var i = 0; i < 3; i++)
            ui.Frame(1f / 60f, display, Vector2.One, [Plate, Rib], selection, Shelf, face: face, ringCentre: selection is null ? null : new Vector2(width / 2f, height * 0.55f), ringBody: 120f, proposals: cards);

        var panels = ui.PanelRectangles.ToList();
        var accept = ui.ItemRectangle(DesktopUi.ProposalAcceptItem);
        // The sheet is the smallest panel that holds Accept; on a phone the panel of the bodies holds it too.
        Vector4? sheet = accept is { } a ? panels.Where(panel => Holds(panel, a)).Cast<Vector4?>().MinBy(panel => panel!.Value.W - panel.Value.Y) : null;
        var windows = panels.Concat(ui.FixedRectangles).ToList();
        var faults = new List<string>();
        foreach (var (name, r) in ui.ItemRectangles)
        {
            if (panels.Any(panel => Scrolled(panel, r)))
                continue;
            if (r.X < -0.5f || r.Y < -0.5f || r.Z > width + 0.5f || r.W > height + 0.5f)
                faults.Add($"{name} {Text(r)} goes out of the display");
            var middle = new Vector2((r.X + r.Z) / 2f, (r.Y + r.W) / 2f);
            foreach (var window in windows)
            {
                if (!Inside(window, middle))
                    continue;
                // A tool of the ring and its label are on the scene, so they are never a control of a window.
                var ring = name.StartsWith("Ring", StringComparison.Ordinal);
                if (!ring && Own(window, r))
                {
                    if (r.Z > window.Z + 0.5f)
                        faults.Add($"{name} {Text(r)} goes past the right edge of its window {Text(window)}");
                    continue;
                }

                if (window == sheet && !ring)
                    continue;
                faults.Add($"{name} {Text(r)} is under the window {Text(window)}");
            }
        }

        var fixedWindows = ui.FixedRectangles.ToList();
        for (var i = 0; i < windows.Count; i++)
        {
            for (var j = Math.Max(i + 1, panels.Count); j < windows.Count; j++)
            {
                if (windows[i] != sheet && Overlap(windows[i], windows[j]))
                    faults.Add($"the window {Text(windows[i])} overlaps the fixed window {Text(windows[j])}");
            }
        }

        return faults;
    }

    // ADR-0113 item 1: the pictures for the owner at the phone sizes and the smallest landscape display, with a selected body,
    // and with a proposal of an agent.
    [VulkanImageFact]
    public async Task The_Pictures_Show_Each_Control_In_The_Display()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var plate = Out(await host.Session.Apply(new CreateBoxCommand { SizeX = 40, SizeY = 24, SizeZ = 6 }), "bodyId");
        await host.Session.Apply(new RenameBodyCommand { BodyId = plate, NewName = "Plate" });
        var post = Out(await host.Session.Apply(new CreateCylinderCommand { Radius = 4, Height = 20 }), "bodyId");
        await host.Session.Apply(new RenameBodyCommand { BodyId = post, NewName = "Post" });

        foreach (var (width, height) in new[] { (360u, 780u), (390u, 844u), (412u, 915u), (844u, 390u), (640u, 360u) })
        {
            foreach (var proposal in new[] { false, true })
            {
                using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
                var scene = SessionScene.Create();
                var camera = scene.Cameras[0];
                var display = new Vector2(width, height);
                IReadOnlyList<ProposalCard>? cards = proposal ? [new ProposalCard(Guid.NewGuid(), "Widen the plate to 75 mm", ["SetExtrudeDistance 1"])] : null;
                byte[] pixels;
                using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
                {
                    using var target = new OffscreenTarget(device, new VkExtent2D(width, height));
                    using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
                    using var panelRenderer = new UiRenderer(device, target.RenderPass);
                    var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
                    ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
                    renderer.Update(scene, camera, (float)width / height);
                    var data = UiDrawData.Empty;
                    for (var frame = 0; frame < 3; frame++)
                    {
                        ui.Route([]);
                        data = ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies, plate, Shelf, ringCentre: new Vector2(width / 2f, height * 0.55f), ringBody: 120f, proposals: cards);
                    }

                    pixels = target.Render((commandBuffer, framebuffer, frameExtent) =>
                        renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                            overlay => panelRenderer.Record(overlay, frameExtent, data, 0), plate));
                }

                var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
                    ? output
                    : Path.Combine(AppContext.BaseDirectory, "pictures");
                Directory.CreateDirectory(folder);
                Png.Write(Path.Combine(folder, $"responsive-{(proposal ? "proposal" : "body")}-{width}x{height}.png"), (int)width, (int)height, pixels);
            }
        }

        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");
    }

    private static Guid Out(CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    private static bool Inside(Vector4 box, Vector2 point) => point.X >= box.X && point.X < box.Z && point.Y >= box.Y && point.Y < box.W;

    private static bool Holds(Vector4 box, Vector4 r) => r.X >= box.X - 0.5f && r.Z <= box.Z + 0.5f && r.Y >= box.Y - 0.5f && r.W <= box.W + 0.5f;

    // A control of a window: its left side is in the window and its top is in the window, so the window clips the rest.
    private static bool Own(Vector4 box, Vector4 r) => r.X >= box.X - 0.5f && r.X < box.Z && r.Y >= box.Y - 0.5f && r.Y < box.W;

    // A control that its panel scrolls out of view: in the columns of the panel, and its top at or below the bottom of it.
    private static bool Scrolled(Vector4 panel, Vector4 r) => r.X >= panel.X - 0.5f && r.X < panel.Z && r.Y >= panel.W - 0.5f;

    private static bool Overlap(Vector4 p, Vector4 q) => Math.Min(p.Z, q.Z) - Math.Max(p.X, q.X) > 1f && Math.Min(p.W, q.W) - Math.Max(p.Y, q.Y) > 1f;

    private static string Text(Vector4 r) => $"({r.X:0}, {r.Y:0}, {r.Z:0}, {r.W:0})";
}
