using System.Numerics;
using Engine.Contracts;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0084 (the redesign, slice d, TASK-0267): the rows of the selected body, with no panel and no GPU. ADR-0084 names this
// file in its enforced-by field. ADR-0099 (the redesign, slice p, TASK-0313): the rows take the place of the panel at the
// upper left; ADR-0099 names this file in its enforced-by field too.
[Collection(DearImGui.Collection)]
public sealed class BodyRowsTests
{
    private static readonly Vector2 Display = new(1280, 720);
    private static readonly Guid Plate = Guid.Parse("62000000-0000-0000-0000-000000000000");
    private static readonly Guid Sketch = Guid.Parse("62000000-0000-0000-0000-000000000001");
    private static readonly Guid Width = Guid.Parse("62000000-0000-0000-0000-000000000002");
    private static readonly LiveBody Extrude = new(Plate, "Extrude", "Plate", true);
    private static readonly LiveBody Box = new(Guid.Parse("62000000-0000-0000-0000-000000000003"), "Box", null, true);

    // The top of the panel, under the top bar of ADR-0093 item 1: 16, the height of the top bar, and 8.
    private const float PanelTop = 16f + 32f + 8f;

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

    private static KeySample Key(InputKey key, bool down = true) => new(key, down, KeyModifiers.None, 0);

