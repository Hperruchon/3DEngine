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
        Assert.Equal(new Vector4(0x0B / 255f, 0x12 / 255f, 0x19 / 255f, 0xF0 / 255f), Theme.Vector(Theme.Chrome[1].Rgba));
        Assert.Equal((new Vector2(12f, DesktopUi.FramePaddingY), 6f, 4f), ui.StyleSizes());
    }

    // ADR-0108 items 1 and 2 (phase R18a, TASK-0332): the ground is almost black, and a button and a field have no fill at
    // rest; a hover and a press give a light fill.
    [Fact]
    public void The_Ground_Is_Almost_Black_And_A_Control_Has_No_Fill_At_Rest()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Assert.True(Luminance(Theme.Clear.R, Theme.Clear.G, Theme.Clear.B) < 0.01, "The ground is not almost black.");
        var look = ui.LookColours();
        Assert.Equal(0f, look.AtRest.W);
        Assert.Equal(0f, look.Field.W);
        Assert.InRange(look.Hover.W, 0.1f, 0.4f);
        Assert.InRange(look.Press.W, 0.1f, 0.4f);
    }

    // ADR-0108 item 1: each colour of a text keeps a contrast of 4.5:1 on the ground, and each colour of a mark or an
    // outline keeps 3:1, as WCAG 2.2 gives the ratio.
    [Fact]
    public void Each_Colour_Keeps_Its_Contrast_On_The_Ground()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var ground = Luminance(Theme.Clear.R, Theme.Clear.G, Theme.Clear.B);
        var look = ui.LookColours();
        static (byte R, byte G, byte B) Style(Vector4 colour) => ((byte)Math.Round(colour.X * 255f), (byte)Math.Round(colour.Y * 255f), (byte)Math.Round(colour.Z * 255f));
        static (byte R, byte G, byte B) Packed(uint abgr) => ((byte)(abgr & 0xFF), (byte)((abgr >> 8) & 0xFF), (byte)((abgr >> 16) & 0xFF));
        (string Name, (byte R, byte G, byte B) Colour, double Least)[] colours =
        [
            ("text", Style(look.Text), 4.5),
            ("quiet text", Style(look.Quiet), 4.5),
            ("correct", Packed(Theme.Correct), 4.5),
            ("failed", Packed(DesktopUi.FailedColour), 4.5),
            ("amber", Packed(DesktopUi.DimensionColour), 4.5),
            ("accent", Packed(Theme.Accent), 3.0),
            ("outline", Style(look.Outline), 3.0),
            ("sketch", Packed(DesktopUi.SketchColour), 3.0),
            ("other sketch", Packed(Theme.OtherSketch), 3.0),
            ("reference", Packed(Theme.Reference), 3.0),
            ("snap", Packed(Theme.Snap), 3.0),
        ];
        var faults = colours
            .Select(entry => (entry.Name, entry.Least, Ratio: (Luminance(entry.Colour.R, entry.Colour.G, entry.Colour.B) + 0.05) / (ground + 0.05)))
            .Where(entry => entry.Ratio < entry.Least)
            .Select(entry => $"{entry.Name} gives {entry.Ratio:0.00}:1, less than {entry.Least}:1")
            .ToList();
        Assert.True(faults.Count == 0, string.Join(Environment.NewLine, faults));
    }

    // The relative luminance of WCAG 2.2.
    private static double Luminance(byte r, byte g, byte b)
    {
        static double Linear(byte channel)
        {
            var c = channel / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Linear(r)) + (0.7152 * Linear(g)) + (0.0722 * Linear(b));
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
