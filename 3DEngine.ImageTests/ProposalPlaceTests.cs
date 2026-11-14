using System.Numerics;
using Engine.Core.Queries;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0134 (phase R18n, TASK-0365): the arc of a proposal on the ring of the selected body lies on no row of the body and on
// no tool of the ring: its ring moves right of the rows, and the command lines keep off the tools. With no GPU. ADR-0134 names
// this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class ProposalPlaceTests
{
    private static readonly Guid Bracket = Guid.Parse("77000000-0000-0000-0000-000000000001");
    private static readonly Guid Sketch = Guid.Parse("77000000-0000-0000-0000-000000000002");
    private static readonly Guid Width = Guid.Parse("77000000-0000-0000-0000-000000000003");
    private static readonly LiveBody Body = new(Bracket, "Extrude", "Bracket", true);

    private static readonly IReadOnlyList<HistoryStep> Steps =
    [
        new(Bracket, "Extrude", "Bracket", GetFeaturesQueryHandler.Correct, null, HistoryTap.SelectBody),
        new(Sketch, "CreateSketch", "Plate on XY", GetFeaturesQueryHandler.Correct, null, HistoryTap.EditSketch, true),
    ];

    private static readonly ProposalCard Card = new(Guid.Parse("77000000-0000-0000-0000-000000000009"), "Widen the bracket to 75 mm",
        ["SetSketchConstraintValue 77000000 77000000 75"], Values: [new ProposedValue(Sketch, Width, null, 75) { Name = "Plate", Label = "C1", Now = 60 }],
        CommandIds: [Guid.Parse("77000000-0000-0000-0000-00000000000a")]);

    private static readonly string[] Tools =
        [DesktopUi.EditSketchLabel, DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel, DesktopUi.FocusLabel, DesktopUi.DimensionsLabel, DesktopUi.RenameLabel];

    private static bool Meets(Vector4 a, Vector4 b) => a.X < b.Z && b.X < a.Z && a.Y < b.W && b.Y < a.W;

    // Items 1 and 2 (gap H2): at each landscape size, with the body at three places, each item of the arc is inside the
    // display. From 1024 pixels wide no item of the arc lies on the rows of the body, and neither the caption nor a line lies
    // on the target of a tool of the ring. Under that, the moved arc passes the right edge, so it stays on the ring of the body,
    // and that ring is too small to keep its lines off the tools (R-0066).
    // Until TASK-0365 the arc and Reject lay on the rows "Made from" and "Distance" at 1024 by 768.
    [Theory]
    [InlineData(800, 600, false)]
    [InlineData(900, 600, false)]
    [InlineData(1024, 768, true)]
    [InlineData(1280, 720, true)]
    [InlineData(1440, 900, true)]
    [InlineData(1920, 1080, true)]
    public void The_Arc_Lies_On_No_Row_And_No_Tool(int width, int height, bool large)
    {
        var display = new Vector2(width, height);
        IReadOnlyList<EdgeValues.Value> values =
        [
            new(EdgeValues.DistanceKey(Bracket), "10.000 mm", new Vector2(width * 0.3f, height * 0.6f), 10, Bracket, null, null, "Distance"),
            new(EdgeValues.ConstraintKey(Width), "60.000 mm", new Vector2(width * 0.45f, height * 0.4f), 60, Bracket, Sketch, Width, "Width", Name: "C1"),
        ];
        var faults = new List<string>();
        var moved = 0;
        foreach (var across in new[] { 0.4f, 0.5f, 0.6f })
        {
            var centre = new Vector2(width * across, height * 0.55f);
            using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
            for (var frame = 0; frame < 3; frame++)
            {
                ui.Route([]);
                ui.Frame(1f / 60f, display, Vector2.One, [Body], Bracket, null, ringCentre: centre, values: values, history: Steps, proposals: [Card]);
            }

            var rows = ui.RowsArea ?? throw new InvalidOperationException("No rows.");
            var ring = ui.ProposalRing ?? throw new InvalidOperationException("No ring.");
            moved += ring.Centre.X > centre.X + 1f ? 1 : 0;
            var arc = new[] { DesktopUi.ProposalRejectItem, DesktopUi.ProposalAcceptItem }.Concat(ui.ProposalTexts.Keys.Where(item => item.StartsWith("Proposal:", StringComparison.Ordinal) && item != DesktopUi.ProposalWaitingItem))
                .Select(item => (Item: item, Box: ui.ItemRectangle(item))).Where(entry => entry.Box is not null).Select(entry => (entry.Item, Box: entry.Box!.Value))
                .Concat(ui.ProposalWords.Select(word => (Item: "word", Box: word))).ToList();
            var tools = Tools.Select(label => (Label: label, Target: ui.RingTarget(label))).Where(entry => entry.Target is not null).Select(entry => (entry.Label, Target: entry.Target!.Value)).ToList();
            Assert.NotEmpty(tools);
            foreach (var (item, box) in arc)
            {
                if (large && Meets(box, rows))
                    faults.Add($"{across}: {item} {box} lies on the rows {rows}");
                // The ends of the arc are on the band of the ring with the tools, and they take a tap first (finding V160 of the
                // codebase review of 2026-11-09 (b)), so only the caption and the lines must keep off the tools.
                if (large && item.StartsWith("Proposal:", StringComparison.Ordinal) && item is not DesktopUi.ProposalRejectItem and not DesktopUi.ProposalAcceptItem)
                    faults.AddRange(tools.Where(tool => Meets(box, tool.Target)).Select(tool => $"{across}: {item} {box} lies on {tool.Label} {tool.Target}"));
                if (box.X < 0f || box.Y < 0f || box.Z > width || box.W > height)
                    faults.Add($"{across}: {item} {box} is out of the display");
            }
        }

        Assert.True(faults.Count == 0, string.Join("; ", faults));
        if (width == 1024)
            Assert.True(moved > 0, "No ring of the arc moved right of the rows at 1024 by 768.");
    }
}
