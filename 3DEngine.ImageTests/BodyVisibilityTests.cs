using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// Finding V27 of the codebase review of 2026-10-28 (TASK-0090): the count of visible bodies
// that each frame reads is kept until a change, and a read of the visibility is a lookup.
public class BodyVisibilityTests
{
    [Fact]
    public void The_Count_Holds_The_Visible_Bodies_That_Have_A_Mesh()
    {
        var visibility = new BodyVisibility();
        visibility.Set("a", true);
        visibility.SetMesh("a");
        visibility.Set("b", false);
        visibility.SetMesh("b");
        visibility.Set("c", true);

        Assert.Equal(1, visibility.VisibleCount);
        Assert.True(visibility.IsVisible("c"));
        Assert.False(visibility.IsVisible("b"));
        Assert.False(visibility.IsVisible("unknown"));

        visibility.Set("b", true);
        visibility.Remove("a");
        Assert.Equal(1, visibility.VisibleCount);
        Assert.False(visibility.IsVisible("a"));
    }

    // A frame reads the count once; with no change the count is not computed again.
    [Fact]
    public void A_Frame_With_No_Change_Does_Not_Count_Again()
    {
        var visibility = new BodyVisibility();
        for (var index = 0; index < 1000; index++)
        {
            visibility.Set($"body {index}", index % 2 == 0);
            visibility.SetMesh($"body {index}");
        }

        for (var frame = 0; frame < 1000; frame++)
            Assert.Equal(500, visibility.VisibleCount);
        Assert.Equal(1, visibility.Counts);

        visibility.Set("body 1", true);
        Assert.Equal(501, visibility.VisibleCount);
        Assert.Equal(2, visibility.Counts);
    }
}
