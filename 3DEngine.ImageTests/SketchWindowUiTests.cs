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
        [new SketchConstraintInfo(Fix, "fixed", [A], [3, 4], 0)],
        true, 2);

    private static SketchPanel Panel(params Guid[] selection) => new([Sketch], Sketch, SketchToolKind.Select, false, selection, null);

    private static void Step(DesktopUi ui, SketchPanel? panel, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, [], null, null, null, panel);
    }

    private static DesktopUi Opened(SketchPanel? panel)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
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

    // ADR-0035 item 8: the line under the title says if the sketch is solved, with its free degrees.
    [Fact]
    public void The_Status_Line_Says_If_The_Sketch_Is_Solved()
    {
        Assert.Equal("Solved, 2 free", DesktopUi.SketchStatus(Sketch));
        Assert.Equal("Not solved", DesktopUi.SketchStatus(Sketch with { Solved = false }));
    }

    // ADR-0034 item 8: the tools, Next point while the tool Line is active, End and Close while a
    // chain is open, and each control at least 44 pixels high (ADR-0027 item 6).
    [Fact]
    public void The_Tools_Show_For_Their_State()
    {
        using var line = Opened(new SketchPanel([Sketch], Sketch, SketchToolKind.Line, true, [], null));
        using var select = Opened(Panel());

        string[] lineControls = [DesktopUi.LineToolLabel, DesktopUi.SelectToolLabel, DesktopUi.FinishLabel, DesktopUi.ExtrudeLabel, DesktopUi.NextPointLabel, DesktopUi.EndLabel, DesktopUi.CloseLabel, "O", "P1", "P2", "L1", "C1"];
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
        // V43 (TASK-0106): a selection with the origin O gives no Delete, because the engine refuses
        // to delete O and the delete of the other items gave no message for it. Since K2f (TASK-0125)
        // O and a line also give On line and Midpoint: O is not an end of the line.
        Assert.Equal(["Distance", DesktopUi.OnLineLabel, "Midpoint"], Buttons(Line, SketchId));
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

    private static readonly Guid Ring = Guid.Parse("20000000-0000-0000-0000-000000000000");
    private static readonly Guid Bow = Guid.Parse("40000000-0000-0000-0000-000000000000");

    private static readonly Guid C = Guid.Parse("cc000000-0000-0000-0000-000000000000");
    private static readonly Guid Other = Guid.Parse("2e000000-0000-0000-0000-000000000000");

    // Phase K2f (TASK-0125): the sketch of the fixture with P3 (7, 4) and the line L2 from P1 to P3,
    // at a right angle to L1.
    private static SketchPanel LinePanel(params Guid[] selection)
    {
        var sketch = Sketch with { Points = [.. Sketch.Points, new SketchPoint(C, 7, 4)], Lines = [.. Sketch.Lines, new SketchLine(Other, A, C)] };
        return new([sketch], sketch, SketchToolKind.Select, false, selection, null);
    }

    // The kinds of K2c on lines fit their selections; a point of pointOn or midpoint is not an end of
    // its line.
    [Fact]
    public void The_Buttons_Of_Lines_Are_The_Kinds_Of_K2c_That_Fit_Them()
    {
        static string[] Buttons(params Guid[] selection)
        {
            using var ui = Opened(LinePanel(selection));
            string[] all = [.. DesktopUi.ConstraintButtons.Select(button => button.Label), DesktopUi.MovePointLabel, DesktopUi.ValueLabel, DesktopUi.DeleteLabel];
            return [.. all.Where(label => ui.ItemRectangle(DesktopUi.SketchItem(label)) is not null)];
        }

        Assert.Equal(["Equal", "Parallel", "Perpendicular", "Angle", DesktopUi.DeleteLabel], Buttons(Line, Other));
        Assert.Equal(["Distance", DesktopUi.OnLineLabel, "Midpoint", DesktopUi.DeleteLabel], Buttons(C, Line));
        Assert.Equal(["Distance", DesktopUi.DeleteLabel], Buttons(B, Line));
        Assert.Equal(["Coincident", "Distance"], Buttons(C, SketchId));
    }

    // Phase K2g (TASK-0127): two points and a line give Symmetric when no point is an end of the line,
    // and its action takes the points first.
    [Fact]
    public void Two_Points_And_A_Line_Give_Symmetric()
    {
        static string[] Buttons(params Guid[] selection)
        {
            using var ui = Opened(LinePanel(selection));
            string[] all = [.. DesktopUi.ConstraintButtons.Select(button => button.Label), DesktopUi.MovePointLabel, DesktopUi.ValueLabel, DesktopUi.DeleteLabel];
            return [.. all.Where(label => ui.ItemRectangle(DesktopUi.SketchItem(label)) is not null)];
        }

        Assert.Equal(["Symmetric"], Buttons(C, Line, SketchId));
        Assert.Equal([DesktopUi.DeleteLabel], Buttons(A, C, Line));

        var symmetric = Assert.IsType<AddConstraintAction>(Assert.Single(Taps(DeviceKind.Touch, LinePanel(Line, C, SketchId), DesktopUi.SketchItem("Symmetric"))));
        Assert.Equal("symmetric", symmetric.Kind);
        Assert.Equal([C, SketchId, Line], symmetric.Items);
    }

    // Angle opens the pad with the current angle in degrees, and its apply gives the cosine and the
    // sine: L1 points along +y and L2 along +x, so the angle from L1 to L2 is −90 degrees.
    [Fact]
    public void Angle_Opens_A_Pad_Of_Degrees_And_Gives_The_Cosine_And_The_Sine()
    {
        var panel = LinePanel(Line, Other);
        using var ui = Opened(panel);
        Tap(ui, DeviceKind.Touch, DesktopUi.SketchItem("Angle"), panel);

        Assert.Equal(DesktopUi.AngleUnit, ui.Pad!.Unit);
        Assert.Equal("-90", Assert.Single(ui.Pad.Fields).Text);
        Tap(ui, DeviceKind.Touch, DesktopUi.PadItem("Apply"), panel);

        var angle = Assert.IsType<AddConstraintAction>(Assert.Single(ui.TakeActions()));
        Assert.Equal("angle", angle.Kind);
        Assert.Equal([Line, Other], angle.Items);
        Assert.Equal([0.0, -1.0], angle.Values);
        var thirty = DesktopUi.CosineAndSine(30);
        Assert.Equal(Math.Sqrt(3) / 2, thirty[0], 15);
        Assert.Equal(0.5, thirty[1], 15);
    }

    // Phase K2e (TASK-0122): the sketch of the fixture with the circle R1 about P1 of radius 2.5,
    // and the arc A1 about O from P1 to P2.
    private static SketchPanel CurvePanel(params Guid[] selection)
    {
        var sketch = Sketch with { Circles = [new SketchCircle(Ring, A, 2.5)], Arcs = [new SketchArc(Bow, SketchId, A, B)] };
        return new([sketch], sketch, SketchToolKind.Select, false, selection, null);
    }

    // The kinds on a curve fit their selections, with the items in the order of the kind: the
    // point, then the line, then the curve.
    [Fact]
    public void The_Buttons_Of_A_Curve_Are_The_Kinds_That_Fit_It()
    {
        static string[] Buttons(params Guid[] selection)
        {
            using var ui = Opened(CurvePanel(selection));
            string[] all = [.. DesktopUi.ConstraintButtons.Select(button => button.Label), DesktopUi.MovePointLabel, DesktopUi.ValueLabel, DesktopUi.DeleteLabel];
            return [.. all.Where(label => ui.ItemRectangle(DesktopUi.SketchItem(label)) is not null)];
        }

        Assert.Equal(["Radius", DesktopUi.DeleteLabel], Buttons(Ring));
        Assert.Equal(["On curve", DesktopUi.DeleteLabel], Buttons(Ring, B));
        Assert.Equal(["Tangent", DesktopUi.DeleteLabel], Buttons(Bow, Line));
        Assert.Equal(["Tangent", DesktopUi.InsideTangentLabel, "Equal", DesktopUi.DeleteLabel], Buttons(Ring, Bow));
    }

    // Inside tangent gives −1, Tangent of a line gives no value, and On curve takes the point
    // first; Radius opens the pad with the current radius.
    [Fact]
    public void The_Kinds_On_A_Curve_Give_Their_Actions()
    {
        var two = Assert.IsType<AddConstraintAction>(Assert.Single(Taps(DeviceKind.Touch, CurvePanel(Ring, Bow), DesktopUi.SketchItem(DesktopUi.InsideTangentLabel))));
        Assert.Equal("tangent", two.Kind);
        Assert.Equal([Ring, Bow], two.Items);
        Assert.Equal([-1.0], two.Values);
        var line = Assert.IsType<AddConstraintAction>(Assert.Single(Taps(DeviceKind.Mouse, CurvePanel(Bow, Line), DesktopUi.SketchItem("Tangent"))));
        Assert.Equal("tangent", line.Kind);
        Assert.Equal([Line, Bow], line.Items);
        Assert.Empty(line.Values);
        var on = Assert.IsType<AddConstraintAction>(Assert.Single(Taps(DeviceKind.Mouse, CurvePanel(Ring, B), DesktopUi.SketchItem("On curve"))));
        Assert.Equal("pointOn", on.Kind);
        Assert.Equal([B, Ring], on.Items);

        var panel = CurvePanel(Ring);
        using var ui = Opened(panel);
        Tap(ui, DeviceKind.Touch, DesktopUi.SketchItem("Radius"), panel);
        Assert.Equal("2.5", Assert.Single(ui.Pad!.Fields).Text);
        Tap(ui, DeviceKind.Touch, DesktopUi.PadItem("Apply"), panel);
        var radius = Assert.IsType<AddConstraintAction>(Assert.Single(ui.TakeActions()));
        Assert.Equal("radius", radius.Kind);
        Assert.Equal([Ring], radius.Items);
        Assert.Equal([2.5], radius.Values);
    }

    // Findings V59 and V60 of the codebase review of 2026-10-31 (TASK-0133): the window offers On curve
    // and Tangent only where the engine accepts them, and CosineAndSine reads the turn.
    [Fact]
    public void The_Kinds_On_A_Curve_Take_The_Item_Checks_Of_The_Engine()
    {
        static string[] Buttons(params Guid[] selection)
        {
            using var ui = Opened(CurvePanel(selection));
            string[] all = [.. DesktopUi.ConstraintButtons.Select(button => button.Label), DesktopUi.MovePointLabel, DesktopUi.ValueLabel, DesktopUi.DeleteLabel];
            return [.. all.Where(label => ui.ItemRectangle(DesktopUi.SketchItem(label)) is not null)];
        }

        // A is the centre of R1 and the start of A1, and an end of L1.
        Assert.Equal([DesktopUi.DeleteLabel], Buttons(Ring, A));
        Assert.Equal([DesktopUi.DeleteLabel], Buttons(Bow, A));
        Assert.Equal([DesktopUi.DeleteLabel], Buttons(Ring, Line));
        Assert.Equal(DesktopUi.CosineAndSine(30), DesktopUi.CosineAndSine(360000030));
        Assert.Equal(DesktopUi.CosineAndSine(1e20 % 360), DesktopUi.CosineAndSine(1e20));
    }

    private static readonly Guid AngleId = Guid.Parse("a1000000-0000-0000-0000-000000000000");
    private static readonly Guid TangentId = Guid.Parse("71000000-0000-0000-0000-000000000000");

    // Finding V56 (TASK-0133): the Value pad of an angle shows its degrees and gives the cosine and the
    // sine; a tangent of two curves has no Value.
    [Fact]
    public void The_Value_Of_An_Angle_Is_In_Degrees()
    {
        var lines = LinePanel().Active!;
        var angle = lines with { Constraints = [.. lines.Constraints, new SketchConstraintInfo(AngleId, "angle", [Line, Other], [0, 1], 0)] };
        var panel = new SketchPanel([angle], angle, SketchToolKind.Select, false, [AngleId], null);
        using var ui = Opened(panel);
        Tap(ui, DeviceKind.Mouse, DesktopUi.SketchItem(DesktopUi.ValueLabel), panel);

        Assert.Equal(DesktopUi.AngleUnit, ui.Pad!.Unit);
        Assert.Equal("90", Assert.Single(ui.Pad.Fields).Text);
        foreach (var key in new[] { "Clear", "4", "5", "Apply" })
            Tap(ui, DeviceKind.Mouse, DesktopUi.PadItem(key), panel);
        var set = Assert.IsType<SetConstraintValueAction>(Assert.Single(ui.TakeActions()));
        Assert.Equal(DesktopUi.CosineAndSine(45), set.Values);

        var curves = CurvePanel().Active!;
        var tangent = curves with { Constraints = [.. curves.Constraints, new SketchConstraintInfo(TangentId, "tangent", [Ring, Bow], [1], 0)] };
        var tangentPanel = new SketchPanel([tangent], tangent, SketchToolKind.Select, false, [TangentId], null);
        using var other = Opened(tangentPanel);
        Assert.Null(other.ItemRectangle(DesktopUi.SketchItem(DesktopUi.ValueLabel)));
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

    // ADR-0036 item 9 (Q4 A): Extrude opens a pad of one field, distance, with the text 10, and its
    // apply gives the extrude by that distance, by a tap and by a click.
    [Theory]
    [InlineData(DeviceKind.Mouse)]
    [InlineData(DeviceKind.Touch)]
    public void Extrude_Opens_A_Pad_Of_One_Distance(DeviceKind kind)
    {
        var panel = Panel();
        using var ui = Opened(panel);
        Tap(ui, kind, DesktopUi.SketchItem(DesktopUi.ExtrudeLabel), panel);

        var field = Assert.Single(ui.Pad!.Fields);
        Assert.Equal(("distance", "10"), (field.Name, field.Text));
        Tap(ui, kind, DesktopUi.PadItem("Apply"), panel);
        Assert.Equal(10.0, Assert.IsType<ExtrudeAction>(Assert.Single(ui.TakeActions())).Distance);
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

        // The pad of the next point has the scope of the chain and not of the selection, so a
        // change of the selection with the tool Line keeps it (V40, TASK-0106).
        ui.OpenNextPointPad(chainOpen: false);
        Step(ui, new SketchPanel([Sketch], Sketch, SketchToolKind.Line, false, [B], null));
        Step(ui, new SketchPanel([Sketch], Sketch, SketchToolKind.Line, false, [], null));
        Assert.NotNull(ui.Pad);
    }
}
