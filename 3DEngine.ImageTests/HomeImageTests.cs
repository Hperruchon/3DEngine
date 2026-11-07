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
        Vector4 open, newPart, recent;

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
            newPart = ui.ItemRectangle(DesktopUi.NewPartLabel) ?? throw new InvalidOperationException("No New part.");
            recent = ui.ItemRectangle(DesktopUi.RecentItem("bracket")) ?? throw new InvalidOperationException("No recent row.");

            pixels = target.Render((commandBuffer, framebuffer, frameExtent) =>
                renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                    overlay => panelRenderer.Record(overlay, frameExtent, data, 0), null));
        }

        var picture = SavePicture(pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");

        // Finding V132 of the codebase review of 2026-11-07 (b) (TASK-0271): each control that the name of the test names is
        // drawn: its rectangle holds pixels of a colour that is not the clear. Until TASK-0271 the test read "Open file" only.
        var clear = Theme.Clear;
        foreach (var (name, rectangle) in new[] { ("Open file", open), ("New part", newPart), ("the recent file bracket", recent) })
        {
            var drawn = 0;
            for (var y = (int)rectangle.Y; y < (int)rectangle.W; y++)
            {
                for (var x = (int)rectangle.X; x < (int)rectangle.Z; x++)
                {
                    var index = ((y * (int)Width) + x) * 4;
                    if (Math.Abs(pixels[index] - clear.R) > 2 || Math.Abs(pixels[index + 1] - clear.G) > 2 || Math.Abs(pixels[index + 2] - clear.B) > 2)
                        drawn++;
                }
            }

            Assert.True(drawn > 20, $"The picture {picture}: {name} at {rectangle} has {drawn} pixels that are not the clear.");
        }
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
