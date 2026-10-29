using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// The desktop follows the rebuild of an extrude (ADR-0037 item 8, K0c, TASK-0109): the scene of
// the renderer takes the new mesh when the geometry key of a body changes, the panel gets the
// fault, and an undo gives back the old mesh. The test runs the renderer on a GPU with the
// validation layer, and reads the box of the scene and the live bodies.
public class RebuildImageTests
{
    private static async Task<Guid> Applied(EngineHost host, Command command, string output)
    {
        var result = await host.Session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(output, out var id) ? id : Guid.Empty;
    }

    [VulkanImageFact]
    public async Task The_Scene_Takes_The_Rebuilt_Mesh_And_The_Panel_The_Fault()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend(), "manifold", ManifoldGeometryBackend.NativeVersion);
        var sketch = await Applied(host, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(await Applied(host, new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }, "pointId"));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(await Applied(host, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }, "lineId"));
        await Applied(host, new ExtrudeCommand { SketchId = sketch, Distance = 5 }, "bodyId");

        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(320, 240));
            using var renderer = new SceneRenderer(device, target.RenderPass, host);
            // One entity in the scene at each step: a rebuild replaces the entity of the body.
            double MaxX()
            {
                renderer.Update(scene, camera, 4f / 3);
                Assert.Single(scene.Entities);
                return renderer.Bounds!.Value.Max.X;
            }

            Assert.Equal(10.0, MaxX());

            await Applied(host, new MoveSketchPointCommand { SketchId = sketch, PointId = points[2], X = 16, Y = 6 }, "pointId");
            Assert.Equal(16.0, MaxX());
            Assert.Null(Assert.Single(renderer.LiveBodies).Fault);

            await Applied(host, new DeleteSketchItemCommand { SketchId = sketch, ItemId = lines[3] }, "itemId");
            Assert.Equal(16.0, MaxX());
            Assert.Equal(DiagnosticCodes.SketchRebuildFailed, Assert.Single(renderer.LiveBodies).Fault?.Code);

            await Applied(host, new UndoCommand(), "commandId");
            Assert.Equal(16.0, MaxX());
            Assert.Null(Assert.Single(renderer.LiveBodies).Fault);

            await Applied(host, new UndoCommand(), "commandId");
            Assert.Equal(10.0, MaxX());
            target.Render((commandBuffer, framebuffer, extent) => renderer.Record(commandBuffer, framebuffer, extent, scene, camera));
        }

        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));
    }
}
