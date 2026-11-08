using ThreeDEngine.Core.Measuring;

namespace ThreeDEngine.Tests;

// The dimensions of a box for the overlay of the desktop (ADR-0056, TASK-0178). ADR-0056 names this file in
// enforced-by. The place of each line from the side of the eye, the values and the labels, the thickness, and the union
// of the boxes.
public class DimensionsTests
{
    // A shelf of 1200 by 1900 by 370 with its lowest corner at the origin.
    private static readonly (double X, double Y, double Z) Min = (0, 0, 0);
    private static readonly (double X, double Y, double Z) Max = (1200, 1900, 370);

    // Item 4: from an eye on the side of +X and +Z, the lines of X and Z lie at the lowest Y on the near faces, out by one
    // twentieth of 1900, and the line of Y lies on the near X at the far Z.
    [Fact]
    public void Each_Line_Lies_On_The_Side_Of_The_Eye()
    {
        var dimensions = Dimensions.Of(Min, Max, (3000, 1000, 2000));

        Assert.Equal([Dimensions.Axis.X, Dimensions.Axis.Y, Dimensions.Axis.Z], dimensions.Select(d => d.Along));
        Assert.Equal([1200.0, 1900.0, 370.0], dimensions.Select(d => d.Value));
        var (x, y, z) = (dimensions[0], dimensions[1], dimensions[2]);
        Assert.Equal(((0.0, 0.0, 370.0), (1200.0, 0.0, 370.0)), (x.EdgeFrom, x.EdgeTo));
        Assert.Equal(((0.0, 0.0, 465.0), (1200.0, 0.0, 465.0)), (x.From, x.To));
        Assert.Equal(((1200.0, 0.0, 0.0), (1200.0, 1900.0, 0.0)), (y.EdgeFrom, y.EdgeTo));
        Assert.Equal(((1295.0, 0.0, 0.0), (1295.0, 1900.0, 0.0)), (y.From, y.To));
        Assert.Equal(((1295.0, 0.0, 0.0), (1295.0, 0.0, 370.0)), (z.From, z.To));
    }

    // Finding V147 of the codebase review of 2026-11-08 (c) (TASK-0311): with the z axis up, from an eye on the side of +X
    // and +Y, the lines of X and Y lie at the lowest Z on the near faces, out by one twentieth of 1900, and the line of Z
    // lies on the near X at the far Y; an eye on the side of −Y moves the line of X to the face of −Y. Until TASK-0311 the
    // lines read only the X and the Z of the eye.
    [Fact]
    public void With_Z_Up_Each_Line_Lies_On_The_Side_Of_The_Eye()
    {
        var dimensions = Dimensions.Of(Min, Max, (3000, 4000, 1000), zUp: true);

        Assert.Equal([Dimensions.Axis.X, Dimensions.Axis.Y, Dimensions.Axis.Z], dimensions.Select(d => d.Along));
        Assert.Equal([1200.0, 1900.0, 370.0], dimensions.Select(d => d.Value));
        var (x, y, z) = (dimensions[0], dimensions[1], dimensions[2]);
        Assert.Equal(((0.0, 1995.0, 0.0), (1200.0, 1995.0, 0.0)), (x.From, x.To));
        Assert.Equal(((1295.0, 0.0, 0.0), (1295.0, 1900.0, 0.0)), (y.From, y.To));
        Assert.Equal(((1200.0, 0.0, 0.0), (1200.0, 0.0, 370.0)), (z.EdgeFrom, z.EdgeTo));

        var front = Dimensions.Of(Min, Max, (3000, -4000, 1000), zUp: true);
        Assert.Equal(((0.0, -95.0, 0.0), (1200.0, -95.0, 0.0)), (front[0].From, front[0].To));
        Assert.Equal(((1200.0, 1900.0, 0.0), (1200.0, 1900.0, 370.0)), (front[2].EdgeFrom, front[2].EdgeTo));
    }

    // Item 4: from an eye on the side of −X and −Z, each line moves to the other faces.
    [Fact]
    public void An_Eye_On_The_Other_Side_Moves_Each_Line()
    {
        var dimensions = Dimensions.Of(Min, Max, (-500, 1000, -800));

        Assert.Equal(((0.0, 0.0, -95.0), (1200.0, 0.0, -95.0)), (dimensions[0].From, dimensions[0].To));
        Assert.Equal(((-95.0, 0.0, 370.0), (-95.0, 1900.0, 370.0)), (dimensions[1].From, dimensions[1].To));
        Assert.Equal(((-95.0, 0.0, 0.0), (-95.0, 0.0, 370.0)), (dimensions[2].From, dimensions[2].To));
    }

    // Item 4: an extent of zero has no line, and a box with no extent has no dimension.
    [Fact]
    public void An_Extent_Of_Zero_Has_No_Line()
    {
        Assert.Equal([Dimensions.Axis.X, Dimensions.Axis.Z], Dimensions.Of((0, 5, 0), (10, 5, 4), (20, 20, 20)).Select(d => d.Along));
        Assert.Empty(Dimensions.Of((1, 2, 3), (1, 2, 3), (20, 20, 20)));
        Assert.Empty(Dimensions.Of((0, 0, 0), (double.PositiveInfinity, 1, 1), (20, 20, 20)));
    }

    // Item 5: at most two decimals in the invariant culture, a half away from zero, and the axis and the unit.
    [Theory]
    [InlineData(1200.0, "1200")]
    [InlineData(18.0, "18")]
    [InlineData(2.5, "2.5")]
    [InlineData(0.125, "0.13")]
    [InlineData(1234.5678, "1234.57")]
    public void A_Value_Has_At_Most_Two_Decimals(double value, string text)
        => Assert.Equal(text, Dimensions.Format(value));

    [Fact]
    public void A_Label_Gives_The_Axis_The_Value_And_The_Unit()
        => Assert.Equal(["X 1200 mm", "Y 1900 mm", "Z 370 mm"], Dimensions.Of(Min, Max, (3000, 1000, 2000)).Select(d => Dimensions.Label(d, "mm")));

    // Item 6 (Q1 A): the distance in effect of a live extrude, as an absolute value, else the smallest extent of the box.
    [Fact]
    public void The_Thickness_Is_The_Distance_Of_An_Extrude_Else_The_Smallest_Extent()
    {
        Assert.Equal(18.0, Dimensions.Thickness((0, 0, 0), (1200, 18, 370), null));
        Assert.Equal(18.0, Dimensions.Thickness((0, 0, 0), (1200, 200, 370), -18));
        Assert.Equal(25.0, Dimensions.Thickness((0, 0, 0), (12, 200, 370), 25));
    }

    // Item 3: the union of the boxes, and none for no box.
    [Fact]
    public void The_Overall_Box_Is_The_Union_Of_The_Boxes()
    {
        Assert.Null(Dimensions.Union([]));
        Assert.Equal(((-5.0, 0.0, 0.0), (1200.0, 1900.0, 650.0)), Dimensions.Union([(Min, Max), ((-5, 10, 0), (18, 500, 650))]));
    }
}
