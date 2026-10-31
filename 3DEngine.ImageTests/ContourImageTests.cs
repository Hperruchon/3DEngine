using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts.Sketch;
using Engine.Core.Assets;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Queries;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// Phase V3 (ADR-0047, TASK-0155): a PNG file of an L-shaped dark part on a light ground, placed on the plane
// XY and calibrated to 0.5 mm for each pixel. The desktop proposes its contour, the person accepts it, and
// the overlay draws the new sketch over the image in the colour of a sketch. The file contour.png is the
// picture of the visual check. Dear ImGui keeps one global context, so the test is in the collection of
// the tests of the panel; out of it, a parallel test of the panel crashed the test host.
[Collection(DearImGui.Collection)]
public sealed class ContourImageTests : IDisposable
{
    private const uint Width = 1280;
    private const uint Height = 720;
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-contour-image-" + Guid.NewGuid().ToString("N"));

    public ContourImageTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // The camera looks along -z; the image is to the right of the panel.
    private static Camera Facing() => new() { Target = (10, 25, 0), Distance = 120, YawDegrees = 0, PitchDegrees = 0 };

    // A picture of 160 by 100 pixels: light, with a dark L of a bar from (20, 20) to (139, 49) and a foot
    // from (20, 50) to (59, 84).
    private string Part()
    {
        var rgba = new byte[160 * 100 * 4];
        for (var y = 0; y < 100; y++)
        {
            for (var x = 0; x < 160; x++)
            {
                var dark = (x is >= 20 and <= 139 && y is >= 20 and <= 49) || (x is >= 20 and <= 59 && y is >= 50 and <= 84);
                rgba.AsSpan(((y * 160) + x) * 4, 3).Fill(dark ? (byte)40 : (byte)210);
                rgba[(((y * 160) + x) * 4) + 3] = 255;
            }
        }

        var path = Path.Combine(_folder, "part.png");
        Png.Write(path, 160, 100, rgba);
        return path;
    }

    [VulkanImageFact]
    public async Task An_Accepted_Contour_Draws_Over_Its_Image()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new InProcessMeshBackend());
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var store = new AssetStore(Path.Combine(_folder, "store"));
        var actions = new ImageActions(host.Session, store);
        using var images = new SessionImages(host, store);

        actions.Place(Part(), Datums.XY.Id);
        var placed = Assert.Single(await host.Session.Read((document, _) => document.Images.ToList()));
        await host.Session.Apply(new CalibrateImageCommand { ImageId = placed.Id, X1 = 0, Y1 = 0, X2 = 160, Y2 = 0, Distance = 80 });
        var image = Assert.Single(await host.Session.Read((document, _) => document.Images.ToList()));
        actions.Propose(image.Id, [image]);
        Assert.Equal(6, actions.Proposal?.Pixels.Count);
        var accepted = actions.Accept([image])!.Value;
        var sketch = (await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = accepted.Sketch })).Result!;

        var scene = SessionScene.Create();
        var camera = Facing();
        var display = new Vector2(Width, Height);
        var strokes = SketchOverlay.Of([sketch], camera, Width, Height, new SketchOverlay.Focus(sketch.SketchId, [], null));
        byte[] pixels;
        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, host);
            using var panelRenderer = new UiRenderer(device, target.RenderPass);
            var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
            ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
            renderer.Update(scene, camera, (float)Width / Height);
            images.Apply((asset, picture) => renderer.Upload(asset, (uint)picture.Width, (uint)picture.Height, picture.Rgba));
            var quads = images.Quads();

            var data = UiDrawData.Empty;
            for (var frame = 0; frame < 3; frame++)
                data = ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies, null, null, strokes);

            pixels = target.Render((commandBuffer, framebuffer, extent) =>
                renderer.Record(commandBuffer, framebuffer, extent, scene, camera,
                    overlay => panelRenderer.Record(overlay, extent, data, 0), images: quads));
        }

        var picture = SavePicture(pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));

        // The middle of each side of the sketch has the colour of a sketch within two pixels: 0xFF30C8F0 of
        // DesktopUi.SketchColour is (240, 200, 48) in RGBA.
        var corners = sketch.Points.Skip(1).ToList();
        Assert.Equal(6, corners.Count);
        for (var i = 0; i < corners.Count; i++)
        {
            var (a, b) = (corners[i], corners[(i + 1) % corners.Count]);
            var middle = SketchOverlay.Pixel(sketch, camera, Width, Height, ((a.X + b.X) / 2, (a.Y + b.Y) / 2))!.Value;
            Assert.True(Near(pixels, middle, (240, 200, 48)), $"Side {i} has no colour of a sketch near {middle}.");
        }

        // Inside the part the image is dark, and outside it the image is light.
        Assert.True(Grey(pixels, SketchOverlay.Pixel(sketch, camera, Width, Height, ImagePlacement.At(image, (80, 35)))!.Value) < 80);
        Assert.True(Grey(pixels, SketchOverlay.Pixel(sketch, camera, Width, Height, ImagePlacement.At(image, (100, 75)))!.Value) > 170);
        Assert.True(picture.Length > 0);
    }

    private static int Index(Vector2 at) => (((int)at.Y * (int)Width) + (int)at.X) * 4;

    private static int Grey(byte[] pixels, Vector2 at) => (pixels[Index(at)] + pixels[Index(at) + 1] + pixels[Index(at) + 2]) / 3;

    private static bool Near(byte[] pixels, Vector2 at, (int R, int G, int B) colour)
    {
        for (var dy = -2; dy <= 2; dy++)
        {
            for (var dx = -2; dx <= 2; dx++)
            {
                var index = Index(at + new Vector2(dx, dy));
                if (Math.Abs(pixels[index] - colour.R) < 30 && Math.Abs(pixels[index + 1] - colour.G) < 30 && Math.Abs(pixels[index + 2] - colour.B) < 30)
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
        var path = Path.Combine(folder, "contour.png");
        Png.Write(path, (int)Width, (int)Height, pixels);
        return path;
    }
}