    // ADR-0099 item 1: the rows are at the upper left in the place of the panel, 16 pixels inside the display and under the
    // top bar, 360 pixels wide: the name and the state, then one row for each value, each 44 pixels high, and the rows are a
    // part of the panels for a tap. The panel does not show.
    [Fact]
    public void The_Rows_Of_A_Selected_Extrude_Take_The_Place_Of_The_Panel()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, Extrude, Display);
        Frame(ui, Extrude, Display);

        var title = ui.ItemRectangle(DesktopUi.RowsTitleItem) ?? throw new InvalidOperationException("No rows.");
        Assert.Equal(new Vector4(16, PanelTop, 16 + DesktopUi.RowsWidth, PanelTop + DesktopUi.RowHeight), title);
        var top = title.W;
        foreach (var value in Values)
        {
            var row = ui.ItemRectangle(DesktopUi.RowItem(value.Key)) ?? throw new InvalidOperationException($"No row of {value.Label}.");
            Assert.Equal(new Vector4(title.X, top, title.Z, top + DesktopUi.RowHeight), row);
            var centre = Centre(row);
            Assert.True(ui.OverPanel(centre.X, centre.Y), $"A tap on the row {value.Label} reaches the scene.");
            top = row.W;
        }

        Assert.Empty(ui.PanelRectangles);
        Assert.Null(ui.ItemRectangle(DesktopUi.ShowAllLabel));
    }

    // Item 1: no rows with no selection and on a portrait display, and the panel shows; the safe area moves the rows.
    [Fact]
    public void The_Rows_Need_A_Selection_And_A_Landscape_Display_And_Keep_The_Safe_Area()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, null, Display);
        Frame(ui, null, Display);
        Assert.Null(ui.ItemRectangle(DesktopUi.RowsTitleItem));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.ShowAllLabel));

        var portrait = new Vector2(390, 844);
        Frame(ui, Extrude, portrait);
        Frame(ui, Extrude, portrait);
        Assert.Null(ui.ItemRectangle(DesktopUi.RowsTitleItem));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.ShowAllLabel));

        var inset = new Vector4(30f, 40f, 24f, 0f);
        Frame(ui, Extrude, Display, null, inset);
        Frame(ui, Extrude, Display, null, inset);
        var title = ui.ItemRectangle(DesktopUi.RowsTitleItem) ?? throw new InvalidOperationException("No rows.");
        Assert.Equal((16 + inset.X, PanelTop + inset.Y), (title.X, title.Y));
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

    // ADR-0099 item 1: on a landscape display the ring of a body is right of its centre, and the centre is at least the reach
    // of the ring from the left edge, so no segment is on the rows at the left. Until TASK-0313 the rows were at the right,
    // and a tap on a row over a segment gave the row only.
    [Fact]
    public void No_Segment_Of_The_Ring_Is_On_The_Rows()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, Extrude, Display);
        Frame(ui, Extrude, Display);
        var rows = ui.RowsArea ?? throw new InvalidOperationException("No rows.");
        string[] labels = [DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel, DesktopUi.SketchOnFaceButtonLabel];
        foreach (var centre in new[] { new Vector2(0, 0), new Vector2(0, 720), new Vector2(200, 300), new Vector2(640, 360) })
        {
            var layout = DesktopUi.Layout(Display, centre, labels.Length);
            for (var x = rows.X; x < rows.Z; x += 4)
            {
                for (var y = rows.Y; y < rows.W; y += 4)
                    Assert.Null(layout.At(labels, new Vector2(x, y)));
            }
        }
    }

    // Item 4: a chip that the placement puts on the rows goes under them, at the same x.
    [Fact]
    public void A_Chip_On_The_Rows_Goes_Under_Them()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        IReadOnlyList<EdgeValues.Value> values = [Values[0] with { At = new Vector2(100, 90) }, Values[1]];
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
        IReadOnlyList<EdgeValues.Value> values = [Values[0] with { At = new Vector2(200, 70) }, Values[1] with { At = new Vector2(200, 120) }];
        void Step(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [Extrude], Plate, null, values: values);
        }

        Step();
        Step();
        var (distance, length) = (ui.ItemRectangle(Values[0].Key)!.Value, ui.ItemRectangle(Values[1].Key)!.Value);
        var rows = ui.RowsArea!.Value;
        Assert.True(distance.Y >= rows.W && length.Y >= rows.W, $"The chips {distance} and {length} are on the rows, which end at {rows.W}.");
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

    // ADR-0084 item 6 and ADR-0099 item 1: while the rows show, neither the panel nor the window of the live sketches shows;
    // with no selection both show again.
    [Fact]
    public void The_Rows_Take_The_Place_Of_The_Panel_And_Of_The_Window_Of_The_Live_Sketches()
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
        Assert.Empty(ui.PanelRectangles);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RowItem(Values[0].Key)));
        Step(null);
        Step(null);

        Assert.Equal(2, ui.PanelRectangles.Count);
    }

    // ADR-0099 item 2: a tap on the first row opens the name field with the name selected; a typed name and Enter give one
    // rename, and the field closes.
    [Fact]
    public void A_Tap_On_The_Name_Opens_A_Field_And_Enter_Renames_The_Body()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, Extrude, Display);
        Frame(ui, Extrude, Display);
        Tap(ui, Extrude, Centre(ui.ItemRectangle(DesktopUi.RowsTitleItem)!.Value), DeviceKind.Mouse);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RenameItem));

        // The focus of ImGui takes the field in a frame after the request, and the field is active in the frame after that.
        Frame(ui, Extrude, Display);
        Frame(ui, Extrude, Display);
        Frame(ui, Extrude, Display, null, default, new TextSample("Bracket", 0));
        Frame(ui, Extrude, Display, null, default, Key(InputKey.Enter));
        Frame(ui, Extrude, Display, null, default, Key(InputKey.Enter, false));
        Frame(ui, Extrude, Display);

        Assert.Equal([new RenameBodyAction(Plate, "Bracket")], ui.TakeActions());
        Assert.Null(ui.ItemRectangle(DesktopUi.RenameItem));
    }

    // ADR-0099 item 2: Escape closes the name field with no command, and the keys go back to the scene.
    [Fact]
    public void Escape_Closes_The_Name_Field_With_No_Command()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, Extrude, Display);
        Frame(ui, Extrude, Display);
        Tap(ui, Extrude, Centre(ui.ItemRectangle(DesktopUi.RowsTitleItem)!.Value), DeviceKind.Touch);
        Frame(ui, Extrude, Display);
        Frame(ui, Extrude, Display);
        Frame(ui, Extrude, Display, null, default, new TextSample("Other", 0));
        Frame(ui, Extrude, Display, null, default, Key(InputKey.Escape));
        Frame(ui, Extrude, Display, null, default, Key(InputKey.Escape, false));
        Frame(ui, Extrude, Display);

        Assert.Empty(ui.TakeActions());
        Assert.Null(ui.ItemRectangle(DesktopUi.RenameItem));
        Assert.Contains(ui.Route([Key(InputKey.H)]), sample => sample is KeySample { Key: InputKey.H });
    }

    // ADR-0099 items 3 and 4: the rows hold the list of a cut while another body is visible, then the lines of the selected
    // face and edge, the size while the dimensions show, and the message of the document, in the column of the rows; a choice
    // in the list gives the cut.
    [Fact]
    public void The_Rows_Hold_The_Cut_The_Face_The_Edge_And_The_Message_Of_The_Document()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var face = new FaceLine("Face +z of Plate", true, [], "Edge 3 of Plate");
        var document = new DocumentLine("bracket.3de", "Saved bracket.3de");
        ui.ShowDimensionsOn();
        void Step(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [Extrude, Box], Plate, document, face: face, size: "Size 60 x 40 x 10 mm", values: Values);
        }

        Step();
        Step();
        var last = ui.ItemRectangle(DesktopUi.RowItem(Values[1].Key))!.Value;
        var cut = ui.ItemRectangle(DesktopUi.CutLabel) ?? throw new InvalidOperationException("No list of a cut.");
        Assert.True(cut.Y >= last.W && cut.X >= last.X && cut.Z <= last.Z + 0.5f, $"The list of a cut at {cut} is not under the rows at {last}.");
        var below = cut.W;
        foreach (var item in new[] { DesktopUi.FaceLabel, DesktopUi.EdgeLabel, DesktopUi.SizeLabel, DesktopUi.DocumentMessageLabel })
        {
            var line = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No line {item}.");
            Assert.True(line.Y >= below && line.X == last.X, $"The line {item} at {line} is not under {below}.");
            below = line.W;
        }

        Assert.Empty(ui.PanelRectangles);
        var at = Centre(cut);
        Step(new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
        Step(new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));
        Step();
        Step();
        var tool = ui.ItemRectangle(DesktopUi.CutItem(Box.Id)) ?? throw new InvalidOperationException("No open list of a cut.");
        Assert.True(ui.OverPanel(0, 0), "A tap on the scene reaches it while the list is open.");
        var choice = Centre(tool);
        Step(new PointerSample(DeviceKind.Touch, 1, 3, choice.X, choice.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
        Step(new PointerSample(DeviceKind.Touch, 1, 3, choice.X, choice.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));
        Step();

        Assert.Equal([new CutBodyAction(Plate, Box.Id)], ui.TakeActions());
    }

    // ADR-0099 item 4: the command line holds the controls of the panel that the rows hide: Rename, a cut with each other
    // visible body, the armed tap on a face, and the toggles of the dimensions; Hidden too only while the dimensions show.
    [Fact]
    public void The_Command_Line_Holds_The_Controls_That_The_Rows_Hide()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        LiveBody[] bodies = [Extrude, Box, Box with { Id = Guid.NewGuid(), Name = "Hidden", Visible = false }];
        var names = ui.Commands(bodies, Plate).Select(command => command.Name).ToList();
        Assert.Contains(DesktopUi.RenameLabel, names);
        Assert.Contains($"{DesktopUi.CutLabel} Box", names);
        Assert.DoesNotContain($"{DesktopUi.CutLabel} Hidden", names);
        Assert.Contains(DesktopUi.SketchOnFaceLabel, names);
        Assert.Contains(DesktopUi.DimensionsLabel, names);
        Assert.DoesNotContain(DesktopUi.HiddenTooLabel, names);

        ui.Commands(bodies, Plate).Single(command => command.Name == DesktopUi.DimensionsLabel).Run();
        Assert.True(ui.ShowDimensions);
        Assert.Contains(DesktopUi.HiddenTooLabel, ui.Commands(bodies, Plate).Select(command => command.Name));
        ui.Commands(bodies, Plate).Single(command => command.Name == $"{DesktopUi.CutLabel} Box").Run();
        Assert.Equal([new CutBodyAction(Plate, Box.Id)], ui.TakeActions());
    }
}
