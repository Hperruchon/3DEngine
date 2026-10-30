using System.Numerics;
using Engine.Api.Http;
using Engine.Core.Geometry;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// The image test of the panel (ADR-0027 item 8, TASK-0084). The renderer of the desktop draws
// the scene and then the panel of the user interface, in one render pass of an offscreen target
// of a device with no window. The picture is compared by structure, with no reference picture:
//
// 1. Inside the panel, most pixels have the dark colour of its background, and some pixels are
//    bright: the glyphs of the text.
// 2. Outside the panel, the pixels have the colour of the clear.
//
// The validation layer checks the synchronization, and each message fails the test.
[Collection(DearImGui.Collection)]
public class PanelImageTests
{
    private const uint Width = 640;
    private const uint Height = 480;

    [VulkanImageFact]
    public void The_Panel_Draws_Its_Text_Over_The_Scene()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        byte[] pixels;
        Vector4 panel;

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
                data = ui.Frame(1f / 60f, new Vector2(Width, Height), Vector2.One, renderer.LiveBodies);
            panel = ui.PanelRectangles.Single();

            pixels = target.Render((commandBuffer, framebuffer, extent) =>
                renderer.Record(commandBuffer, framebuffer, extent, scene, camera,
                    overlay => panelRenderer.Record(overlay, extent, data, 0)));
        }

        var picture = SavePicture(pixels);

        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));

        // The panel lies inside the picture, so that each count below reads it.
        Assert.True(panel.Z <= Width && panel.W <= Height, $"The panel {panel} is not inside the picture {Width} x {Height}.");

        int dark = 0, bright = 0, inside = 0;
        for (var row = (int)panel.Y + 8; row < (int)panel.W - 8; row++)
        {
            for (var column = (int)panel.X + 8; column < (int)panel.Z - 8; column++)
            {
                var (r, g, b) = Pixel(pixels, column, row);
                inside++;
                if (r < 60 && g < 60 && b < 60)
                    dark++;
                if (r > 180 && g > 180 && b > 180)
                    bright++;
            }
        }

        // The buttons are blue and not dark: since the line of a new sketch (TASK-0101) about 70 %
        // of the panel is dark, so the limit is 60 %. A panel that is absent gives no dark pixel.
        Assert.True(dark > inside * 0.6, $"The picture {picture}: {dark} of {inside} pixels of the panel are dark.");
        Assert.True(bright > 100, $"The picture {picture}: {bright} pixels of the panel are bright; the text is absent.");

        var clear = SceneRenderer.Background;
        foreach (var (column, row) in new[] { ((int)Width - 4, (int)Height - 4), ((int)panel.Z + 20, (int)panel.Y + 20) })
        {
            var (r, g, b) = Pixel(pixels, column, row);
            Assert.True(
                Math.Abs(r - clear.R * 255) < 2 && Math.Abs(g - clear.G * 255) < 2 && Math.Abs(b - clear.B * 255) < 2,
                $"The picture {picture}: the pixel ({column}, {row}) outside the panel is ({r}, {g}, {b}), not the colour of the clear.");
        }
    }

    // ADR-0032 items 5 and 11 (TASK-0094): the pad is a second window of dark pixels with bright
    // glyphs, beside the panel, in a picture as large as the window of the desktop.
    [VulkanImageFact]
    public void The_Pad_Draws_Its_Fields_And_Buttons_Over_The_Scene()
    {
        const uint PadWidth = 1280, PadHeight = 720;
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        var display = new Vector2(PadWidth, PadHeight);
        byte[] pixels;
        Vector4 pad;

        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(PadWidth, PadHeight));
            using var renderer = new SceneRenderer(device, target.RenderPass, host);
            using var panelRenderer = new UiRenderer(device, target.RenderPass);
            var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
            ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
            renderer.Update(scene, camera, (float)PadWidth / PadHeight);

            for (var frame = 0; frame < 2; frame++)
                ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies);
            var button = ui.ItemRectangle(DesktopUi.NewBoxLabel)!.Value;
            var (x, y) = ((button.X + button.Z) / 2, (button.Y + button.W) / 2);
            foreach (var phase in new[] { PointerPhase.Down, PointerPhase.Up })
            {
                ui.Route([new PointerSample(DeviceKind.Mouse, 1, 0, x, y, 1, PointerButtons.Left, KeyModifiers.None, phase, 0)]);
                ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies);
            }

            // A new window of Dear ImGui is hidden for its first frame.
            var data = UiDrawData.Empty;
            for (var frame = 0; frame < 3; frame++)
                data = ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies);
            Assert.NotNull(ui.Pad);
            pad = ui.PanelRectangles[1];

            pixels = target.Render((commandBuffer, framebuffer, extent) =>
                renderer.Record(commandBuffer, framebuffer, extent, scene, camera,
                    overlay => panelRenderer.Record(overlay, extent, data, 0)));
        }

        var picture = SavePicture(pixels, PadWidth, PadHeight, "pad.png");
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));
        Assert.True(pad.Z <= PadWidth && pad.W <= PadHeight, $"The pad {pad} is not inside the picture {PadWidth} x {PadHeight}.");

        int dark = 0, bright = 0, inside = 0;
        for (var row = (int)pad.Y + 8; row < (int)pad.W - 8; row++)
        {
            for (var column = (int)pad.X + 8; column < (int)pad.Z - 8; column++)
            {
                var index = (row * (int)PadWidth + column) * 4;
                var (r, g, b) = (pixels[index], pixels[index + 1], pixels[index + 2]);
                inside++;
                if (r < 60 && g < 60 && b < 60)
                    dark++;
                if (r > 180 && g > 180 && b > 180)
                    bright++;
            }
        }

        // The buttons of the pad are blue, so fewer pixels are dark than in the panel.
        Assert.True(dark > inside * 0.4, $"The picture {picture}: {dark} of {inside} pixels of the pad are dark.");
        Assert.True(bright > 300, $"The picture {picture}: {bright} pixels of the pad are bright; the text is absent.");
    }

    private static (int R, int G, int B) Pixel(byte[] pixels, int column, int row)
    {
        var index = (row * (int)Width + column) * 4;
        return (pixels[index], pixels[index + 1], pixels[index + 2]);
    }

    private static string SavePicture(byte[] pixels) => SavePicture(pixels, Width, Height, "panel.png");

    private static string SavePicture(byte[] pixels, uint width, uint height, string name)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, name);
        Png.Write(path, (int)width, (int)height, pixels);
        return path;
    }
}
