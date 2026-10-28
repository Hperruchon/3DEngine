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

        Assert.True(camera.Frame(min, max, aspect));

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

    // Finding V11 of the codebase review of 2026-10-13 (TASK-0074): each parameter of the
    // camera is checked, and a zoom between close planes does not throw.
    [Fact]
    public void The_Camera_Refuses_A_Parameter_That_Draws_Nothing()
    {
        var camera = new Camera();

        Assert.Throws<ArgumentOutOfRangeException>(() => camera.NearPlane = 0f);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.FarPlane = float.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.FieldOfViewDegrees = 0f);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.FieldOfViewDegrees = 180f);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Projection(0f));

        camera.NearPlane = 10f;
        camera.FarPlane = 5f;
        Assert.Throws<InvalidOperationException>(() => camera.Projection(1f));
    }

    // Finding V22 of the codebase review of 2026-10-17 (TASK-0079): a distance of 0 gave a
    // view of NaN, and an aspect of NaN passed the check of Frame and gave a distance of NaN.
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void The_Camera_Refuses_A_Distance_That_Draws_Nothing(double distance)
    {
        var camera = new Camera();

        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Distance = distance);
        Assert.Equal(10.0, camera.Distance);
    }

    [Fact]
    public void The_Camera_Refuses_A_Zoom_Or_An_Aspect_Of_NaN()
    {
        var camera = new Camera();

        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Zoom(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Frame((0, 0, 0), (1, 1, 1), float.NaN));
        Assert.True(double.IsFinite(camera.Distance));
    }

    // V38 of the second codebase review of 2026-10-28: the yaw and the target accepted NaN, and
    // each view was NaN.
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void The_Camera_Keeps_A_Finite_Yaw_And_Target(double value)
    {
        var set = new Camera { YawDegrees = value };
        var orbited = new Camera { YawDegrees = 30 };
        orbited.Orbit(value, 0);
        var targeted = new Camera { Target = (1, 2, 3) };

        Assert.Throws<ArgumentOutOfRangeException>(() => targeted.Target = (value, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => targeted.Target = (0, 0, value));
        Assert.Equal((1.0, 2.0, 3.0), targeted.Target);
        foreach (var camera in new[] { set, orbited, targeted })
        {
            var view = camera.View();
            Assert.True(float.IsFinite(view.M11) && float.IsFinite(view.M13) && float.IsFinite(view.M41) && float.IsFinite(view.M43), $"The view is {view}.");
        }
    }

    // V22: with a near plane of 1 and a far plane of 1.5 the distance was 2, beyond the far
    // plane; a box of 3,000 units put the eye inside the body. Frame now says so.
    [Theory]
    [InlineData(1f, 1.5f, 1.0)]
    [InlineData(0.1f, 1000f, 3000.0)]
    public void Frame_Reports_A_Box_That_The_Planes_Cannot_Hold(float near, float far, double size)
    {
        var camera = new Camera { NearPlane = near, FarPlane = far };

        Assert.False(camera.Frame((0, 0, 0), (size, size, size), 16f / 9f));
    }

    [Fact]
    public void A_Zoom_Between_Close_Planes_Keeps_The_Eye_At_Two_Times_The_Near_Plane()
    {
        var camera = new Camera { NearPlane = 1f, FarPlane = 3f, Distance = 2 };

        camera.Zoom(0.5);

        Assert.Equal(2.0, camera.Distance);
    }

    // ADR-0026 (TASK-0082): a pan moves the target and the eye together, in the plane of
    // the view. A drag to the right moves the scene to the right, so the target moves to the
    // left; the distance, the yaw and the pitch do not change.
    [Fact]
    public void A_Pan_Moves_The_Target_In_The_Plane_Of_The_View()
    {
        var camera = new Camera { Target = (1, 2, 3), Distance = 10, YawDegrees = 0, PitchDegrees = 0, FieldOfViewDegrees = 90f };
        var eyeBefore = camera.Eye();

        camera.Pan(50, 0, 100);

        // One pixel is 2 * 10 * tan(45 degrees) / 100 = 0.2 at the depth of the target.
        Assert.Equal(1 - 10, camera.Target.X, 9);
        Assert.Equal(2, camera.Target.Y, 9);
        Assert.Equal(3, camera.Target.Z, 9);
        var eyeAfter = camera.Eye();
        Assert.Equal(eyeBefore.X - 10, eyeAfter.X, 9);
        Assert.Equal(eyeBefore.Z, eyeAfter.Z, 9);
        Assert.Equal((10.0, 0.0, 0.0), (camera.Distance, camera.YawDegrees, camera.PitchDegrees));

        camera.Pan(0, 50, 100);
        Assert.Equal(2 + 10, camera.Target.Y, 9);
    }
}
