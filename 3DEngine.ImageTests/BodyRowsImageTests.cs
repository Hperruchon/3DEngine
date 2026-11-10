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

// ADR-0084 (the redesign, slice d, TASK-0267): the rows of a selected extrude, with its values on its edges, over the
// scene on the GPU, at the size of the window of the desktop. The picture is kept for the owner.
[Collection(DearImGui.Collection)]
public class BodyRowsImageTests
{
    private const uint Width = 1280;
    private const uint Height = 720;

    private static Guid Out(Engine.Contracts.CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    [VulkanImageFact]
    public async Task The_Rows_Of_An_Extrude_Are_At_The_Upper_Right()
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
        await host.Session.Apply(new RenameBodyCommand { BodyId = body, NewName = "Bracket" });
        var faces = (await host.Session.Query<FacesInfo>(new GetFacesQuery { BodyId = body })).Result ?? throw new InvalidOperationException("No faces.");
        var info = (await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = sketch })).Result!;

        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        var values = EdgeValues.Of(body, 10, faces.Edges, [info], camera, Width, Height, "mm");
        Assert.Equal(["Distance", "Width", "Depth"], values.Select(value => value.Label));
        byte[] pixels;
        Vector4 title;

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
            title = ui.ItemRectangle(DesktopUi.RowsTitleItem) ?? throw new InvalidOperationException("No rows.");

            pixels = target.Render((commandBuffer, framebuffer, frameExtent) =>
                renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                    overlay => panelRenderer.Record(overlay, frameExtent, data, 0), body));
        }

        var picture = SavePicture(pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");

        // The rows have no ground, so the text of the name is the only colour that is not the clear in the first row.
        var clear = Theme.Clear;
        var drawn = 0;
        for (var y = (int)title.Y; y < (int)title.W; y++)
        {
            for (var x = (int)title.X; x < (int)title.Z; x++)
            {
                var index = ((y * (int)Width) + x) * 4;
                if (Math.Abs(pixels[index] - clear.R) > 2 || Math.Abs(pixels[index + 1] - clear.G) > 2 || Math.Abs(pixels[index + 2] - clear.B) > 2)
                    drawn++;
            }
        }

        Assert.True(drawn > 50, $"The picture {picture}: the first row at {title} has {drawn} pixels that are not the clear.");
    }

    private static string SavePicture(byte[] pixels)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "rows.png");
        Png.Write(path, (int)Width, (int)Height, pixels);
        return path;
    }
}
