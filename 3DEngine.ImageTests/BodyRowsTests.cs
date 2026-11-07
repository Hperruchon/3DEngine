using System.Numerics;
using Engine.Contracts;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0084 (the redesign, slice d, TASK-0267): the rows of the selected body, with no panel and no GPU. ADR-0084 names this
// file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class BodyRowsTests
{
    private static readonly Vector2 Display = new(1280, 720);
    private static readonly Guid Plate = Guid.Parse("62000000-0000-0000-0000-000000000000");
    private static readonly Guid Sketch = Guid.Parse("62000000-0000-0000-0000-000000000001");
    private static readonly Guid Width = Guid.Parse("62000000-0000-0000-0000-000000000002");
    private static readonly LiveBody Extrude = new(Plate, "Extrude", "Plate", true);

    private static readonly IReadOnlyList<EdgeValues.Value> Values =
    [
        new(EdgeValues.DistanceKey(Plate), "10 mm", new Vector2(500, 500), 10, Plate, null, null, "Distance"),
        new(EdgeValues.ConstraintKey(Width), "60 mm", new Vector2(400, 560), 60, Plate, Sketch, Width, "Length"),
    ];

    private static void Frame(DesktopUi ui, LiveBody? body, Vector2 display, Vector2? ring = null, Vector4 inset = default, params InputSample[] samples)
    {
        ui.Route(samples);
        ui.Frame(1f / 60f, display, Vector2.One, body is null ? [] : [body], body?.Id, null, ringCentre: ring, safeInsets: inset, values: body is null ? null : Values);
    }

    private static void Tap(DesktopUi ui, LiveBody body, Vector2 at, DeviceKind kind, Vector2? ring = null)
    {
        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        foreach (var phase in new[] { PointerPhase.Down, PointerPhase.Up })
            Frame(ui, body, Display, ring, default, new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, phase, 0));
        Frame(ui, body, Display, ring);
    }

    private static Vector2 Centre(Vector4 rectangle) => new((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);

    // Items 1 and 2: the rows are at the upper right, 16 pixels inside the display, 360 pixels wide: the name and the state,
    // then one row for each value, each 44 pixels high, and the rows are a part of the panels for a tap.
    [Fact]
    public void The_Rows_Of_A_Selected_Extrude_Are_At_The_Upper_Right()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, Extrude, Display);
        Frame(ui, Extrude, Display);

        var title = ui.ItemRectangle(DesktopUi.RowsTitleItem) ?? throw new InvalidOperationException("No rows.");
        Assert.Equal(new Vector4(Display.X - 16 - DesktopUi.RowsWidth, 16, Display.X - 16, 16 + DesktopUi.RowHeight), title);
        var top = title.W;
        foreach (var value in Values)
        {
            var row = ui.ItemRectangle(DesktopUi.RowItem(value.Key)) ?? throw new InvalidOperationException($"No row of {value.Label}.");
            Assert.Equal(new Vector4(title.X, top, title.Z, top + DesktopUi.RowHeight), row);
            var centre = Centre(row);
            Assert.True(ui.OverPanel(centre.X, centre.Y), $"A tap on the row {value.Label} reaches the scene.");
            top = row.W;
        }
    }

    // Item 1: no rows with no selection and on a portrait display; the safe area moves the rows.
    [Fact]
    public void The_Rows_Need_A_Selection_And_A_Landscape_Display_And_Keep_The_Safe_Area()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, null, Display);
        Frame(ui, null, Display);
        Assert.Null(ui.ItemRectangle(DesktopUi.RowsTitleItem));

        var portrait = new Vector2(390, 844);
        Frame(ui, Extrude, portrait);
        Frame(ui, Extrude, portrait);
        Assert.Null(ui.ItemRectangle(DesktopUi.RowsTitleItem));

        var inset = new Vector4(0f, 40f, 24f, 0f);
        Frame(ui, Extrude, Display, null, inset);
        Frame(ui, Extrude, Display, null, inset);
        var title = ui.ItemRectangle(DesktopUi.RowsTitleItem) ?? throw new InvalidOperationException("No rows.");
        Assert.Equal((Display.X - 16 - inset.Z, 16 + inset.Y), (title.Z, title.Y));
    }

    // Item 2: the state after the kind: "correct", or "failed" and the code; and "hidden".
    [Fact]
    public void The_State_Gives_Correct_Or_Failed_With_The_Code_And_Hidden()
    {
        Assert.Equal("Extrude · correct", DesktopUi.RowState(Extrude));
        Assert.Equal("Extrude · failed E-FEATURE-REBUILD · hidden",
            DesktopUi.RowState(Extrude with { Visible = false, Fault = new BodyFault("E-FEATURE-REBUILD", "The profile is open.") }));
    }

    // Item 3: a tap on the row of the distance asks the host for its pad, by touch and by mouse; a tap on the row of a
    // length opens the pad of that value, and the row and the chip are pending.
    [Theory]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Mouse)]
    public void A_Tap_On_A_Row_Opens_The_Pad_Of_Its_Value(DeviceKind kind)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, Extrude, Display);
        Frame(ui, Extrude, Display);

        Tap(ui, Extrude, Centre(ui.ItemRectangle(DesktopUi.RowItem(Values[0].Key))!.Value), kind);
        Assert.Equal([new EditDistanceAction(Plate)], ui.TakeActions());

        Tap(ui, Extrude, Centre(ui.ItemRectangle(DesktopUi.RowItem(Values[1].Key))!.Value), kind);
        Assert.Equal(DesktopUi.EdgeValueLabel, ui.Pad?.Title);
        Assert.Equal(Values[1].Key, ui.PendingValue);
        Assert.Empty(ui.TakeActions());
    }

    // Item 3: the rows are drawn over the ring, so a release on a row that lies on a segment gives the row only.
    [Fact]
    public void A_Tap_On_A_Row_Over_A_Segment_Gives_The_Row_Only()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var ring = new Vector2(1280, 0);
        Frame(ui, Extrude, Display, ring);
        Frame(ui, Extrude, Display, ring);
        var layout = DesktopUi.Layout(Display, ring, 3);
        string[] labels = [DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel];
        var row = ui.ItemRectangle(DesktopUi.RowItem(Values[1].Key)) ?? throw new InvalidOperationException("No row.");
        Vector2? both = null;
        for (var x = row.X + 2; x < row.Z && both is null; x += 4)
        {
            for (var y = row.Y + 2; y < row.W && both is null; y += 4)
            {
                if (layout.At(labels, new Vector2(x, y)) is not null)
                    both = new Vector2(x, y);
            }
        }

        Assert.True(both is not null, $"No point of the row {row} is on the ring at {layout.Centre}.");
        Tap(ui, Extrude, both.Value, DeviceKind.Touch, ring);

        Assert.Equal(DesktopUi.EdgeValueLabel, ui.Pad?.Title);
        Assert.Empty(ui.TakeActions());
    }

    // Item 4: a chip that the placement puts on the rows goes under them, at the same x.
    [Fact]
    public void A_Chip_On_The_Rows_Goes_Under_Them()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        IReadOnlyList<EdgeValues.Value> values = [Values[0] with { At = new Vector2(Display.X - 100, 90) }, Values[1]];
        for (var frame = 0; frame < 2; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, Display, Vector2.One, [Extrude], Plate, null, values: values);
        }

        var last = ui.ItemRectangle(DesktopUi.RowItem(Values[1].Key)) ?? throw new InvalidOperationException("No row.");
        var chip = ui.ItemRectangle(Values[0].Key) ?? throw new InvalidOperationException("No chip.");
        Assert.True(chip.Y >= last.W, $"The chip at {chip} is on the rows, which end at {last.W}.");
    }

    // Finding V128 of the codebase review of 2026-11-07 (b) (TASK-0271): two chips that go under the rows stay apart, and a tap
    // on the length opens the pad of the length. Until TASK-0271 both went to one place, and the tap opened the distance.
    [Fact]
    public void Two_Chips_Under_The_Rows_Stay_Apart_And_A_Tap_Opens_The_Visible_One()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        IReadOnlyList<EdgeValues.Value> values = [Values[0] with { At = new Vector2(1100, 70) }, Values[1] with { At = new Vector2(1100, 120) }];
        void Step(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [Extrude], Plate, null, values: values);
        }

        Step();
        Step();
        var (distance, length) = (ui.ItemRectangle(Values[0].Key)!.Value, ui.ItemRectangle(Values[1].Key)!.Value);
        Assert.False(distance.X < length.Z && length.X < distance.Z && distance.Y < length.W && length.Y < distance.W, $"The chips {distance} and {length} cover each other.");
        var at = Centre(length);
        Step(new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
        Step(new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));
        Step();

        Assert.Equal(DesktopUi.EdgeValueLabel, ui.Pad?.Title);
        Assert.Equal(Values[1].Key, ui.PendingValue);
    }

    // Finding V129 of the codebase review of 2026-11-07 (b) (TASK-0271): the name and the state each fit the column, on two
    // lines, for a failed and hidden body; only "failed" and the code are red. Until TASK-0271 the name was "...".
    [Fact]
    public void The_Name_And_The_State_Of_A_Failed_Hidden_Body_Each_Fit_The_Column()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var body = Extrude with { Visible = false, Fault = new BodyFault("E-FEATURE-REBUILD", "The profile is open.") };
        Frame(ui, body, Display);

        var (name, state) = ui.RowTitle(body);

        Assert.Equal("Plate", name);
        Assert.Contains(("failed E-FEATURE-REBUILD", true), state);
        Assert.All(state.Where(part => part.Text != "failed E-FEATURE-REBUILD"), part => Assert.False(part.Red));
        Assert.Equal([("Extrude · ", false), ("correct", false)], ui.RowTitle(Extrude).State);
        Assert.True(ui.TextSize(string.Concat(state.Select(part => part.Text))).X <= DesktopUi.RowsWidth, "The state is wider than the column.");
    }

    // The review of Codex of v0.252 (TASK-0271): the rows end 16 pixels above the anchors; a value with no room keeps its chip
    // only. Until TASK-0271 sixteen values went past the bottom of the display.
    [Fact]
    public void The_Rows_End_Above_The_Anchors()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        IReadOnlyList<EdgeValues.Value> values = [.. Enumerable.Range(0, 16).Select(i => new EdgeValues.Value(EdgeValues.ConstraintKey(Guid.NewGuid()), $"{i} mm", new Vector2(300, 300 + (i * 4)), i, Plate, Sketch, Guid.NewGuid(), "Length"))];
        for (var frame = 0; frame < 2; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, Display, Vector2.One, [Extrude], Plate, null, values: values);
        }

        var rows = values.Select(value => ui.ItemRectangle(DesktopUi.RowItem(value.Key))).OfType<Vector4>().ToList();
        Assert.InRange(rows.Count, 1, values.Count - 1);
        Assert.True(rows.Max(row => row.W) <= Display.Y - 32 - DesktopUi.AnchorSize, $"A row ends at {rows.Max(row => row.W)}.");
    }

    // The review of Codex of v0.252 (TASK-0271), item 6 of ADR-0084: while the rows show, they take the place of the window of
    // the live sketches, which was over them; with no selection that window shows as before.
    [Fact]
    public void The_Rows_Take_The_Place_Of_The_Window_Of_The_Live_Sketches()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var sketch = new Engine.Core.Queries.SketchInfo(Sketch, Engine.Contracts.Sketch.Datums.XY.Id, "Plate", [], [], [], [], [], true, 0);
        var panel = new SketchPanel([sketch], null, ThreeDEngine.Core.Sketching.SketchToolKind.Line, false, [], null);
        void Step(LiveBody? selected)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, Display, Vector2.One, [Extrude], selected?.Id, null, sketchPanel: panel, values: selected is null ? null : Values);
        }

        Step(Extrude);
        Step(Extrude);
        var withRows = ui.PanelRectangles.Count;
        var row = ui.ItemRectangle(DesktopUi.RowItem(Values[0].Key)) ?? throw new InvalidOperationException("No row.");
        Step(null);
        Step(null);

        Assert.Equal(withRows + 1, ui.PanelRectangles.Count);
        Assert.DoesNotContain(ui.PanelRectangles.Take(withRows), panel => panel.X < row.Z && row.X < panel.Z && panel.Y < row.W && row.Y < panel.W);
    }
}
