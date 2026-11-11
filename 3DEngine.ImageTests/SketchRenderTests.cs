using System.Numerics;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Numeric;
using ThreeDEngine.Core.Sketching;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0120 (phase R18f, TASK-0347): the sketch as in the render: the rows of its constraints and "Makes", "Next point" and
// "Extrude" on the ring and in the command line, and the plate with its label and its caption. ADR-0120 names this file in its
// enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class SketchRenderTests
{
    private static readonly Vector2 Display = new(1440, 900);
    private static readonly Camera Facing = new() { YawDegrees = 0, PitchDegrees = 0, Distance = 200, Target = (0, 0, 0) };
    private static readonly Guid SketchId = Guid.Parse("65000000-0000-0000-0000-000000000000");
    private static readonly Guid A = Guid.Parse("65000000-0000-0000-0000-000000000001");
    private static readonly Guid B = Guid.Parse("65000000-0000-0000-0000-000000000002");
    private static readonly Guid C = Guid.Parse("65000000-0000-0000-0000-000000000003");
    private static readonly Guid L1 = Guid.Parse("65000000-0000-0000-0000-000000000011");
    private static readonly Guid L2 = Guid.Parse("65000000-0000-0000-0000-000000000012");
    private static readonly Guid Horizontal = Guid.Parse("65000000-0000-0000-0000-000000000021");
    private static readonly Guid Length = Guid.Parse("65000000-0000-0000-0000-000000000022");
    private static readonly Guid Angle = Guid.Parse("65000000-0000-0000-0000-000000000023");
    private static readonly Guid Bracket = Guid.Parse("65000000-0000-0000-0000-000000000031");

    // Two lines from A: L1 to B along x, of 60, and L2 to C; a horizontal of L1, a distance of 60 on L1, and an angle of 60
    // degrees between L1 and L2.
    private static readonly SketchInfo Plate = new(
        SketchId, Datums.XY.Id, "Plate",
        [new(SketchId, 0, 0), new(A, -30, -20), new(B, 30, -20), new(C, -10, 14.64)],
        [new(L1, A, B), new(L2, A, C)],
        [], [],
        [
            new SketchConstraintInfo(Horizontal, "horizontal", [L1], [], 0),
            new SketchConstraintInfo(Length, "distance", [L1], [60], 0),
            new SketchConstraintInfo(Angle, "angle", [L1, L2], [0.5, 0.8660254037844386], 0.002),
        ],
        true, 2);

    private static readonly IReadOnlyList<HistoryStep> Makes = [new(Bracket, "Extrude", "Bracket", "correct", null, HistoryTap.SelectBody)];

    private static SketchPanel Panel(SketchToolKind tool = SketchToolKind.Select, IReadOnlyList<HistoryStep>? makes = null)
        => new([Plate], Plate, tool, false, [], null, makes ?? Makes);

    private static void Step(DesktopUi ui, SketchPanel panel, bool ring = false, SketchOverlay.Strokes? strokes = null, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, [], null, null, strokes, panel, ringCentre: ring ? new Vector2(760, 450) : null, ringBody: 80f);
    }

    private static IReadOnlyList<PanelAction> Tap(DesktopUi ui, SketchPanel panel, string item, bool ring = false)
    {
        ui.TakeActions();
        var at = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");
        var (x, y) = ((at.X + at.Z) / 2f, (at.Y + at.W) / 2f);
        PointerSample Mouse(PointerPhase phase) => new(DeviceKind.Mouse, 1, 0, x, y, 1, phase == PointerPhase.Move ? PointerButtons.None : PointerButtons.Left, KeyModifiers.None, phase, 0);
        Step(ui, panel, ring, null, Mouse(PointerPhase.Move));
        Step(ui, panel, ring, null, Mouse(PointerPhase.Down));
        Step(ui, panel, ring, null, Mouse(PointerPhase.Up));
        Step(ui, panel, ring);
        return ui.TakeActions();
    }

    // Item 1: the state as the render gives it, and the text of each row of a constraint: a value with three decimals and the
    // unit, an angle in degrees, and the residual of a constraint with no value.
    [Fact]
    public void A_Constraint_Row_Gives_Its_Value_Or_Its_Residual()
    {
        Assert.Equal("solved · 2 free", DesktopUi.SketchState(Plate));
        Assert.Equal("not solved", DesktopUi.SketchState(Plate with { Solved = false }));
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var names = SketchText.Labels(Plate);
        var rows = Plate.Constraints.Select(constraint => ui.ConstraintRow(Plate, constraint, names)).ToList();
        Assert.Equal(("C1 horizontal L1", "r 0"), rows[0]);
        Assert.Equal(("C2 distance L1", "60.000 mm"), rows[1]);
        Assert.Equal(("C3 angle L1 L2", "60.000 deg"), rows[2]);
    }

    // Item 1: the rows are the constraints only, and the row "Makes" follows them; a tap on a row of a constraint selects it, and
    // a tap on "Makes" ends the sketch and selects the body. With no body made, the row "Makes" does not show.
    [Fact]
    public void The_Rows_Are_The_Constraints_And_Makes()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Panel());
        Step(ui, Panel());
        foreach (var constraint in Plate.Constraints)
            Assert.NotNull(ui.ItemRectangle(DesktopUi.ConstraintRowItem(constraint.Id)));
        foreach (var item in new[] { "O", "P1", "P2", "P3", "L1", "L2" })
            Assert.Null(ui.ItemRectangle(DesktopUi.SketchItem(item)));
        var last = ui.ItemRectangle(DesktopUi.ConstraintRowItem(Angle))!.Value;
        var makes = ui.ItemRectangle(DesktopUi.MakesItem) ?? throw new InvalidOperationException("No row Makes.");
        Assert.True(makes.Y >= last.W - 0.5f && makes.W - makes.Y >= DesktopUi.MinimumControlHeight - 0.5f, $"The row Makes at {makes} is not a row under the last constraint at {last}.");

        Assert.Equal([new ToggleSketchItemAction(Length)], Tap(ui, Panel(), DesktopUi.ConstraintRowItem(Length)));
        Assert.Equal([new FinishSketchAction(), new SelectBodyAction(Bracket)], Tap(ui, Panel(), DesktopUi.MakesItem));

        Step(ui, Panel(makes: []));
        Step(ui, Panel(makes: []));
        Assert.Null(ui.ItemRectangle(DesktopUi.MakesItem));
    }

    // Item 2: the bodies that a sketch makes: an Extrude that reads it, and the feature that consumed that body, to a live
    // body; an Extrude of another sketch and a body that is not live give none.
    [Fact]
    public void Makes_Follows_The_Extrude_Of_The_Sketch_To_The_Live_Body()
    {
        var (extrude, cut, other, gone) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var features = new FeaturesInfo(
        [
            new FeatureInfo(SketchId, "CreateSketch", [Datums.XY.Id], null, "correct", null),
            new FeatureInfo(extrude, "Extrude", [SketchId], cut, "consumed", null),
            new FeatureInfo(cut, "Subtract", [extrude, other], null, "correct", null),
            new FeatureInfo(other, "Extrude", [Guid.NewGuid()], null, "correct", null),
            new FeatureInfo(gone, "Extrude", [SketchId], null, "correct", null),
        ]);
        IReadOnlyList<LiveBody> live = [new(cut, "Subtract", "Bracket", true), new(other, "Extrude", null, true)];

        var made = HistoryCache.Makes(features, live, SketchId);

        Assert.Equal([(cut, "Bracket", HistoryTap.SelectBody)], made.Select(step => (step.Id, step.Text, step.Tap)));
        Assert.Empty(HistoryCache.Makes(features, live, null));
        Assert.Empty(HistoryCache.Makes(null, live, SketchId));
    }

    // Item 2: the ring of a sketch holds "Next point" while the tool Line is active and "Extrude" before Finish, in the span of
    // a body ring; a tap on each opens its pad. The window does not show them while the ring holds the tools.
    [Fact]
    public void Next_Point_And_Extrude_Are_On_The_Ring_Of_A_Sketch()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var line = Panel(SketchToolKind.Line);
        Step(ui, line, ring: true);
        Step(ui, line, ring: true);
        Assert.Equal([DesktopUi.LineToolLabel, DesktopUi.SelectToolLabel, DesktopUi.CircleToolLabel, DesktopUi.ArcToolLabel, DesktopUi.NextPointLabel, DesktopUi.ExtrudeLabel, DesktopUi.FinishLabel], ui.RingLabels);
        var ring = ui.RingLayoutOfFrame ?? throw new InvalidOperationException("No ring.");
        var segments = ring.Segments(ui.RingLabels);
        Assert.True(segments[^1].To - segments[0].From <= DesktopUi.RingSpan + 0.001f, $"The ring of the sketch spans {segments[^1].To - segments[0].From} radians.");
        Assert.Null(ui.ItemRectangle(DesktopUi.SketchItem(DesktopUi.ExtrudeLabel)));
        Assert.Null(ui.ItemRectangle(DesktopUi.SketchItem(DesktopUi.NextPointLabel)));

        Tap(ui, line, DesktopUi.RingItem(DesktopUi.ExtrudeLabel), ring: true);
        Assert.Equal(DesktopUi.ExtrudeLabel, ui.Pad?.Title);
        ui.EndPad(null);
        Step(ui, line, ring: true);
        Tap(ui, line, DesktopUi.RingItem(DesktopUi.NextPointLabel), ring: true);
        Assert.Equal(DesktopUi.NextPointLabel, ui.Pad?.Title);

        using var select = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(select, Panel(), ring: true);
        Step(select, Panel(), ring: true);
        Assert.DoesNotContain(DesktopUi.NextPointLabel, select.RingLabels);
        Assert.Contains(DesktopUi.ExtrudeLabel, select.RingLabels);
    }

    // Item 2: the command line holds "Extrude" while a sketch is active, and its run opens the pad of an extrude.
    [Fact]
    public void Extrude_Is_A_Command_Of_An_Active_Sketch()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Panel());
        var extrude = Assert.Single(ui.Commands([], null), command => command.Name == DesktopUi.ExtrudeLabel);
        extrude.Run();
        Step(ui, Panel());
        Assert.Equal(DesktopUi.ExtrudeLabel, ui.Pad?.Title);
    }

    // Item 3: the plate of the active sketch is four corners on the screen around each of its points, and none for a sketch that
    // is not active; the label shows with a plate, and the caption only while the sketch is under its body.
    [Fact]
    public void The_Plate_Holds_The_Sketch_And_Shows_Its_Label_And_Its_Caption()
    {
        var strokes = SketchOverlay.Of([Plate], Facing, Display.X, Display.Y, new SketchOverlay.Focus(SketchId, [], null));
        var plate = strokes.Plate ?? throw new InvalidOperationException("No plate.");
        Assert.Equal(4, plate.Count);
        var (low, high) = (plate.Aggregate(Vector2.Min), plate.Aggregate(Vector2.Max));
        foreach (var point in Plate.Points.Where(point => point.Id != SketchId))
        {
            var pixel = SketchOverlay.Pixel(Plate, Facing, Display.X, Display.Y, (point.X, point.Y)) ?? throw new InvalidOperationException("Behind the eye.");
            Assert.True(pixel.X > low.X && pixel.X < high.X && pixel.Y > low.Y && pixel.Y < high.Y, $"The point at {pixel} is not inside the plate {low} {high}.");
        }

        Assert.Null(SketchOverlay.Of([Plate], Facing, Display.X, Display.Y).Plate);

        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Panel(), strokes: strokes);
        Step(ui, Panel(), strokes: strokes);
        var label = ui.ItemRectangle(DesktopUi.PlateLabelItem) ?? throw new InvalidOperationException("No label of the plate.");
        Assert.True(label.Y >= high.Y, $"The label at {label} is not under the plate, which ends at {high.Y}.");
        Assert.Null(ui.ItemRectangle(DesktopUi.PlateCaptionItem));

        Step(ui, Panel(), strokes: strokes with { Under = true });
        Step(ui, Panel(), strokes: strokes with { Under = true });
        var caption = ui.ItemRectangle(DesktopUi.PlateCaptionItem) ?? throw new InvalidOperationException("No caption.");
        Assert.True(caption.Y >= label.W, $"The caption at {caption} is not under the label at {label}.");
        Assert.True(caption.Z <= ((low.X + high.X) / 2f) + 0.5f || caption.X <= 16.5f, $"The caption at {caption} goes past the middle of the plate.");
    }
}
