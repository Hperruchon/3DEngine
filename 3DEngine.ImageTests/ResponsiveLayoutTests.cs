using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0113 (phase R19, TASK-0336): at each size of the list and in each state, each control stays in the display, no
// control goes past the right edge of its panel, no window covers a control of another window or a tool of the ring, and
// no two fixed windows overlap. A control that its panel scrolls out of view is not a fault. The sheet of a proposal may
// cover the panel (ADR-0092 item 7), but not a tool of the ring (ADR-0111 item 4) when the sheet fits above the ring.
// ADR-0113 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class ResponsiveLayoutTests
{
    private static readonly Guid A = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
    private static readonly Guid B = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000");
    private static readonly LiveBody Plate = new(A, "Extrude", "Plate", true);
    private static readonly LiveBody Rib = new(B, "Extrude", "Rib", true, new BodyFault("E-KERNEL-EXTRUDE-OPEN", "The profile is not closed."));
    private static readonly DocumentLine Shelf = new("entry-hall-shelf.3de", null, Kind: Document.Part);

    // The states of the probe of TASK-0336. ADR-0172 item 5 (TASK-0418): and the home screen, an active sketch, an open pad,
    // the history arc, the button "Images..." in the panel, and the open images with a selected body. ADR-0174 item 7
    // (TASK-0419): the state Images is the ring of the part with "Images", and the open structure view and the open file menu
    // are two more states. The state of the open structure view has a centre of the part from the host, the other states with
    // no selection have none. The open net of the views is a state too, because it holds "Dimensions" now. ADR-0175 item 5
    // (TASK-0420): the state StructureOpen has the density of the device ("Compact" on each runner), and StructureForTouch is
    // the open structure view with the density "For touch".
    public enum State
    {
        Nothing,
        Body,
        Face,
        Proposal,
        Home,
        Sketch,
        Pad,
        History,
        Images,
        ImagesOpen,
        StructureOpen,
        StructureForTouch,
        FileMenu,
        ViewsOpen,
    }

    // The sizes of ADR-0113 item 1: the phones in portrait and in landscape, a tablet in both, and two windows.
    // ADR-0163 item 2 (TASK-0404): and the smallest window of the desktop.
    public static TheoryData<int, int> Sizes => new()
    {
        { 360, 640 }, { 360, 780 }, { 384, 824 }, { 390, 844 }, { 393, 873 }, { 412, 915 },
        { 640, 360 }, { 844, 390 }, { 915, 412 },
        { 768, 1024 }, { 1024, 768 }, { 1280, 720 }, { 1440, 900 },
        { DesktopUi.MinimumWindowWidth, DesktopUi.MinimumWindowHeight },
    };

    [Theory]
    [MemberData(nameof(Sizes))]
    public void Each_Control_Stays_In_The_Display_And_Free_For_A_Tap(int width, int height)
    {
        var faults = new List<string>();
        foreach (var state in Enum.GetValues<State>())
            faults.AddRange(Faults(width, height, state).Select(fault => $"{width}x{height} {state}: {fault}"));
        Assert.True(faults.Count == 0, string.Join(Environment.NewLine, faults));
    }

    // ADR-0163 item 4 (TASK-0404): a person resizes the window of the desktop to each size from its minimum. The rule of
    // ADR-0113 item 1 holds at each step of 8 pixels on three lines of a resize: the smallest height from the smallest width
    // to 2560, the smallest width up to a height of 1440, and the width of a maximized window of 1920 up to 1440.
    [Fact]
    public void Each_Size_Of_A_Resize_From_The_Minimum_Keeps_Each_Control_Free()
    {
        const int Step = 8;
        int minimumWidth = DesktopUi.MinimumWindowWidth, minimumHeight = DesktopUi.MinimumWindowHeight;
        var sizes = new List<(int Width, int Height)>();
        for (var width = minimumWidth; width <= 2560; width += Step)
            sizes.Add((width, minimumHeight));
        for (var height = minimumHeight + Step; height <= 1440; height += Step)
        {
            sizes.Add((minimumWidth, height));
            sizes.Add((1920, height));
        }

        var faults = new List<string>();
        foreach (var (width, height) in sizes)
        {
            foreach (var state in Enum.GetValues<State>())
                faults.AddRange(Faults(width, height, state).Take(1).Select(fault => $"{width}x{height} {state}: {fault}"));
        }
        Assert.True(faults.Count == 0, $"{faults.Count} faults:{Environment.NewLine}{string.Join(Environment.NewLine, faults.Take(20))}");
    }

    // ADR-0172 item 4 (TASK-0418): a tap on "Images..." opens the images in the place of the panel, under the top bar at the
    // left, and a tap on Close gives the panel back. Until TASK-0418 the images were a window that the person moved, which
    // started folded at x 492 (ADR-0163 item 5). ADR-0174 item 4 (TASK-0419): "Images" of the ring of the part opens them, and
    // Close gives the structure view back.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(360, 640)]
    public void The_Images_Take_The_Place_Of_The_Panel_Until_Close(int width, int height)
    {
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        var images = new ImagePanel([], null, 0, null);
        var display = new Vector2(width, height);
        void Step(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, display, Vector2.One, [Plate, Rib], null, Shelf, imagePanel: images);
        }

        void Tap(string item)
        {
            var box = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"{width}x{height}: no control {item}.");
            var (x, y) = ((box.X + box.Z) / 2, (box.Y + box.W) / 2);
            Step(new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
            Step(new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 10));
            Step();
            Step();
        }

        Step();
        Step();
        Step();
        Assert.Null(ui.ImagesRectangle);
        var panel = ui.PanelRectangles[0];
        Tap(DesktopUi.RingTextItem(DesktopUi.PartImagesLabel));
        var open = ui.ImagesRectangle ?? throw new InvalidOperationException($"{width}x{height}: the images did not open.");
        Assert.Equal(panel.X, open.X, 0.5f);
        Assert.Equal(panel.Y, open.Y, 0.5f);
        Assert.Null(ui.ItemRectangle(DesktopUi.DocumentRowItem));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.ImageFileLabel));
        Tap(DesktopUi.CloseImagesLabel);
        Assert.Null(ui.ImagesRectangle);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.DocumentRowItem));
    }

    // ADR-0172 item 2 (TASK-0418): each field and each key of an open pad shows inside the pad, with no scroll, on the phones,
    // in the smallest window and in a short wide window, for a pad of one field and a pad of three fields ("New box"). A pad
    // that scrolls is not a fault of the rule of ADR-0113 item 1, so this test holds the compact pad. Until TASK-0418 the keys
    // were 108 pixels wide, three columns were outside a phone, and at 360 by 560 a pad of three fields scrolled its keys out.
    [Theory]
    [InlineData(640, 360, 1)]
    [InlineData(640, 360, 3)]
    [InlineData(844, 390, 3)]
    [InlineData(915, 412, 1)]
    [InlineData(360, 560, 1)]
    [InlineData(360, 560, 3)]
    [InlineData(360, 640, 3)]
    [InlineData(900, 560, 3)]
    public void Each_Field_And_Key_Of_The_Pad_Shows_With_No_Scroll(int width, int height, int fields)
    {
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        var display = new Vector2(width, height);
        for (var i = 0; i < 3; i++)
        {
            if (i == 1 && fields == 1)
                ui.OpenDistancePad(A, 10);
            if (i == 1 && fields == 3)
                ui.OpenNew(DesktopUi.NewBoxLabel);
            ui.Frame(1f / 60f, display, Vector2.One, [Plate, Rib], fields == 1 ? A : null, Shelf, ringCentre: new Vector2(width / 2f, height * 0.55f), ringBody: 120f);
        }

        Assert.Equal(fields, ui.Pad?.Fields.Count);
        var pad = ui.PanelRectangles[^1];
        foreach (var item in DesktopUi.PadRows.SelectMany(row => row).Concat(ui.Pad!.Fields.Select(field => field.Name)))
        {
            var box = ui.ItemRectangle(DesktopUi.PadItem(item)) ?? throw new InvalidOperationException($"{width}x{height}: the pad has no {item}.");
            Assert.True(box.X >= pad.X - 0.5f && box.Y >= pad.Y - 0.5f && box.Z <= pad.Z + 0.5f && box.W <= pad.W + 0.5f,
                $"{width}x{height}, {fields} fields: {item} at {box} is not inside the pad at {pad}.");
        }

        // The text of each key fits its key at the least scale of its text, with its inner margin, so "Delete" and "Cancel" show.
        foreach (var key in DesktopUi.PadRows.SelectMany(row => row))
        {
            var box = ui.ItemRectangle(DesktopUi.PadItem(key))!.Value;
            Assert.True(ui.PadKeyNeed(key) <= box.Z - box.X + 0.5f, $"{width}x{height}: the text {key} needs {ui.PadKeyNeed(key)} pixels in a key of {box.Z - box.X}.");
        }
    }

    // Finding V212 of the codebase review of 2026-11-20 (TASK-0426): a pad of a sketch on a phone opens under the window of the
    // sketch, and it decides "compact" and "low" from that top, so each field, each key and Apply show inside the pad. Until
    // TASK-0426 the pad decided from the top of the panel and opened lower, so its last keys scrolled out of view.
    [Theory]
    [InlineData(390, 844)]
    [InlineData(412, 915)]
    public void Each_Key_Of_A_Pad_Of_A_Sketch_Shows_On_A_Phone(int width, int height)
    {
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        var display = new Vector2(width, height);
        var sketch = new Engine.Core.Queries.SketchInfo(Guid.NewGuid(), Engine.Contracts.Sketch.Datums.XY.Id, "Plate", [], [], [], [], [], true, 0);
        var panel = new SketchPanel([sketch], sketch, ThreeDEngine.Core.Sketching.SketchToolKind.Line, false, [], null);
        // While the pad is open the ring does not show, so the window of the sketch shows each tool and grows in the first frames
        // of the pad; the pad reads the window of the last frame, so its place holds from the third frame of the pad.
        for (var i = 0; i < 5; i++)
        {
            if (i == 1)
                ui.OpenNextPointPad(false);
            ui.Frame(1f / 60f, display, Vector2.One, [Plate, Rib], null, Shelf, sketchPanel: panel, ringCentre: new Vector2(width / 2f, height * 0.55f), ringBody: 120f);
        }

        Assert.Equal(DesktopUi.NextPointLabel, ui.Pad?.Title);
        var pad = ui.PanelRectangles[^1];
        foreach (var item in DesktopUi.PadRows.SelectMany(row => row).Concat(ui.Pad!.Fields.Select(field => field.Name)))
        {
            var box = ui.ItemRectangle(DesktopUi.PadItem(item)) ?? throw new InvalidOperationException($"{width}x{height}: the pad has no {item}.");
            Assert.True(Holds(pad, box), $"{width}x{height}: {item} at {Text(box)} is not inside the pad at {Text(pad)}.");
        }
    }

    // The faults of the layout at one size in one state, after three frames.
    internal static IReadOnlyList<string> Faults(int width, int height, State state)
    {
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        var display = new Vector2(width, height);
        Guid? selection = state is State.Nothing or State.Images or State.StructureOpen or State.StructureForTouch or State.FileMenu or State.ViewsOpen ? null : A;
        var face = state == State.Face ? new FaceLine("Face +z: 100 square mm", true, [], null, null, "Face +z", null) : null;
        IReadOnlyList<ProposalCard>? cards = state == State.Proposal
            ? [new ProposalCard(Guid.NewGuid(), "Widen the plate to 75 mm", ["SetExtrudeDistance 1", "SetExtrudeDistance 2"])]
            : null;
        if (state == State.Home)
        {
            ui.ShowHome(["entry-hall-shelf", "bracket"]);
            selection = null;
        }
        if (state == State.Sketch)
            selection = null;
        var sketchInfo = new Engine.Core.Queries.SketchInfo(Guid.NewGuid(), Engine.Contracts.Sketch.Datums.XY.Id, "Plate", [], [], [], [], [], true, 0);
        var sketchPanel = state == State.Sketch ? new SketchPanel([sketchInfo], sketchInfo, ThreeDEngine.Core.Sketching.SketchToolKind.Line, false, [], null) : null;
        IReadOnlyList<HistoryStep>? history = state == State.History
            ? [new HistoryStep(A, "Extrude", "Plate", "correct", null, HistoryTap.SelectBody), new HistoryStep(B, "CreateSketch", "Plate on XY", "correct", null, HistoryTap.EditSketch, MadeFrom: true)]
            : null;
        var images = state is State.Images or State.ImagesOpen ? new ImagePanel([], null, 0, null) : null;
        for (var i = 0; i < 3; i++)
        {
            if (state == State.Pad && i == 1)
                ui.OpenDistancePad(A, 10);
            if (state == State.ImagesOpen && i == 1)
                ui.OpenImages();
            if (state is State.StructureOpen or State.StructureForTouch && i == 1)
                ui.OpenStructure();
            if (state == State.StructureForTouch && i == 0)
                ui.Density = DesktopUi.RowDensity.ForTouch;
            if (state == State.FileMenu && i == 1)
                ui.OpenFileMenu();
            // The structure view is open too, so the state holds that the net takes its place.
            if (state == State.ViewsOpen && i == 0)
            {
                ui.OpenStructure();
                ui.OpenViews();
            }
            ui.Frame(1f / 60f, display, Vector2.One, [Plate, Rib], selection, Shelf, sketchPanel: sketchPanel, imagePanel: images, face: face, ringCentre: selection is null && sketchPanel is null && state is not (State.StructureOpen or State.StructureForTouch) ? null : new Vector2(width / 2f, height * 0.55f), ringBody: 120f, history: history, proposals: cards);
        }

        var panels = ui.PanelRectangles.ToList();
        var accept = ui.ItemRectangle(DesktopUi.ProposalAcceptItem);
        // The sheet is the smallest panel that holds Accept; on a phone the panel of the bodies holds it too.
        Vector4? sheet = accept is { } a ? panels.Where(panel => Holds(panel, a)).Cast<Vector4?>().MinBy(panel => panel!.Value.W - panel.Value.Y) : null;
        var windows = panels.Concat(ui.FixedRectangles).ToList();
        // ADR-0111 item 4: a sheet that does not fit between the top bar and the ring ends above the anchors and covers the
        // ring. ADR-0131 item 1 (phase R18k, TASK-0360): at 360 by 640 the sheet does not fit. Until TASK-0360 the panel of
        // the selected body showed there, and its rule of the scrolled controls hid the tools of the ring under the sheet.
        var sheetOnRing = sheet is { } covering && ui.RingTop() is { } ringTop && covering.W > ringTop - 8f + 0.5f;
        // ADR-0174 item 4 (TASK-0419): the open file menu is over each window and takes each tap, as an open list, so a control
        // under it is no fault, and its own entries are under no window.
        var menu = ui.ItemRectangle(DesktopUi.FileMenuItem);
        var faults = new List<string>();
        foreach (var (name, r) in ui.ItemRectangles)
        {
            if (panels.Any(panel => Scrolled(panel, r)))
                continue;
            if (r.X < -0.5f || r.Y < -0.5f || r.Z > width + 0.5f || r.W > height + 0.5f)
                faults.Add($"{name} {Text(r)} goes out of the display");
            if (menu is { } open && Holds(open, r))
                continue;
            var middle = new Vector2((r.X + r.Z) / 2f, (r.Y + r.W) / 2f);
            // ADR-0131 item 1 (phase R18k, TASK-0360): the rows take each tap on them (finding V169), and they are no window,
            // so a tool of the ring on the rows is a fault too.
            if (name.StartsWith("Ring", StringComparison.Ordinal) && ui.RowsArea is { } rows && Inside(rows, middle))
                faults.Add($"{name} {Text(r)} is under the rows {Text(rows)}");
            foreach (var window in windows)
            {
                if (!Inside(window, middle) || window == menu)
                    continue;
                // A tool of the ring and its label are on the scene, so they are never a control of a window.
                var ring = name.StartsWith("Ring", StringComparison.Ordinal);
                if (!ring && Own(window, r))
                {
                    if (r.Z > window.Z + 0.5f)
                        faults.Add($"{name} {Text(r)} goes past the right edge of its window {Text(window)}");
                    continue;
                }

                if (window == sheet && (!ring || sheetOnRing))
                    continue;
                faults.Add($"{name} {Text(r)} is under the window {Text(window)}");
            }
        }

        // ADR-0114 item 4 (finding T181 of the codebase review of 2026-11-10, TASK-0339): each pair of windows, two panels too.
        // A control is the control of the window that holds its upper left corner, so a panel over a control of another
        // panel passed the check of the controls; the check of the pairs finds it. The sheet may cover the panel.
        for (var i = 0; i < windows.Count; i++)
        {
            for (var j = i + 1; j < windows.Count; j++)
            {
                if (windows[i] != sheet && windows[j] != sheet && windows[i] != menu && windows[j] != menu && Overlap(windows[i], windows[j]))
                    faults.Add($"the window {Text(windows[i])} overlaps the window {Text(windows[j])}");
            }
        }

        return faults;
    }

    // ADR-0113 item 1: the pictures for the owner at the phone sizes and the smallest landscape display, with a selected body,
    // and with a proposal of an agent.
    [VulkanImageFact]
    public async Task The_Pictures_Show_Each_Control_In_The_Display()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var plate = Out(await host.Session.Apply(new CreateBoxCommand { SizeX = 40, SizeY = 24, SizeZ = 6 }), "bodyId");
        await host.Session.Apply(new RenameBodyCommand { BodyId = plate, NewName = "Plate" });
        var post = Out(await host.Session.Apply(new CreateCylinderCommand { Radius = 4, Height = 20 }), "bodyId");
        await host.Session.Apply(new RenameBodyCommand { BodyId = post, NewName = "Post" });

        foreach (var (width, height) in new[] { (360u, 780u), (390u, 844u), (412u, 915u), (844u, 390u), (640u, 360u) })
        {
            foreach (var proposal in new[] { false, true })
            {
                using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
                var scene = SessionScene.Create();
                var camera = scene.Cameras[0];
                var display = new Vector2(width, height);
                IReadOnlyList<ProposalCard>? cards = proposal ? [new ProposalCard(Guid.NewGuid(), "Widen the plate to 75 mm", ["SetExtrudeDistance 1"])] : null;
                byte[] pixels;
                using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
                {
                    using var target = new OffscreenTarget(device, new VkExtent2D(width, height));
                    using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
                    using var panelRenderer = new UiRenderer(device, target.RenderPass);
                    var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
                    ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
                    renderer.Update(scene, camera, (float)width / height);
                    var data = UiDrawData.Empty;
                    for (var frame = 0; frame < 3; frame++)
                    {
                        ui.Route([]);
                        data = ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies, plate, Shelf, ringCentre: new Vector2(width / 2f, height * 0.55f), ringBody: 120f, proposals: cards);
                    }

                    pixels = target.Render((commandBuffer, framebuffer, frameExtent) =>
                        renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                            overlay => panelRenderer.Record(overlay, frameExtent, data, 0), plate));
                }

                var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
                    ? output
                    : Path.Combine(AppContext.BaseDirectory, "pictures");
                Directory.CreateDirectory(folder);
                Png.Write(Path.Combine(folder, $"responsive-{(proposal ? "proposal" : "body")}-{width}x{height}.png"), (int)width, (int)height, pixels);
            }
        }

        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");
    }

    private static Guid Out(CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    private static bool Inside(Vector4 box, Vector2 point) => point.X >= box.X && point.X < box.Z && point.Y >= box.Y && point.Y < box.W;

    private static bool Holds(Vector4 box, Vector4 r) => r.X >= box.X - 0.5f && r.Z <= box.Z + 0.5f && r.Y >= box.Y - 0.5f && r.W <= box.W + 0.5f;

    // A control of a window: its left side is in the window and its top is in the window, so the window clips the rest.
    private static bool Own(Vector4 box, Vector4 r) => r.X >= box.X - 0.5f && r.X < box.Z && r.Y >= box.Y - 0.5f && r.Y < box.W;

    // A control that its panel scrolls out of view: in the columns of the panel, and its top at or below the bottom of it.
    // ADR-0163 item 4 (TASK-0404): or its top in the panel and its middle at or below the bottom, so the panel clips the
    // lower half and a tap on its visible part reaches it. Until TASK-0404 such a row under Undo and Redo was a fault at
    // each height where the bottom of the panel cut a row, for example 1280 by 380 and 1280 by 500.
    private static bool Scrolled(Vector4 panel, Vector4 r) => r.X >= panel.X - 0.5f && r.X < panel.Z
        && (r.Y >= panel.W - 0.5f || (r.Y >= panel.Y - 0.5f && (r.Y + r.W) / 2f >= panel.W - 0.5f));

    private static bool Overlap(Vector4 p, Vector4 q) => Math.Min(p.Z, q.Z) - Math.Max(p.X, q.X) > 1f && Math.Min(p.W, q.W) - Math.Max(p.Y, q.Y) > 1f;

    private static string Text(Vector4 r) => $"({r.X:0}, {r.Y:0}, {r.Z:0}, {r.W:0})";
}
