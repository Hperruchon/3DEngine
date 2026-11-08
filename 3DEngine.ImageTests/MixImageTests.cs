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

// ADR-0093 (the redesign, slice j, TASK-0304): the scene of the render "mix" of the designer brief at 1440 x 900, the size
// at which the owner judges each slice of the redesign: a bracket of 60 x 40 x 10, selected, framed as the button Frame
// does, with its ring and its values. The picture is kept for the owner, beside the render.
[Collection(DearImGui.Collection)]
public class MixImageTests
{
    private const uint Width = 1440;
    private const uint Height = 900;

    private static Guid Out(Engine.Contracts.CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    [VulkanImageFact]
    public async Task The_Selected_Bracket_Of_The_Render_Draws_At_1440_By_900()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new Engine.Geometry.Manifold.ManifoldGeometryBackend());
        var sketch = Out(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(-30, -20), (30, -20), (30, 20), (-30, 20)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(Out(await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }), "lineId"));
        await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 60 });
        await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[1], Value = 40 });
        var body = Out(await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 10 }), "bodyId");
        await host.Session.Apply(new RenameBodyCommand { BodyId = body, NewName = "Bracket" });
        var faces = (await host.Session.Query<FacesInfo>(new GetFacesQuery { BodyId = body })).Result ?? throw new InvalidOperationException("No faces.");
        var info = (await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = sketch })).Result!;

        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        var display = new Vector2(Width, Height);
        byte[] pixels;

        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
            using var panelRenderer = new UiRenderer(device, target.RenderPass);
            var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
            ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
            renderer.Update(scene, camera, (float)Width / Height);
            var bounds = renderer.Bounds ?? throw new InvalidOperationException("No bounds.");
            camera.Frame(bounds.Min, bounds.Max, (float)Width / Height);
            renderer.Update(scene, camera, (float)Width / Height);

            var extent = renderer.BoxOf(body) ?? throw new InvalidOperationException("The body has no mesh.");
            var middle = ((extent.Min.X + extent.Max.X) / 2, (extent.Min.Y + extent.Max.Y) / 2, (extent.Min.Z + extent.Max.Z) / 2);
            var centre = SketchOverlay.Segment(camera, Width, Height, middle, middle)?.A ?? throw new InvalidOperationException("The body is behind the eye.");
            var diagonal = SketchOverlay.Segment(camera, Width, Height, extent.Min, extent.Max) ?? throw new InvalidOperationException("The body is behind the eye.");
            var size = Vector2.Distance(diagonal.A, diagonal.B) / 2f;
            var values = EdgeValues.Of(body, 10, faces.Edges, [info], camera, Width, Height, "mm");
            var data = UiDrawData.Empty;
            for (var frame = 0; frame < 3; frame++)
                data = ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies, body, new DocumentLine("bracket", null),
                    ringCentre: centre, ringBody: size, values: values);

            pixels = target.Render((commandBuffer, framebuffer, frameExtent) =>
                renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                    overlay => panelRenderer.Record(overlay, frameExtent, data, 0), body));
        }

        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        var picture = Path.Combine(folder, "mix-selected.png");
        Png.Write(picture, (int)Width, (int)Height, pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");
        Assert.True(pixels.Length == Width * Height * 4, $"The picture {picture} has {pixels.Length} bytes.");
    }
}
