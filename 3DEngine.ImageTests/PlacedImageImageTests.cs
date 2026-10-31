using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Assets;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// Phase V2b (ADR-0046, TASK-0153): the chain of the desktop for a placed image. A PNG file of 4 by 2
// pixels, with a colour in each quarter, goes into the store and onto the plane XY with the window
// of the images. Two taps on its upper edge and a distance of 80 mm calibrate it to 20 mm for each
// pixel. The projection of the session gives its quad, and a box of 20 at the origin hides its
// lower-left corner. The file placed-image.png is the picture of the visual check.
public sealed class PlacedImageImageTests : IDisposable
{
    private const uint Width = 640;
    private const uint Height = 360;
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-placed-" + Guid.NewGuid().ToString("N"));

    public PlacedImageImageTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // The camera looks along -z at the centre of the calibrated image.
    private static Camera Facing() => new() { Target = (40, 20, 0), Distance = 160, YawDegrees = 0, PitchDegrees = 0 };

    [VulkanImageFact]
    public async Task A_Placed_And_Calibrated_Png_File_Shows_On_Its_Datum()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 20 })).Status);

        // The top row: red, red, white, white. The bottom row: blue, blue, yellow, yellow.
        var file = Path.Combine(_folder, "quarters.png");
        Png.Write(file, 4, 2,
        [
            255, 0, 0, 255, 255, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            0, 0, 255, 255, 0, 0, 255, 255, 255, 255, 0, 255, 255, 255, 0, 255,
        ]);
        var store = new AssetStore(Path.Combine(_folder, "store"));
        var actions = new ImageActions(host.Session, store);
        using var images = new SessionImages(host, store);
        var camera = Facing();

        actions.Place(file, Datums.XY.Id);
        var placed = await host.Session.Read((document, _) => document.Images.ToList());
        var image = Assert.Single(placed);
        actions.StartCalibration(image.Id);
        // The upper-left corner and the upper-right corner of the image, at 1 mm for each pixel.
        Assert.True(actions.Tap(camera, Column(camera, (0, 2, 0)), Row(camera, (0, 2, 0)), Width, Height, placed));
        Assert.True(actions.Tap(camera, Column(camera, (4, 2, 0)), Row(camera, (4, 2, 0)), Width, Height, placed));
        actions.Calibrate(80);

        var scene = SessionScene.Create();
        byte[] pixels;
        var uploads = new List<string>();
        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, host);
            renderer.Update(scene, scene.Cameras[0], (float)Width / Height);
            images.Apply((asset, picture) =>
            {
                uploads.Add(asset);
                renderer.Upload(asset, (uint)picture.Width, (uint)picture.Height, picture.Rgba);
            });
            var quads = images.Quads();
            pixels = target.Render((commandBuffer, framebuffer, extent) =>
                renderer.Record(commandBuffer, framebuffer, extent, scene, camera, images: quads));
        }

        var picture = SavePicture(pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));

        var calibrated = Assert.Single(images.Live);
        Assert.True(Math.Abs(calibrated.Scale - 20) < 1e-3, $"The scale is {calibrated.Scale}.");
        Assert.Equal([calibrated.Asset], uploads);
        Assert.Equal(AssetStore.NameOf(File.ReadAllBytes(file)), calibrated.Asset);

        // Row 0 of the file is at the top of the image, and column 0 at the left (ADR-0045 item 2).
        Assert.Equal("red", Colour(pixels, camera, (20, 30, 0)));
        Assert.Equal("white", Colour(pixels, camera, (60, 30, 0)));
        Assert.Equal("blue", Colour(pixels, camera, (30, 10, 0)));
        Assert.Equal("yellow", Colour(pixels, camera, (60, 10, 0)));
        // The box of 20 at the origin hides the lower-left corner of the image.
        Assert.Equal("other", Colour(pixels, camera, (5, 5, 10)));
        // Outside the image: the clear.
        Assert.Equal("background", Colour(pixels, camera, (40, 45, 0)));
        Assert.True(picture.Length > 0);
    }

    private static Vector4 Clip(Camera camera, (double X, double Y, double Z) point)
    {
        var eye = camera.Eye();
        return Vector4.Transform(new Vector4((float)(point.X - eye.X), (float)(point.Y - eye.Y), (float)(point.Z - eye.Z), 1), camera.View() * camera.Projection((float)Width / Height));
    }

    private static double Column(Camera camera, (double X, double Y, double Z) point)
        => (Clip(camera, point).X / Clip(camera, point).W + 1) / 2 * Width;

    private static double Row(Camera camera, (double X, double Y, double Z) point)
        => (1 - (Clip(camera, point).Y / Clip(camera, point).W)) / 2 * Height;

    private static string Colour(byte[] pixels, Camera camera, (double X, double Y, double Z) point)
    {
        var index = (((int)Row(camera, point) * (int)Width) + (int)Column(camera, point)) * 4;
        return (pixels[index], pixels[index + 1], pixels[index + 2]) switch
        {
            ( > 200, < 60, < 60) => "red",
            ( > 230, > 230, > 230) => "white",
            ( < 60, < 60, > 200) => "blue",
            ( > 200, > 200, < 60) => "yellow",
            ( < 30, > 230, < 30) => "background",
            _ => "other",
        };
    }

    private static string SavePicture(byte[] pixels)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "placed-image.png");
        Png.Write(path, (int)Width, (int)Height, pixels);
        return path;
    }
}
