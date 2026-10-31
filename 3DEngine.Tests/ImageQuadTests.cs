using System.Numerics;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Rendering;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.Tests;

// ADR-0043 (phase V1a, TASK-0146): the matrix of an image on a quad, and the fixed state of its
// pipeline, with no GPU.
public sealed class ImageQuadTests
{
    // ADR-0043 item 2: each corner of the unit square goes to its corner of the quad, relative to
    // the eye and turned by the view, as a point of a body does.
    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(1.0, 0.0)]
    [InlineData(1.0, 1.0)]
    [InlineData(0.0, 1.0)]
    [InlineData(0.25, 0.5)]
    public void The_Matrix_Takes_The_Unit_Square_To_The_Quad(double s, double t)
    {
        var camera = new Camera { Target = (3, -2, 1), Distance = 60, YawDegrees = 35, PitchDegrees = 20 };
        var quad = new ImageQuad("image", (10, 5, -4), (40, 0, 10), (0, 20, 0));

        var seen = Vector3.Transform(new Vector3((float)s, (float)t, 0), quad.ModelView(camera));

        var eye = camera.Eye();
        var world = (X: 10 + (s * 40), Y: 5 + (t * 20), Z: -4 + (s * 10));
        var expected = Vector3.Transform(new Vector3((float)(world.X - eye.X), (float)(world.Y - eye.Y), (float)(world.Z - eye.Z)), camera.View());
        Assert.True(Vector3.Distance(expected, seen) < 1e-3f, $"The corner ({s}, {t}) goes to {seen}, not {expected}.");
    }

    // ADR-0043 item 2: far from the origin, the eye moves first in double precision, so the corner
    // keeps its place to a small part of a unit.
    [Fact]
    public void A_Quad_Far_From_The_Origin_Keeps_Its_Place()
    {
        var camera = new Camera { Target = (1e7, 0, 0), Distance = 50, YawDegrees = 0, PitchDegrees = 0 };
        var quad = new ImageQuad("image", (1e7 + 0.5, 0.25, 0), (1, 0, 0), (0, 1, 0));

        var seen = Vector3.Transform(Vector3.Zero, quad.ModelView(camera));

        Assert.True(Vector3.Distance(new Vector3(0.5f, 0.25f, -50f), seen) < 1e-3f, $"The corner is at {seen}.");
    }

    // ADR-0043 item 3: the image tests and writes the depth as a body does, culls no face, and has the
    // push constant block of the mesh pipeline.
    [Fact]
    public void The_Pipeline_Of_The_Images_Has_The_State_Of_The_Record()
    {
        Assert.Equal(VkCullModeFlags.None, ImageRenderer.CullMode);
        Assert.Equal(RenderPipeline.DepthCompare, ImageRenderer.DepthCompare);
        Assert.Equal(RenderPipeline.PushConstantSize, ImageRenderer.PushConstantSize);
        Assert.Equal(16, ImageRenderer.MaximumImages);
        Assert.Contains("image.vert", ShaderLibrary.Names);
        Assert.Contains("image.frag", ShaderLibrary.Names);
    }

    // The unit square is two counter-clockwise triangles that cover it one time.
    [Fact]
    public void The_Unit_Square_Is_Two_Counter_Clockwise_Triangles()
    {
        var square = ImageRenderer.UnitSquare;
        Assert.Equal(12, square.Length);
        var area = 0.0;
        for (var i = 0; i < square.Length; i += 6)
        {
            var twice = ((square[i + 2] - square[i]) * (square[i + 5] - square[i + 1])) - ((square[i + 4] - square[i]) * (square[i + 3] - square[i + 1]));
            Assert.True(twice > 0, $"Triangle {i / 6} is not counter-clockwise.");
            area += twice / 2;
        }
        Assert.Equal(1.0, area);
    }
}
