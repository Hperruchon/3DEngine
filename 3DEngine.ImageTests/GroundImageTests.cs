using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0178 (phase R18p, TASK-0423): the ground of the scene has the soft light of the render "mix", from the colour of a
// corner to a lighter colour at the middle, and each body lies over it. The test reads each pixel of a picture against
// Theme.GroundAt, with no reference picture. ADR-0178 names this file in its enforced-by field.
// The class creates a Vulkan device, so it runs in the collection of the devices (R-0062).
[Collection(DearImGui.Collection)]
public sealed class GroundImageTests
{
    // Item 1: the colour of the middle at the middle, the colour of a corner at each corner, the same colour at two pixels
    // symmetric about the middle, and no channel gets lighter from the middle to a corner.
    [Fact]
    public void The_Ground_Goes_From_The_Colour_Of_A_Corner_To_The_Colour_Of_The_Middle()
    {
        foreach (var (width, height) in new[] { (1280, 720), (360, 780) })
        {
            Assert.Equal(Theme.GroundMiddle, Theme.GroundAt(width / 2, height / 2, width, height));
            foreach (var (column, row) in new[] { (0, 0), (width - 1, 0), (0, height - 1), (width - 1, height - 1) })
                Near(Theme.GroundCorner, Theme.GroundAt(column, row, width, height), 1, $"the corner ({column}, {row}) of {width} x {height}");

            // Step 0 is the corner (0, 0) and step 64 is the middle.
            for (var step = 0; step <= 64; step++)
            {
                var (column, row) = (width / 2 * step / 64, height / 2 * step / 64);
                Assert.Equal(Theme.GroundAt(column, row, width, height), Theme.GroundAt(width - 1 - column, height - 1 - row, width, height));
                if (step == 0)
                    continue;
                var (inner, outer) = (Theme.GroundAt(column, row, width, height), Theme.GroundAt(width / 2 * (step - 1) / 64, height / 2 * (step - 1) / 64, width, height));
                Assert.True(outer.R <= inner.R && outer.G <= inner.G && outer.B <= inner.B, $"The ground at ({column}, {row}) of {width} x {height} is darker than nearer the corner.");
            }
        }
    }

    // Items 1 and 2: with no body, each pixel of the picture is the ground at that pixel, within 1 for the rounding of the GPU,
    // and the middle is lighter than the corners. Until TASK-0423 each pixel was the one colour of the clear.
    [VulkanImageFact]
    public void An_Empty_Scene_Shows_The_Soft_Light_Of_The_Ground()
    {
        foreach (var (width, height) in new[] { (1280u, 720u), (360u, 780u) })
        {
            using var host = new EngineHost(new ManifoldGeometryBackend());
            var pixels = Draw(host, width, height, $"ground-{width}x{height}.png");

            var faults = 0;
            for (var row = 0; row < height; row++)
            {
                for (var column = 0; column < width; column++)
                {
                    if (!GroundPixels.IsGround(pixels, column, row, (int)width, (int)height, 1))
                        faults++;
                }
            }

            Assert.True(faults == 0, $"{faults} pixels of the picture {width} x {height} are not the ground at their place.");
            var middle = ((((int)height / 2 * (int)width) + ((int)width / 2)) * 4) + 2;
            Assert.True(pixels[middle] >= pixels[2] + 20, $"The blue of the middle ({pixels[middle]}) is not lighter than the blue of a corner ({pixels[2]}).");
        }
    }

    // Item 2: the ground is the first draw, so a body at the middle lies over the light, and a corner stays the ground.
    [VulkanImageFact]
    public async Task A_Body_At_The_Middle_Lies_Over_The_Light()
    {
        const uint width = 800, height = 600;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var result = await host.Session.Apply(new CreateBoxCommand { SizeX = 40, SizeY = 24, SizeZ = 10 });
        Assert.Equal(CommandStatus.Applied, result.Status);

        var pixels = Draw(host, width, height, "ground-body.png");

        Assert.False(GroundPixels.IsGround(pixels, (int)width / 2, (int)height / 2, (int)width, (int)height), "The middle of the picture is the ground, not the body.");
        Assert.True(GroundPixels.IsGround(pixels, 0, 0, (int)width, (int)height, 1), "The corner of the picture is not the ground.");
    }

    // Draws the scene of the session into a picture of width by height with no overlay, and saves it.
    private static byte[] Draw(EngineHost host, uint width, uint height, string name)
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        byte[] pixels;
        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(width, height));
            using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
            renderer.Update(scene, camera, (float)width / height);
            pixels = target.Render((commandBuffer, framebuffer, extent) => renderer.Record(commandBuffer, framebuffer, extent, scene, camera));
        }

        Png.Write(Picture(name), (int)width, (int)height, pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));
        return pixels;
    }

    private static void Near((byte R, byte G, byte B) expected, (byte R, byte G, byte B) actual, int tolerance, string where)
        => Assert.True(Math.Abs(expected.R - actual.R) <= tolerance && Math.Abs(expected.G - actual.G) <= tolerance && Math.Abs(expected.B - actual.B) <= tolerance,
            $"The ground at {where} is {actual}, not {expected}.");

    // The picture goes to the folder of IMAGE_TEST_OUTPUT, which the pipeline keeps, or to the folder of the test.
    private static string Picture(string name)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, name);
    }
}
