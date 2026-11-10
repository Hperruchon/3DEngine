using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0101 (phase R17b, TASK-0315): a focus on one body is a state of the client. It draws one body, it writes nothing to the
// log, and it ends with one action. ADR-0101 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class BodyFocusTests
{
    private static readonly Guid A = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
    private static readonly Guid B = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000");
    private static readonly Vector2 Landscape = new(1440, 900);
    private static readonly DocumentLine Bracket = new("bracket.3de", null, Kind: Document.Part);
    private static readonly LiveBody Plate = new(A, "Box", "Plate", true);
    private static readonly LiveBody Post = new(B, "Box", "Post", true);

    private static IntentMachine Keys => new();

    private static Intent Escape => Assert.Single(Keys.Step([new KeySample(InputKey.Escape, true, KeyModifiers.None, 0)]));

    // Items 1 and 3: a focus and each exit of it add no entry to the log, and the visibility of each body does not change. The
    // test fails when the focus hides the other bodies with SetBodyVisibility.
    [Fact]
    public async Task A_Focus_And_Its_Exits_Add_No_Entry_To_The_Log()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);
        var before = Count(host);

        actions.Select(A);
        actions.ToggleFocus();
        Assert.Equal(A, actions.Focus);
        actions.ToggleFocus();
        Assert.Null(actions.Focus);
        actions.ToggleFocus();
        actions.EndFocus();
        actions.ToggleFocus();
        Assert.True(actions.Apply(Escape, Top, []));

        Assert.Null(actions.Focus);
        Assert.Equal(A, actions.Selection);
        Assert.Equal(before, Count(host));
        Assert.All(Live(host), body => Assert.True(body.Visible, $"The body {body.Id} is hidden."));
    }

    // Item 1: with no selected body, "Focus" starts no focus.
    [Fact]
    public async Task With_No_Selected_Body_Focus_Starts_No_Focus()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);
        actions.ToggleFocus();
        Assert.Null(actions.Focus);
    }

    // Items 5 and 6: Escape goes from the edge to the face, from the face to the body, then ends the focus and keeps the body,
    // and then goes to the document. The test fails when Escape at the level of the body clears the body first.
    [Fact]
    public async Task Escape_Ends_The_Focus_Before_It_Clears_The_Body()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);
        var edge = (First: Face("+x"), Second: Face("+z"));
        (DatumReference, DatumReference)? Near(double x, double y) => x > 100 ? edge : null;
        actions.Apply(new SelectIntent(0, 0), Top, [], Near);
        actions.ToggleFocus();
        actions.Apply(new SelectIntent(0, 0), Top, [], Near);
        actions.Apply(new SelectIntent(200, 0), Top, [], Near);
        Assert.Equal((A, Face("+z"), ((DatumReference, DatumReference)?)edge, (Guid?)A), (actions.Selection, actions.Face, actions.Edge, actions.Focus));

        actions.Apply(Escape, Top, []);
        Assert.Equal((Face("+z"), (Guid?)A), (actions.Face, actions.Focus));
        actions.Apply(Escape, Top, []);
        Assert.Equal(((DatumReference?)null, (Guid?)A, (Guid?)A), (actions.Face, actions.Selection, actions.Focus));
        actions.Apply(Escape, Top, []);
        Assert.Equal(((Guid?)A, (Guid?)null), (actions.Selection, actions.Focus));
        actions.Apply(Escape, Top, []);
        Assert.Null(actions.Selection);
    }

    // Item 7: inside a focus, a tap on no body goes up to the level of the body, and the focus stays. A tap on the segment of
    // the document ends the focus and the selection.
    [Fact]
    public async Task A_Tap_On_No_Body_Keeps_The_Focus()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);
        actions.Apply(new SelectIntent(0, 0), Top, []);
        actions.ToggleFocus();
        actions.Apply(new SelectIntent(0, 0), Top, []);
        Assert.Equal(Face("+z"), actions.Face);

        actions.Apply(new SelectIntent(0, 0), (_, _) => null, []);
        Assert.Equal(((Guid?)A, (DatumReference?)null, (Guid?)A), (actions.Selection, actions.Face, actions.Focus));

        actions.Up(PathLevel.Body);
        Assert.Equal(A, actions.Focus);
        actions.Up(PathLevel.Document);
        Assert.Equal(((Guid?)null, (Guid?)null), (actions.Selection, actions.Focus));
    }

    // Item 5: a body that leaves the session ends the focus: a body that an agent consumes, and a body that an agent hides. A
    // selection of another body ends it too.
    [Fact]
    public async Task A_Body_That_Leaves_The_Session_Ends_The_Focus()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);

        actions.Select(A);
        actions.ToggleFocus();
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new SetBodyVisibilityCommand { BodyId = A, Visible = false })).Status);
        actions.Forget(Live(host));
        Assert.Equal(((Guid?)null, (Guid?)null), (actions.Selection, actions.Focus));

        actions.Select(B);
        actions.ToggleFocus();
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new TranslateCommand { BodyId = B, Dx = 1, Dy = 0, Dz = 0 })).Status);
        actions.Forget(Live(host));
        Assert.Equal(((Guid?)null, (Guid?)null), (actions.Selection, actions.Focus));

        var moved = Live(host).Single(body => body.Visible).Id;
        actions.Select(moved);
        actions.ToggleFocus();
        actions.Select(A);
        Assert.Null(actions.Focus);
    }

    // Item 8: a move of the body of the focus from the pad makes a new body, and the focus goes to it.
    [Fact]
    public async Task A_Move_Of_The_Body_Of_The_Focus_Keeps_The_Focus_On_The_New_Body()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);
        actions.Select(A);
        actions.ToggleFocus();

        Assert.Null(actions.Move(A, 5, 0, 0));
        Assert.NotEqual(A, actions.Selection);
        Assert.Equal(actions.Selection, actions.Focus);
        actions.Forget(Live(host));
        Assert.NotNull(actions.Focus);
    }

    // Item 2: while a focus lasts, the renderer picks the body of the focus only, and the dimensions take its box only.
    [VulkanImageFact]
    public async Task The_Renderer_Picks_And_Measures_The_Body_Of_The_Focus_Only()
    {
        using var host = await TwoBoxes(apart: true);
        using var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true);
        using var target = new OffscreenTarget(device, new VkExtent2D(64, 64));
        using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
        var scene = SessionScene.Create();
        renderer.Update(scene, scene.Cameras[0], 1f);
        Assert.Equal(2, renderer.PickBodies.Count(body => body.Visible));

        renderer.Focus = A;
        Assert.Equal([A], renderer.PickBodies.Where(body => body.Visible).Select(body => body.Id));
        Assert.Equal(renderer.BoxOf(A), renderer.Union(hiddenToo: false));
        Assert.Equal(renderer.BoxOf(A), renderer.Union(hiddenToo: true));

        renderer.Focus = null;
        Assert.Equal(2, renderer.PickBodies.Count(body => body.Visible));
        Assert.NotEqual(renderer.BoxOf(A), renderer.Union(hiddenToo: false));
    }

    // Item 3: "Focus" is a segment of the ring of a selected body, after Hide, on a landscape and on a portrait display, and
    // a tap on it gives the action of the focus; while a focus lasts it is still there.
    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public void Focus_Is_A_Segment_Of_The_Ring_After_Hide(int width, int height)
    {
        var display = new Vector2(width, height);
        var centre = new Vector2(width / 2f, height / 2f);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        void Step(params InputSample[] frame)
        {
            ui.Route(frame);
            ui.Frame(1f / 60f, display, Vector2.One, [Plate, Post], A, Bracket, ringCentre: centre);
        }

        foreach (var focus in new Guid?[] { null, A })
        {
            ui.Focus = focus;
            Step();
            Step();
            var hide = Rectangle(ui, DesktopUi.RingItem(DesktopUi.HideLabel));
            var at = Rectangle(ui, DesktopUi.RingItem(DesktopUi.FocusLabel));
            Assert.NotEqual(hide, at);
            ui.TakeActions();
            var (x, y) = ((at.X + at.Z) / 2, (at.Y + at.W) / 2);
            Step(Mouse(x, y, PointerPhase.Move, 0));
            Step(Mouse(x, y, PointerPhase.Down, 10));
            Step(Mouse(x, y, PointerPhase.Up, 20));
            Assert.Equal([new FocusAction()], ui.TakeActions());
        }
    }

    // Item 3: "Focus" is a command of the command line for a selected body only.
    [Fact]
    public void Focus_Is_A_Command_For_A_Selected_Body()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Assert.DoesNotContain(DesktopUi.FocusLabel, ui.Commands([Plate], null).Select(command => command.Name));
        var focus = Assert.Single(ui.Commands([Plate], A), command => command.Name == DesktopUi.FocusLabel);
        ui.TakeActions();
        focus.Run();
        Assert.Equal([new FocusAction()], ui.TakeActions());
    }

    // Item 4: while a focus lasts, the path line shows "Focus" before the name of the body and a close mark of 44 by 44 pixels
    // at its end; a tap on the close mark gives the end of the focus. With no focus neither one shows.
    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public void The_Path_Line_Shows_The_Focus_And_A_Close_Mark(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        void Step(params InputSample[] frame)
        {
            ui.Route(frame);
            ui.Frame(1f / 60f, display, Vector2.One, [Plate, Post], A, Bracket);
        }

        Step();
        Step();
        Assert.Null(ui.ItemRectangle(DesktopUi.FocusMarkItem));
        Assert.Null(ui.ItemRectangle(DesktopUi.FocusCloseItem));

        ui.Focus = A;
        Step();
        Step();
        var mark = Rectangle(ui, DesktopUi.FocusMarkItem);
        var body = Rectangle(ui, DesktopUi.PathSegmentItem(PathLevel.Body));
        var close = Rectangle(ui, DesktopUi.FocusCloseItem);
        Assert.True(mark.Z <= body.X + 0.5f, $"\"Focus\" at {mark} is not before the body at {body}.");
        Assert.True(close.X >= body.Z - 0.5f, $"The close mark at {close} is not after the body at {body}.");
        Assert.True(close.Z - close.X >= DesktopUi.MinimumControlHeight - 0.5f && close.W - close.Y >= DesktopUi.MinimumControlHeight - 0.5f,
            $"The close mark at {close} is smaller than 44 by 44 pixels.");
        Assert.True(close.Z <= display.X - 16f + 0.5f, $"The close mark at {close} goes past the right margin.");

        ui.TakeActions();
        var (x, y) = ((close.X + close.Z) / 2, (close.Y + close.W) / 2);
        Step(Mouse(x, y, PointerPhase.Move, 0));
        Step(Mouse(x, y, PointerPhase.Down, 10));
        Step(Mouse(x, y, PointerPhase.Up, 20));
        Assert.Equal([new EndFocusAction()], ui.TakeActions());
    }

    // Items 2 and 4: the picture for the owner at 1440 by 900. With the focus on the plate, the post is not drawn, so fewer
    // pixels differ from the clear than with no focus; the path line shows "Focus" and the close mark.
    [VulkanImageFact]
    public async Task The_Picture_Shows_One_Body_And_The_Focus_In_The_Path_Line()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = await TwoBoxes(apart: true);
        var drawn = new Dictionary<bool, int>();
        foreach (var focused in new[] { false, true })
        {
            const uint width = 1440, height = 900;
            using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
            var scene = SessionScene.Create();
            var camera = scene.Cameras[0];
            byte[] pixels;
            using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
            {
                using var target = new OffscreenTarget(device, new VkExtent2D(width, height));
                using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
                renderer.Update(scene, camera, (float)width / height);
                renderer.Focus = focused ? A : null;
                var bare = target.Render((commandBuffer, framebuffer, frameExtent) => renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera));
                drawn[focused] = Drawn(bare);

                using var panelRenderer = new UiRenderer(device, target.RenderPass);
                var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
                ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
                ui.Focus = renderer.Focus;
                var data = UiDrawData.Empty;
                for (var frame = 0; frame < 3; frame++)
                {
                    ui.Route([]);
                    data = ui.Frame(1f / 60f, new Vector2(width, height), Vector2.One, renderer.LiveBodies, A, Bracket, ringCentre: new Vector2(800, 410), ringBody: 70f);
                }

                if (focused)
                {
                    Assert.NotNull(ui.ItemRectangle(DesktopUi.FocusMarkItem));
                    Assert.NotNull(ui.ItemRectangle(DesktopUi.FocusCloseItem));
                }

                pixels = target.Render((commandBuffer, framebuffer, frameExtent) =>
                    renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                        overlay => panelRenderer.Record(overlay, frameExtent, data, 0), A));
            }

            SavePicture(pixels, width, height, $"focus-{(focused ? "plate" : "none")}-{width}x{height}.png");
        }

        Assert.True(drawn[true] > 0, "The focus draws no body.");
        Assert.True(drawn[true] < drawn[false], $"The focus draws {drawn[true]} pixels of bodies, and no focus {drawn[false]}.");
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");
    }

    // The count of pixels that differ from the clear colour.
    private static int Drawn(byte[] rgba)
    {
        var (r, g, b) = ((byte)Math.Round(SceneRenderer.Background.R * 255), (byte)Math.Round(SceneRenderer.Background.G * 255), (byte)Math.Round(SceneRenderer.Background.B * 255));
        var count = 0;
        for (var i = 0; i + 3 < rgba.Length; i += 4)
        {
            if (Math.Abs(rgba[i] - r) > 2 || Math.Abs(rgba[i + 1] - g) > 2 || Math.Abs(rgba[i + 2] - b) > 2)
                count++;
        }

        return count;
    }

    private static Vector4 Rectangle(DesktopUi ui, string item)
        => ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");

    private static PointerSample Mouse(double x, double y, PointerPhase phase, ulong ms)
        => new(DeviceKind.Mouse, 1, 0, x, y, 1, phase == PointerPhase.Move ? PointerButtons.None : PointerButtons.Left, KeyModifiers.None, phase, ms * 1_000_000);

    // A hit on the top face of the box A, which is 10 units high around its centre.
    private static PickHit? Top(double x, double y) => new PickHit(A, 1) { Point = (0, 0, 5), Normal = (0, 0, 1) };

    private static DatumReference Face(string element) => new(A, element, null);

    private static int Count(EngineHost host) => host.Session.Read((document, _) => document.Log.Count).GetAwaiter().GetResult();

    private static IReadOnlyList<LiveBody> Live(EngineHost host)
        => host.Session.Read((document, _) => document.Bodies.Select(body => new LiveBody(body.Handle.Id, body.Kind, body.Name, body.Visible)).ToList()).GetAwaiter().GetResult();

    // Two boxes, A and B. Apart, B is 30 units along x, so the two bodies show in two places of the picture.
    private static async Task<EngineHost> TwoBoxes(bool apart = false)
    {
        var host = new EngineHost(new ManifoldGeometryBackend());
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new CreateBoxCommand { CommandId = A, SizeX = 10, SizeY = 10, SizeZ = 10 })).Status);
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new CreateBoxCommand { CommandId = B, SizeX = 6, SizeY = 6, SizeZ = 20 })).Status);
        if (apart)
        {
            var moved = await host.Session.Apply(new TranslateCommand { BodyId = B, Dx = 30, Dy = 0, Dz = 0 });
            Assert.Equal(CommandStatus.Applied, moved.Status);
        }

        return host;
    }

    private static void SavePicture(byte[] pixels, uint width, uint height, string name)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        Png.Write(Path.Combine(folder, name), (int)width, (int)height, pixels);
    }
}
