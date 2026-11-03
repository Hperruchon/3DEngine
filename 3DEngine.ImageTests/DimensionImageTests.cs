using System.Numerics;
using Engine.Api.Http;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Measuring;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// The image test of the dimension overlay (ADR-0056, TASK-0178): an L-shaped shelf of boxes in millimetres, with Y up,
// drawn with the dimensions on and one board selected. The picture holds pixels of the orange of the overall dimensions
// and of the white of the selected board outside the panel, and the validation layer gives no message.
[Collection(DearImGui.Collection)]
public class DimensionImageTests
{
    private const uint Width = 1280;
    private const uint Height = 720;

    // The boards of the shelf: the size and the place of the lowest corner of each one.
    private static readonly (double X, double Y, double Z, double Dx, double Dy, double Dz)[] Boards =
    [
        (1200, 1900, 18, 0, 0, 0),
        (1200, 18, 370, 0, 80, 18),
        (1200, 18, 370, 0, 280, 18),
        (1200, 18, 370, 0, 500, 18),
        (18, 1900, 650, -18, 0, 0),
    ];

    [VulkanImageFact]
    public async Task The_Dimensions_Draw_Over_The_Shelf()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var boards = new List<Guid>();
        foreach (var (x, y, z, dx, dy, dz) in Boards)
        {
            var box = await host.Session.Apply(new CreateBoxCommand { SizeX = x, SizeY = y, SizeZ = z });
            Assert.True(box.Outputs.TryGet<Guid>("bodyId", out var body));
            var moved = await host.Session.Apply(new TranslateCommand { BodyId = body, Dx = dx + (x / 2), Dy = dy + (y / 2), Dz = dz + (z / 2) });
            Assert.True(moved.Outputs.TryGet<Guid>("bodyId", out var placed));
            boards.Add(placed);
        }

        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        camera.FarPlane = 20000f;
        var display = new Vector2(Width, Height);
        var selected = boards[2];
        byte[] pixels;
        Vector4 panel;
        string size;

        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, host);
            using var panelRenderer = new UiRenderer(device, target.RenderPass);
            var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
            ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
            renderer.Update(scene, camera, (float)Width / Height);
            camera.YawDegrees = 35;
            camera.PitchDegrees = 20;
            camera.Distance *= 1.3;

            for (var frame = 0; frame < 2; frame++)
                ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies, selected);
            var toggle = ui.ItemRectangle(DesktopUi.DimensionsLabel)!.Value;
            var (tx, ty) = ((toggle.X + toggle.Z) / 2, (toggle.Y + toggle.W) / 2);
            // A tap is a frame with the press, a frame with the release, and two frames with no sample (DesktopUiTests).
            foreach (var phase in new[] { PointerPhase.Down, PointerPhase.Up })
            {
                ui.Route([new PointerSample(DeviceKind.Touch, 1, 3, tx, ty, 1, PointerButtons.None, KeyModifiers.None, phase, 0)]);
                ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies, selected);
            }
            for (var frame = 0; frame < 2; frame++)
                ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies, selected);

            Assert.True(ui.ShowDimensions);
            var box = renderer.BoxOf(selected)!.Value;
            size = DimensionOverlay.SizeText(box, null, "mm");
            var strokes = DimensionOverlay.Of(Dimensions.Union(renderer.Boxes(ui.HiddenToo)), box, camera, Width, Height, "mm");
            var data = UiDrawData.Empty;
            for (var frame = 0; frame < 3; frame++)
                data = ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies, selected, dimensions: strokes, size: size);
            panel = ui.PanelRectangles[0];

            pixels = target.Render((commandBuffer, framebuffer, extent) =>
                renderer.Record(commandBuffer, framebuffer, extent, scene, camera,
                    overlay => panelRenderer.Record(overlay, extent, data, 0), selected));
        }

        var picture = SavePicture(pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));
        Assert.Equal("Size 1200 x 18 x 370 mm\nThickness 18 mm", size);

        // DimensionColour is (255, 160, 64) and the selection is white; the bodies are grey and the clear is dark.
        int orange = 0, white = 0;
        for (var row = 0; row < (int)Height; row++)
        {
            for (var column = 0; column < (int)Width; column++)
            {
                if (column >= panel.X && column <= panel.Z && row >= panel.Y && row <= panel.W)
                    continue;
                var (r, g, b) = Pixel(pixels, column, row);
                if (r > 220 && g > 130 && g < 190 && b < 100)
                    orange++;
                if (r > 235 && g > 235 && b > 235)
                    white++;
            }
        }

        Assert.True(orange > 300, $"The picture {picture}: {orange} pixels are orange; the overall dimensions are absent.");
        Assert.True(white > 100, $"The picture {picture}: {white} pixels are white; the dimensions of the board are absent.");
    }

    private static (int R, int G, int B) Pixel(byte[] pixels, int column, int row)
    {
        var index = (row * (int)Width + column) * 4;
        return (pixels[index], pixels[index + 1], pixels[index + 2]);
    }

    private static string SavePicture(byte[] pixels)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "dimensions.png");
        Png.Write(path, (int)Width, (int)Height, pixels);
        return path;
    }
}
