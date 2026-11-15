using System.Numerics;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0137 (phase R21a, TASK-0369): the commands of the flat views in the command line, and the overlay of the window in a
// flat view, with no perspective. With no GPU.
[Collection(DearImGui.Collection)]
public sealed class FlatViewWindowTests
{
    private static readonly LiveBody Body = new(Guid.Parse("79000000-0000-0000-0000-000000000001"), "Box", "Block", true);

    // Item 4: the command line has the six flat views and the return to the view with perspective, with or without a
    // selected body; each command asks for its view.
    [Fact]
    public void The_Command_Line_Has_The_Flat_Views()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        foreach (var selection in new Guid?[] { null, Body.Id })
        {
            var commands = ui.Commands([Body], selection);
            foreach (var (label, view) in DesktopUi.FlatViews)
            {
                var (_, run) = Assert.Single(commands, command => command.Name == label);
                ui.TakeActions();
                run();
                Assert.Equal([new FlatViewAction(view)], ui.TakeActions());
            }
        }

        Assert.Equal(["Front view", "Back view", "Left view", "Right view", "Top view", "Bottom view", "3D view"], DesktopUi.FlatViews.Select(view => view.Label));
    }

    // Item 3: in a flat view the overlay of the window projects with no perspective: an edge along the view is one point, and
    // an edge across it has the same length at each depth, as the scale of the target gives it.
    [Fact]
    public void The_Overlay_Of_A_Flat_View_Has_No_Perspective()
    {
        var camera = new Camera { ZUp = true, Target = (0, 0, 0), Distance = 300, FarPlane = 5000f };
        camera.ShowFlat(FlatView.Front);
        var (width, height) = (1200.0, 800.0);

        var along = SketchOverlay.Segment(camera, width, height, (10, -50, 5), (10, 60, 5)) ?? throw new InvalidOperationException("No segment.");
        Assert.True(Vector2.Distance(along.A, along.B) < 1e-3f, $"An edge along the view is {Vector2.Distance(along.A, along.B)} pixels long.");

        var near = SketchOverlay.Segment(camera, width, height, (0, -80, 0), (40, -80, 0)) ?? throw new InvalidOperationException("No segment.");
        var far = SketchOverlay.Segment(camera, width, height, (0, 90, 0), (40, 90, 0)) ?? throw new InvalidOperationException("No segment.");
        var pixels = 40 / camera.HalfHeight * height / 2;
        Assert.Equal(pixels, Vector2.Distance(near.A, near.B), 3);
        Assert.Equal(pixels, Vector2.Distance(far.A, far.B), 3);

        // Finding V188 (TASK-0374): a zoom in a flat view changes the scale of the overlay as the scale of the picture.
        camera.Zoom(0.5);
        var zoomed = SketchOverlay.Segment(camera, width, height, (0, -80, 0), (40, -80, 0)) ?? throw new InvalidOperationException("No segment.");
        Assert.Equal(40 / camera.HalfHeight * height / 2, Vector2.Distance(zoomed.A, zoomed.B), 3);
        Assert.Equal(pixels * 2, Vector2.Distance(zoomed.A, zoomed.B), 2);
    }
}
