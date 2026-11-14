using System.Numerics;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0140 (phase R21c, TASK-0372): the free part of the scene in the windows of the last frame, and the overlay of the window
// with the shift of the centre of the picture. With no GPU.
[Collection(DearImGui.Collection)]
public sealed class FrameFreePartWindowTests
{
    private static readonly LiveBody Body = new(Guid.Parse("7b000000-0000-0000-0000-000000000001"), "Box", "Block", true);

    private static DesktopUi Shown(Vector2 display, Guid? selection, SketchPanel? sketches = null)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        for (var frame = 0; frame < 3; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, display, Vector2.One, [Body], selection, new DocumentLine("block", null), ringCentre: selection is null ? null : display / 2f, ringBody: 80f,
                sketchPanel: sketches);
        }

        return ui;
    }

    // Finding V191 of the codebase review of 2026-11-14 (e) (TASK-0379): with a live sketch and no selection, the window of the
    // live sketches is at the right edge, and the free part lies on no window, that one too.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1440, 900)]
    public void The_Free_Part_Leaves_The_Window_Of_The_Live_Sketches(int width, int height)
    {
        var sketch = new Engine.Core.Queries.SketchInfo(Guid.Parse("7b000000-0000-0000-0000-000000000002"), Engine.Contracts.Sketch.Datums.XY.Id, "Plate", [], [], [], [], [], true, 0);
        var panel = new SketchPanel([sketch], null, ThreeDEngine.Core.Sketching.SketchToolKind.Line, false, [], null);
        using var ui = Shown(new Vector2(width, height), null, panel);
        var (x0, y0, x1, y1) = ui.FreePart();
        var free = new Vector4((float)((x0 + 1) / 2 * width), (float)((1 - y1) / 2 * height), (float)((x1 + 1) / 2 * width), (float)((1 - y0) / 2 * height));
        static bool Meets(Vector4 a, Vector4 b) => a.X < b.Z - 0.5f && b.X < a.Z - 0.5f && a.Y < b.W - 0.5f && b.Y < a.W - 0.5f;

        Assert.Contains(ui.PanelRectangles, box => box.Z > width * 0.75f && box.X > width * 0.4f);
        Assert.True(free.Z - free.X >= 100 && free.W - free.Y >= 100, $"{width}x{height}: the free part {free} is too small.");
        Assert.DoesNotContain(ui.PanelRectangles.Concat(ui.FixedRectangles), cover => Meets(cover, free));
    }

    // Item 1: with a shift, the overlay puts a point where the projection of the renderer puts it.
    [Fact]
    public void The_Overlay_Follows_The_Shift()
    {
        var camera = new Camera { ZUp = true, YawDegrees = 30, PitchDegrees = 25, FarPlane = 5000f };
        camera.Frame((0, 0, 0), (60, 40, 10), 1280f / 720f, (-0.3, -0.8, 0.95, 0.75));
        foreach (var point in new (double X, double Y, double Z)[] { camera.Target, (60, 0, 10), (0, 40, 0) })
        {
            var eye = camera.Eye();
            var clip = Vector4.Transform(new Vector4((float)(point.X - eye.X), (float)(point.Y - eye.Y), (float)(point.Z - eye.Z), 1f), camera.View() * camera.Projection(1280f / 720f));
            var expected = new Vector2(((clip.X / clip.W) + 1) / 2 * 1280f, (1 - (clip.Y / clip.W)) / 2 * 720f);
            var pixel = SketchOverlay.Segment(camera, 1280, 720, point, point)?.A ?? throw new InvalidOperationException("No pixel.");
            Assert.True(Vector2.Distance(expected, pixel) < 0.05f, $"The overlay puts {point} at {pixel}, the projection at {expected}.");
        }
    }

    // Item 2: the free part lies on no window of the last frame: the panel of the document or the rows of a body, the top bar,
    // the command line and the anchors; on a portrait display it is above the rows under the part.
    [Theory]
    [InlineData(1280, 720, false)]
    [InlineData(1280, 720, true)]
    [InlineData(1440, 900, true)]
    [InlineData(390, 844, true)]
    public void The_Free_Part_Leaves_The_Windows(int width, int height, bool selected)
    {
        var display = new Vector2(width, height);
        using var ui = Shown(display, selected ? Body.Id : null);
        var (x0, y0, x1, y1) = ui.FreePart();
        var (left, bottom, right, top) = ((x0 + 1) / 2 * width, (1 - y0) / 2 * height, (x1 + 1) / 2 * width, (1 - y1) / 2 * height);
        Assert.True(right - left >= 100 && bottom - top >= 100, $"{width}x{height}: the free part ({left}, {top}, {right}, {bottom}) is the whole picture or too small.");
        var free = new Vector4((float)left, (float)top, (float)right, (float)bottom);
        static bool Meets(Vector4 a, Vector4 b) => a.X < b.Z - 0.5f && b.X < a.Z - 0.5f && a.Y < b.W - 0.5f && b.Y < a.W - 0.5f;
        var covers = ui.PanelRectangles.Concat(ui.FixedRectangles).Concat(ui.RowsArea is { } rows ? [rows] : Array.Empty<Vector4>()).ToList();
        Assert.NotEmpty(covers);
        Assert.DoesNotContain(covers, cover => Meets(cover, free));
        if (width < height)
            Assert.True(bottom <= (ui.RowsArea?.Y ?? height), $"{width}x{height}: the free part ends at {bottom}, under the top of the rows.");
    }
}
