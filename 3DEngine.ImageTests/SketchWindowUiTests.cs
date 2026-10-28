using System.Numerics;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Sketching;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// The buttons of a new sketch and the window of the sketch (ADR-0034 items 6 to 8, TASK-0101), with
// no GPU: the controls of each state, the buttons that fit a selection, the pads that start with
// the current value, and a tap and a click that give the same actions.
[Collection(DearImGui.Collection)]
public sealed class SketchWindowUiTests
{
    private static readonly Vector2 Display = new(1280, 720);

    private static readonly Guid SketchId = Guid.Parse("50000000-0000-0000-0000-000000000000");
    private static readonly Guid A = Guid.Parse("a0000000-0000-0000-0000-000000000000");
    private static readonly Guid B = Guid.Parse("b0000000-0000-0000-0000-000000000000");
    private static readonly Guid Line = Guid.Parse("1e000000-0000-0000-0000-000000000000");
    private static readonly Guid Fix = Guid.Parse("c0000000-0000-0000-0000-000000000000");

    // A sketch on XY: the origin O, P1 (3, 4), P2 (3, 10), the line L1 from P1 to P2, and the
    // constraint C1 that fixes P1.
    private static readonly SketchInfo Sketch = new(
        SketchId, Datums.XY.Id, "Plate",
        [new(SketchId, 0, 0), new(A, 3, 4), new(B, 3, 10)],
        [new(Line, A, B)],
        [], [],
        [new SketchConstraintInfo(Fix, "fixed", [A], [3, 4], 0)]);

    private static SketchPanel Panel(params Guid[] selection) => new([Sketch], Sketch, SketchToolKind.Select, false, selection, null);

