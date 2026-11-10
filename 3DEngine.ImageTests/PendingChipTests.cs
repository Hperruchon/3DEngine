using System.Numerics;
using System.Text.Json;
using Engine.Contracts;
using Engine.Core.Queries;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Numeric;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0110 (phase R16t, TASK-0334): a proposal shows each value that it changes on the chip of that value, with no GPU.
// ADR-0110 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class PendingChipTests
{
    private static readonly Vector2 Display = new(1440, 900);
    private static readonly Vector2 Portrait = new(390, 844);

    private static readonly Guid Bracket = Guid.Parse("75000000-0000-0000-0000-000000000001");
    private static readonly Guid Plate = Guid.Parse("75000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Width = Guid.Parse("75000000-0000-0000-0000-0000000000c1");
    private static readonly Guid Depth = Guid.Parse("75000000-0000-0000-0000-0000000000c2");
    private static readonly LiveBody BracketBody = new(Bracket, "Extrude", "Bracket", true);

    // The sketch "Plate" with two distances: C1 of 60 and C2 of 40.
    private static readonly SketchInfo PlateSketch = new(Plate, Engine.Contracts.Sketch.Datums.XY.Id, "Plate", [], [], [], [],
        [new SketchConstraintInfo(Width, "distance", [], [60], 0), new SketchConstraintInfo(Depth, "distance", [], [40], 0)], true, 0);

    // The chips of the selected bracket: its distance of 10 and the width of 60 of its sketch, with no label.
    private static readonly IReadOnlyList<EdgeValues.Value> BodyChips =
    [
        new(EdgeValues.DistanceKey(Bracket), "10 mm", new Vector2(1100, 760), 10, Bracket, null, null, "Distance"),
        new(EdgeValues.ConstraintKey(Width), "60 mm", new Vector2(900, 760), 60, Bracket, Plate, Width, "Length"),
    ];

    private static (string Name, string Parameters) SetValue(Guid constraint, string value)
        => (ProposedValues.SetConstraintValue, $"{{\"sketchId\":\"{Plate}\",\"constraintId\":\"{constraint}\",\"value\":{value}}}");

    private static (string Name, string Parameters) SetDistance(Guid extrude, string distance)
        => (ProposedValues.SetExtrudeDistance, $"{{\"extrudeId\":\"{extrude}\",\"distance\":{distance}}}");

    private static (string Name, string Parameters) Box() => ("CreateBox", "{\"sizeX\":75,\"sizeY\":40,\"sizeZ\":10}");

    private static List<KeyValuePair<string, JsonElement>> Parameters(string json)
    {
        using var document = JsonDocument.Parse(json);
        return [.. document.RootElement.EnumerateObject().Select(member => KeyValuePair.Create(member.Name, member.Value.Clone()))];
    }

    // A card as the host gives it: the line and the proposed value of each command, then the names of the frame.
    private static ProposalCard Card(IReadOnlyList<EdgeValues.Value>? values, params (string Name, string Parameters)[] commands)
    {
        var card = new ProposalCard(Guid.Parse("75000000-0000-0000-0000-0000000000f1"), "Widen the bracket to 75 mm",
            [.. commands.Select(command => ProposalCard.Line(command.Name, Parameters(command.Parameters)))],
            Values: [.. commands.Select(command => ProposedValues.Of(command.Name, Parameters(command.Parameters)))]);
        return ProposedValues.Named([card], [PlateSketch], [BracketBody], values)![0];
    }

    private static void Frame(DesktopUi ui, IReadOnlyList<ProposalCard> cards, Vector2? display = null, IReadOnlyList<EdgeValues.Value>? values = null,
        SketchPanel? sketchPanel = null, IReadOnlyList<EdgeValues.Value>? sketchValues = null, params InputSample[] samples)
    {
        ui.Route(samples);
        ui.Frame(1f / 60f, display ?? Display, Vector2.One, [BracketBody], Bracket, new DocumentLine("bracket", null), sketchPanel: sketchPanel,
            values: values ?? BodyChips, proposals: cards, sketchValues: sketchValues);
    }

    private static void Frames(DesktopUi ui, IReadOnlyList<ProposalCard> cards, Vector2? display = null, IReadOnlyList<EdgeValues.Value>? values = null,
        SketchPanel? sketchPanel = null, IReadOnlyList<EdgeValues.Value>? sketchValues = null)
    {
        Frame(ui, cards, display, values, sketchPanel, sketchValues);
        Frame(ui, cards, display, values, sketchPanel, sketchValues);
    }

    private static (string Key, string Text, bool Pending) Chip(DesktopUi ui, string key) => ui.Chips.Single(chip => chip.Key == key);

    // Item 1: SetSketchConstraintValue gives the value of its constraint, and SetExtrudeDistance the distance of its extrude.
    // A number as a text binds, as the binder of the handlers reads it. Another command gives no value.
    [Fact]
    public void Each_Value_Command_Gives_Its_Proposed_Value()
    {
        var extrude = Guid.NewGuid();
        var (name, json) = SetValue(Width, "75");
        Assert.Equal(new ProposedValue(Plate, Width, null, 75), ProposedValues.Of(name, Parameters(json)));
        (name, json) = SetValue(Width, "\"75.5\"");
        Assert.Equal(new ProposedValue(Plate, Width, null, 75.5), ProposedValues.Of(name, Parameters(json)));
        (name, json) = SetDistance(extrude, "30");
        Assert.Equal(new ProposedValue(null, null, extrude, 30), ProposedValues.Of(name, Parameters(json)));
        (name, json) = Box();
        Assert.Null(ProposedValues.Of(name, Parameters(json)));

        // Item 2: the keys of the chips of each value.
        Assert.Equal([EdgeValues.ConstraintKey(Width), SketchValues.Key(Width)], new ProposedValue(Plate, Width, null, 75).Keys);
        Assert.Equal([EdgeValues.DistanceKey(extrude)], new ProposedValue(null, null, extrude, 30).Keys);
    }

    // Item 1: a later command for the same value replaces the earlier one; a command with no value gives nothing.
    [Fact]
    public void A_Later_Command_For_The_Same_Value_Replaces_The_Earlier()
    {
        var values = ProposedValues.Of([
            new ProposedValue(Plate, Width, null, 70),
            null,
            new ProposedValue(null, null, Bracket, 12),
            new ProposedValue(Plate, Depth, null, 45),
            new ProposedValue(Plate, Width, null, 75),
        ]);

        Assert.Equal([new ProposedValue(Plate, Width, null, 75), new ProposedValue(null, null, Bracket, 12), new ProposedValue(Plate, Depth, null, 45)], values);
    }

    // Item 1: a parameter that is absent, or that does not bind to the type of the schema, gives no value.
    [Theory]
    [InlineData("SetSketchConstraintValue", "{\"sketchId\":\"75000000-0000-0000-0000-0000000000a1\",\"constraintId\":\"75000000-0000-0000-0000-0000000000c1\"}")]
    [InlineData("SetSketchConstraintValue", "{\"sketchId\":\"75000000-0000-0000-0000-0000000000a1\",\"constraintId\":\"75000000-0000-0000-0000-0000000000c1\",\"value\":\"wide\"}")]
    [InlineData("SetSketchConstraintValue", "{\"sketchId\":\"plate\",\"constraintId\":\"75000000-0000-0000-0000-0000000000c1\",\"value\":75}")]
    [InlineData("SetSketchConstraintValue", "{\"sketchId\":\"75000000-0000-0000-0000-0000000000a1\",\"constraintId\":\"75000000-0000-0000-0000-0000000000c1\",\"value\":true}")]
    [InlineData("SetSketchConstraintValue", "{\"sketchId\":\"75000000-0000-0000-0000-0000000000a1\",\"constraintId\":\"75000000-0000-0000-0000-0000000000c1\",\"Value\":75}")]
    [InlineData("SetExtrudeDistance", "{\"extrudeId\":12,\"distance\":30}")]
    [InlineData("SetExtrudeDistance", "{\"extrudeId\":\"75000000-0000-0000-0000-000000000001\"}")]
    public void A_Parameter_That_Does_Not_Bind_Gives_No_Value(string name, string json)
        => Assert.Null(ProposedValues.Of(name, Parameters(json)));

    // Item 3: the frame names each value: the name of its sketch and the label of its constraint, or the name of its body and
    // the word "distance", and its value now. A value that the frame does not hold has no name.
    [Fact]
    public void The_Frame_Names_Each_Value()
    {
        var card = Card(BodyChips, SetValue(Depth, "45"), SetDistance(Bracket, "12"), SetValue(Guid.NewGuid(), "1"), Box());

        Assert.Equal(("Plate", "C2", (double?)40), (card.Values![0]!.Name, card.Values[0]!.Label, card.Values[0]!.Now));
        Assert.Equal(("Bracket", "distance", (double?)10), (card.Values[1]!.Name, card.Values[1]!.Label, card.Values[1]!.Now));
        Assert.Null(card.Values[2]!.Name);
        Assert.Null(card.Values[3]);
    }

    // Item 2: the chip of a constraint on the edge of the selected body shows "now → proposed unit" with the dashed border of a
    // pending chip, while the proposal waits; the chip of the distance does not change. With no proposal the chip is as before.
    [Fact]
    public void A_Body_Constraint_Chip_Shows_The_Change_And_Is_Pending()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frames(ui, [Card(BodyChips, SetValue(Width, "75"))]);

        Assert.Equal((EdgeValues.ConstraintKey(Width), "60 → 75 mm", true), Chip(ui, EdgeValues.ConstraintKey(Width)));
        Assert.Equal((EdgeValues.DistanceKey(Bracket), "10 mm", false), Chip(ui, EdgeValues.DistanceKey(Bracket)));

        Frames(ui, []);
        Assert.Equal((EdgeValues.ConstraintKey(Width), "60 mm", false), Chip(ui, EdgeValues.ConstraintKey(Width)));
    }

    // Item 2: the chip of the distance of the selected extrude shows the change of SetExtrudeDistance.
    [Fact]
    public void The_Distance_Chip_Shows_The_Change_And_Is_Pending()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frames(ui, [Card(BodyChips, SetDistance(Bracket, "12.5"))]);

        Assert.Equal((EdgeValues.DistanceKey(Bracket), "10 → 12.5 mm", true), Chip(ui, EdgeValues.DistanceKey(Bracket)));
        Assert.Equal((EdgeValues.ConstraintKey(Width), "60 mm", false), Chip(ui, EdgeValues.ConstraintKey(Width)));
    }

    // Item 2: while the sketch is active, the chip of its constraint keeps its label: "C1  60 → 75 mm".
    [Fact]
    public void A_Sketch_Chip_Shows_Its_Label_And_The_Change()
    {
        IReadOnlyList<EdgeValues.Value> sketchChips =
        [
            new(SketchValues.Key(Width), "C1  60 mm", new Vector2(900, 760), 60, Guid.Empty, Plate, Width, "Length"),
            new(SketchValues.Key(Depth), "C2  40 mm", new Vector2(1100, 760), 40, Guid.Empty, Plate, Depth, "Length"),
        ];
        var panel = new SketchPanel([PlateSketch], PlateSketch, ThreeDEngine.Core.Sketching.SketchToolKind.Select, false, [], null);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frames(ui, [Card(BodyChips, SetValue(Width, "75"))], sketchPanel: panel, sketchValues: sketchChips);

        Assert.Equal((SketchValues.Key(Width), "C1  60 → 75 mm", true), Chip(ui, SketchValues.Key(Width)));
        Assert.Equal((SketchValues.Key(Depth), "C2  40 mm", false), Chip(ui, SketchValues.Key(Depth)));
    }

    // Item 2: a tap on a pending chip does what a tap on the chip does: it opens the pad of the value now, and gives no
    // action of the proposal.
    [Fact]
    public void A_Tap_On_A_Pending_Chip_Opens_The_Pad_Of_The_Value_Now()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        IReadOnlyList<ProposalCard> cards = [Card(BodyChips, SetValue(Width, "75"))];
        Frames(ui, cards);
        var chip = ui.ItemRectangle(EdgeValues.ConstraintKey(Width)) ?? throw new InvalidOperationException("No chip.");
        var (x, y) = ((chip.X + chip.Z) / 2, (chip.Y + chip.W) / 2);
        foreach (var phase in new[] { PointerPhase.Down, PointerPhase.Up })
            Frame(ui, cards, samples: new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, phase, 0));
        Frames(ui, cards);

        Assert.Equal([new PadField("value", "60")], ui.Pad!.Fields);
        Assert.Equal(EdgeValues.ConstraintKey(Width), ui.PendingValue);
        Assert.DoesNotContain(ui.TakeActions(), action => action is AcceptProposalAction or RejectProposalAction);
    }

    // Item 3: a line of the arc, or of the sheet on a portrait display, names each value and its change, with no identifier.
    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public void The_Lines_Name_Each_Value_And_Its_Change(int width, int height)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frames(ui, [Card(BodyChips, SetValue(Width, "75"), SetDistance(Bracket, "12"))], new Vector2(width, height));

        Assert.Equal("Plate C1  60 → 75 mm", ui.ProposalTexts[DesktopUi.ProposalLineItem(0)]);
        Assert.Equal("Bracket distance  10 → 12 mm", ui.ProposalTexts[DesktopUi.ProposalLineItem(1)]);
        Assert.Equal("2 commands · 2 steps of undo", ui.ProposalTexts[DesktopUi.ProposalCountItem]);
    }

    // Item 3: a value with no value now in the frame, as the distance of an extrude that is not selected, shows the arrow and
    // the proposed value only.
    [Fact]
    public void A_Value_With_No_Value_Now_Shows_The_Arrow_And_The_Proposed_Value()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frames(ui, [Card(null, SetDistance(Bracket, "12"))], values: []);

        Assert.Equal("Bracket distance  → 12 mm", ui.ProposalTexts[DesktopUi.ProposalLineItem(0)]);
    }

    // Item 3: each other command, and a value that the frame does not hold, keep the line of a command: the name and the
    // parameters, with an identifier cut to 8 characters.
    [Fact]
    public void The_Lines_Of_Other_Commands_Do_Not_Change()
    {
        var unknown = Guid.Parse("9e8d7c6b-0000-0000-0000-000000000002");
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frames(ui, [Card(BodyChips, Box(), SetValue(unknown, "75"))]);

        Assert.Equal("CreateBox 75 40 10", ui.ProposalTexts[DesktopUi.ProposalLineItem(0)]);
        Assert.Equal("SetSketchConstraintValue 75000000 9e8d7c6b 75", ui.ProposalTexts[DesktopUi.ProposalLineItem(1)]);
        Assert.All(ui.Chips, chip => Assert.False(chip.Pending, $"The chip {chip.Key} is pending."));
    }

    // Item 1: a proposal that an agent posts to the desktop reaches the window through HostedSession with the proposed value
    // of each command.
    [Fact]
    public async Task A_Posted_Proposal_Gives_Its_Values_To_The_Window()
    {
        var folder = Path.Combine(Path.GetTempPath(), "3de-pending-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "documents"));
        try
        {
            string[] args =
            [
                "--port", "0",
                "--file", Path.Combine(folder, "documents", "plate.3de"),
                $"--{DesktopSurface.DocumentFolderSetting}={Path.Combine(folder, "documents")}",
                $"--{DesktopSurface.UnsavedFolderSetting}={Path.Combine(folder, "unsaved")}",
                $"--Engine:AssetFolder={Path.Combine(folder, "assets")}",
            ];
            var (surface, code, message) = DesktopEngineHost.Start(args, null);
            Assert.True(surface is not null, $"{code}: {message}");
            using var session = surface.Session;
            using (surface)
            {
                using var client = new HttpClient { BaseAddress = new Uri(surface.Address) };
                string Envelope((string Name, string Parameters) command) => $"{{\"name\":\"{command.Name}\",\"schemaVersion\":1,\"parameters\":{command.Parameters}}}";
                var commands = string.Join(",", new[] { SetValue(Width, "75"), Box(), SetDistance(Bracket, "30") }.Select(Envelope));
                using var body = new StringContent($"{{\"summary\":\"Widen the bracket\",\"commands\":[{commands}]}}", System.Text.Encoding.UTF8, "application/json");
                Assert.Equal(System.Net.HttpStatusCode.Created, (await client.PostAsync("/proposals", body)).StatusCode);

                var card = Assert.Single(new HostedSession(surface.Host).Waiting());
                Assert.Equal([new ProposedValue(Plate, Width, null, 75), null, new ProposedValue(null, null, Bracket, 30)], card.Values!);
            }
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
