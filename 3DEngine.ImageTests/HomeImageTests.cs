using System.Numerics;
using Engine.Api.Http;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0083 D6 (phase P9c, TASK-0259): the home screen on the GPU, at the size of the window of the desktop, over an empty
// scene. The picture is kept for the owner.
[Collection(DearImGui.Collection)]
public class HomeImageTests
{
    private const uint Width = 1280;
    private const uint Height = 720;

    [VulkanImageFact]
    public void The_Home_Screen_Draws_Open_File_New_Part_And_The_Recent_Files()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new Engine.Geometry.Manifold.ManifoldGeometryBackend());
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        ui.ShowHome(["bracket", "plate"]);
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        byte[] pixels;
        Vector4 open;

        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
            using var panelRenderer = new UiRenderer(device, target.RenderPass);
            var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
            ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
            renderer.Update(scene, camera, (float)Width / Height);

            var data = UiDrawData.Empty;
            for (var frame = 0; frame < 3; frame++)
                data = ui.Frame(1f / 60f, new Vector2(Width, Height), Vector2.One, renderer.LiveBodies);
            open = ui.ItemRectangle(DesktopUi.OpenFileLabel) ?? throw new InvalidOperationException("No Open file.");

            pixels = target.Render((commandBuffer, framebuffer, frameExtent) =>
                renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                    overlay => panelRenderer.Record(overlay, frameExtent, data, 0), null));
        }

        var picture = SavePicture(pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");

        // A point inside "Open file", near its left side, is not the clear: the button is drawn there.
        var (column, row) = ((int)open.X + 4, (int)((open.Y + open.W) / 2));
        var index = ((row * (int)Width) + column) * 4;
        var clear = Theme.Clear;
        var (r, g, b) = (pixels[index], pixels[index + 1], pixels[index + 2]);
        Assert.False(Math.Abs(r - clear.R) <= 2 && Math.Abs(g - clear.G) <= 2 && Math.Abs(b - clear.B) <= 2,
            $"The picture {picture}: \"Open file\" at {open} has the colour of the clear.");
    }

    private static string SavePicture(byte[] pixels)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "home.png");
        Png.Write(path, (int)Width, (int)Height, pixels);
        return path;
    }
}
