using System.Numerics;
using Engine.Core.Queries;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0133 (phase R18m, TASK-0364): the caption of a proposal is on the band of its arc, along its top, and the command lines
// under the arc lie on no chip and on no item of the history arc. With no GPU. ADR-0133 names this file in its enforced-by
// field.
[Collection(DearImGui.Collection)]
public sealed class ProposalCaptionTests
{
    private static readonly Guid Bracket = Guid.Parse("76000000-0000-0000-0000-000000000001");
    private static readonly Guid Sketch = Guid.Parse("76000000-0000-0000-0000-000000000002");
    private static readonly Guid Width = Guid.Parse("76000000-0000-0000-0000-000000000003");
    private static readonly LiveBody Body = new(Bracket, "Extrude", "Bracket", true);

    private static readonly IReadOnlyList<HistoryStep> Steps =
    [
        new(Bracket, "Extrude", "Bracket", GetFeaturesQueryHandler.Correct, null, HistoryTap.SelectBody),
        new(Sketch, "CreateSketch", "Plate on XY", GetFeaturesQueryHandler.Correct, null, HistoryTap.EditSketch, true),
    ];

    // A proposal that sets the width C1 of the plate from 60 to 75, as the host names it (ProposedValues.Named).
    private static ProposalCard Card(string summary = "Widen the bracket to 75 mm") => new(Guid.Parse("76000000-0000-0000-0000-000000000009"), summary,
        ["SetSketchConstraintValue 76000000 76000000 75"], Values: [new ProposedValue(Sketch, Width, null, 75) { Name = "Plate", Label = "C1", Now = 60 }],
        CommandIds: [Guid.Parse("76000000-0000-0000-0000-00000000000a")]);

    private static bool Meets(Vector4 a, Vector4 b) => a.X < b.Z && b.X < a.Z && a.Y < b.W && b.Y < a.W;

    private static DesktopUi Shown(Vector2 display, Vector2 centre, ProposalCard card, IReadOnlyList<EdgeValues.Value>? values = null)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        for (var frame = 0; frame < 3; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, display, Vector2.One, [Body], Bracket, null, ringCentre: centre, values: values, history: Steps, proposals: [card]);
        }

        return ui;
    }

    private static Vector4 Item(DesktopUi ui, string item) => ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");

    // Item 1: the caption is on the band, between Reject and Accept, with its middle at the top of the ring, and its ends go down
    // along the band, so its box is higher than one line.
    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(1440, 900)]
    [InlineData(1280, 720)]
    public void The_Caption_Is_On_The_Band_Along_The_Top_Of_The_Arc(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Shown(display, new Vector2(width / 2f, height * 0.55f), Card());
        var ring = ui.ProposalRing ?? throw new InvalidOperationException("No ring.");
        var caption = Item(ui, DesktopUi.ProposalCaptionItem);

        Assert.StartsWith("AGENT · Widen", ui.ProposalTexts[DesktopUi.ProposalCaptionItem], StringComparison.Ordinal);
        Assert.True(Math.Abs(((caption.X + caption.Z) / 2f) - ring.Centre.X) < 4f, $"The middle of the caption {caption} is not at the top of the ring {ring}.");
        Assert.True(caption.Y >= ring.Centre.Y - ring.Outer && caption.Y < ring.Centre.Y - ring.Inner, $"The caption {caption} is not on the band of {ring}.");
        Assert.True(caption.X >= Item(ui, DesktopUi.ProposalRejectItem).Z && caption.Z <= Item(ui, DesktopUi.ProposalAcceptItem).X,
            $"The caption {caption} is not between Reject and Accept.");
        Assert.True(caption.W - caption.Y > DesktopUi.ProposalTextSize * 1.2f, $"The caption {caption} is a straight line, not along the arc.");
    }

    // Item 1: a caption longer than the arc is cut there with "...", and the whole caption is the first line under the arc. A
    // caption that the arc holds has no such line.
    [Fact]
    public void A_Long_Caption_Is_Cut_On_The_Arc_And_Whole_Under_It()
    {
        var display = new Vector2(1440, 900);
        var centre = new Vector2(720, 495);
        const string summary = "Widen the bracket and round its edges";

        using var cut = Shown(display, centre, Card(summary));
        var band = cut.ProposalTexts[DesktopUi.ProposalCaptionItem];
        Assert.StartsWith("AGENT · Widen", band, StringComparison.Ordinal);
        Assert.EndsWith("...", band, StringComparison.Ordinal);
        Assert.Equal($"AGENT · {summary}", cut.ProposalTexts[DesktopUi.ProposalWholeCaptionItem]);
        Assert.True(Item(cut, DesktopUi.ProposalWholeCaptionItem).Y < Item(cut, DesktopUi.ProposalLineItem(0)).Y, "The whole caption is not the first line.");

        using var whole = Shown(display, centre, Card());
        Assert.Equal("AGENT · Widen the bracket to 75 mm", whole.ProposalTexts[DesktopUi.ProposalCaptionItem]);
        Assert.False(whole.ProposalTexts.ContainsKey(DesktopUi.ProposalWholeCaptionItem));
    }

    // Item 2 (gap H1): with the chip of the width at each place inside the ring, no line of the proposal lies on the chip or on
    // an item of the history arc. Until TASK-0364 the box of the lines lay on the chip of the width at 1280 by 720.
    [Theory]
    [InlineData(1440, 900)]
    [InlineData(1280, 720)]
    public void No_Line_Of_A_Proposal_Lies_On_A_Chip(int width, int height)
    {
        var display = new Vector2(width, height);
        var centre = new Vector2(width / 2f, height * 0.55f);
        var under = new List<string>();
        var chips = 0;
        for (var dx = -200; dx <= 200; dx += 50)
        {
            for (var dy = -220; dy <= 60; dy += 20)
            {
                var at = centre + new Vector2(dx, dy);
                IReadOnlyList<EdgeValues.Value> values = [new(EdgeValues.ConstraintKey(Width), "60.000 mm", at, 60, Bracket, Sketch, Width, "Width", Name: "C1")];
                using var ui = Shown(display, centre, Card(), values);
                if (ui.ItemRectangle(EdgeValues.ConstraintKey(Width)) is not { } chip)
                    continue;
                chips++;
                var covers = new List<(string Item, Vector4 Box)> { ("chip", chip) };
                covers.AddRange(Steps.Select(step => DesktopUi.HistoryItem(step.Id)).Where(item => ui.ItemRectangle(item) is not null).Select(item => (item, ui.ItemRectangle(item)!.Value)));
                var lines = ui.ProposalTexts.Keys.Where(item => item.StartsWith("Proposal:", StringComparison.Ordinal) && item != DesktopUi.ProposalCaptionItem && item != DesktopUi.ProposalWaitingItem)
                    .Select(item => (Item: item, Box: Item(ui, item)));
                under.AddRange(lines.SelectMany(line => covers.Where(cover => Meets(line.Box, cover.Box)).Select(cover => $"chip at {at}: {line.Item} {line.Box} on {cover.Item} {cover.Box}")));
            }
        }

        Assert.True(chips > 50, $"Only {chips} chips show.");
        Assert.True(under.Count == 0, string.Join("; ", under));
    }
}
