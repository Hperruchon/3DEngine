using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0091 (the redesign, slice i, TASK-0292): the atlas of the interface holds Instrument Sans, the font of the interface,
// and JetBrains Mono, the font of a value. JetBrains Mono gives each character one width; Instrument Sans does not. Until
// TASK-0292 the atlas held the default bitmap font of Dear ImGui only.
[Collection(DearImGui.Collection)]
public sealed class FontTests
{
    [Fact]
    public void The_Atlas_Holds_The_Font_Of_The_Interface_And_The_Font_Of_A_Value()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);

        Assert.Equal(2, ui.FontCount);
        var (wide, narrow, one, eight) = (ui.TextWidths("WWWWWWWWWW"), ui.TextWidths("iiiiiiiiii"), ui.TextWidths("1111111111"), ui.TextWidths("8888888888"));
        Assert.Equal(wide.Mono, narrow.Mono);
        Assert.Equal(one.Mono, eight.Mono);
        Assert.NotEqual(wide.Sans, narrow.Sans);
    }
}
