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
        var polygon = ContourProposal.Propose(Width, Height, Image(Rectangle));

        Assert.Equal([(40.5, 30.5), (159.5, 30.5), (159.5, 69.5), (40.5, 69.5)], polygon);
    }

    // The class of the region is the class that touches the border less, so a dark part on a light
    // background gives the same polygon.
    [Fact]
    public void A_Dark_Rectangle_On_A_Light_Background_Gives_The_Same_Corners()
    {
        var polygon = ContourProposal.Propose(Width, Height, Image(Rectangle, background: 230, region: 20));

        Assert.Equal([(40.5, 30.5), (159.5, 30.5), (159.5, 69.5), (40.5, 69.5)], polygon);
    }

    // Of two regions, the larger one.
    [Fact]
    public void The_Proposal_Takes_The_Largest_Region()
    {
        var polygon = ContourProposal.Propose(Width, Height, Image((x, y) => (x is >= 5 and <= 14 && y is >= 5 and <= 14) || Rectangle(x, y)));

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

        var polygon = ContourProposal.Propose(Width, Height, rgba);

        Assert.Equal([(40.5, 30.5), (159.5, 30.5), (159.5, 69.5), (40.5, 69.5)], polygon);
    }

    // A disc of radius 40 gives a polygon whose vertices are within two pixels of its circle, with more
    // than eight vertices and fewer than the limit.
    [Fact]
    public void A_Disc_Gives_A_Polygon_Near_Its_Circle()
    {
        static bool Disc(int x, int y) => ((x - 100) * (x - 100)) + ((y - 50) * (y - 50)) <= 40 * 40;

        var polygon = ContourProposal.Propose(Width, Height, Image(Disc))!;

        Assert.InRange(polygon.Count, 9, ContourProposal.MaximumVertices);
        Assert.All(polygon, vertex =>
            Assert.InRange(Math.Sqrt(((vertex.X - 100.5) * (vertex.X - 100.5)) + ((vertex.Y - 50.5) * (vertex.Y - 50.5))), 38.0, 41.0));
    }

    // A region with a border of noise gives at most the limit of vertices.
    [Fact]
    public void A_Noisy_Border_Gives_At_Most_The_Limit_Of_Vertices()
    {
        var polygon = ContourProposal.Propose(Width, Height, Image((x, y) => x is >= 10 and <= 189 && y >= 10 && y <= 50 + ((x * 7919) % 41)))!;

        Assert.InRange(polygon.Count, 3, ContourProposal.MaximumVertices);
    }

    // An image of one grey has no two classes, and a region of one pixel has no polygon.
    [Fact]
    public void An_Image_With_No_Region_Gives_No_Proposal()
    {
        Assert.Null(ContourProposal.Propose(Width, Height, Image((_, _) => false, background: 128)));
        Assert.Null(ContourProposal.Propose(Width, Height, Image((x, y) => x == 100 && y == 50)));
        Assert.Throws<ArgumentException>(() => ContourProposal.Propose(Width, Height, new byte[10]));
    }
}
