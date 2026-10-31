using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Gizmo;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// The overlay pipeline of the gizmo (ADR-0038 item 9, TASK-0113): the renderer of the desktop draws
// a box, and the overlay draws the gizmo of its centre in front of it, with no depth test. The
// middle of the arrow X is red and the middle of the arrow Y is green, also where the body is under
// them. The validation layer checks the synchronization, and each message fails the test.
public class GizmoImageTests
{
    private const uint Width = 320;
    private const uint Height = 240;

    [VulkanImageFact]
    public async Task The_Overlay_Draws_The_Gizmo_In_Front_Of_The_Body()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new CreateBoxCommand { SizeX = 4, SizeY = 4, SizeZ = 4 })).Status);
        var box = await host.Session.Read((document, _) => document.Bodies.Single().Handle.Id);

        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        byte[] plain;
        byte[] drawn;
        GizmoLayout layout;
        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, host);
            using var overlay = new OverlayRenderer(device, target.RenderPass);
            renderer.Update(scene, camera, (float)Width / Height);
            layout = TranslateGizmo.Layout(camera, Width, Height, renderer.Pivot(box)!.Value)!;
            var draws = TranslateGizmo.Shapes(layout, null).Select(shape => new OverlayDraw(shape.Triangles, shape.Colour)).ToArray();

            plain = target.Render((commandBuffer, framebuffer, extent) => renderer.Record(commandBuffer, framebuffer, extent, scene, camera));
            drawn = target.Render((commandBuffer, framebuffer, extent) => renderer.Record(commandBuffer, framebuffer, extent, scene, camera,
                commands => overlay.Record(commands, extent, new Vector2(Width, Height), draws, 0)));
        }

        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));

        var x = Pixel(layout.Origin + ((layout.Ends[GizmoHandle.X] - layout.Origin) * 0.6f));
        var y = Pixel(layout.Origin + ((layout.Ends[GizmoHandle.Y] - layout.Origin) * 0.6f));
        var (rx, gx, bx) = Colour(drawn, x);
        var (ry, gy, by) = Colour(drawn, y);

        Assert.True(Colour(plain, x) != (rx, gx, bx), $"The pixel {x} has the colour {(rx, gx, bx)} with and with no gizmo.");
        Assert.True(rx > 180 && gx < 90 && bx < 90, $"The middle of the arrow X at {x} has the colour {(rx, gx, bx)}, not red.");
        Assert.True(gy > 150 && ry < 90 && by < 110, $"The middle of the arrow Y at {y} has the colour {(ry, gy, by)}, not green.");
    }

    private static (int Column, int Row) Pixel(Vector2 point) => ((int)MathF.Round(point.X), (int)MathF.Round(point.Y));

    private static (int R, int G, int B) Colour(byte[] pixels, (int Column, int Row) at)
    {
        var offset = (at.Row * (int)Width + at.Column) * 4;
        return (pixels[offset], pixels[offset + 1], pixels[offset + 2]);
    }
}
