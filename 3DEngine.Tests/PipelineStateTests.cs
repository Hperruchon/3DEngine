using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.Tests;

// The fixed state of the pipelines of R3 (TASK-0069), with no GPU. The viewport,
// the front face and the culled face are one choice: the engine gives each
// triangle counter-clockwise when seen from outside (ADR-0018 item 9) in a space
// with y up, the viewport turns y up, and the pipeline culls the back face. A
// change of one part alone shows the inside of each body.
public class PipelineStateTests
{
    [Fact]
    public void The_Viewport_Turns_Y_Up_Over_The_Whole_Extent()
    {
        var viewport = RenderPipeline.Viewport(new VkExtent2D(1280, 720));

        Assert.Equal(0f, viewport.x);
        Assert.Equal(720f, viewport.y);
        Assert.Equal(1280f, viewport.width);
        Assert.Equal(-720f, viewport.height);
        Assert.Equal(0f, viewport.minDepth);
        Assert.Equal(1f, viewport.maxDepth);
    }

    // The other half of the choice: the triangles that the engine gives face out of
    // the body. With y up, the screen area of a triangle has the sign of the z part
    // of its normal, so the culling keeps each face that points to the viewer. The
    // mesh is the cut body of the first demonstration, as GetTessellation gave it
    // (TASK-0028) and as the desktop draws it; a box looks the same from inside, so
    // a picture of the window cannot show this.
    [Fact]
    public void Each_Triangle_Of_The_First_Demonstration_Faces_Out_Of_The_Body()
    {
        double[] positions = [-5, -5, -5, -5, -5, 5, -5, 5, -5, -5, 5, 5, 0, -5, -5, 0, -5, 5, 0, 5, -5, 0, 5, 5];
        uint[] indices = [0, 2, 4, 0, 1, 3, 0, 5, 1, 0, 3, 2, 2, 3, 6, 1, 7, 3, 0, 4, 5, 1, 5, 7, 2, 6, 4, 3, 7, 6, 5, 4, 7, 6, 7, 4];
        (double X, double Y, double Z) centre = (-2.5, 0, 0);

        for (var t = 0; t < indices.Length; t += 3)
        {
            var (a, b, c) = (Point(indices[t]), Point(indices[t + 1]), Point(indices[t + 2]));
            var (ux, uy, uz) = (b.X - a.X, b.Y - a.Y, b.Z - a.Z);
            var (vx, vy, vz) = (c.X - a.X, c.Y - a.Y, c.Z - a.Z);
            var normal = (X: uy * vz - uz * vy, Y: uz * vx - ux * vz, Z: ux * vy - uy * vx);
            var outward = (a.X + b.X + c.X) / 3 - centre.X;
            var dot = normal.X * outward + normal.Y * ((a.Y + b.Y + c.Y) / 3 - centre.Y) + normal.Z * ((a.Z + b.Z + c.Z) / 3 - centre.Z);

            Assert.True(dot > 0, $"Triangle {t / 3} faces into the body.");
        }

        (double X, double Y, double Z) Point(uint index) => (positions[3 * index], positions[3 * index + 1], positions[3 * index + 2]);
    }

    // Reversed depth is one choice of three parts (ADR-0023 item 1): the clear gives
    // the depth of the far plane, the projection gives a nearer point a greater
    // depth, and the pipeline keeps the greater depth. A change of one part alone
    // shows the far body over the near one.
    [Fact]
    public void The_Clear_The_Projection_And_The_Comparison_Agree_So_That_A_Nearer_Fragment_Wins()
    {
        var camera = new ThreeDEngine.Core.Models.Camera { NearPlane = 0.1f, FarPlane = 1000f };
        var projection = camera.Projection(1f);
        float Depth(float z)
        {
            var clip = System.Numerics.Vector4.Transform(new System.Numerics.Vector4(0, 0, z, 1), projection);
            return clip.Z / clip.W;
        }

        Assert.Equal(VkFormat.D32Sfloat, DepthBuffer.Format);
        Assert.Equal(DepthBuffer.ClearDepth, Depth(-camera.FarPlane), 5);
        Assert.Equal(VkCompareOp.Greater, RenderPipeline.DepthCompare);
        Assert.True(Depth(-5f) > Depth(-50f));
    }

    [Fact]
    public void A_Counter_Clockwise_Triangle_Is_The_Front_Face_And_The_Back_Face_Is_Culled()
    {
        Assert.Equal(VkFrontFace.CounterClockwise, RenderPipeline.FrontFace);
        Assert.Equal(VkCullModeFlags.Back, RenderPipeline.CullMode);
        Assert.Equal(VkPrimitiveTopology.TriangleList, RenderPipeline.Topology);
    }
}
