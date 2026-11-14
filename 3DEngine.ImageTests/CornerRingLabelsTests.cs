using System.Numerics;
using Engine.Core.Queries;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0132 (phase R18l, TASK-0363): on the corner ring of a portrait display each label is in its segment, with no label on
// another one and each inside the display; a label wider than its room is drawn smaller. With no GPU. ADR-0132 names this
// file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class CornerRingLabelsTests
{
    private static readonly Guid Plate = Guid.Parse("75000000-0000-0000-0000-000000000001");
    private static readonly Guid Sketch = Guid.Parse("75000000-0000-0000-0000-000000000002");
    private static readonly LiveBody Body = new(Plate, "Extrude", "Plate", true);

    // The history of an extrude of a sketch, so the ring holds Edit sketch, Move, Distance, Hide, Focus and Dimensions.
    private static readonly IReadOnlyList<HistoryStep> Steps =
    [
        new(Plate, "Extrude", "Plate", GetFeaturesQueryHandler.Correct, null, HistoryTap.SelectBody),
        new(Sketch, "CreateSketch", "Plate on XY", GetFeaturesQueryHandler.Correct, null, HistoryTap.EditSketch, true),
    ];

    private static bool Overlap(Vector4 a, Vector4 b) => a.X < b.Z && b.X < a.Z && a.Y < b.W && b.Y < a.W;

    [Theory]
    [InlineData(390, 844)]
    [InlineData(360, 640)]
    [InlineData(384, 824)]
    [InlineData(412, 915)]
    [InlineData(768, 1024)]
    public void No_Label_Of_The_Corner_Ring_Lies_On_Another(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        for (var frame = 0; frame < 3; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, display, Vector2.One, [Body], Plate, null, ringCentre: display / 2f, ringBody: 80f, history: Steps);
        }

        var labels = new[] { DesktopUi.EditSketchLabel, DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel, DesktopUi.FocusLabel, DesktopUi.DimensionsLabel };
        var texts = labels.Select(label => (Label: label, Text: ui.ItemRectangle(DesktopUi.RingTextItem(label)))).Where(entry => entry.Text is not null)
            .Select(entry => (entry.Label, Text: entry.Text!.Value)).ToList();
        Assert.Contains(texts, entry => entry.Label == DesktopUi.DimensionsLabel);
        foreach (var (label, text) in texts)
            Assert.True(text.X >= 0f && text.Z <= width && text.Y >= 0f && text.W <= height, $"{label} at {text} is out of the display {display}.");
        for (var i = 0; i < texts.Count; i++)
        {
            for (var j = i + 1; j < texts.Count; j++)
                Assert.False(Overlap(texts[i].Text, texts[j].Text), $"{texts[i].Label} at {texts[i].Text} lies on {texts[j].Label} at {texts[j].Text} on {display}.");
        }
    }
}
