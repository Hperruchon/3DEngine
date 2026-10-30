using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts.Sketch;
using Engine.Core.Geometry;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Sketching;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// Phase K2e (TASK-0122): the picture of an active sketch with a circle, an arc and a line tangent
// to the circle, as the desktop draws it: the scene, then the strokes of the overlay and the window
// of the sketch on the panel, in one render pass of an offscreen target with no window. The
// picture is compared by structure: each vertex of each curve has a pixel of the colour of a
// sketch near it. The file curves.png is the picture of the visual check.
[Collection(DearImGui.Collection)]
public class CurveImageTests
{
    private const uint Width = 1280;
    private const uint Height = 720;

    // The camera looks along -z at the plane XY, as the sketch mode turns it (ADR-0034 item 6).
    private static Camera Facing() => new() { Target = (0, 0, 0), Distance = 100, YawDegrees = 0, PitchDegrees = 0 };

    [VulkanImageFact]
    public void A_Circle_And_An_Arc_Draw_In_The_Colour_Of_The_Sketch()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var actions = new SketchActions(host.Session);
        actions.Create(Datums.XY.Id);

        // A circle about (-8, 2) of radius 5, an arc about (8, 2) from (14, 2) to (8, 8), and a line
        // below them that the solve makes tangent to the circle, between the two windows.
        actions.Tool.Kind = SketchToolKind.Circle;
        actions.Next(-8, 2);
        actions.Next(-8, 7);
        actions.Tool.Kind = SketchToolKind.Arc;
        actions.Next(8, 2);
        actions.Next(14, 2);
        actions.Next(8, 8);
        var (a, b) = (actions.AddPoint(-17, -4)!.Value, actions.AddPoint(17, -4)!.Value);
        var line = actions.AddLine(a, b)!.Value;
        var sketch = actions.Current()!;
        Assert.Null(actions.AddConstraint("tangent", [line, sketch.Circles.Single().Id], []));
        sketch = actions.Current()!;
        Assert.True(sketch.Solved);

        var scene = SessionScene.Create();
        var camera = Facing();
        var display = new Vector2(Width, Height);
        var strokes = SketchOverlay.Of([sketch], camera, Width, Height, new SketchOverlay.Focus(sketch.SketchId, [], null));
        var panel = new SketchPanel([sketch], sketch, SketchToolKind.Arc, false, [], null);
        byte[] pixels;
        Vector4[] windows;

        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, host);
            using var panelRenderer = new UiRenderer(device, target.RenderPass);
            var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
            ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
            renderer.Update(scene, camera, (float)Width / Height);

            // A new window of Dear ImGui is hidden for its first frame.
            var data = UiDrawData.Empty;
            for (var frame = 0; frame < 3; frame++)
                data = ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies, null, null, strokes, panel);
            windows = [.. ui.PanelRectangles];

            pixels = target.Render((commandBuffer, framebuffer, extent) =>
                renderer.Record(commandBuffer, framebuffer, extent, scene, camera,
                    overlay => panelRenderer.Record(overlay, extent, data, 0)));
        }

        var picture = SavePicture(pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));

        // The colour 0xFF30C8F0 of DesktopUi.SketchColour is (240, 200, 48) in RGBA. A window of the
        // panel covers the strokes under it.
        var curves = SketchOverlay.Picture(sketch, camera, Width, Height).Curves;
        Assert.Equal(2, curves.Count);
        var seen = 0;
        foreach (var curve in curves)
        {
            foreach (var (vertex, _) in curve.Pixels)
            {
                if (windows.Any(w => vertex.X >= w.X - 4 && vertex.X <= w.Z + 4 && vertex.Y >= w.Y - 4 && vertex.Y <= w.W + 4))
                    continue;
                Assert.True(NearSketchColour(pixels, vertex.X, vertex.Y), $"No pixel of the sketch near {vertex} of {curve.Id} in {picture}.");
                seen++;
            }
        }

        Assert.Equal(SketchOverlay.TurnSegments + (SketchOverlay.TurnSegments / 4), seen);
    }

    // A pixel within 2 pixels of (x, y) with the colour of a sketch.
    private static bool NearSketchColour(byte[] pixels, double x, double y)
    {
        for (var row = (int)y - 2; row <= (int)y + 2; row++)
        {
            for (var column = (int)x - 2; column <= (int)x + 2; column++)
            {
                if (row < 0 || column < 0 || row >= Height || column >= Width)
                    continue;
                var index = (row * (int)Width + column) * 4;
                if (pixels[index] > 200 && pixels[index + 1] > 160 && pixels[index + 1] < 240 && pixels[index + 2] < 100)
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
        var path = Path.Combine(folder, "curves.png");
        Png.Write(path, (int)Width, (int)Height, pixels);
        return path;
    }
}
