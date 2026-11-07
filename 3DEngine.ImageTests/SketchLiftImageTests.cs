using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Queries;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0085 (the redesign, slice e, TASK-0273): the active sketch of an extrude under its body, with a line from each point to
// its place, over the scene on the GPU, at the size of the window of the desktop, from the camera of a session. The picture
// is kept for the owner.
[Collection(DearImGui.Collection)]
public class SketchLiftImageTests
{
    private const uint Width = 1280;
    private const uint Height = 720;

    private static Guid Out(Engine.Contracts.CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    [VulkanImageFact]
    public async Task The_Sketch_Of_An_Extrude_Is_Under_Its_Body()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new Engine.Geometry.Manifold.ManifoldGeometryBackend());
        var sketch = Out(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(-20, -12), (20, -12), (20, 12), (-20, 12)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        for (var i = 0; i < points.Count; i++)
            await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] });
        var body = Out(await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 10 }), "bodyId");
        var info = (await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = sketch })).Result!;

        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        byte[] pixels;
        Vector2 corner;

        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
            using var panelRenderer = new UiRenderer(device, target.RenderPass);
            var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
            ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
            renderer.Update(scene, camera, (float)Width / Height);
            // An oblique view from above and farther away than the first frame, so that the sketch under the body shows.
            (camera.Target, camera.Distance, camera.YawDegrees, camera.PitchDegrees) = ((0, 0, 0), 150, 30, 30);
            renderer.Update(scene, camera, (float)Width / Height);

            var box = renderer.BoxOf(body) ?? throw new InvalidOperationException("No box of the body.");
            var lift = SketchLift.Under(sketch, SketchOverlay.PlaneOf(info)!, box.Min, box.Max) ?? throw new InvalidOperationException("No move.");
            var strokes = SketchOverlay.Of([lift.Apply(info)], camera, Width, Height, new SketchOverlay.Focus(sketch, [], null));
            strokes = strokes with { Lines = [.. strokes.Lines, .. lift.Links(info, camera, Width, Height)] };
            corner = SketchOverlay.Pixel(lift.Apply(info), camera, Width, Height, (20, -12)) ?? throw new InvalidOperationException("No pixel.");
            Assert.True(lift.Dz < -box.Max.Z / 2, $"The sketch at {lift.Dz} is not under the body.");

            var data = UiDrawData.Empty;
            for (var frame = 0; frame < 3; frame++)
                data = ui.Frame(1f / 60f, new Vector2(Width, Height), Vector2.One, renderer.LiveBodies, null, sketches: strokes);

            pixels = target.Render((commandBuffer, framebuffer, frameExtent) =>
                renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                    overlay => panelRenderer.Record(overlay, frameExtent, data, 0), null));
        }

        var picture = SavePicture(pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");

        // A pixel near the moved corner is drawn: the point of the sketch is there, under the body.
        var clear = Theme.Clear;
        var drawn = false;
        for (var dy = -2; dy <= 2 && !drawn; dy++)
        {
            for (var dx = -2; dx <= 2 && !drawn; dx++)
            {
                var index = ((((int)corner.Y + dy) * (int)Width) + (int)corner.X + dx) * 4;
                drawn = Math.Abs(pixels[index] - clear.R) > 2 || Math.Abs(pixels[index + 1] - clear.G) > 2 || Math.Abs(pixels[index + 2] - clear.B) > 2;
            }
        }

        Assert.True(drawn, $"The picture {picture}: nothing is drawn at the moved corner {corner}.");
    }

    private static string SavePicture(byte[] pixels)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "sketch-under-body.png");
        Png.Write(path, (int)Width, (int)Height, pixels);
        return path;
    }
}
