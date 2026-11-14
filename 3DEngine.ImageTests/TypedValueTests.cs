using System.Numerics;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Numeric;
using ThreeDEngine.Core.Sketching;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0128 (phase R18h, TASK-0357): a value typed in the command line goes to the pad of its value, through the evaluator,
// with no GPU. ADR-0128 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class TypedValueTests
{
    private static readonly Vector2 Display = new(1440, 900);
    private static readonly Guid Body = Guid.Parse("71000000-0000-0000-0000-00000000000b");
    private static readonly Guid Sketch = Guid.Parse("71000000-0000-0000-0000-00000000000c");
    private static readonly Guid Constraint = Guid.Parse("71000000-0000-0000-0000-00000000000d");

    // The distance of an extrude and the length "C3" of its sketch, as two chips on its edges.
    private static readonly IReadOnlyList<EdgeValues.Value> BodyValues =
    [
        new(EdgeValues.DistanceKey(Body), "3.000 mm", new Vector2(700, 300), 3, Body, null, null, "Distance"),
        new(EdgeValues.ConstraintKey(Constraint), "60.000 mm", new Vector2(800, 420), 60, Body, Sketch, Constraint, "Width", Name: "C3"),
    ];

    private sealed record State(IReadOnlyList<EdgeValues.Value>? Values, Guid? Selection, SketchPanel? Sketch, IReadOnlyList<EdgeValues.Value>? SketchValues);

    private static void Frame(DesktopUi ui, State state, params InputSample[] samples)
    {
        ui.Route(samples);
        LiveBody[] bodies = state.Selection is { } body ? [new LiveBody(body, "Extrude", "Plate", true)] : [];
        ui.Frame(1f / 60f, Display, Vector2.One, bodies, state.Selection, null, sketchPanel: state.Sketch, values: state.Values, sketchValues: state.SketchValues);
    }

    private static KeySample Key(InputKey key, bool down = true) => new(key, down, KeyModifiers.None, 0);

    // Types the text after Control and K, then each key with its release, one frame each.
    private static void Type(DesktopUi ui, State state, string text, params InputKey[] keys)
    {
        Frame(ui, state, new KeySample(InputKey.K, true, KeyModifiers.Control, 0));
        // The focus of ImGui takes the field in a frame after the request, and the field is active in the frame after that.
        Frame(ui, state);
        Frame(ui, state);
        Frame(ui, state);
        Frame(ui, state, new TextSample(text, 0));
        Frame(ui, state);
        foreach (var key in keys)
        {
            Frame(ui, state, Key(key));
            Frame(ui, state, Key(key, false));
        }

        Frame(ui, state);
    }

    // Item 1: a typed expression lists each value of the frame with its result, and Enter opens the pad of the first one with
    // the typed text; Enter in the pad applies the result, as a tap on the chip and the pad would.
    [Fact]
    public void A_Typed_Expression_Opens_The_Pad_Of_A_Value_With_The_Text()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var state = new State(BodyValues, Body, null, null);
        Frame(ui, state);
        Frame(ui, state);

        Assert.Equal(["Distance = 75.000 mm", "C3 = 75.000 mm"], ui.ValueCommands("40+35").Select(command => command.Name));

        Type(ui, state, "40+35", InputKey.Enter);
        Assert.Equal([new PadField("distance", "40+35")], ui.Pad!.Fields);
        Assert.Equal(EdgeValues.DistanceKey(Body), ui.PendingValue);
        Frame(ui, state, Key(InputKey.Enter));
        Frame(ui, state, Key(InputKey.Enter, false));
        Assert.Equal([new SetDistanceAction(Body, 75)], ui.TakeActions());
    }

    // Item 1: the arrows choose the second value, and its pad changes the constraint of the sketch of the extrude.
    [Fact]
    public void The_Arrows_Choose_The_Value_Of_A_Constraint()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var state = new State(BodyValues, Body, null, null);
        Frame(ui, state);
        Frame(ui, state);

        Type(ui, state, "2*40", InputKey.ArrowDown, InputKey.Enter);
        Assert.Equal([new PadField("value", "2*40")], ui.Pad!.Fields);
        Assert.Equal(EdgeValues.ConstraintKey(Constraint), ui.PendingValue);
        Frame(ui, state, Key(InputKey.Enter));
        Frame(ui, state, Key(InputKey.Enter, false));
        Assert.Equal([new SetBodyValueAction(Sketch, Constraint, 80)], ui.TakeActions());
    }

    // Item 1: while a sketch is active, each value of the sketch has its label in the sketch, and "Extrude" opens the pad of an
    // extrude with the typed text.
    [Fact]
    public void A_Sketch_Lists_Its_Values_And_Extrude()
    {
        var (a, b) = (Guid.NewGuid(), Guid.NewGuid());
        var sketch = new SketchInfo(Sketch, Datums.XY.Id, "Plate", [new(Sketch, 0, 0), new(a, 0, 0), new(b, 60, 0)], [], [], [],
            [new(Guid.NewGuid(), "fixed", [a], [], 0), new(Guid.NewGuid(), "horizontal", [a, b], [], 0), new(Constraint, "distance", [a, b], [60], 0)], true, 0);
        var panel = new SketchPanel([sketch], sketch, SketchToolKind.Select, false, [], null);
        IReadOnlyList<EdgeValues.Value> sketchValues = [new(SketchValues.Key(Constraint), "C3  60.000 mm", new Vector2(700, 400), 60, Guid.Empty, Sketch, Constraint, "Length")];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var state = new State(null, null, panel, sketchValues);
        Frame(ui, state);
        Frame(ui, state);

        Assert.Equal(["C3 = 30.000 mm", "Extrude 30.000 mm"], ui.ValueCommands("30").Select(command => command.Name));

        Type(ui, state, "30", InputKey.ArrowDown, InputKey.Enter);
        Assert.Equal([new PadField("distance", "30")], ui.Pad!.Fields);
        Frame(ui, state, Key(InputKey.Enter));
        Frame(ui, state, Key(InputKey.Enter, false));
        Assert.Contains(ui.TakeActions(), action => action == new ExtrudeAction(30));
    }

    // Item 1: a text that the evaluator does not read as a number, and a number with no value in the frame, list no value; the
    // commands whose name holds the text still show.
    [Fact]
    public void A_Text_That_Is_Not_A_Number_Lists_No_Value()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var state = new State(BodyValues, Body, null, null);
        Frame(ui, state);
        Frame(ui, state);
        foreach (var text in new[] { "", "fra", "2*", "40 mm" })
            Assert.Empty(ui.ValueCommands(text));

        Type(ui, state, "fra");
        Assert.NotNull(ui.ItemRectangle(DesktopUi.CommandListItem(DesktopUi.FrameLabel)));

        using var empty = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var none = new State(null, null, null, null);
        Frame(empty, none);
        Frame(empty, none);
        Assert.Empty(empty.ValueCommands("5"));
    }
}