    private static void Step(DesktopUi ui, SketchPanel? panel, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, [], null, null, null, panel);
    }

    private static DesktopUi Opened(SketchPanel? panel)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180");
        Step(ui, panel);
        Step(ui, panel);
        return ui;
    }

    private static (double X, double Y) Centre(DesktopUi ui, string item)
    {
        var rectangle = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"The user interface has no control {item}.");
        return ((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);
    }

    private static void Tap(DesktopUi ui, DeviceKind kind, string item, SketchPanel? panel)
    {
        var at = Centre(ui, item);
        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        Step(ui, panel, new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Down, 0));
        Step(ui, panel, new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Up, 0));
        Step(ui, panel);
        Step(ui, panel);
    }

    private static IReadOnlyList<PanelAction> Taps(DeviceKind kind, SketchPanel? panel, params string[] items)
    {
        using var ui = Opened(panel);
        foreach (var item in items)
            Tap(ui, kind, item, panel);
        return ui.TakeActions();
    }

    // ADR-0034 item 6: the panel has a button of a new sketch on each origin plane.
    [Theory]
    [InlineData(DeviceKind.Mouse)]
    [InlineData(DeviceKind.Touch)]
    public void The_Panel_Makes_A_Sketch_On_Each_Origin_Plane(DeviceKind kind)
    {
        var actions = Taps(kind, null, [.. DesktopUi.NewSketchButtons.Select(button => button.Label)]);

        Assert.Equal(Datums.All.Select(datum => (PanelAction)new NewSketchAction(datum.Id)), actions);
    }

    // With no active sketch, the window lists each live sketch with Edit, and shows nothing when no
    // sketch is live.
    [Fact]
    public void With_No_Active_Sketch_The_Window_Lists_Each_Live_Sketch()
    {
        var idle = new SketchPanel([Sketch], null, SketchToolKind.Line, false, [], null);
        var edit = DesktopUi.SketchItem($"{DesktopUi.EditLabel} {DesktopUi.SketchTitle(Sketch)}");

        Assert.Equal([new EditSketchAction(SketchId)], Taps(DeviceKind.Touch, idle, edit));
        using var empty = Opened(new SketchPanel([], null, SketchToolKind.Line, false, [], null));
        Assert.Null(empty.ItemRectangle(DesktopUi.SketchItem(DesktopUi.FinishLabel)));
        Assert.Single(empty.PanelRectangles);
    }

    // ADR-0034 item 8: the tools, Next point while the tool Line is active, End and Close while a
    // chain is open, and each control at least 44 pixels high (ADR-0027 item 6).
    [Fact]
    public void The_Tools_Show_For_Their_State()
    {
        using var line = Opened(new SketchPanel([Sketch], Sketch, SketchToolKind.Line, true, [], null));
        using var select = Opened(Panel());

        string[] lineControls = [DesktopUi.LineToolLabel, DesktopUi.SelectToolLabel, DesktopUi.FinishLabel, DesktopUi.NextPointLabel, DesktopUi.EndLabel, DesktopUi.CloseLabel, "O", "P1", "P2", "L1", "C1"];
        foreach (var control in lineControls)
        {
            var rectangle = line.ItemRectangle(DesktopUi.SketchItem(control));
            Assert.True(rectangle is { } r && r.W - r.Y >= DesktopUi.MinimumControlHeight - 0.5f, $"The control {control} is absent or lower than 44 pixels.");
        }

        Assert.Null(select.ItemRectangle(DesktopUi.SketchItem(DesktopUi.NextPointLabel)));
        Assert.Null(select.ItemRectangle(DesktopUi.SketchItem(DesktopUi.CloseLabel)));
    }

    // ADR-0034 item 8: the buttons of the kinds that fit the selection, and nothing else.
    [Fact]
    public void The_Buttons_Of_A_Selection_Are_The_Kinds_That_Fit_It()
    {
        static string[] Buttons(params Guid[] selection)
        {
            using var ui = Opened(Panel(selection));
            string[] all = [.. DesktopUi.ConstraintButtons.Select(button => button.Label), DesktopUi.MovePointLabel, DesktopUi.ValueLabel, DesktopUi.DeleteLabel];
            return [.. all.Where(label => ui.ItemRectangle(DesktopUi.SketchItem(label)) is not null)];
        }

        Assert.Equal(["Horizontal", "Vertical", "Distance", DesktopUi.DeleteLabel], Buttons(Line));
        Assert.Equal(["Coincident", "Distance", DesktopUi.DeleteLabel], Buttons(A, B));
        Assert.Equal(["Distance", DesktopUi.DeleteLabel], Buttons(Line, SketchId));
        Assert.Equal(["Fixed", DesktopUi.MovePointLabel, DesktopUi.DeleteLabel], Buttons(B));
        Assert.Equal([DesktopUi.ValueLabel, DesktopUi.DeleteLabel], Buttons(Fix));
        Assert.Empty(Buttons());
    }

    // A kind with no value gives its action at once, with the points first; a row toggles its item.
    [Theory]
    [InlineData(DeviceKind.Mouse)]
    [InlineData(DeviceKind.Touch)]
    public void A_Kind_With_No_Value_And_A_Row_Give_Their_Actions(DeviceKind kind)
    {
        var actions = Taps(kind, Panel(Line), DesktopUi.SketchItem("Horizontal"), DesktopUi.SketchItem("P2"));

        Assert.Equal(2, actions.Count);
        var horizontal = Assert.IsType<AddConstraintAction>(actions[0]);
        Assert.Equal("horizontal", horizontal.Kind);
        Assert.Equal([Line], horizontal.Items);
        Assert.Empty(horizontal.Values);
        Assert.Equal(new ToggleSketchItemAction(B), actions[1]);
    }

    // ADR-0034 item 8: Distance opens a pad of one field with the current distance, and its apply
    // gives the constraint with the items in the order of the kind: the point, then the line.
    [Fact]
    public void Distance_Opens_A_Pad_With_The_Current_Distance()
    {
        var panel = Panel(Line, SketchId);
        using var ui = Opened(panel);
        Tap(ui, DeviceKind.Touch, DesktopUi.SketchItem("Distance"), panel);

        var pad = Assert.IsType<Core.Numeric.NumericPad>(ui.Pad);
        Assert.Equal("3", Assert.Single(pad.Fields).Text);
        Tap(ui, DeviceKind.Touch, DesktopUi.PadItem("Apply"), panel);

        var distance = Assert.IsType<AddConstraintAction>(Assert.Single(ui.TakeActions()));
        Assert.Equal("distance", distance.Kind);
        Assert.Equal([SketchId, Line], distance.Items);
        Assert.Equal([3.0], distance.Values);
    }

    // Value opens the pad with the values of the constraint, and Move point with the position.
    [Fact]
    public void Value_And_Move_Point_Start_With_The_Current_Numbers()
    {
        var fixedPanel = Panel(Fix);
        using var value = Opened(fixedPanel);
        Tap(value, DeviceKind.Mouse, DesktopUi.SketchItem(DesktopUi.ValueLabel), fixedPanel);
        Assert.Equal(["3", "4"], value.Pad!.Fields.Select(field => field.Text));
        Tap(value, DeviceKind.Mouse, DesktopUi.PadItem("Apply"), fixedPanel);
        var set = Assert.IsType<SetConstraintValueAction>(Assert.Single(value.TakeActions()));
        Assert.Equal(Fix, set.Constraint);
        Assert.Equal([3.0, 4.0], set.Values);

        var pointPanel = Panel(B);
        using var move = Opened(pointPanel);
        Tap(move, DeviceKind.Mouse, DesktopUi.SketchItem(DesktopUi.MovePointLabel), pointPanel);
        Assert.Equal(["3", "10"], move.Pad!.Fields.Select(field => field.Text));
    }

    // ADR-0034 item 7: Next point opens a pad of x and y with no chain, and of dx and dy with a
    // chain; a pad of the sketch does not cover the window of the sketch.
    [Fact]
    public void Next_Point_Opens_A_Pad_Beside_The_Window_Of_The_Sketch()
    {
        var panel = new SketchPanel([Sketch], Sketch, SketchToolKind.Line, false, [], null);
        using var ui = Opened(panel);
        Tap(ui, DeviceKind.Touch, DesktopUi.SketchItem(DesktopUi.NextPointLabel), panel);
        Step(ui, panel);

        Assert.Equal(["x", "y"], ui.Pad!.Fields.Select(field => field.Name));
        Assert.Equal(3, ui.PanelRectangles.Count);
        var (sketchWindow, padWindow) = (ui.PanelRectangles[1], ui.PanelRectangles[2]);
        Assert.True(padWindow.Z <= sketchWindow.X, $"The pad ends at {padWindow.Z}, and the window of the sketch starts at {sketchWindow.X}.");

        ui.EndPad(null);
        ui.OpenNextPointPad(chainOpen: true);
        Assert.Equal(["dx", "dy"], ui.Pad!.Fields.Select(field => field.Name));
    }

    // V32 of the second codebase review of 2026-10-28: a pad of a sketch that names a selection
    // closes when the selection changes, and the pad of the next point, which names none, stays.
    [Fact]
    public void A_Pad_Of_The_Selection_Closes_When_The_Selection_Changes()
    {
        var both = Panel(A, B);
        using var ui = Opened(both);
        Tap(ui, DeviceKind.Touch, DesktopUi.SketchItem("Distance"), both);
        Assert.NotNull(ui.Pad);

        Step(ui, Panel(A, B));
        Assert.NotNull(ui.Pad);
        Step(ui, Panel(A));
        Assert.Null(ui.Pad);

        ui.OpenNextPointPad(chainOpen: false);
        Step(ui, Panel(B));
        Step(ui, Panel());
        Assert.NotNull(ui.Pad);
    }
}
