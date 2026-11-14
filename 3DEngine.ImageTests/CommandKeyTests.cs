using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0150 item 2 (phase P0.154, TASK-0385): the command line shows the keys of its platform: "Cmd K" on macOS and "Ctrl K"
// on the other platforms (R-0060). ADR-0150 names this file in enforced-by.
public sealed class CommandKeyTests
{
    [Fact]
    public void The_Command_Line_Shows_The_Keys_Of_Its_Platform()
    {
        Assert.Equal("Cmd K", DesktopUi.CommandKeyTextFor(macOS: true));
        Assert.Equal("Ctrl K", DesktopUi.CommandKeyTextFor(macOS: false));
        Assert.Equal(DesktopUi.CommandKeyTextFor(OperatingSystem.IsMacOS()), DesktopUi.CommandKeyText);
    }
}
