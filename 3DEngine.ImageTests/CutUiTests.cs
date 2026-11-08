using System.Numerics;
using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0073 (phase R14e3, TASK-0227): a person cuts the selected body with another visible body in the panel, with no
// agent, as on the phone. The panel with no GPU, and the action on a session of its own.
[Collection(DearImGui.Collection)]
public sealed class CutUiTests
{
    // ADR-0099 item 1 (the redesign, slice p, TASK-0313): on a landscape display the rows of a selected body take the place of
    // the panel, so these tests of the panel use a portrait display, where the panel keeps the controls of the selected body.
    private static readonly Vector2 Display = new(768, 1024);

    private static readonly LiveBody Plate = new(Guid.Parse("51000000-0000-0000-0000-000000000000"), "Box", "Plate", true);

    private static readonly LiveBody Tool = new(Guid.Parse("52000000-0000-0000-0000-000000000000"), "Box", "Tool", true);

    private static readonly LiveBody Hidden = new(Guid.Parse("53000000-0000-0000-0000-000000000000"), "Box", "Hidden", false);

    private static void Step(DesktopUi ui, Guid? selection, LiveBody[] bodies, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, bodies, selection);
    }

    private static void Tap(DesktopUi ui, string item, Guid? selection, LiveBody[] bodies)
    {
        var rectangle = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"The panel has no control {item}.");
        var (x, y) = ((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);
        Step(ui, selection, bodies, new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
        Step(ui, selection, bodies, new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));
        Step(ui, selection, bodies);
        Step(ui, selection, bodies);
    }

    private static DesktopUi Opened(Guid? selection, LiveBody[] bodies)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, selection, bodies);
        Step(ui, selection, bodies);
        return ui;
    }

    private static bool Inside(Vector4 item, Vector4 panel)
        => item.X >= panel.X && item.Z <= panel.Z && item.Y >= panel.Y && item.W <= panel.W;

    // The list shows only with a selected body and another visible body, and it lies inside the panel. Finding V98 of
    // the codebase review of 2026-11-06 (c) (TASK-0232): for an extrude, Set distance is on the line too, and the list lay
    // 178 pixels outside the panel. Show all stays inside the panel too (finding V50).
    [Theory]
    [InlineData("Box")]
    [InlineData("Extrude")]
    public void The_List_Of_A_Cut_Shows_Only_With_Another_Visible_Body(string kind)
    {
        var plate = Plate with { Kind = kind };
        using (var none = Opened(null, [plate, Tool]))
            Assert.Null(none.ItemRectangle(DesktopUi.CutLabel));
        using (var alone = Opened(plate.Id, [plate, Hidden]))
            Assert.Null(alone.ItemRectangle(DesktopUi.CutLabel));

        using var ui = Opened(plate.Id, [plate, Tool]);
        var list = ui.ItemRectangle(DesktopUi.CutLabel)!.Value;
        var showAll = ui.ItemRectangle(DesktopUi.ShowAllLabel)!.Value;
        var panel = ui.PanelRectangles[0];
        Assert.True(Inside(list, panel), $"The list {list} is outside the panel {panel}.");
        Assert.True(Inside(showAll, panel), $"Show all {showAll} is outside the panel {panel}.");
    }

    // The review of Codex of v0.212, finding 1 (TASK-0232): the popup of the list added no rectangle to the panels, so a tap
    // on a part of the popup outside the panel reached the scene. The open popup is a panel of the frame.
    [Fact]
    public void The_Open_List_Is_A_Panel()
    {
        LiveBody[] bodies = [Plate, Tool];
        using var ui = Opened(Plate.Id, bodies);

        Tap(ui, DesktopUi.CutLabel, Plate.Id, bodies);

        var item = ui.ItemRectangle(DesktopUi.CutItem(Tool.Id)) ?? throw new InvalidOperationException("The list did not open.");
        Assert.Contains(ui.PanelRectangles.Skip(1), panel => Inside(item, panel));
    }

    // Finding V99 of the codebase review of 2026-11-06 (c) (TASK-0232): with no IDocumentFiles, as on the phone, the panel
    // had no line of the document, so a rejected cut showed nothing. The line shows the message and no file control.
    [Fact]
    public void A_Host_With_No_Files_Shows_The_Rejection_Of_A_Cut()
    {
        var line = NativeThreeDEngine.LineOf(null, null, "E-GEOM-NATIVE-OP: the subtraction gives an empty solid.");
        LiveBody[] bodies = [Plate, Tool];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        for (var frame = 0; frame < 2; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, Display, Vector2.One, bodies, Plate.Id, line);
        }

        Assert.NotNull(ui.ItemRectangle(DesktopUi.DocumentMessageLabel));
        Assert.Null(ui.ItemRectangle(DesktopUi.SaveAsLabel));
    }

    // Finding V116 of the codebase review of 2026-11-06 (e) (TASK-0252): while the list "Cut with" or the list "New" is open,
    // a tap on the scene, by touch and by mouse, closes the list, gives no sample to the scene and no action. Until TASK-0252
    // the scene took the tap, and the list stayed open.
    [Theory]
    [InlineData("Cut", DeviceKind.Touch)]
    [InlineData("Cut", DeviceKind.Mouse)]
    [InlineData("New", DeviceKind.Touch)]
    [InlineData("New", DeviceKind.Mouse)]
    public void A_Tap_On_The_Scene_Closes_An_Open_List_And_Does_Not_Reach_The_Scene(string list, DeviceKind kind)
    {
        LiveBody[] bodies = [Plate, Tool];
        using var ui = Opened(Plate.Id, bodies);
        var (box, entry) = list == "Cut" ? (DesktopUi.CutLabel, DesktopUi.CutItem(Tool.Id)) : (DesktopUi.NewLabel, DesktopUi.NewBoxLabel);
        Tap(ui, box, Plate.Id, bodies);
        Assert.NotNull(ui.ItemRectangle(entry));

        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        var scene = new List<InputSample>();
        foreach (var phase in new[] { PointerPhase.Down, PointerPhase.Up })
        {
            scene.AddRange(ui.Route([new PointerSample(kind, 1, pointer, 900, 400, 1, buttons, KeyModifiers.None, phase, 0)]));
            ui.Frame(1f / 60f, Display, Vector2.One, bodies, Plate.Id);
        }
        Step(ui, Plate.Id, bodies);
        Step(ui, Plate.Id, bodies);

        Assert.Empty(scene);
        Assert.Null(ui.ItemRectangle(entry));
        Assert.Empty(ui.TakeActions());
    }

    // A tap on the list and a tap on a tool give the cut of the selected body with that tool. A hidden body is no tool.
    [Fact]
    public void A_Tap_On_A_Tool_Gives_The_Cut_Of_The_Selected_Body()
    {
        LiveBody[] bodies = [Plate, Tool, Hidden];
        using var ui = Opened(Plate.Id, bodies);

        Tap(ui, DesktopUi.CutLabel, Plate.Id, bodies);
        Assert.Null(ui.ItemRectangle(DesktopUi.CutItem(Hidden.Id)));
        Tap(ui, DesktopUi.CutItem(Tool.Id), Plate.Id, bodies);

        Assert.Equal([new CutBodyAction(Plate.Id, Tool.Id)], ui.TakeActions());
    }

    // The cut is the command Subtract: a box less a box through it gives one body, which becomes the selection.
    [Fact]
    public async Task The_Cut_Subtracts_The_Tool_And_Selects_The_Result()
    {
        using var host = new LocalSessionHost(new ManifoldGeometryBackend());
        var plate = (await host.Session.Apply(new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 2 })).Outputs;
        var tool = (await host.Session.Apply(new CreateBoxCommand { SizeX = 4, SizeY = 4, SizeZ = 4 })).Outputs;
        Assert.True(plate.TryGet<Guid>("bodyId", out var plateId));
        Assert.True(tool.TryGet<Guid>("bodyId", out var toolId));
        var actions = new BodyActions(host.Session);
        actions.Select(plateId);

        var rejection = actions.Cut(plateId, toolId);

        Assert.Null(rejection);
        var live = await host.Session.Read((document, _) => document.Bodies.Select(body => body.Handle.Id).ToList());
        Assert.Equal(actions.Selection, Assert.Single(live));
    }
}
