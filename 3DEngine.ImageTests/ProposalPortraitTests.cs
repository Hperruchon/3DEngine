using System.Numerics;
using Engine.Core.Queries;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0135 (phase R18o, TASK-0368): on a portrait display the arc of a proposal is at the top and the sheet of its lines under
// it; the rows of the selected body stay under the sheet, and while the proposal waits no chip lies on the arc, the sheet or
// the rows. With no GPU. ADR-0135 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class ProposalPortraitTests
{
    private static readonly Guid Bracket = Guid.Parse("78000000-0000-0000-0000-000000000001");
    private static readonly Guid Sketch = Guid.Parse("78000000-0000-0000-0000-000000000002");
    private static readonly Guid Width = Guid.Parse("78000000-0000-0000-0000-000000000003");
    private static readonly LiveBody Body = new(Bracket, "Extrude", "Bracket", true);

    private static readonly IReadOnlyList<HistoryStep> Steps =
    [
        new(Bracket, "Extrude", "Bracket", GetFeaturesQueryHandler.Correct, null, HistoryTap.SelectBody),
        new(Sketch, "CreateSketch", "Plate on XY", GetFeaturesQueryHandler.Correct, null, HistoryTap.EditSketch, true),
    ];

    private static readonly ProposalCard Card = new(Guid.Parse("78000000-0000-0000-0000-000000000009"), "Widen the bracket to 75 mm",
        ["SetSketchConstraintValue 78000000 78000000 75"], Values: [new ProposedValue(Sketch, Width, null, 75) { Name = "Plate", Label = "C1", Now = 60 }],
        CommandIds: [Guid.Parse("78000000-0000-0000-0000-00000000000a")]);

    private static bool Meets(Vector4 a, Vector4 b) => a.X < b.Z && b.X < a.Z && a.Y < b.W && b.Y < a.W;

    private static bool Holds(Vector4 panel, Vector4 box) => box.X >= panel.X && box.X < panel.Z && box.Y >= panel.Y && box.Y < panel.W;

    private static DesktopUi Shown(Vector2 display, IReadOnlyList<EdgeValues.Value> values)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        for (var frame = 0; frame < 4; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, display, Vector2.One, [Body], Bracket, null, ringCentre: new Vector2(display.X / 2f, display.Y * 0.4f), values: values, history: Steps, proposals: [Card]);
        }

        return ui;
    }

    // The sheet is the smallest panel that holds the top left of its first text that shows; lines that do not fit scroll.
    private static Vector4 Sheet(DesktopUi ui)
    {
        var first = new[] { DesktopUi.ProposalWholeCaptionItem, DesktopUi.ProposalCaptionItem, DesktopUi.ProposalLineItem(0) }
            .Select(ui.ItemRectangle).Where(box => box is not null).Select(box => box!.Value).Where(box => ui.PanelRectangles.Any(panel => Holds(panel, box))).MinBy(box => box.Y);
        return ui.PanelRectangles.Where(panel => Holds(panel, first)).MinBy(panel => panel.W - panel.Y);
    }

    // Item 4: with the chip of the width at each place from 15 to 70 percent of the height of a phone and of a tablet, no chip
    // lies on Reject, Accept, their words, the caption, the sheet or the rows. Until TASK-0368 the sheet lay on the chip of the
    // width. The mix test at 390 by 844 proves the rows too, with the dimension lines of the bracket.
    [Theory]
    [InlineData(390, 844)]
    [InlineData(768, 1024)]
    public void No_Chip_Lies_On_The_Arc_The_Sheet_Or_The_Rows(int width, int height)
    {
        var display = new Vector2(width, height);
        var faults = new List<string>();
        var chips = 0;
        for (var x = 0.2f; x <= 0.81f; x += 0.15f)
        {
            for (var y = 0.15f; y <= 0.71f; y += 0.05f)
            {
                IReadOnlyList<EdgeValues.Value> values = [new(EdgeValues.ConstraintKey(Width), "60.000 mm", new Vector2(width * x, height * y), 60, Bracket, Sketch, Width, "Width", Name: "C1")];
                using var ui = Shown(display, values);
                if (ui.ItemRectangle(EdgeValues.ConstraintKey(Width)) is not { } chip)
                    continue;
                chips++;
                var covers = new[] { DesktopUi.ProposalRejectItem, DesktopUi.ProposalAcceptItem, DesktopUi.ProposalCaptionItem }
                    .Select(item => (Item: item, Box: ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No {item}.")))
                    .Concat(ui.ProposalWords.Select(word => (Item: "word", Box: word)))
                    .Append((Item: "sheet", Box: Sheet(ui)))
                    .Append((Item: "rows", Box: ui.RowsArea ?? throw new InvalidOperationException("No rows.")));
                faults.AddRange(covers.Where(cover => Meets(chip, cover.Box)).Select(cover => $"chip at {x}/{y} {chip} lies on {cover.Item} {cover.Box}"));
            }
        }

        Assert.True(chips > 20, $"Only {chips} chips show.");
        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    // Item 3: the rows of the selected body stay under the sheet, at each portrait size, also where the room under the middle
    // of the display is lower than the first rows.
    [Theory]
    [InlineData(360, 640)]
    [InlineData(390, 560)]
    [InlineData(390, 844)]
    [InlineData(768, 1024)]
    public void The_Rows_Stay_Under_The_Sheet(int width, int height)
    {
        using var ui = Shown(new Vector2(width, height), []);
        var rows = ui.RowsArea ?? throw new InvalidOperationException("No rows.");
        var sheet = Sheet(ui);
        Assert.True(rows.Y >= sheet.W, $"{width}x{height}: the rows {rows} go above the bottom of the sheet {sheet}.");
    }

    // Finding V190 of the codebase review of 2026-11-14 (d) (TASK-0374): in the first frame of a proposal the rows are already
    // under the sheet. Until TASK-0374 the floor of the rows came from the sheet of the last frame.
    [Theory]
    [InlineData(360, 640)]
    [InlineData(390, 844)]
    public void In_The_First_Frame_Of_A_Proposal_The_Rows_Are_Under_The_Sheet(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        for (var frame = 0; frame < 3; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, display, Vector2.One, [Body], Bracket, null, ringCentre: new Vector2(width / 2f, height * 0.4f), history: Steps);
        }

        ui.Route([]);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], Bracket, null, ringCentre: new Vector2(width / 2f, height * 0.4f), history: Steps, proposals: [Card]);
        var rows = ui.RowsArea ?? throw new InvalidOperationException("No rows.");
        var sheet = Sheet(ui);
        Assert.True(rows.Y >= sheet.W, $"{width}x{height}: the rows {rows} go above the bottom of the sheet {sheet} in the first frame.");
    }
}
