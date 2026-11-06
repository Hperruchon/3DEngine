using System.Numerics;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0078 (the redesign, slice a, TASK-0246): the theme "Proposed (harmonized)", with no GPU. ADR-0078 names this file in
// its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class ThemeTests
{
    private static readonly Vector2 Display = new(1280, 720);

    // Item 1: the panel takes the colours of the theme, and the sizes of the controls stay.
    [Fact]
    public void The_Panel_Takes_The_Colours_Of_The_Theme_And_Keeps_Its_Sizes()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        foreach (var (slot, rgba) in Theme.Chrome)
            Assert.Equal(Theme.Vector(rgba), ui.StyleColour(slot));
        Assert.Equal(new Vector4(0x10 / 255f, 0x14 / 255f, 0x19 / 255f, 0xF0 / 255f), Theme.Vector(Theme.Chrome[1].Rgba));
        Assert.Equal((new Vector2(12f, DesktopUi.FramePaddingY), 6f, 4f), ui.StyleSizes());
    }

    // Item 2: each line of a sketch lies over a dark casing, wider by two pixels on each side, so it holds its contrast on
    // each face and on the clear.
    [Fact]
    public void A_Line_Of_A_Sketch_Lies_Over_A_Casing()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var strokes = new SketchOverlay.Strokes([new(new Vector2(700, 300), new Vector2(900, 500), SketchOverlay.Mark.Active)], [], [], []);

        var drawn = ui.Frame(1f / 60f, Display, Vector2.One, [], null, null, strokes);

        var casing = drawn.Vertices.Where(vertex => vertex.Colour == Theme.Casing).ToList();
        var line = drawn.Vertices.Where(vertex => vertex.Colour == DesktopUi.SketchColour).ToList();
        Assert.NotEmpty(casing);
        Assert.NotEmpty(line);
        Assert.True(drawn.Vertices.ToList().IndexOf(casing[0]) < drawn.Vertices.ToList().IndexOf(line[0]), "The casing is not under the line.");
    }
}
