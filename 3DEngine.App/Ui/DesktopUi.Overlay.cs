using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// Phase R15 (TASK-0234): the overlays of the scene, the sketches (ADR-0034) and the dimensions (ADR-0056), a part
// of DesktopUi. Until TASK-0234 it was in DesktopUi.cs; no line changed in the split.
internal sealed unsafe partial class DesktopUi
{
    // ADR-0033 item 13: the colour of the lines and the points of a sketch, as ImGui packs
    // it (alpha, blue, green, red), and their sizes in pixels of the window.
    internal const uint SketchColour = 0xFF30C8F0;
    private const float SketchLineWidth = 2f;
    private const float SketchPointRadius = 4f;

    // ADR-0034 item 9: a selected item in white and wider, and each sketch that is not active in grey.
    internal const uint SelectedColour = 0xFFFFFFFF;

    // The mark of a snap: a square of 12 pixels (ABGR). ADR-0078 item 1 (TASK-0246): magenta, the one hue that the chrome,
    // the sketch and the axes do not use; until TASK-0246 it was cyan.
    internal const uint SnapColour = Theme.Snap;
    private const float SnapHalfSize = 6f;
    internal const uint OtherSketchColour = Theme.OtherSketch;

    // ADR-0053 item 8: a line or a point of a reference of the sketch, dim.
    internal const uint ReferenceColour = Theme.Reference;
    private const float SelectedLineWidth = 4f;
    private const float SelectedPointRadius = 7f;
    private const float RingRadius = 12f;

    // ADR-0075 item 3 (phase R12a, TASK-0236): the outline of the selected face, in the colour and the width of a
    // selected item, on the background list, over the scene and under each window of the panel.
    private static void FaceOutline(FaceLine face)
    {
        var list = ImGui.GetBackgroundDrawList();
        foreach (var (a, b) in face.Outline)
            Cased(list, a, b, SelectedColour, SelectedLineWidth);

        // ADR-0077 item 4 (phase R12b, TASK-0245): the selected edge over the outline, wider.
        foreach (var (a, b) in face.Edge ?? [])
            Cased(list, a, b, SelectedColour, SelectedEdgeWidth);
    }

    // ADR-0078 item 2 (TASK-0246): a mark of the viewport over a dark casing of 2 pixels on each side, so that it holds
    // its contrast on each face and on the clear.
    private static void Cased(ImDrawListPtr list, Vector2 a, Vector2 b, uint colour, float width)
    {
        list.AddLine(a, b, Theme.Casing, width + (2 * Theme.CasingWidth));
        list.AddLine(a, b, colour, width);
    }

    private const float SelectedEdgeWidth = 8f;

    // ADR-0033 item 13: the sketches, read only, on the background list of ImGui, so they
    // come over the scene and under each window of the panel.
    // ADR-0120 item 3 (phase R18f, TASK-0347): the item of the label of the plate and of its caption, for a test.
    public const string PlateLabelItem = "Plate:label";
    public const string PlateCaptionItem = "Plate:caption";
    internal const string PlateCaption = "SKETCH · UNDER THE BODY IT MAKES";

    // ADR-0120 item 3: the plate of the active sketch, as the render: a fill and an edge of the accent with a glow, under the
    // lines of the sketch.
    private static void Plate(SketchOverlay.Strokes sketches, Engine.Core.Queries.SketchInfo? active)
    {
        if (sketches.Plate is not { Count: 4 } corners || active is null)
            return;
        var list = ImGui.GetBackgroundDrawList();
        var points = corners.ToArray();
        list.AddConvexPolyFilled(ref points[0], points.Length, PlateAccent(0.10f));
        foreach (var (width, alpha) in new[] { (7f, 0.12f), (1.5f, 0.9f) })
        {
            for (var i = 0; i < 4; i++)
                list.AddLine(points[i], points[(i + 1) % 4], PlateAccent(alpha), width);
        }
    }

    // ADR-0120 item 3: over the lines of the sketch, under the plate and left of the tools of the ring at the right: the name and
    // the state of the sketch in capitals in the accent, on the ground of the scene, and, while the sketch is under its body,
    // the caption under them. Both end at the middle of the plate. The render puts the name along the far edge of the plate;
    // there the values on the edges and the body cover it, so it is under the plate with the caption.
    private void PlateText(SketchOverlay.Strokes sketches, Engine.Core.Queries.SketchInfo? active)
    {
        if (sketches.Plate is not { Count: 4 } corners || active is null)
            return;
        var list = ImGui.GetBackgroundDrawList();
        var bottom = corners.Max(point => point.Y);
        var middle = (corners.Min(point => point.X) + corners.Max(point => point.X)) / 2f;
        var left = 16f + _inset.X;

        var label = $"{SketchTitle(active).ToUpperInvariant()} · {SketchState(active).ToUpperInvariant()}";
        var size = ImGui.CalcTextSize(label);
        var at = new Vector2(Math.Max(left, middle - size.X), bottom + 12f);
        list.AddRectFilled(at - new Vector2(6f, 3f), at + size + new Vector2(6f, 3f), Theme.Plate(0.85f), 4f);
        list.AddText(at, PlateAccent(0.95f), label);
        _items[PlateLabelItem] = new Vector4(at.X, at.Y, at.X + size.X, at.Y + size.Y);
        if (sketches.Under)
        {
            var caption = Spaced(PlateCaption);
            var extent = ImGui.CalcTextSize(caption);
            var place = new Vector2(Math.Max(left, middle - extent.X), at.Y + size.Y + 8f);
            list.AddText(place, ImGui.GetColorU32(ImGuiCol.TextDisabled), caption);
            _items[PlateCaptionItem] = new Vector4(place.X, place.Y, place.X + extent.X, place.Y + extent.Y);
        }
    }

