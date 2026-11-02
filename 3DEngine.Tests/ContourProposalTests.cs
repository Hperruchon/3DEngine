using ThreeDEngine.Core.Vision;

namespace ThreeDEngine.Tests;

// ADR-0047 items 1 and 2 (phase V3, TASK-0155): the proposal of a contour. The images are synthetic, so
// each result is exact in pixels; the same rectangle gives the same four corners as the vision library
// of the spike (3DEngine.VisionTests), at the centres of the pixels.
public class ContourProposalTests
{
    private const int Width = 200;
    private const int Height = 100;

    // An image of one colour, with a second colour where the test says.
    private static byte[] Image(Func<int, int, bool> inside, byte background = 0, byte region = 255)
    {
        var rgba = new byte[Width * Height * 4];
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var value = inside(x, y) ? region : background;
                rgba.AsSpan(((y * Width) + x) * 4, 4).Fill(value);
                rgba[(((y * Width) + x) * 4) + 3] = 255;
            }
        }

        return rgba;
    }

    private static bool Rectangle(int x, int y) => x is >= 40 and <= 159 && y is >= 30 and <= 69;

    [Fact]
    public void A_Light_Rectangle_On_A_Dark_Background_Gives_Its_Four_Corners()
    {
        var polygon = ContourProposal.Propose(Width, Height, Image(Rectangle), out _);

        Assert.Equal([(40.5, 30.5), (159.5, 30.5), (159.5, 69.5), (40.5, 69.5)], polygon);
    }

    // The class of the region is the class that touches the border less, so a dark part on a light
    // background gives the same polygon.
    [Fact]
    public void A_Dark_Rectangle_On_A_Light_Background_Gives_The_Same_Corners()
    {
        var polygon = ContourProposal.Propose(Width, Height, Image(Rectangle, background: 230, region: 20), out _);

        Assert.Equal([(40.5, 30.5), (159.5, 30.5), (159.5, 69.5), (40.5, 69.5)], polygon);
    }

    // Of two regions, the larger one.
    [Fact]
    public void The_Proposal_Takes_The_Largest_Region()
    {
        var polygon = ContourProposal.Propose(Width, Height, Image((x, y) => (x is >= 5 and <= 14 && y is >= 5 and <= 14) || Rectangle(x, y)), out _);

        Assert.Equal([(40.5, 30.5), (159.5, 30.5), (159.5, 69.5), (40.5, 69.5)], polygon);
    }

    // The threshold of Otsu decides the shape: a part of grey 60 with a ring of grey 150 and of 5 pixels
    // around it, on a ground of grey 220. The variance between the part and the rest (4225 for each pixel)
    // is larger than between the part with its ring and the ground (4087), so the ring is not in the part.
    [Fact]
    public void A_Ring_Lighter_Than_The_Part_Stays_Outside_The_Contour()
    {
        static bool Ring(int x, int y) => x is >= 35 and <= 164 && y is >= 25 and <= 74;
        var rgba = Image((x, y) => Rectangle(x, y) || Ring(x, y), background: 220, region: 150);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (Rectangle(x, y))
                    rgba.AsSpan(((y * Width) + x) * 4, 3).Fill(60);
            }
        }

        var polygon = ContourProposal.Propose(Width, Height, rgba, out _);

        Assert.Equal([(40.5, 30.5), (159.5, 30.5), (159.5, 69.5), (40.5, 69.5)], polygon);
    }

    // A disc of radius 40 gives a polygon whose vertices are within two pixels of its circle, with more
    // than eight vertices and fewer than the limit.
    [Fact]
    public void A_Disc_Gives_A_Polygon_Near_Its_Circle()
    {
        static bool Disc(int x, int y) => ((x - 100) * (x - 100)) + ((y - 50) * (y - 50)) <= 40 * 40;

        var polygon = ContourProposal.Propose(Width, Height, Image(Disc), out _)!;

        Assert.InRange(polygon.Count, 9, ContourProposal.MaximumVertices);
        Assert.All(polygon, vertex =>
            Assert.InRange(Math.Sqrt(((vertex.X - 100.5) * (vertex.X - 100.5)) + ((vertex.Y - 50.5) * (vertex.Y - 50.5))), 38.0, 41.0));
    }

    // A region with a border of noise gives at most the limit of vertices.
    [Fact]
    public void A_Noisy_Border_Gives_At_Most_The_Limit_Of_Vertices()
    {
        var polygon = ContourProposal.Propose(Width, Height, Image((x, y) => x is >= 10 and <= 189 && y >= 10 && y <= 50 + ((x * 7919) % 41)), out _)!;

        Assert.InRange(polygon.Count, 3, ContourProposal.MaximumVertices);
    }

    // An image of one grey has no two classes, and a region of one pixel has no polygon.
    [Fact]
    public void An_Image_With_No_Region_Gives_No_Proposal()
    {
        Assert.Null(ContourProposal.Propose(Width, Height, Image((_, _) => false, background: 128), out var flat));
        Assert.Null(ContourProposal.Propose(Width, Height, Image((x, y) => x == 100 && y == 50), out var pixel));
        Assert.Throws<ArgumentException>(() => ContourProposal.Propose(Width, Height, new byte[10], out _));
        Assert.Equal(ContourProposal.NoRegion, flat);
        Assert.Equal(ContourProposal.NoRegion, pixel);
    }

    // Finding V69 of the codebase review of 2026-11-01 (TASK-0162): the trace went out and back along a spur
    // of one pixel, so the polygon touched itself at the tip. The opening of ADR-0047 item 2 removes a spur
    // of one and of two pixels, and keeps the four corners of the rectangle.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void A_Spur_Of_One_Or_Two_Pixels_Goes_Before_The_Trace(int width)
    {
        var polygon = ContourProposal.Propose(Width, Height, Image((x, y) => Rectangle(x, y) || (y >= 50 && y < 50 + width && x is >= 160 and <= 179)), out var reason);

        Assert.Equal([(40.5, 30.5), (159.5, 30.5), (159.5, 69.5), (40.5, 69.5)], polygon);
        Assert.Null(reason);
    }

    // Finding V69: two rectangles with a neck of one pixel gave a polygon that went out and back along the
    // neck. The opening removes the neck, and the proposal takes the larger rectangle.
    [Fact]
    public void A_Neck_Of_One_Pixel_Splits_The_Region()
    {
        var polygon = ContourProposal.Propose(Width, Height, Image((x, y) => (x is >= 20 and <= 89 && y is >= 20 and <= 79)
            || (x is >= 110 and <= 169 && y is >= 30 and <= 69) || (y == 50 && x is >= 90 and <= 109)), out _);

        Assert.Equal([(20.5, 20.5), (89.5, 20.5), (89.5, 79.5), (20.5, 79.5)], polygon);
    }

    // Finding V69: a polygon that is still not simple after the opening gives no proposal and the reason. A
    // slot of 2 pixels along three quarters of a circle of radius 20, open to the top edge: Douglas and Peucker
    // cut across the slot, because the tolerance is larger than the slot. The same slot with a radius of 30
    // gives a simple polygon.
    [Theory]
    [InlineData(20, false)]
    [InlineData(30, true)]
    public void A_Polygon_That_Is_Not_Simple_Gives_The_Reason(int radius, bool simple)
    {
        bool Slotted(int x, int y)
        {
            var d2 = ((x - 100) * (x - 100)) + ((y - 50) * (y - 50));
            var arc = d2 >= radius * radius && d2 < (radius + 2) * (radius + 2) && !(x > 100 && y < 50);
            var cut = x is >= 98 and < 100 && y < 50 - radius + 1;
            return x is >= 5 and <= 194 && y is >= 5 and <= 94 && !arc && !cut;
        }

        var polygon = ContourProposal.Propose(Width, Height, Image(Slotted), out var reason);

        Assert.Equal(simple, polygon is not null);
        Assert.Equal(simple ? null : ContourProposal.NotSimple, reason);
    }
}
