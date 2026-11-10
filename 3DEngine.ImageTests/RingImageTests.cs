using System.Numerics;
using Engine.Api.Http;
using Engine.Core.Commands;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0081 (the redesign, slice b2, TASK-0253): the tool ring over the scene on the GPU, at the size of the window of the
// desktop. The picture is kept for the owner.
[Collection(DearImGui.Collection)]
public class RingImageTests
{
    private const uint Width = 1280;
    private const uint Height = 720;

    [VulkanImageFact]
    public async Task The_Ring_Draws_Its_Segments_Around_The_Selected_Body()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new Engine.Geometry.Manifold.ManifoldGeometryBackend());
        var box = await host.Session.Apply(new CreateBoxCommand { SizeX = 40, SizeY = 30, SizeZ = 10 });
        Assert.True(box.Outputs.TryGet<Guid>("bodyId", out var body));
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        byte[] pixels;
        DesktopUi.RingLayout? ring = null;

        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
            using var panelRenderer = new UiRenderer(device, target.RenderPass);
            var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
            ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
            renderer.Update(scene, camera, (float)Width / Height);

            var extent = renderer.BoxOf(body) ?? throw new InvalidOperationException("The box has no mesh.");
            var middle = ((extent.Min.X + extent.Max.X) / 2, (extent.Min.Y + extent.Max.Y) / 2, (extent.Min.Z + extent.Max.Z) / 2);
            var centre = SketchOverlay.Segment(camera, Width, Height, middle, middle)?.A ?? throw new InvalidOperationException("The box is behind the eye.");
            var diagonal = SketchOverlay.Segment(camera, Width, Height, extent.Min, extent.Max) ?? throw new InvalidOperationException("The box is behind the eye.");
            var size = Vector2.Distance(diagonal.A, diagonal.B) / 2f;
            var data = UiDrawData.Empty;
            for (var frame = 0; frame < 3; frame++)
                data = ui.Frame(1f / 60f, new Vector2(Width, Height), Vector2.One, renderer.LiveBodies, body, ringCentre: centre, ringBody: size);
            // ADR-0101 item 3 (TASK-0315): a box has Move, Hide and Focus; ADR-0119 item 1 (TASK-0346): and Rename.
            ring = DesktopUi.Layout(new Vector2(Width, Height), centre, 4, size);

            pixels = target.Render((commandBuffer, framebuffer, frameExtent) =>
                renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                    overlay => panelRenderer.Record(overlay, frameExtent, data, 0), body));
        }

        var picture = SavePicture(pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");

        // ADR-0108 item 2 (phase R18a, TASK-0332): the outline of the segment Move is drawn, and the segment has no fill at
        // rest, so a point inside it, away from its icon and its label, has the colour of the clear. Until TASK-0332 the
        // middle of the segment had a blue ground.
        var segment = ring.Segments([DesktopUi.MoveLabel, DesktopUi.HideLabel, DesktopUi.FocusLabel, DesktopUi.RenameLabel])[0];
        var angle = (segment.From + segment.To) / 2f;
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var outline = ring.Centre + (direction * ring.Outer);
        Assert.True(AnyNotClear(pixels, outline, 2), $"The picture {picture}: the outline of the segment Move at {outline} is not drawn.");
        var inside = segment.From + (0.08f * (segment.To - segment.From));
        var empty = ring.Centre + (new Vector2(MathF.Cos(inside), MathF.Sin(inside)) * ((ring.Inner + ring.Outer) / 2f));
        Assert.False(AnyNotClear(pixels, empty, 1), $"The picture {picture}: the segment Move at {empty} has a fill.");
    }

    // A pixel within this distance of the point is not the clear.
    private static bool AnyNotClear(byte[] pixels, Vector2 point, int distance)
    {
        var clear = Theme.Clear;
        for (var y = (int)point.Y - distance; y <= (int)point.Y + distance; y++)
        {
            for (var x = (int)point.X - distance; x <= (int)point.X + distance; x++)
            {
                var index = ((y * (int)Width) + x) * 4;
                if (Math.Abs(pixels[index] - clear.R) > 2 || Math.Abs(pixels[index + 1] - clear.G) > 2 || Math.Abs(pixels[index + 2] - clear.B) > 2)
                    return true;
            }
        }

        return false;
    }

    private static string SavePicture(byte[] pixels)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "ring.png");
        Png.Write(path, (int)Width, (int)Height, pixels);
        return path;
    }
}
