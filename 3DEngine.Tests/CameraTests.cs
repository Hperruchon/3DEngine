using System.Numerics;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Tests;

// The orbit camera and its reversed projection (ADR-0023 items 1 and 2,
// TASK-0070), with no GPU. ADR-0023 names this file in enforced-by.
public class CameraTests
{
    [Fact]
    public void With_No_Turn_The_Eye_Is_On_The_Positive_Z_Side_Of_The_Target()
    {
        var camera = new Camera { Target = (1, 2, 3), Distance = 10 };

        var eye = camera.Eye();

        Assert.Equal(1, eye.X, 12);
        Assert.Equal(2, eye.Y, 12);
        Assert.Equal(13, eye.Z, 12);
    }

    [Fact]
    public void A_Yaw_Of_90_Degrees_Puts_The_Eye_On_The_Positive_X_Side()
    {
        var camera = new Camera { Distance = 10 };

        camera.Orbit(90, 0);
        var eye = camera.Eye();

        Assert.Equal(10, eye.X, 9);
        Assert.Equal(0, eye.Z, 9);
    }

    [Fact]
    public void The_Pitch_Stays_Between_Minus_89_And_89_Degrees()
    {
        var camera = new Camera();

        camera.Orbit(0, 200);
        Assert.Equal(Camera.MaximumPitchDegrees, camera.PitchDegrees);
        camera.Orbit(0, -500);
        Assert.Equal(Camera.MinimumPitchDegrees, camera.PitchDegrees);
    }

    [Fact]
    public void A_Zoom_Keeps_The_Eye_Between_The_Planes_And_Refuses_A_Factor_That_Is_Not_Positive()
    {
        var camera = new Camera { NearPlane = 0.1f, FarPlane = 1000f, Distance = 10 };

        camera.Zoom(1e-6);
        Assert.Equal(0.2, camera.Distance, 6);
        camera.Zoom(1e9);
        Assert.Equal(500, camera.Distance, 6);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Zoom(0));
    }

    [Fact]
    public void The_View_Puts_The_Target_On_The_Negative_Z_Axis_At_The_Distance()
    {
        var camera = new Camera { Target = (5, -3, 2), Distance = 12 };
        camera.Orbit(37, 21);
        var eye = camera.Eye();
        var target = new Vector3((float)(5 - eye.X), (float)(-3 - eye.Y), (float)(2 - eye.Z));

        var inView = Vector3.Transform(target, camera.View());

        Assert.Equal(0, inView.X, 4);
        Assert.Equal(0, inView.Y, 4);
        Assert.Equal(-12, inView.Z, 4);
    }

    // Reversed depth: the near plane gives 1, the far plane gives 0, and a nearer
    // point gives a greater depth, which the pipeline keeps.
    [Fact]
    public void The_Projection_Gives_Depth_1_At_The_Near_Plane_And_0_At_The_Far_Plane()
    {
        var camera = new Camera { NearPlane = 0.1f, FarPlane = 1000f };
        var projection = camera.Projection(16f / 9f);

        float Depth(float z)
        {
            var clip = Vector4.Transform(new Vector4(0, 0, z, 1), projection);
            return clip.Z / clip.W;
        }

        Assert.Equal(1f, Depth(-0.1f), 5);
        Assert.Equal(0f, Depth(-1000f), 5);
        Assert.True(Depth(-1f) > Depth(-10f));
    }

    [Fact]
    public void The_Projection_Narrows_X_By_The_Aspect()
    {
        var camera = new Camera { FieldOfViewDegrees = 90f };

        var projection = camera.Projection(2f);

        Assert.Equal(1f, projection.M22, 5);
        Assert.Equal(0.5f, projection.M11, 5);
    }

    // ADR-0025 item 6 (TASK-0076): each corner of a framed box is in the picture, for a
    // wide picture and for a tall one, and the yaw and the pitch do not change.
    [Theory]
    [InlineData(16f / 9f)]
    [InlineData(9f / 16f)]
    public void A_Framed_Box_Is_Inside_The_Picture(float aspect)
    {
        var camera = new Camera { YawDegrees = 30, PitchDegrees = 20, Distance = 500 };
        (double X, double Y, double Z) min = (95, -5, -5), max = (105, 5, 5);

        camera.Frame(min, max, aspect);

        Assert.Equal((100.0, 0.0, 0.0), camera.Target);
        Assert.Equal(30, camera.YawDegrees);
        Assert.Equal(20, camera.PitchDegrees);

        var eye = camera.Eye();
        var viewProjection = camera.View() * camera.Projection(aspect);
        var largest = 0f;
        foreach (var x in new[] { min.X, max.X })
        foreach (var y in new[] { min.Y, max.Y })
        foreach (var z in new[] { min.Z, max.Z })
        {
            var clip = Vector4.Transform(new Vector4((float)(x - eye.X), (float)(y - eye.Y), (float)(z - eye.Z), 1f), viewProjection);
            largest = Math.Max(largest, Math.Max(Math.Abs(clip.X / clip.W), Math.Abs(clip.Y / clip.W)));
        }

        // Inside the picture, and not much smaller than it: the sphere around the box
        // touches the narrower edge.
        Assert.InRange(largest, 0.5f, 1f);
    }
}
