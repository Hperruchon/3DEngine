using System.Numerics;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;
using ThreeDEngine.Core.Sketching;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Numeric;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// The pad in the panel of the desktop (ADR-0032 items 3 to 6, TASK-0094), with no GPU: the
// buttons that open it, a tap and a click on its buttons, the keyboard that gives the same
// states, the keys that it takes while it is open, and the size of its controls.
[Collection(DearImGui.Collection)]
public sealed class NumericPadUiTests
{
    // ADR-0099 item 1 (the redesign, slice p, TASK-0313): on a landscape display the rows of a selected body take the place of
    // the panel, so these tests of the panel use a portrait display, where the panel keeps the controls of the selected body.
    private static readonly Vector2 Display = new(720, 1280);

    private static readonly LiveBody Body = new(Guid.Parse("22222222-0000-0000-0000-000000000000"), "Box", null, true);

    // A body of the kind Extrude, which the panel lists only while it is the selection, so each
    // other test draws the panel of one body (ADR-0049 item 8).
    private static readonly LiveBody Extruded = new(Guid.Parse("44444444-0000-0000-0000-000000000000"), "Extrude", null, true);

    private static IReadOnlyList<InputSample> Step(DesktopUi ui, Guid? selection, params InputSample[] frame)
    {
        var intents = ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, selection == Extruded.Id ? [Body, Extruded] : [Body], selection);
        return intents;
    }

    private static DesktopUi Opened(Guid? selection = null, string unit = Engine.Contracts.Document.Millimetre)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", unit);
        Step(ui, selection);
        Step(ui, selection);
        return ui;
    }

    private static (double X, double Y) Centre(DesktopUi ui, string item)
    {
        var rectangle = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"The panel has no control {item}.");
        return ((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);
    }

    private static void Tap(DesktopUi ui, DeviceKind kind, string item, Guid? selection = null)
    {
        var at = Centre(ui, item);
        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        Step(ui, selection, new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Down, 0));
        Step(ui, selection, new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Up, 0));
        Step(ui, selection);
        Step(ui, selection);
    }

    // Phase K5b (ADR-0076 item 7, TASK-0244): a tap on the list "New", and then a tap on its entry.
    private static void OpenFromNew(DesktopUi ui, DeviceKind kind, string entry, Guid? selection = null)
    {
        Tap(ui, kind, DesktopUi.NewLabel, selection);
        Tap(ui, kind, entry, selection);
    }

    private static DesktopUi WithNewBoxPad()
    {
        var ui = Opened();
        OpenFromNew(ui, DeviceKind.Mouse, DesktopUi.NewBoxLabel);
        Assert.NotNull(ui.Pad);
        Step(ui, null);
        return ui;
    }

    [Fact]
    public void New_Box_Opens_The_Pad_And_An_Apply_Gives_The_Typed_Sizes()
    {
        using var ui = WithNewBoxPad();

        foreach (var label in new[] { "Clear", "2", "5", "Next", "Clear", "2", "*", "pi", "Apply" })
            Tap(ui, DeviceKind.Touch, DesktopUi.PadItem(label));

        Assert.Equal([new CreateBoxAction(25, 2 * Math.PI, 10)], ui.TakeActions());
        Assert.NotNull(ui.Pad);
        ui.EndPad(null);
        Assert.Null(ui.Pad);
    }

    // P8e (TASK-0119): the pad shows the length unit that the host gives, not a constant. The
    // engine permits the millimetre only (ADR-0015 item 7), so the test gives another text.
    [Fact]
    public void Each_Pad_Shows_The_Length_Unit_Of_The_Document()
    {
        using var ui = Opened(Body.Id, "in");

        OpenFromNew(ui, DeviceKind.Mouse, DesktopUi.NewBoxLabel, Body.Id);
        Assert.Equal(DesktopUi.NewBoxLabel, ui.Pad?.Title);
        Assert.Equal("in", ui.Pad!.Unit);
        Tap(ui, DeviceKind.Mouse, DesktopUi.PadItem("Cancel"), Body.Id);
        Assert.Null(ui.Pad);

        Tap(ui, DeviceKind.Mouse, DesktopUi.MoveLabel, Body.Id);
        Assert.Equal(DesktopUi.MoveLabel, ui.Pad?.Title);
        Assert.Equal("in", ui.Pad!.Unit);
    }

    // ADR-0032 item 3: a script of buttons and a script of keys give the same states and the
    // same action.
    [Fact]
    public void The_Keyboard_Gives_The_States_And_The_Action_Of_The_Buttons()
    {
        using var buttons = WithNewBoxPad();
        foreach (var label in new[] { "Clear", "1", ".", "5", "Next", "Delete", "Delete", "sqrt(", "4", ")", "Next", "-", "3" })
            Tap(buttons, DeviceKind.Mouse, DesktopUi.PadItem(label));

        using var keys = WithNewBoxPad();
        Step(keys, null, new KeySample(InputKey.Backspace, true, KeyModifiers.None, 0), new KeySample(InputKey.Backspace, true, KeyModifiers.None, 0));
        Step(keys, null, new TextSample("1.5", 0), new KeySample(InputKey.Tab, true, KeyModifiers.None, 0));
        Step(keys, null, new KeySample(InputKey.Backspace, true, KeyModifiers.None, 0), new KeySample(InputKey.Backspace, true, KeyModifiers.None, 0));
        Step(keys, null, new TextSample("sqrt(4)", 0), new KeySample(InputKey.Tab, true, KeyModifiers.None, 0), new TextSample("-3", 0));

        Assert.Equal(buttons.Pad!.Fields, keys.Pad!.Fields);
        Assert.Equal(buttons.Pad.Active, keys.Pad.Active);

        Tap(buttons, DeviceKind.Touch, DesktopUi.PadItem("Apply"));
        Step(keys, null, new KeySample(InputKey.Enter, true, KeyModifiers.None, 0));
        var action = Assert.Single(buttons.TakeActions());
        Assert.Equal(new CreateBoxAction(1.5, 2, 10 - 3), action);
        Assert.Equal([action], keys.TakeActions());
    }

    // ADR-0032 item 6: while the pad is open, no key and no text reaches the scene; Escape
    // closes it, and then the keys reach the scene again.
    [Fact]
    public void The_Pad_Takes_The_Keys_While_It_Is_Open()
    {
        using var ui = WithNewBoxPad();
        var arrow = new KeySample(InputKey.ArrowLeft, true, KeyModifiers.None, 0);
        var undo = new KeySample(InputKey.Z, true, KeyModifiers.Control, 0);

        Assert.Empty(Step(ui, null, arrow, undo, new TextSample("h", 0)));
        Step(ui, null, new KeySample(InputKey.Escape, true, KeyModifiers.None, 0));

        Assert.Null(ui.Pad);
        Assert.Equal([arrow, undo], Step(ui, null, arrow, undo));
    }

    [Fact]
    public void Move_Shows_Only_With_A_Selection_And_Gives_The_Offsets_Of_The_Selected_Body()
    {
        using var none = Opened();
        Assert.Null(none.ItemRectangle(DesktopUi.MoveLabel));

        using var ui = Opened(Body.Id);
        Tap(ui, DeviceKind.Touch, DesktopUi.MoveLabel, Body.Id);
        Assert.Equal(["dx", "dy", "dz"], ui.Pad!.Fields.Select(field => field.Name));
        Step(ui, Body.Id);
        foreach (var label in new[] { "Clear", "5", "Next", "Next", "Clear", "-", "2", "Apply" })
            Tap(ui, DeviceKind.Mouse, DesktopUi.PadItem(label), Body.Id);

        Assert.Equal([new MoveBodyAction(Body.Id, 5, 0, -2)], ui.TakeActions());
    }

    // ADR-0049 item 8 (TASK-0163): Set distance shows only for a selected extrude and asks the host
    // for the pad; the pad starts with the distance in effect, and an apply gives the new distance.
    [Fact]
    public void Set_Distance_Shows_Only_For_An_Extrude_And_Gives_The_New_Distance()
    {
        using var box = Opened(Body.Id);
        Assert.Null(box.ItemRectangle(DesktopUi.SetDistanceLabel));

        using var ui = Opened(Extruded.Id);
        Tap(ui, DeviceKind.Touch, DesktopUi.SetDistanceLabel, Extruded.Id);
        Assert.Equal([new EditDistanceAction(Extruded.Id)], ui.TakeActions());

        ui.OpenDistancePad(Extruded.Id, 5);
        Step(ui, Extruded.Id);
        Assert.Equal([new PadField("distance", "5")], ui.Pad!.Fields);
        foreach (var label in new[] { "Clear", "1", "2", "Apply" })
            Tap(ui, DeviceKind.Mouse, DesktopUi.PadItem(label), Extruded.Id);

        Assert.Equal([new SetDistanceAction(Extruded.Id, 12)], ui.TakeActions());
    }

    // ADR-0032 item 8: a rejection of the command stays in the pad, and a bad field gives no action.
    [Fact]
    public void A_Rejection_And_A_Bad_Field_Keep_The_Pad_Open()
    {
        using var ui = WithNewBoxPad();

        ui.EndPad("E-GEOM-INVALID-PARAM: CreateBox requires sizeX, sizeY, sizeZ all strictly greater than zero.");
        Assert.StartsWith("E-GEOM-INVALID-PARAM", ui.Pad!.Message, StringComparison.Ordinal);
        // The line of the message moves the buttons down, so one frame places them first.
        Step(ui, null);

        Tap(ui, DeviceKind.Mouse, DesktopUi.PadItem("("));
        Tap(ui, DeviceKind.Mouse, DesktopUi.PadItem("Apply"));
        Assert.Empty(ui.TakeActions());
        Assert.StartsWith("x: ", ui.Pad!.Message, StringComparison.Ordinal);
    }

    // V35 of the second codebase review of 2026-10-28: the pad stays inside a tablet display, so a
    // person with touch only reaches Apply. Apply, Cancel, Delete and the fields were outside it.
    [Theory]
    [InlineData(768f, 1024f, false)]
    [InlineData(1024f, 768f, false)]
    [InlineData(768f, 1024f, true)]
    [InlineData(1024f, 768f, true)]
    [InlineData(1280f, 720f, true)]
    public void The_Pad_Stays_Inside_The_Display(float width, float height, bool bySketch)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        // The pad of a sketch opens while a sketch is active and the tool Line has an open
        // chain, and it closes when that state changes (finding V40, TASK-0106).
        var id = Guid.Parse("50000000-0000-0000-0000-000000000000");
        var sketch = new SketchInfo(id, Datums.XY.Id, null, [new SketchPoint(id, 0, 0)], [], [], [], [], true, 0);
        SketchPanel? panel = bySketch ? new SketchPanel([sketch], sketch, SketchToolKind.Line, true, [], null) : null;
        void Draw() => ui.Frame(1f / 60f, display, Vector2.One, [Body], null, null, null, panel);
        Draw();
        if (bySketch)
            ui.OpenNextPointPad(chainOpen: true);
        else
        {
            foreach (var item in new[] { DesktopUi.NewLabel, DesktopUi.NewBoxLabel })
            {
                var at = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"The panel has no {item}.");
                var (x, y) = ((at.X + at.Z) / 2, (at.Y + at.W) / 2);
                ui.Route([new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0)]);
                Draw();
                ui.Route([new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0)]);
                Draw();
                Draw();
                // ADR-0091 (TASK-0292): the popup of the list takes two more frames to reach its size; with the fonts of the
                // redesign, a tap in its second frame fell outside it.
                Draw();
                Draw();
            }
        }

        Draw();
        Draw();

        Assert.NotNull(ui.Pad);
        var items = DesktopUi.PadRows.SelectMany(row => row).Concat(ui.Pad!.Fields.Select(field => field.Name)).Select(DesktopUi.PadItem);
        foreach (var item in items)
        {
            var rectangle = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"The pad has no control {item}.");
            Assert.True(rectangle.X >= 0 && rectangle.Y >= 0 && rectangle.Z <= width && rectangle.W <= height, $"{item} is at {rectangle} on a display of {width} x {height}.");
        }
    }

    // V32 of the second codebase review of 2026-10-28: the Move pad moved the body that was
    // selected when it opened. It closes when the selection changes or its body leaves the list.
    [Fact]
    public void The_Move_Pad_Closes_When_The_Selection_Changes()
    {
        var other = new LiveBody(Guid.Parse("33333333-0000-0000-0000-000000000000"), "Box", null, true);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        void Draw(Guid? selection, params LiveBody[] bodies) => ui.Frame(1f / 60f, Display, Vector2.One, bodies, selection);
        Draw(Body.Id, Body, other);
        Draw(Body.Id, Body, other);
        var at = Centre(ui, DesktopUi.MoveLabel);
        ui.Route([new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Down, 0)]);
        Draw(Body.Id, Body, other);
        ui.Route([new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Up, 0)]);
        Draw(Body.Id, Body, other);
        Assert.NotNull(ui.Pad);

        Draw(Body.Id, Body, other);
        Assert.NotNull(ui.Pad);
        Draw(other.Id, Body, other);
        Assert.Null(ui.Pad);
        Assert.Empty(ui.TakeActions());

        // A body that leaves the list ends the pad too, also when the host keeps its selection.
        Draw(other.Id, Body, other);
        at = Centre(ui, DesktopUi.MoveLabel);
        ui.Route([new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0)]);
        Draw(other.Id, Body, other);
        ui.Route([new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0)]);
        Draw(other.Id, Body, other);
        Assert.NotNull(ui.Pad);
        Draw(other.Id, Body);
        Assert.Null(ui.Pad);

        // New box names no selection, so it stays open.
        OpenFromNew(ui, DeviceKind.Mouse, DesktopUi.NewBoxLabel, Body.Id);
        Assert.NotNull(ui.Pad);
        Step(ui, null);
        Assert.NotNull(ui.Pad);
    }

    // V34: two presses of Enter in one frame gave two CreateBoxAction. The pad ignores a second
    // apply until the host ends it, and a rejection lets the person apply again.
    [Fact]
    public void A_Second_Apply_Before_The_End_Of_The_Pad_Gives_Nothing()
    {
        using var ui = WithNewBoxPad();
        var enter = new KeySample(InputKey.Enter, true, KeyModifiers.None, 0);

        Step(ui, null, enter, enter);
        Tap(ui, DeviceKind.Touch, DesktopUi.PadItem("Apply"));
        Assert.Single(ui.TakeActions());

        ui.EndPad("E-GEOM-INVALID-PARAM: no");
        Step(ui, null, enter, enter);
        Assert.Single(ui.TakeActions());
    }

    // V33: while the pad is open, the text fields of the panel are disabled. A press on the file
    // name field made it active, so it kept the keys after the pad closed.
    [Fact]
    public void The_Text_Fields_Are_Disabled_While_The_Pad_Is_Open()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var line = new DocumentLine("plate.3de", null);
        void Draw(params InputSample[] frame)
        {
            ui.Route(frame);
            ui.Frame(1f / 60f, Display, Vector2.One, [Body], null, line);
        }

        Draw();
        Draw();
        foreach (var item in new[] { DesktopUi.NewLabel, DesktopUi.NewBoxLabel })
        {
            var box = Centre(ui, item);
            Draw(new PointerSample(DeviceKind.Mouse, 1, 0, box.X, box.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Down, 0));
            Draw(new PointerSample(DeviceKind.Mouse, 1, 0, box.X, box.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Up, 0));
            Draw();
        }
        Assert.NotNull(ui.Pad);

        // ADR-0100 item 1 (TASK-0314): the panel has no file name field, so the test uses the filter of the bodies.
        var field = Centre(ui, DesktopUi.FilterLabel);
        Draw(new PointerSample(DeviceKind.Mouse, 1, 0, field.X, field.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Down, 0));
        Draw(new PointerSample(DeviceKind.Mouse, 1, 0, field.X, field.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Up, 0));
        Draw();
        Draw(new KeySample(InputKey.Escape, true, KeyModifiers.None, 0));
        Draw();
        Assert.Null(ui.Pad);

        var key = new KeySample(InputKey.H, true, KeyModifiers.None, 0);
        Assert.Equal([key], ui.Route([key]));
    }

    // Phase K5b (ADR-0076 item 7, TASK-0244): the list "New" holds Box and Cylinder, each at least 44 pixels high; Cylinder
    // opens a pad of two values, its radius and its height, for a finger and for the mouse, and an apply gives one action
    // with the typed values.
    [Theory]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Mouse)]
    public void New_Cylinder_Opens_A_Pad_Of_Two_Values_And_An_Apply_Gives_Them(DeviceKind kind)
    {
        using var ui = Opened();
        Tap(ui, kind, DesktopUi.NewLabel);
        foreach (var entry in new[] { DesktopUi.NewBoxLabel, DesktopUi.NewCylinderLabel })
        {
            var rectangle = ui.ItemRectangle(entry) ?? throw new InvalidOperationException($"The list has no {entry}.");
            Assert.True(rectangle.W - rectangle.Y >= DesktopUi.MinimumControlHeight, $"{entry} is {rectangle.W - rectangle.Y} pixels high.");

            // The open list is a rectangle of the panels, so a tap on it does not reach the scene.
            Assert.True(ui.OverPanel((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2), $"{entry} is outside the panels.");
        }
        Tap(ui, kind, DesktopUi.NewCylinderLabel);
        Assert.Equal(DesktopUi.NewCylinderLabel, ui.Pad?.Title);
        Step(ui, null);

        foreach (var label in new[] { "Clear", "3", "Next", "Clear", "2", "*", "pi", "Apply" })
            Tap(ui, kind, DesktopUi.PadItem(label));

        Assert.Equal([new CreateCylinderAction(3, 2 * Math.PI)], ui.TakeActions());
        Assert.Equal(["radius", "height"], ui.Pad!.Fields.Select(field => field.Name));
        ui.EndPad(null);
        Assert.Null(ui.Pad);
    }

    // Phase K5e (ADR-0089 item 5, TASK-0298): the list "New" holds Cone, Sphere and Torus too, each at least 44 pixels high
    // and on the panels; each opens its pad, and an apply of the defaults gives one action with them.
    [Theory]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Mouse)]
    public void New_Cone_Sphere_And_Torus_Open_Their_Pads(DeviceKind kind)
    {
        var cases = new (string Entry, string[] Fields, PanelAction Action)[]
        {
            (DesktopUi.NewConeLabel, ["radius", "height"], new CreateConeAction(5, 10)),
            (DesktopUi.NewSphereLabel, ["radius"], new CreateSphereAction(5)),
            (DesktopUi.NewTorusLabel, ["radius", "tube radius"], new CreateTorusAction(10, 3)),
        };
        foreach (var (entry, fields, action) in cases)
        {
            using var ui = Opened();
            Tap(ui, kind, DesktopUi.NewLabel);
            var rectangle = ui.ItemRectangle(entry) ?? throw new InvalidOperationException($"The list has no {entry}.");
            Assert.True(rectangle.W - rectangle.Y >= DesktopUi.MinimumControlHeight, $"{entry} is {rectangle.W - rectangle.Y} pixels high.");
            Assert.True(ui.OverPanel((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2), $"{entry} is outside the panels.");

            Tap(ui, kind, entry);
            Assert.Equal(entry, ui.Pad?.Title);
            Assert.Equal(fields, ui.Pad!.Fields.Select(field => field.Name));
            Step(ui, null);
            Tap(ui, kind, DesktopUi.PadItem("Apply"));
            Assert.Equal([action], ui.TakeActions());
        }
    }

    // ADR-0027 item 6 for the controls of ADR-0032 items 4 and 5.
    [Fact]
    public void Each_Control_Of_The_Pad_Is_At_Least_44_Pixels_High()
    {
        using var ui = WithNewBoxPad();

        var items = DesktopUi.PadRows.SelectMany(row => row).Select(DesktopUi.PadItem)
            .Concat(new[] { "x", "y", "z" }.Select(DesktopUi.PadItem))
            .Append(DesktopUi.NewLabel);
        foreach (var item in items)
        {
            var rectangle = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"The pad has no control {item}.");
            Assert.True(rectangle.W - rectangle.Y >= DesktopUi.MinimumControlHeight, $"{item} is {rectangle.W - rectangle.Y} pixels high.");
        }
    }
}
