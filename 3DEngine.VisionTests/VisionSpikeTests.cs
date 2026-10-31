using OpenCvSharp;

namespace ThreeDEngine.VisionTests;

// Phase V0 (ADR-0042, TASK-0145): the vision library on the three platforms of the pipeline. Each test
// fails when the native library does not load, so a runner with no payload is red and not skipped.
// The image is synthetic, so each result is exact in pixels on each platform.
public sealed class VisionSpikeTests
{
    // ADR-0042 item 1: the native library loads and is the version of the pins.
    [Fact]
    public void The_Native_Library_Loads_And_Gives_Its_Version()
    {
        Assert.StartsWith("4.13.", Cv2.GetVersionString(), StringComparison.Ordinal);
    }

    // V3 turns a contour into a sketch. A filled rectangle gives one outer contour, and its polygon has
    // the four corners of the rectangle.
    [Fact]
    public void The_Contour_Of_A_Rectangle_Has_Its_Four_Corners()
    {
        using var image = Rectangle();

        Cv2.FindContours(image, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        var contour = Assert.Single(contours);
        var corners = Cv2.ApproxPolyDP(contour, 2, closed: true)
            .Select(point => (point.X, point.Y))
            .Order()
            .ToArray();
        Assert.Equal([(40, 30), (40, 69), (159, 30), (159, 69)], corners);
    }

    // The edges of the same rectangle lie on its border only.
    [Fact]
    public void The_Edges_Of_A_Rectangle_Lie_On_Its_Border()
    {
        using var image = Rectangle();
        using var edges = new Mat();

        Cv2.Canny(image, edges, 50, 150);

        Assert.True(Cv2.CountNonZero(edges) > 0);
        // The analyzer of the package refuses a call of Rows or Cols in each iteration (OCVS002).
        var (rows, cols) = (edges.Rows, edges.Cols);
        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < cols; x++)
            {
                if (edges.At<byte>(y, x) == 0)
                    continue;
                var nearBorder = x is >= 38 and <= 41 or >= 158 and <= 161 || y is >= 28 and <= 31 or >= 68 and <= 71;
                Assert.True(nearBorder, $"An edge pixel at ({x}, {y}) is not on the border of the rectangle.");
            }
        }
    }

    // A black image of 200 by 100 pixels with a white rectangle from (40, 30) to (159, 69).
    private static Mat Rectangle()
    {
        var image = new Mat(100, 200, MatType.CV_8UC1, Scalar.Black);
        Cv2.Rectangle(image, new Point(40, 30), new Point(159, 69), Scalar.White, thickness: -1);
        return image;
    }
}
