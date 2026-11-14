using System.Numerics;
using Engine.Api.Http;
using Engine.Core.Commands;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0127 (phase R18g, TASK-0354): a picture of the fixed controls of a landscape display: Undo, Redo and "Undo: C3 = 75"
// on the row of the anchors, the log and the agent in the status line, and "mm · saved" in the top bar. The text of the step
// comes from the history of a real session, as the host keeps it.
[Collection(DearImGui.Collection)]
public class FixedStatusImageTests
{
    private const uint Width = 1280;
    private const uint Height = 720;

    [VulkanImageFact]
    public async Task The_Row_Of_The_Anchors_Shows_The_Step_Of_Undo()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new Engine.Geometry.Manifold.ManifoldGeometryBackend());
        var steps = new StepTexts();
        using var subscription = await host.Session.Read((document, _) =>
        {
            steps.Seed([.. document.Log.Select(entry => entry.Command.Name)]);
            return host.Subscribe(steps.Take);
        });
        await host.Session.Apply(new CreateBoxCommand { SizeX = 40, SizeY = 24, SizeZ = 10 });
        var second = Guid.NewGuid();
        steps.Expect(second, "C3 = 75");
        await host.Session.Apply(new CreateBoxCommand { CommandId = second, SizeX = 10, SizeY = 10, SizeZ = 30 });
        Assert.Equal((2, "C3 = 75"), (steps.LogCount, steps.Undo));

        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        ui.ShowStatus(new SessionStatus(steps.LogCount, true, steps.Undo, true));
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        byte[] pixels;
        Vector4 step;

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
                data = ui.Frame(1f / 60f, new Vector2(Width, Height), Vector2.One, renderer.LiveBodies);
            step = ui.ItemRectangle(DesktopUi.UndoStepItem) ?? throw new InvalidOperationException("No step of Undo.");
            Assert.Equal("log 2 · all correct · agent connected · http://127.0.0.1:5180", ui.StatusShown);

            pixels = target.Render((commandBuffer, framebuffer, frameExtent) =>
                renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                    overlay => panelRenderer.Record(overlay, frameExtent, data, 0), null));
        }

        var picture = SavePicture(pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");

        // The row has no ground, so the text of the step is the only colour that is not the clear in its rectangle.
        var clear = Theme.Clear;
        var drawn = 0;
        for (var y = (int)step.Y; y < (int)step.W; y++)
        {
            for (var x = (int)step.X; x < (int)step.Z; x++)
            {
                var index = ((y * (int)Width) + x) * 4;
                if (Math.Abs(pixels[index] - clear.R) > 2 || Math.Abs(pixels[index + 1] - clear.G) > 2 || Math.Abs(pixels[index + 2] - clear.B) > 2)
                    drawn++;
            }
        }

        Assert.True(drawn > 50, $"The picture {picture}: the step at {step} has {drawn} pixels that are not the clear.");
    }

    private static string SavePicture(byte[] pixels)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "fixed-status.png");
        Png.Write(path, (int)Width, (int)Height, pixels);
        return path;
    }
}
