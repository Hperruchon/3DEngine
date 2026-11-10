using System.Numerics;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Sketching;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0104 (the redesign, phase R16q, TASK-0325): the history arc, with no GPU. The steps of a body come from the rows of
// GetFeatures (HistorySteps); the arc shows them inside the ring of the body. ADR-0104 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class HistoryArcTests
{
    private static readonly Vector2 Display = new(1280, 720);
    private static readonly Vector2 Centre = new(640, 360);

    private static readonly Guid Sketch = Guid.Parse("71000000-0000-0000-0000-000000000001");
    private static readonly Guid Bracket = Guid.Parse("71000000-0000-0000-0000-000000000002");
    private static readonly Guid Moved = Guid.Parse("71000000-0000-0000-0000-000000000003");
    private static readonly Guid Base = Guid.Parse("71000000-0000-0000-0000-000000000004");
    private static readonly Guid Datum = Guid.Parse("71000000-0000-0000-0000-000000000005");
    private static readonly Guid FaceSketch = Guid.Parse("71000000-0000-0000-0000-000000000006");
    private static readonly Guid Boss = Guid.Parse("71000000-0000-0000-0000-000000000007");

    private static FeatureInfo Feature(Guid id, string kind, Guid? consumedBy = null, string state = GetFeaturesQueryHandler.Correct, params Guid[] inputs)
        => new(id, kind, inputs, consumedBy, consumedBy is null ? state : GetFeaturesQueryHandler.Consumed, null);

    private static SketchInfo SketchRecord(Guid id, Guid datum, string? name) => new(id, datum, name, [], [], [], [], [], true, 0);

    private static readonly SketchInfo Plate = SketchRecord(Sketch, Datums.XY.Id, "Plate");

    // The bracket of the render: a sketch "Plate" on XY and its extrude "Bracket".
    private static readonly FeaturesInfo Extruded = new(
    [
        Feature(Sketch, "CreateSketch", inputs: Datums.XY.Id),
        Feature(Bracket, "Extrude", inputs: Sketch),
    ]);

    private static readonly LiveBody BracketBody = new(Bracket, "Extrude", "Bracket", true);

    private static void Frame(DesktopUi ui, IReadOnlyList<LiveBody> bodies, Guid? selection, IReadOnlyList<HistoryStep> steps, Vector2 display, SketchPanel? sketch = null, params InputSample[] samples)
    {
        ui.Route(samples);
        ui.Frame(1f / 60f, display, Vector2.One, bodies, selection, null, sketchPanel: sketch, ringCentre: display.X >= display.Y ? Centre : new Vector2(display.X / 2f, display.Y / 2f), history: steps);
    }

    private static void Tap(DesktopUi ui, IReadOnlyList<LiveBody> bodies, Guid selection, IReadOnlyList<HistoryStep> steps, Vector2 at, DeviceKind kind)
    {
        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        foreach (var phase in new[] { PointerPhase.Down, PointerPhase.Up })
            Frame(ui, bodies, selection, steps, Display, null, new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, phase, 0));
        Frame(ui, bodies, selection, steps, Display);
    }

    private static Vector2 Middle(Vector4 rectangle) => new((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);

    // Item 1: the steps of an extrude are its feature and its sketch, the newest first; the text of a body is its name, and
    // the text of a sketch is its name, or "Sketch", with "on" and its plane: an origin plane, or "a face".
    [Fact]
    public void The_Steps_Of_An_Extrude_Go_Back_To_Its_Sketch_And_Its_Plane()
    {
        var steps = HistorySteps.Of(Extruded, [BracketBody], [Plate], Bracket);

        Assert.Equal(
            [
                new HistoryStep(Bracket, "Extrude", "Bracket", GetFeaturesQueryHandler.Correct, null, HistoryTap.SelectBody),
                new HistoryStep(Sketch, "CreateSketch", "Plate on XY", GetFeaturesQueryHandler.Correct, null, HistoryTap.EditSketch),
            ],
            steps);

        // A sketch with no name on XZ, and a body with no name, which gives the word of its kind.
        var other = HistorySteps.Of(Extruded with { Features = [Feature(Sketch, "CreateSketch", inputs: Datums.XZ.Id), Extruded.Features[1]] },
            [BracketBody with { Name = null }], [Plate with { Name = null }], Bracket);
        Assert.Equal(["Extrude", "Sketch on XZ"], other.Select(step => step.Text));

        // A sketch on a datum of a face reads the datum, which reads the body of the face: each one is a step, and a live body
        // that is not consumed takes the tap of a selection.
        var onFace = new FeaturesInfo(
        [
            Feature(Base, "CreateBox"),
            Feature(Datum, "CreateDatum", inputs: Base),
            Feature(FaceSketch, "CreateSketch", inputs: Datum),
            Feature(Boss, "Extrude", inputs: FaceSketch),
        ]);
        var face = HistorySteps.Of(onFace, [new LiveBody(Base, "Box", null, true), new LiveBody(Boss, "Extrude", "Boss", true)], [], Boss);
        Assert.Equal(["Boss", "Sketch on a face", "Datum", "Box"], face.Select(step => step.Text));
        Assert.Equal([HistoryTap.SelectBody, HistoryTap.EditSketch, HistoryTap.None, HistoryTap.SelectBody], face.Select(step => step.Tap));

        // No selection, or a selection that is not a feature, gives no step; another body of the Document is not a step.
        Assert.Empty(HistorySteps.Of(Extruded, [BracketBody], [Plate], null));
        Assert.Empty(HistorySteps.Of(Extruded, [BracketBody], [Plate], Guid.NewGuid()));
        Assert.Empty(HistorySteps.Of(null, [BracketBody], [Plate], Bracket));
        Assert.DoesNotContain(Base, HistorySteps.Of(onFace with { Features = [.. onFace.Features, .. Extruded.Features] }, [BracketBody], [Plate], Bracket).Select(step => step.Id));
    }

    // Item 1: a move reads the body that it moves, and a cut reads its two bodies; a consumed body is a step with no tap.
    [Fact]
    public void A_Move_And_A_Cut_Give_The_Steps_Of_Their_Bodies()
    {
        var moved = new FeaturesInfo([.. Extruded.Features.Take(1), Feature(Bracket, "Extrude", consumedBy: Moved, inputs: Sketch), Feature(Moved, "Translate", inputs: Bracket)]);
        var steps = HistorySteps.Of(moved, [new LiveBody(Moved, "Solid", "Bracket", true)], [Plate], Moved);
        Assert.Equal(["Bracket", "Extrude", "Plate on XY"], steps.Select(step => step.Text));
        Assert.Equal([HistoryTap.SelectBody, HistoryTap.None, HistoryTap.EditSketch], steps.Select(step => step.Tap));
        Assert.Equal(GetFeaturesQueryHandler.Consumed, steps[1].State);

        var cut = Guid.NewGuid();
        var subtracted = new FeaturesInfo(
        [
            Feature(Base, "CreateBox", consumedBy: cut),
            Feature(Boss, "CreateCylinder", consumedBy: cut),
            Feature(cut, "Subtract", inputs: [Base, Boss]),
        ]);
        var parts = HistorySteps.Of(subtracted, [new LiveBody(cut, "Solid", null, true)], [], cut);
        Assert.Equal(["Cut", "Cylinder", "Box"], parts.Select(step => step.Text));
        Assert.Equal([HistoryTap.SelectBody, HistoryTap.None, HistoryTap.None], parts.Select(step => step.Tap));
    }

    // Item 1: at most six steps, the six newest, in the order of the log with the newest first.
    [Fact]
    public void At_Most_Six_Steps_Show_The_Newest_First()
    {
        var ids = Enumerable.Range(0, 9).Select(i => Guid.Parse($"72000000-0000-0000-0000-00000000000{i}")).ToArray();
        var features = new List<FeatureInfo> { Feature(ids[0], "CreateBox", consumedBy: ids[1]) };
        for (var i = 1; i < ids.Length; i++)
            features.Add(Feature(ids[i], "Translate", consumedBy: i + 1 < ids.Length ? ids[i + 1] : null, inputs: ids[i - 1]));

        var steps = HistorySteps.Of(new FeaturesInfo(features), [new LiveBody(ids[^1], "Solid", null, true)], [], ids[^1]);

        Assert.Equal(HistorySteps.Most, steps.Count);
        Assert.Equal(ids.Reverse().Take(HistorySteps.Most), steps.Select(step => step.Id));
    }

    // Item 1: a failed step and an unresolved step say so with the code of their fault, as the rows do.
    [Fact]
    public void A_Failed_Or_Unresolved_Step_Says_So()
    {
        var fault = new FeatureFault("E-FEATURE-REBUILD", "The profile is open.");
        var features = new FeaturesInfo(
        [
            new FeatureInfo(Sketch, "CreateSketch", [Datums.XY.Id], null, GetFeaturesQueryHandler.Unresolved, new FeatureFault("E-REF-LOST", "The edge is gone.")),
            new FeatureInfo(Bracket, "Extrude", [Sketch], null, GetFeaturesQueryHandler.Failed, fault),
        ]);

        var steps = HistorySteps.Of(features, [BracketBody with { Fault = new BodyFault(fault.Code, fault.Message) }], [Plate], Bracket);

        Assert.Equal((GetFeaturesQueryHandler.Failed, "E-FEATURE-REBUILD", true), (steps[0].State, steps[0].Code, steps[0].Fault));
        Assert.Equal((GetFeaturesQueryHandler.Unresolved, "E-REF-LOST", true), (steps[1].State, steps[1].Code, steps[1].Fault));
        Assert.False(HistorySteps.Of(Extruded, [BracketBody], [Plate], Bracket)[0].Fault);
    }

    // Item 1: the host computes the steps again only for a new version of the Document or a new selection.
    [Fact]
    public void The_Steps_Come_Again_Only_For_A_New_Version_Or_Selection()
    {
        var cache = new HistoryCache();
        Task<IReadOnlyList<HistoryStep>> Compute() => Task.FromResult(HistorySteps.Of(Extruded, [BracketBody], [Plate], Bracket));

        var first = cache.Steps(4, Bracket, Compute);
        Assert.Same(first, cache.Steps(4, Bracket, Compute));
        Assert.Equal(1, cache.Computed);

        cache.Steps(5, Bracket, Compute);
        Assert.Equal(2, cache.Computed);
        // Finding V161 (TASK-0331): with no selection there is no read.
        Assert.Empty(cache.Steps(5, null, Compute));
        Assert.Equal(2, cache.Computed);
        Assert.Empty(cache.Steps(6, null, Compute));
        Assert.Equal(2, cache.Computed);
    }

    // Finding V161 of the codebase review of 2026-11-09 (b) (TASK-0331): the frame does not wait for the read of the features.
    // While a read runs, the frame keeps the steps of the last read of the same selection, or none for a new selection; the
    // steps of a complete read come in the next frame, and a failed read gives none. Until TASK-0331 the frame waited for the
    // query, which waits for the serial lock of the session.
    [Fact]
    public void The_Frame_Does_Not_Wait_For_The_Read()
    {
        var cache = new HistoryCache();
        var steps = HistorySteps.Of(Extruded, [BracketBody], [Plate], Bracket);
        var first = new TaskCompletionSource<IReadOnlyList<HistoryStep>>();
        Assert.Empty(cache.Steps(4, Bracket, () => first.Task));
        Assert.Empty(cache.Steps(4, Bracket, () => throw new InvalidOperationException("A second read of one key.")));
        first.SetResult(steps);
        Assert.Same(steps, cache.Steps(4, Bracket, () => throw new InvalidOperationException("A second read of one key.")));

        // A new version of the same body keeps the old steps until its read is complete.
        var second = new TaskCompletionSource<IReadOnlyList<HistoryStep>>();
        Assert.Same(steps, cache.Steps(5, Bracket, () => second.Task));
        second.SetException(new InvalidOperationException("The session closed."));
        Assert.Empty(cache.Steps(5, Bracket, () => throw new InvalidOperationException("A second read of one key.")));

        // A new selection shows no steps of the last body while its read runs.
        Assert.Same(steps, cache.Steps(6, Bracket, () => Task.FromResult(steps)));
        Assert.Empty(cache.Steps(6, Guid.NewGuid(), () => new TaskCompletionSource<IReadOnlyList<HistoryStep>>().Task));
        Assert.Equal(4, cache.Computed);
    }

    // Item 3: on a landscape display with a selection, each step has a node of at least 44 pixels at the lower left on the band
    // of the ring, between its guide circles, the newest at the upper end, with its text beside it, and the caption under the
    // lower end. The nodes and their texts are a part of the panels for a tap.
    [Fact]
    public void The_Nodes_Show_At_The_Lower_Left_On_The_Band_Of_The_Ring()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var steps = HistorySteps.Of(Extruded, [BracketBody], [Plate], Bracket);
        Frame(ui, [BracketBody], Bracket, steps, Display);
        Frame(ui, [BracketBody], Bracket, steps, Display);

        var ring = DesktopUi.Layout(Display, Centre, 3);
        var nodes = steps.Select(step => ui.ItemRectangle(DesktopUi.HistoryItem(step.Id)) ?? throw new InvalidOperationException($"No node of {step.Text}.")).ToList();
        foreach (var node in nodes)
        {
            Assert.True(node.Z - node.X >= DesktopUi.MinimumControlHeight && node.W - node.Y >= DesktopUi.MinimumControlHeight, $"The node {node} is less than 44 pixels.");
            var middle = Middle(node);
            Assert.True(middle.X < ring.Centre.X + 0.5f && middle.Y > ring.Centre.Y - 0.5f, $"The node at {middle} is not at the lower left of {ring.Centre}.");
            Assert.True(Math.Abs(Vector2.Distance(middle, ring.Centre) - (ring.Inner + (DesktopUi.RingBand / 2f))) < 1f, $"The node at {middle} is not on the band of the ring between {ring.Inner} and {ring.Outer}.");
            Assert.True(ui.OverPanel(middle.X, middle.Y), $"A tap on the node at {middle} reaches the scene.");
        }

        Assert.True(Middle(nodes[0]).Y < Middle(nodes[1]).Y, "The newest node is not at the upper end.");
        foreach (var step in steps)
        {
            var text = ui.ItemRectangle(DesktopUi.HistoryTextItem(step.Id)) ?? throw new InvalidOperationException($"No text of {step.Text}.");
            var node = ui.ItemRectangle(DesktopUi.HistoryItem(step.Id))!.Value;
            Assert.True(text.Y < node.W && node.Y < text.W, $"The text {text} is not beside its node {node}.");
            Assert.True(ui.OverPanel(Middle(text).X, Middle(text).Y), $"A tap on the text of {step.Text} reaches the scene.");
        }

        var caption = ui.ItemRectangle(DesktopUi.HistoryCaptionItem) ?? throw new InvalidOperationException("No caption.");
        Assert.True(caption.Y > nodes.Max(node => node.W) - 0.5f, $"The caption {caption} is not under the nodes.");
    }

    // Item 3: no arc on a portrait display, while a sketch is active, while a pad is open, or with no selection.
    [Fact]
    public void No_Node_On_Portrait_With_A_Sketch_With_A_Pad_Or_With_No_Selection()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var steps = HistorySteps.Of(Extruded, [BracketBody], [Plate], Bracket);
        bool Shows() => ui.ItemRectangle(DesktopUi.HistoryItem(Bracket)) is not null || ui.ItemRectangle(DesktopUi.HistoryCaptionItem) is not null;

        var portrait = new Vector2(390, 844);
        Frame(ui, [BracketBody], Bracket, steps, portrait);
        Frame(ui, [BracketBody], Bracket, steps, portrait);
        Assert.False(Shows(), "The arc shows on a portrait display.");

        var sketch = new SketchPanel([Plate], Plate, SketchToolKind.Line, false, [], null);
        Frame(ui, [BracketBody], Bracket, steps, Display, sketch);
        Frame(ui, [BracketBody], Bracket, steps, Display, sketch);
        Assert.False(Shows(), "The arc shows while a sketch is active.");

        Frame(ui, [BracketBody], null, steps, Display);
        Frame(ui, [BracketBody], null, steps, Display);
        Assert.False(Shows(), "The arc shows with no selection.");

        Frame(ui, [BracketBody], Bracket, steps, Display);
        Assert.True(Shows(), "The arc does not show on a landscape display with a selection.");
        ui.OpenDistancePad(Bracket, 10);
        Frame(ui, [BracketBody], Bracket, steps, Display);
        Frame(ui, [BracketBody], Bracket, steps, Display);
        Assert.NotNull(ui.Pad);
        Assert.False(Shows(), "The arc shows while a pad is open.");
    }

    // Item 2: a press and a release on the node of a sketch give the edit of that sketch, by touch and by mouse, and the tap
    // does not reach the scene.
    [Theory]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Mouse)]
    public void A_Tap_On_A_Sketch_Node_Edits_The_Sketch(DeviceKind kind)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var steps = HistorySteps.Of(Extruded, [BracketBody], [Plate], Bracket);
        Frame(ui, [BracketBody], Bracket, steps, Display);
        Frame(ui, [BracketBody], Bracket, steps, Display);

        var at = Middle(ui.ItemRectangle(DesktopUi.HistoryItem(Sketch)) ?? throw new InvalidOperationException("No node of the sketch."));
        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        Assert.Empty(ui.Route([new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Down, 0)]));
        ui.Frame(1f / 60f, Display, Vector2.One, [BracketBody], Bracket, null, ringCentre: Centre, history: steps);
        Frame(ui, [BracketBody], Bracket, steps, Display, null, new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Up, 0));
        Frame(ui, [BracketBody], Bracket, steps, Display);

        Assert.Equal([new EditSketchAction(Sketch)], ui.TakeActions());
    }

    // Item 2: a tap on the node of a live body that is not consumed selects that body; a tap on a consumed body or on a datum
    // gives nothing; a press on one node and a release on another gives nothing.
    [Theory]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Mouse)]
    public void A_Tap_On_A_Live_Body_Node_Selects_The_Body(DeviceKind kind)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var features = new FeaturesInfo(
        [
            Feature(Base, "CreateBox"),
            Feature(Datum, "CreateDatum", inputs: Base),
            Feature(FaceSketch, "CreateSketch", inputs: Datum),
            Feature(Boss, "Extrude", consumedBy: Moved, inputs: FaceSketch),
            Feature(Moved, "Translate", inputs: Boss),
        ]);
        LiveBody[] bodies = [new(Base, "Box", "Base", true), new(Moved, "Solid", "Boss", true)];
        var steps = HistorySteps.Of(features, bodies, [], Moved);
        Frame(ui, bodies, Moved, steps, Display);
        Frame(ui, bodies, Moved, steps, Display);
        Vector2 NodeOf(Guid id) => Middle(ui.ItemRectangle(DesktopUi.HistoryItem(id)) ?? throw new InvalidOperationException($"No node of {id}."));

        Tap(ui, bodies, Moved, steps, NodeOf(Base), kind);
        Assert.Equal([new SelectBodyAction(Base)], ui.TakeActions());

        Tap(ui, bodies, Moved, steps, NodeOf(Boss), kind);
        Tap(ui, bodies, Moved, steps, NodeOf(Datum), kind);
        Assert.Empty(ui.TakeActions());

        var (from, to) = (NodeOf(FaceSketch), NodeOf(Base));
        Frame(ui, bodies, Moved, steps, Display, null, new PointerSample(DeviceKind.Touch, 1, 3, from.X, from.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
        Frame(ui, bodies, Moved, steps, Display, null, new PointerSample(DeviceKind.Touch, 1, 3, to.X, to.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));
        Frame(ui, bodies, Moved, steps, Display);
        Assert.Empty(ui.TakeActions());
    }
}