    private static uint PlateAccent(float alpha) => (Theme.Accent & 0x00FFFFFFu) | ((uint)(alpha * 255f) << 24);

    private static void Sketches(SketchOverlay.Strokes sketches)
    {
        var list = ImGui.GetBackgroundDrawList();
        foreach (var line in sketches.Lines)
            Cased(list, line.A, line.B, Colour(line.Mark), line.Mark == SketchOverlay.Mark.Selected ? SelectedLineWidth : SketchLineWidth);
        foreach (var point in sketches.Points)
        {
            var radius = point.Mark == SketchOverlay.Mark.Selected ? SelectedPointRadius : SketchPointRadius;
            list.AddCircleFilled(point.At, radius + Theme.CasingWidth, Theme.Casing);
            list.AddCircleFilled(point.At, radius, Colour(point.Mark));
        }
        foreach (var ring in sketches.Rings)
        {
            list.AddCircle(ring, RingRadius, Theme.Casing, 0, SketchLineWidth + (2 * Theme.CasingWidth));
            list.AddCircle(ring, RingRadius, SketchColour, 0, SketchLineWidth);
        }
        // ADR-0041 item 6 (phase R11b): a square at the point that a tap under the mouse would take.
        if (sketches.Snap is { } snap)
        {
            list.AddRect(snap - new Vector2(SnapHalfSize), snap + new Vector2(SnapHalfSize), Theme.Casing, 0, ImDrawFlags.None, SketchLineWidth + (2 * Theme.CasingWidth));
            list.AddRect(snap - new Vector2(SnapHalfSize), snap + new Vector2(SnapHalfSize), SnapColour, 0, ImDrawFlags.None, SketchLineWidth);
        }
        foreach (var label in sketches.Labels)
            list.AddText(label.At, SketchColour, label.Text);
    }

    // ADR-0056 item 4: the dimension lines in orange, and those of the selected body in the colour of the selection, on
    // the background list, under each window of the panel.
    internal const uint DimensionColour = 0xFF40A0FF;
    private const float DimensionLineWidth = 1.5f;
    private const float ArrowLength = 12f;
    private const float ArrowHalfWidth = 5f;

    // V79 (TASK-0181): the labels of the last frame where the placement put them, and the size of a text in the font of
    // the panel, for a test.
    internal IReadOnlyList<DimensionOverlay.Label> PlacedLabels { get; private set; } = [];

    internal Vector2 TextSize(string text)
    {
        ImGui.SetCurrentContext(_context);
        return ImGui.CalcTextSize(text);
    }

    private void Dimensions(DimensionOverlay.Strokes dimensions, Vector2 displaySize)
    {
        var list = ImGui.GetBackgroundDrawList();
        foreach (var line in dimensions.Lines)
        {
            var colour = line.Mark == DimensionOverlay.Mark.Selected ? SelectedColour : DimensionColour;
            Cased(list, line.A, line.B, colour, DimensionLineWidth);
            var along = line.B - line.A;
            if (!line.Arrows || !(along.Length() > 2 * ArrowLength))
                continue;
            var unit = Vector2.Normalize(along);
            var side = new Vector2(-unit.Y, unit.X) * ArrowHalfWidth;
            list.AddTriangleFilled(line.A, line.A + (unit * ArrowLength) + side, line.A + (unit * ArrowLength) - side, colour);
            list.AddTriangleFilled(line.B, line.B - (unit * ArrowLength) + side, line.B - (unit * ArrowLength) - side, colour);
        }

        // Finding V79 of the codebase review of 2026-11-05: the labels go apart, with the size of the text of this font.
        PlacedLabels = DimensionOverlay.Place(dimensions.Labels, text => ImGui.CalcTextSize(text), displaySize);
        foreach (var label in PlacedLabels)
            list.AddText(label.At, label.Mark == DimensionOverlay.Mark.Selected ? SelectedColour : DimensionColour, label.Text);
    }

    private static uint Colour(SketchOverlay.Mark mark) => mark switch
    {
        SketchOverlay.Mark.Selected => SelectedColour,
        SketchOverlay.Mark.Other => OtherSketchColour,
        SketchOverlay.Mark.Reference => ReferenceColour,
        _ => SketchColour,
    };
}
