using System.Numerics;
using Engine.Api.Http;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Measuring;
using ThreeDEngine.Core.Models;
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

            // Register entry R-0051 (TASK-0179): the update gives the camera the far plane of the box of the shelf, so the
            // frame holds it whole; up to v0.179 this test set a far plane of 20000 itself.
            var shelf = renderer.Bounds!.Value;
            Assert.Equal(new Camera { Distance = camera.Distance }.FarPlaneFor(shelf.Min, shelf.Max), camera.FarPlane);
            Assert.True(camera.Distance + 1000 < camera.FarPlane, $"The eye at {camera.Distance} does not hold the shelf before the far plane {camera.FarPlane}.");
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
            var strokes = DimensionOverlay.Of(renderer.Union(ui.HiddenToo), box, camera, Width, Height, "mm");
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

    // Findings V78, V81 and V82 of the codebase review of 2026-11-05 (TASK-0181), on the renderer of the desktop: a box A
    // of 100 at the origin and a box B of 100 at x = 5000. The union of the boxes is kept from frame to frame (V81). The
    // camera looks from A to B, and a hide of A keeps B in front of the far plane in the next frame (V78). A preview of a
    // drag of B moves its box (V82).
    [VulkanImageFact]
    public async Task The_Far_Plane_Holds_A_Far_Body_After_A_Hide_And_The_Boxes_Follow_A_Drag()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var a = await host.Session.Apply(new CreateBoxCommand { SizeX = 100, SizeY = 100, SizeZ = 100 });
        Assert.True(a.Outputs.TryGet<Guid>("bodyId", out var bodyA));
        var created = await host.Session.Apply(new CreateBoxCommand { SizeX = 100, SizeY = 100, SizeZ = 100 });
        Assert.True(created.Outputs.TryGet<Guid>("bodyId", out var box));
        var moved = await host.Session.Apply(new TranslateCommand { BodyId = box, Dx = 5000, Dy = 0, Dz = 0 });
        Assert.True(moved.Outputs.TryGet<Guid>("bodyId", out var bodyB));
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];

        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, host);
            renderer.Update(scene, camera, (float)Width / Height);
            var unions = renderer.UnionsMade;
            renderer.Update(scene, camera, (float)Width / Height);
            renderer.Update(scene, camera, (float)Width / Height);
            Assert.Equal(unions, renderer.UnionsMade);

            camera.Target = (0, 0, 0);
            camera.Distance = 173.2;
            camera.YawDegrees = -90;
            camera.PitchDegrees = 0;
            await host.Session.Apply(new SetBodyVisibilityCommand { BodyId = bodyA, Visible = false });
            renderer.Update(scene, camera, (float)Width / Height);

            Assert.True(renderer.UnionsMade > unions, "A hide did not make the union again.");
            var eye = camera.Eye();
            var farSide = Math.Sqrt(Math.Pow(5000 - eye.X, 2) + eye.Y * eye.Y + eye.Z * eye.Z) + Math.Sqrt(3 * 100 * 100) / 2;
            Assert.True(farSide < camera.FarPlane, $"The far side of B is {farSide} from the eye, and the far plane is {camera.FarPlane}.");

            // A pan away from B with no change of a body: the next frame gives the far plane of the new eye.
            camera.Target = (-20000, 0, 0);
            renderer.Update(scene, camera, (float)Width / Height);
            eye = camera.Eye();
            farSide = Math.Sqrt(Math.Pow(5000 - eye.X, 2) + eye.Y * eye.Y + eye.Z * eye.Z) + Math.Sqrt(3 * 100 * 100) / 2;
            Assert.True(farSide < camera.FarPlane, $"After the pan the far side of B is {farSide} from the eye, and the far plane is {camera.FarPlane}.");

            var before = renderer.BoxOf(bodyB)!.Value;
            renderer.Preview(bodyB, new System.Numerics.Vector3(0, 0, 250));
            var dragged = renderer.BoxOf(bodyB)!.Value;
            Assert.Equal((before.Min.Z + 250, before.Max.Z + 250), (dragged.Min.Z, dragged.Max.Z));
            Assert.Equal(dragged, renderer.Union(hiddenToo: false));
            renderer.Preview(bodyB, System.Numerics.Vector3.Zero);
            Assert.Equal(before, renderer.BoxOf(bodyB));
        }

        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));
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
