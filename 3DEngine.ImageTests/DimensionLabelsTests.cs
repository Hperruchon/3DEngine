using System.Numerics;
using Engine.Core.Queries;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0125 (phase R20c, TASK-0352): each dimension says what it measures, on a plate outside the outline of its box, and no
// plate goes under the status line, the ring or the history arc. The scene is the niche of the UX test of 2026-11-10: a
// panel "Cote gauche" of 18 by 400 by 800 at the left of a whole of 3400 by 400 by 2350, with the z axis up, at 1280 by 720.
// ADR-0125 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class DimensionLabelsTests
{
    private static readonly Vector2 Display = new(1280, 720);

    private static readonly LiveBody Panel = new(Guid.Parse("72000000-0000-0000-0000-000000000001"), "Extrude", "Cote gauche", true);
    private static readonly LiveBody Unnamed = new(Guid.Parse("72000000-0000-0000-0000-000000000002"), "Box", null, true);
    private static readonly Guid Sketch = Guid.Parse("72000000-0000-0000-0000-000000000003");

    private static readonly ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) Whole = ((0, 0, 0), (3400, 400, 2350));
    private static readonly ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) Board = ((1300, 0, 700), (1318, 400, 1500));

    private static readonly IReadOnlyList<HistoryStep> Steps =
    [
        new(Panel.Id, "Extrude", "Cote gauche", GetFeaturesQueryHandler.Correct, null, HistoryTap.SelectBody),
        new(Sketch, "CreateSketch", "Profile on XZ", GetFeaturesQueryHandler.Correct, null, HistoryTap.EditSketch, MadeFrom: true),
    ];

    private static Camera Camera()
    {
        var camera = new Camera { ZUp = true, YawDegrees = 30, PitchDegrees = 20, FarPlane = 50000f };
        camera.Frame(Whole.Min, Whole.Max, Display.X / Display.Y);
        return camera;
    }

    private static Vector2? Pixel(Camera camera, (double X, double Y, double Z) point)
        => SketchOverlay.Segment(camera, Display.X, Display.Y, point, point)?.A;

    private static Vector2 Centre(Camera camera, ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) box)
        => Pixel(camera, ((box.Min.X + box.Max.X) / 2, (box.Min.Y + box.Max.Y) / 2, (box.Min.Z + box.Max.Z) / 2))!.Value;

    // The frame of the window with the dimensions on, the panel selected, its ring and its history arc.
    private static (DesktopUi Ui, DimensionOverlay.Strokes Strokes, Camera Camera) Shown(LiveBody selected, Vector2? display = null)
    {
        var size = display ?? Display;
        var camera = Camera();
        var strokes = DimensionOverlay.Of(Whole, Board, camera, size.X, size.Y, "mm");
        var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        LiveBody[] bodies = [selected, new(Guid.Parse("72000000-0000-0000-0000-000000000009"), "Extrude", "Fond", true)];
        for (var frame = 0; frame < 3; frame++)
            ui.Frame(1f / 60f, size, Vector2.One, bodies, selected.Id, dimensions: strokes, ringCentre: Centre(camera, Board), history: Steps);
        return (ui, strokes, camera);
    }

    private static bool Overlap(Vector4 a, Vector4 b) => a.X < b.Z && a.Z > b.X && a.Y < b.W && a.W > b.Y;

    // Items 1 and 2: a plate of the whole says "Overall" and a plate of the selected body gives its name, or its kind and a
    // short identifier as its row; each has the colour of its box.
    [Fact]
    public void Each_Plate_Names_What_It_Measures()
    {
        using var named = Shown(Panel).Ui;
        Assert.Equal(
            ["Overall · X 3400 mm", "Overall · Y 400 mm", "Overall · Z 2350 mm", "Cote gauche · X 18 mm", "Cote gauche · Y 400 mm", "Cote gauche · Z 800 mm"],
            named.DimensionPlates.Select(plate => plate.Text));

        using var unnamed = Shown(Unnamed).Ui;
        Assert.Equal(
            ["Overall · X 3400 mm", "Overall · Y 400 mm", "Overall · Z 2350 mm", "Box 72000000 · X 18 mm", "Box 72000000 · Y 400 mm", "Box 72000000 · Z 800 mm"],
            unnamed.DimensionPlates.Select(plate => plate.Text));
    }

    // Item 1: the colour of the dimensions of the selected body is the colour of its edges in the scene, and the colour of the
    // whole is a grey, far from it.
    [Fact]
    public void The_Selected_Body_Has_The_Colour_Of_The_Selection_And_The_Whole_A_Grey()
    {
        static (int R, int G, int B) Bytes(uint abgr) => ((int)(abgr & 0xFF), (int)((abgr >> 8) & 0xFF), (int)((abgr >> 16) & 0xFF));
        var edge = SceneRenderer.SelectedEdgeColour;
        var selection = Bytes(DesktopUi.SelectionDimensionColour);
        Assert.Equal(((int)MathF.Round(edge.X * 255f), (int)MathF.Round(edge.Y * 255f), (int)MathF.Round(edge.Z * 255f)), selection);

        var grey = Bytes(DesktopUi.OverallDimensionColour);
        Assert.True(Math.Max(grey.R, Math.Max(grey.G, grey.B)) - Math.Min(grey.R, Math.Min(grey.G, grey.B)) <= 24, $"The colour of the whole {grey} is not a grey.");
        Assert.True(Math.Abs(grey.R - selection.R) + Math.Abs(grey.G - selection.G) + Math.Abs(grey.B - selection.B) > 200, $"The grey {grey} is near the selection {selection}.");
    }

    // Item 3: each plate is outside the outline of its box: the normal of its dimension line separates the plate from each
    // corner of the box on the screen. The plates of this scene do not move off a cover, so the test sees the rule itself.
    [Fact]
    public void Each_Plate_Is_Outside_The_Outline_Of_Its_Box()
    {
        var (ui, strokes, camera) = Shown(Panel, new Vector2(1600, 1000));
        using (ui)
        {
            Assert.Equal(6, ui.DimensionPlates.Count);
            for (var i = 0; i < strokes.Measures.Count; i++)
            {
                var (measured, plate) = (strokes.Measures[i], ui.DimensionPlates[i].Plate);
                var box = measured.Mark == DimensionOverlay.Mark.Overall ? Whole : Board;
                var corners = new[] { box.Min.X, box.Max.X }
                    .SelectMany(x => new[] { box.Min.Y, box.Max.Y }.SelectMany(y => new[] { box.Min.Z, box.Max.Z }.Select(z => (x, y, z))))
                    .Select(corner => Pixel(camera, corner)!.Value)
                    .ToList();
                var n = measured.Line.Normal;
                var reach = corners.Max(corner => Vector2.Dot(corner, n));
                var near = new[] { new Vector2(plate.X, plate.Y), new Vector2(plate.Z, plate.Y), new Vector2(plate.X, plate.W), new Vector2(plate.Z, plate.W) }
                    .Min(point => Vector2.Dot(point, n));
                Assert.True(near >= reach + EdgeValues.Gap - 0.5f, $"{ui.DimensionPlates[i].Text} at {plate} is {near - reach} pixels past the outline of its box.");
            }
        }
    }

    // Item 4: no plate is under the status line, the fixed controls, the rows, the ring or the history arc. In this scene
    // at least one plate starts under one of them, so the test fails when the placement does not read them.
    [Fact]
    public void No_Plate_Is_Under_The_Status_Line_The_Ring_Or_The_History_Arc()
    {
        var (ui, strokes, _) = Shown(Panel);
        using (ui)
        {
            var covers = new List<Vector4>([.. ui.FixedRectangles, .. ui.PanelRectangles]);
            if (ui.RowsArea is { } rows)
                covers.Add(rows);
            foreach (var label in ui.RingLabels)
            {
                foreach (var item in new[] { DesktopUi.RingTextItem(label), DesktopUi.RingIconItem(label) })
                {
                    if (ui.ItemRectangle(item) is { } rectangle)
                        covers.Add(rectangle);
                }
            }
            foreach (var item in new[] { DesktopUi.RingCaptionItem, DesktopUi.HistoryCaptionItem }
                .Concat(Steps.SelectMany(step => new[] { DesktopUi.HistoryItem(step.Id), DesktopUi.HistoryTextItem(step.Id) })))
            {
                if (ui.ItemRectangle(item) is { } rectangle)
                    covers.Add(rectangle);
            }
            Assert.NotEmpty(ui.RingLabels);
            Assert.NotNull(ui.ItemRectangle(DesktopUi.HistoryItem(Panel.Id)));

            // Where each plate starts before the placement: the place of ADR-0115 item 2.
            var starts = strokes.Measures.Select((measured, i) =>
            {
                var size = ui.PlateSize(ui.DimensionPlates[i].Text);
                var at = EdgeValues.Placed(measured.Line) + (measured.Line.Normal * DesktopUi.Half(size, measured.Line.Normal)) - (size / 2f);
                return new Vector4(at.X, at.Y, at.X + size.X, at.Y + size.Y);
            }).ToList();
            Assert.Contains(starts, start => covers.Any(cover => Overlap(start, cover)));

            foreach (var (text, _, _, _, plate) in ui.DimensionPlates)
            {
                foreach (var cover in covers)
                    Assert.False(Overlap(plate, cover), $"{text} at {plate} is under {cover}.");
            }
        }
    }

    // Item 4: the placement moves a label off a cover: down while it can, else up, else right of it, else left of it.
    [Fact]
    public void The_Placement_Moves_A_Label_Off_A_Cover()
    {
        static Vector2 Size(string _) => new(100, 20);
        DimensionOverlay.Label[] labels =
        [
            new(new Vector2(500, 300), "A", DimensionOverlay.Mark.Overall),
            new(new Vector2(500, 690), "B", DimensionOverlay.Mark.Overall),
            new(new Vector2(40, 300), "C", DimensionOverlay.Mark.Selected),
        ];
        Vector4[] covers =
        [
            new(450, 290, 650, 330),   // over A: A goes down under it
            new(0, 660, 1280, 720),    // the status line under B: B goes up over it
            new(0, 0, 200, 720),       // a panel of the whole height under C: C goes right of it
        ];

        var placed = DimensionOverlay.Place(labels, Size, Display, covers);

        Assert.Equal(new Vector2(500, 332), placed[0].At);
        Assert.Equal(new Vector2(500, 638), placed[1].At);
        Assert.Equal(new Vector2(202, 300), placed[2].At);
    }

    // Item 5: a body that is not a board has no thickness line.
    [Fact]
    public void A_Body_That_Is_Not_A_Board_Has_No_Thickness()
    {
        Assert.Equal("Size 18 x 400 x 800 mm\nThickness 18 mm", DimensionOverlay.SizeText(((0, 0, 0), (18, 400, 800)), null, "mm"));
        Assert.Equal("Size 400 x 400 x 400 mm", DimensionOverlay.SizeText(((0, 0, 0), (400, 400, 400)), -400, "mm"));
        Assert.Equal("Size 40 x 40 x 800 mm", DimensionOverlay.SizeText(((0, 0, 0), (40, 40, 800)), null, "mm"));
        Assert.Equal("Size 18 x 400 x 800 mm", DimensionOverlay.SizeText(((0, 0, 0), (18, 400, 800)), 800, "mm"));
    }
}
