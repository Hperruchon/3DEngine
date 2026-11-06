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

// ADR-0082 (the redesign, slice c, TASK-0254): the values of a selected extrude on its edges, over the scene on the GPU, at
// the size of the window of the desktop. The picture is kept for the owner.
[Collection(DearImGui.Collection)]
public class EdgeValueImageTests
{
    private const uint Width = 1280;
    private const uint Height = 720;

    private static Guid Out(Engine.Contracts.CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    [VulkanImageFact]
    public async Task The_Values_Of_An_Extrude_Sit_On_Its_Edges()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new Engine.Geometry.Manifold.ManifoldGeometryBackend());
        var sketch = Out(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(-20, -12), (20, -12), (20, 12), (-20, 12)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(Out(await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }), "lineId"));
        await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 40 });
        await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[1], Value = 24 });
        var body = Out(await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 10 }), "bodyId");
        var faces = (await host.Session.Query<FacesInfo>(new GetFacesQuery { BodyId = body })).Result ?? throw new InvalidOperationException("No faces.");
        var info = (await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = sketch })).Result!;

        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        var values = EdgeValues.Of(body, 10, faces.Edges, [info], camera, Width, Height, "mm");
        Assert.Equal(3, values.Count);
        byte[] pixels;
        Vector4 chip;

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
                data = ui.Frame(1f / 60f, new Vector2(Width, Height), Vector2.One, renderer.LiveBodies, body, values: values);
            chip = ui.ItemRectangle(values[0].Key) ?? throw new InvalidOperationException("No chip of the distance.");

            pixels = target.Render((commandBuffer, framebuffer, frameExtent) =>
                renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                    overlay => panelRenderer.Record(overlay, frameExtent, data, 0), body));
        }

        var picture = SavePicture(pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");

        // A point inside the chip of the distance, near its left side, is not the clear: the chip is drawn there.
        var (column, row) = ((int)chip.X + 4, (int)((chip.Y + chip.W) / 2));
        var index = ((row * (int)Width) + column) * 4;
        var clear = Theme.Clear;
        var (r, g, b) = (pixels[index], pixels[index + 1], pixels[index + 2]);
        Assert.False(Math.Abs(r - clear.R) <= 2 && Math.Abs(g - clear.G) <= 2 && Math.Abs(b - clear.B) <= 2,
            $"The picture {picture}: the chip at {chip} has the colour of the clear.");
    }

    private static string SavePicture(byte[] pixels)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "values.png");
        Png.Write(path, (int)Width, (int)Height, pixels);
        return path;
    }
}
