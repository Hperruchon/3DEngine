using System.Numerics;
using System.Runtime.InteropServices;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.Tests;

// The parts of the renderer of the user interface that need no GPU (ADR-0027 item 3,
// TASK-0084): the push constant that maps pixels of the display to clip space, the scissor
// of a command, and the layout of a vertex. The image test of 3DEngine.ImageTests draws.
public class UiRendererTests
{
    [Fact]
    public void The_Transform_Puts_The_Corners_Of_The_Display_At_The_Corners_Of_Clip_Space()
    {
        var position = new Vector2(10, 20);
        var size = new Vector2(800, 600);
        var (scale, translation) = UiRenderer.Transform(position, size);

        Vector2 Clip(Vector2 pixel) => pixel * scale + translation;

        // A float gives 0.9999999 for 1; the tolerance is far below one pixel.
        AssertNear(new Vector2(-1, -1), Clip(position));
        AssertNear(new Vector2(1, 1), Clip(position + size));
        AssertNear(Vector2.Zero, Clip(position + size / 2));
    }

    private static void AssertNear(Vector2 expected, Vector2 actual)
    {
        Assert.Equal(expected.X, actual.X, 5);
        Assert.Equal(expected.Y, actual.Y, 5);
    }

    [Fact]
    public void A_Display_With_No_Area_Is_Refused()
        => Assert.Throws<ArgumentOutOfRangeException>(() => UiRenderer.Transform(Vector2.Zero, new Vector2(0, 600)));

    [Fact]
    public void The_Scissor_Is_The_Clip_Rectangle_In_Pixels_Of_The_Target_Inside_The_Extent()
    {
        var extent = new VkExtent2D(1600, 1200);
        var scale = new Vector2(2, 2);

        var inside = UiRenderer.Scissor(new Vector4(10, 20, 110, 70), Vector2.Zero, scale, extent);
        Assert.Equal(new VkRect2D(20, 40, 200, 100), inside);

        // A rectangle that leaves the display is cut at its edges.
        var cut = UiRenderer.Scissor(new Vector4(-50, 550, 900, 700), Vector2.Zero, scale, extent);
        Assert.Equal(new VkRect2D(0, 1100, 1600, 100), cut);

        // The display position moves the rectangle.
        var moved = UiRenderer.Scissor(new Vector4(110, 120, 210, 170), new Vector2(100, 100), Vector2.One, extent);
        Assert.Equal(new VkRect2D(10, 20, 100, 50), moved);
    }

    [Fact]
    public void A_Clip_Rectangle_With_No_Visible_Area_Gives_No_Scissor()
    {
        var extent = new VkExtent2D(800, 600);

        Assert.Null(UiRenderer.Scissor(new Vector4(10, 10, 10, 50), Vector2.Zero, Vector2.One, extent));
        Assert.Null(UiRenderer.Scissor(new Vector4(900, 10, 950, 50), Vector2.Zero, Vector2.One, extent));
        Assert.Null(UiRenderer.Scissor(new Vector4(10, -60, 50, -10), Vector2.Zero, Vector2.One, extent));
    }

    // The host copies the bytes of the vertices of the library, so the layout must be the
    // layout of that vertex: two floats, two floats, and four bytes, in 20 bytes.
    [Fact]
    public void A_Vertex_Is_Two_Positions_Two_Coordinates_And_Four_Bytes_Of_Colour()
    {
        Assert.Equal((int)UiVertex.Stride, Marshal.SizeOf<UiVertex>());
        Assert.Equal(UiVertex.Stride, UiVertex.Binding.stride);
        Assert.Equal(
            [0u, 8u, 16u],
            UiVertex.Attributes.Select(attribute => attribute.offset).ToArray());
        Assert.Equal(
            [VkFormat.R32G32Sfloat, VkFormat.R32G32Sfloat, VkFormat.R8G8B8A8Unorm],
            UiVertex.Attributes.Select(attribute => attribute.format).ToArray());
        Assert.Equal((nint)16, Marshal.OffsetOf<UiVertex>("<Colour>k__BackingField"));
    }
}
