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
    private static readonly Vector2 Display = new(1024, 768);

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

    // The list shows only with a selected body and another visible body, and it lies inside the panel.
    [Fact]
    public void The_List_Of_A_Cut_Shows_Only_With_Another_Visible_Body()
    {
        using (var none = Opened(null, [Plate, Tool]))
            Assert.Null(none.ItemRectangle(DesktopUi.CutLabel));
        using (var alone = Opened(Plate.Id, [Plate, Hidden]))
            Assert.Null(alone.ItemRectangle(DesktopUi.CutLabel));

        using var ui = Opened(Plate.Id, [Plate, Tool]);
        var list = ui.ItemRectangle(DesktopUi.CutLabel)!.Value;
        var panel = ui.PanelRectangles.First();
        Assert.True(list.X >= panel.X && list.Z <= panel.Z && list.W <= panel.W, $"The list {list} is outside the panel {panel}.");
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
