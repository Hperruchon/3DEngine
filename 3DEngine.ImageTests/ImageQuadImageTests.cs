using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Rendering;
using ThreeDEngine.Desktop;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// Phase V1a (ADR-0043, TASK-0146): an image on a quad with the depth of the bodies. A box of 20 at the
// origin stands between two quads: one behind it, red on its left half and blue on its right half,
// and one small white quad in front of it. The picture is compared by structure at the pixels of
// known points. The file image-quad.png is the picture of the visual check.
public class ImageQuadImageTests
{
    private const uint Width = 640;
    private const uint Height = 360;

    // The camera looks along -z at the origin from z = 100.
    private static Camera Facing() => new() { Target = (0, 0, 0), Distance = 100, YawDegrees = 0, PitchDegrees = 0 };

    [VulkanImageFact]
    public async Task An_Image_Takes_The_Depth_Of_The_Bodies()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var box = await host.Session.Apply(new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 20 });
        Assert.Equal(CommandStatus.Applied, box.Status);

        // Two texels, red and blue, and one white texel.
        byte[] halves = [255, 0, 0, 255, 0, 0, 255, 255];
        byte[] white = [255, 255, 255, 255];
        IReadOnlyList<ImageQuad> quads =
        [
            new ImageQuad("halves", (-40, -20, -20), (80, 0, 0), (0, 40, 0)),
            new ImageQuad("white", (-3, -3, 15), (6, 0, 0), (0, 6, 0)),
            new ImageQuad("absent", (-40, 25, 0), (80, 0, 0), (0, 5, 0)),
        ];

        var scene = SessionScene.Create();
        var camera = Facing();
        byte[] pixels;
        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, host);
            // The first visible body frames the camera that Update gets, so the test keeps its own.
            renderer.Update(scene, scene.Cameras[0], (float)Width / Height);
            renderer.Upload("halves", 2, 1, halves);
            renderer.Upload("white", 1, 1, white);
            renderer.Upload("white", 1, 1, [0, 0, 0, 255]);

            pixels = target.Render((commandBuffer, framebuffer, extent) =>
                renderer.Record(commandBuffer, framebuffer, extent, scene, camera, images: quads));
        }

        var picture = SavePicture(pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));

        // The quad behind the box: red on the left, blue on the right.
        Assert.Equal("red", Colour(pixels, camera, (-30, 0, -20)));
        Assert.Equal("blue", Colour(pixels, camera, (30, 0, -20)));
        // The box hides the quad behind it, and the white quad in front of it hides the box. The second
        // upload of "white" changed nothing (ADR-0043 item 4).
        Assert.Equal("other", Colour(pixels, camera, (7, 7, 10)));
        // The face of the box has one colour, and the quad behind it has a blend of red and blue that
        // differs at the two points.
        Assert.Equal(Rgb(pixels, camera, (-7, -7, 10)), Rgb(pixels, camera, (7, 7, 10)));
        Assert.Equal("white", Colour(pixels, camera, (0, 0, 15)));
        // Outside each quad, and on the quad of an image that the renderer does not hold, the clear.
        Assert.Equal("background", Colour(pixels, camera, (0, 23, -20)));
        Assert.Equal("background", Colour(pixels, camera, (0, 27, 0)));
        Assert.True(picture.Length > 0);
    }

    // The colour of the pixel of a point of the scene, as the GPU projects it.
    private static (byte R, byte G, byte B) Rgb(byte[] pixels, Camera camera, (double X, double Y, double Z) point)
    {
        var eye = camera.Eye();
        var clip = Vector4.Transform(new Vector4((float)(point.X - eye.X), (float)(point.Y - eye.Y), (float)(point.Z - eye.Z), 1), camera.View() * camera.Projection((float)Width / Height));
        var column = (int)((clip.X / clip.W + 1) / 2 * Width);
        var row = (int)((1 - (clip.Y / clip.W)) / 2 * Height);
        var index = ((row * (int)Width) + column) * 4;
        return (pixels[index], pixels[index + 1], pixels[index + 2]);
    }

    // The colour class of the pixel of a point of the scene.
    private static string Colour(byte[] pixels, Camera camera, (double X, double Y, double Z) point)
    {
        return Rgb(pixels, camera, point) switch
        {
            ( > 200, < 60, < 60) => "red",
            ( < 60, < 60, > 200) => "blue",
            ( > 230, > 230, > 230) => "white",
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
        var path = Path.Combine(folder, "image-quad.png");
        Png.Write(path, (int)Width, (int)Height, pixels);
        return path;
    }
}
