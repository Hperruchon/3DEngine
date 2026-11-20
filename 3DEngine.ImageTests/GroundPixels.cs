using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0178 item 1 (phase R18p, TASK-0423): the ground of the scene has the soft light of the render, so its colour changes
// from pixel to pixel. A test that asks "is this pixel the ground" compares the pixel with Theme.GroundAt at that pixel.
// Until TASK-0423 each test compared a pixel with the one colour Theme.Clear.
internal static class GroundPixels
{
    // True when the pixel (column, row) of an RGBA picture of width by height is the ground there, within the tolerance on
    // each channel.
    internal static bool IsGround(byte[] rgba, int column, int row, int width, int height, int tolerance = 2)
    {
        var (r, g, b) = Theme.GroundAt(column, row, width, height);
        var index = ((row * width) + column) * 4;
        return Math.Abs(rgba[index] - r) <= tolerance && Math.Abs(rgba[index + 1] - g) <= tolerance && Math.Abs(rgba[index + 2] - b) <= tolerance;
    }
}
