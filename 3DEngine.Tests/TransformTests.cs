using System.Numerics;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Tests;

// Finding V10 of the codebase review of 2026-10-13 (TASK-0074): a negative scale mirrors
// a body, and the pipeline then culls its outside, so a transform refuses one.
public class TransformTests
{
    [Theory]
    [InlineData(-1f, 1f, 1f)]
    [InlineData(1f, -0.5f, 1f)]
    [InlineData(1f, 1f, float.NaN)]
    public void A_Transform_Refuses_A_Negative_Or_Infinite_Scale(float x, float y, float z)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Transform(Vector3.Zero, Quaternion.Identity, new Vector3(x, y, z)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Transform.Identity with { Scale = new Vector3(x, y, z) });
    }

    [Fact]
    public void A_Transform_Keeps_A_Positive_Or_Zero_Scale()
    {
        Assert.Equal(new Vector3(2, 0, 3), new Transform(Vector3.Zero, Quaternion.Identity, new Vector3(2, 0, 3)).Scale);
        Assert.Equal(Vector3.One, Transform.Identity.Scale);
    }
}
