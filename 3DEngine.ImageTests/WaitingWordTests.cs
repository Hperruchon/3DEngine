using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// TASK-0461 (finding V235, recorded by the answer "Q8 A" of the owner of 2026-11-24): with a long folder of the file on a
// landscape display, the top bar had no room for "AGENT IS WAITING", so the word went under the line of the title, where the
// rows of the document lay on it (the picture r25-live.png of the owner, 2026-11-24). Now the folder of the title is cut at its
// start by the room of the word, so the word keeps its place left of the command line (ADR-0108 item 6). On a display too narrow
// for that, the word goes under the top bar and the panel starts under the word, as on a portrait display (ADR-0113 item 3).
[Collection(DearImGui.Collection)]
public sealed class WaitingWordTests
{
    private static readonly Guid Plate = Guid.Parse("73000000-0000-0000-0000-000000000002");

    // The folder of the picture of the owner: a scratchpad folder of a session, which is longer than the top bar.
    private const string LongFolder = @"C:\Users\owner\AppData\Local\Temp\claude\54c23ded98220c52\d2a83d83-df56-41f1-a909-7f488589460c\scratchpad\r25\docs";

    private static readonly DocumentLine Line = new("r25.3de", null, Files: true, Folder: LongFolder);

    private static ProposalCard Card() => new(Guid.NewGuid(), "Move the plate against the post", ["Translate 2834fc6d 70 0 10", "RenameBody 3789a566 Plate"]);

    private static bool Meets(Vector4 a, Vector4 b) => a.X < b.Z && b.X < a.Z && a.Y < b.W && b.Y < a.W;

    private static UiDrawData Frames(DesktopUi ui, Vector2 display, IReadOnlyList<LiveBody> bodies)
    {
        var data = UiDrawData.Empty;
        for (var frame = 0; frame < 3; frame++)
        {
            ui.Route([]);
            data = ui.Frame(1f / 60f, display, Vector2.One, bodies, null, Line, proposals: [Card()]);
        }

        return data;
    }

    // The word lies on no text of the top bar and on no panel, and stays inside the display.
    private static void Clear(DesktopUi ui, Vector2 display)
    {
        var word = ui.ItemRectangle(DesktopUi.ProposalWaitingItem) ?? throw new InvalidOperationException("The word is not shown.");
        Assert.True(word.X >= 0f && word.Z <= display.X && word.Y >= 0f && word.W <= display.Y, $"The word {word} is out of the display {display}.");
        foreach (var item in new[] { DesktopUi.TitleItem, DesktopUi.TitleFolderItem, DesktopUi.PathItem, DesktopUi.CommandItem })
        {
            if (ui.ItemRectangle(item) is { } box)
                Assert.False(Meets(word, box), $"The word {word} lies on the item {item} {box}.");
        }

        foreach (var panel in ui.PanelRectangles)
            Assert.False(Meets(word, panel), $"The word {word} lies on the panel {panel}.");
    }

    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1024, 768)]
    [InlineData(800, 600)]
    [InlineData(640, 360)]
    public void The_Word_Of_A_Waiting_Proposal_Lies_On_No_Text_And_No_Panel_With_A_Long_Folder(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5199", Document.Millimetre);
        Frames(ui, display, [new LiveBody(Plate, "Box", "Plate", true)]);

        Clear(ui, display);
        // Where the top bar has the room of the word and of a cut file name, the word keeps its place left of the command line.
        if (width >= 1024)
        {
            var word = ui.ItemRectangle(DesktopUi.ProposalWaitingItem)!.Value;
            var command = ui.ItemRectangle(DesktopUi.CommandItem) ?? throw new InvalidOperationException("No command line.");
            Assert.True(Math.Abs(command.X - 12f - word.Z) < 1f, $"The word {word} does not end 12 pixels left of the command line {command}.");
            var folder = ui.ItemRectangle(DesktopUi.TitleFolderItem) ?? throw new InvalidOperationException("The folder is not shown.");
            Assert.True(folder.Z + 16f <= word.X, $"The folder {folder} ends less than 16 pixels left of the word {word}.");
        }
    }

    // The picture for the owner at 1280 by 720, as the window of r25-live.png. The validation layer gives no message.
    [VulkanImageFact]
    public async Task The_Picture_Shows_The_Word_Clear_Of_A_Long_Title()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        const uint width = 1280, height = 720;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new CreateBoxCommand { CommandId = Plate, SizeX = 40, SizeY = 24, SizeZ = 6 })).Status);
        using var ui = new DesktopUi("http://127.0.0.1:5199", Document.Millimetre);
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        var display = new Vector2(width, height);
        byte[] pixels;
        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(width, height));
            using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
            using var panelRenderer = new UiRenderer(device, target.RenderPass);
            var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
            ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
            renderer.Update(scene, camera, (float)width / height);
            var data = Frames(ui, display, renderer.LiveBodies);
            Clear(ui, display);
            pixels = target.Render((commandBuffer, framebuffer, extent) =>
                renderer.Record(commandBuffer, framebuffer, extent, scene, camera, overlay => panelRenderer.Record(overlay, extent, data, 0), null));
        }

        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        Png.Write(Path.Combine(folder, $"waiting-word-long-title-{width}x{height}.png"), (int)width, (int)height, pixels);

        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));
    }
}
