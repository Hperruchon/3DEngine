using System.Numerics;
using ImGuiNET;
using ThreeDEngine.Core.Numeric;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0081 (the redesign, slice b2, TASK-0253): the tool ring of the "mix" direction of the designer brief. While a body
// is selected, its tools are the segments of a ring: on a landscape display to the right of the body, around the centre
// of its box on the screen; on a portrait display, as the phone, in the lower right corner, under the thumb. The ring
// holds the tools that exist today and need no text: Move, Distance for an extrude, Hide or Show, and Sketch on the
// selected face. The band of the ring is a part of the panels for a tap; the inside of the ring is the scene.
internal sealed unsafe partial class DesktopUi
{
    // The depth of a segment, from the inner to the outer radius (size-ring-band of the design system).
    public const float RingBand = 68f;

    // The angle of one segment, in radians, and the gap between two segments.
    internal const float RingSegment = 0.42f;
    private const float RingGap = 0.04f;

    // One segment of the ring: its label, its angles from the positive x axis of the screen (y down), and its action.
    internal sealed record RingSegmentLayout(string Label, float From, float To);

    // The place and the size of the ring for a display: its centre, its inner radius, the angle of its first segment, and
    // the angle of one segment.
    internal sealed record RingLayout(Vector2 Centre, float Inner, float Start, float Direction, float Segment = RingSegment)
    {
        public float Outer => Inner + RingBand;

        // The segments, in order, from the start in the direction of the layout.
        public IReadOnlyList<RingSegmentLayout> Segments(IReadOnlyList<string> labels)
        {
            var list = new List<RingSegmentLayout>();
            for (var i = 0; i < labels.Count; i++)
            {
                var a = Start + (Direction * i * (Segment + RingGap));
                var b = a + (Direction * Segment);
                list.Add(new RingSegmentLayout(labels[i], Math.Min(a, b), Math.Max(a, b)));
            }

            return list;
        }

        // The segment under a point of the window, or null.
        public string? At(IReadOnlyList<string> labels, Vector2 point)
        {
            var d = point - Centre;
            var r = d.Length();
            if (r < Inner || r > Outer)
                return null;
            var angle = MathF.Atan2(d.Y, d.X);
            foreach (var segment in Segments(labels))
            {
                foreach (var turn in new[] { -MathF.Tau, 0f, MathF.Tau })
                {
                    if (angle + turn >= segment.From && angle + turn <= segment.To)
                        return segment.Label;
                }
            }

            return null;
        }

        // The middle of a segment, where its label goes.
        public Vector2 Middle(RingSegmentLayout segment)
        {
            var angle = (segment.From + segment.To) / 2f;
            var radius = (Inner + Outer) / 2f;
            return Centre + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
        }
    }

    // The layout for a display and the centre of the selected body on the screen. Landscape: the ring is around the body,
    // with an inner radius of a third of the height (size-ring-inner: 240 pixels at 720, 375 at 1080), and its segments
    // start above the right side and go down. Portrait: the thumb ring, in the lower right corner, with its segments from
    // the left up to the top.
    // Findings V121 and V122 of the codebase review of 2026-11-07 (TASK-0264): the portrait segments fit between the left
    // and the top for each count, narrower when they must be (four give 0.36 radians, 54 pixels at the inner radius); until
    // TASK-0264 the fourth one went past the top and out of the display. The corner is 16 pixels inside the safe area
    // (left, top, right and bottom), so the navigation bar of the phone does not cover Move.
    internal static RingLayout Layout(Vector2 display, Vector2 centre, int count, float body = 0f, Vector4 inset = default)
    {
        var span = (count * RingSegment) + ((count - 1) * RingGap);
        if (display.X < display.Y)
        {
            var segment = Math.Min(RingSegment, ((MathF.PI / 2f) - ((count - 1) * RingGap)) / Math.Max(count, 1));
            return new RingLayout(new Vector2(display.X - 16f - inset.Z, display.Y - 16f - inset.W), 150f, MathF.PI, 1f, segment);
        }

        // The ring is outside the body on the screen: at least the half of the diagonal of its box, and 24 pixels more.
        var inner = Math.Clamp(Math.Max(display.Y / 3f, body + 24f), 160f, 375f);
        var reach = inner + RingBand + 16f;
        var clamped = new Vector2(Math.Clamp(centre.X, reach, Math.Max(reach, display.X - reach)), Math.Clamp(centre.Y, reach, Math.Max(reach, display.Y - reach)));
        return new RingLayout(clamped, inner, -span / 2f, 1f);
    }

    public static string RingItem(string label) => $"Ring:{label}";

    // The rectangle of the text of the label of a segment.
    public static string RingTextItem(string label) => $"RingText:{label}";

    private RingLayout? _ring;
    private IReadOnlyList<string> _ringLabels = [];

    // True when the point is on a segment of the ring of this frame.
    private bool OverRing(double x, double y)
        => _ring is { } ring && ring.At(_ringLabels, new Vector2((float)x, (float)y)) is not null;

    // The ring of the selected body, or none while a pad is open, a sketch is active, or no body is selected.
    private void Ring(Vector2 displaySize, LiveBody? body, Vector2? centre, float size)
    {
        _ring = null;
        _ringLabels = [];
        if (body is null || centre is not { } at || _pad is not null || _sketchActive)
            return;

        // Finding V130 of the codebase review of 2026-11-07 (b) (TASK-0271): a mesh body takes no move (ADR-0083 item 4).
        var labels = new List<string>();
        if (body.Kind != Engine.Core.Commands.ImportStlCommandHandler.MeshKind)
            labels.Add(MoveLabel);
        if (body.Kind == "Extrude")
            labels.Add(SetDistanceLabel);
        labels.Add(body.Visible ? HideLabel : ShowLabel);
        if (_face is { CanSketch: true })
            labels.Add(SketchOnFaceButtonLabel);

        var ring = Layout(displaySize, at, labels.Count, size, _inset);
        _ring = ring;
        _ringLabels = labels;

        // The press and the release on one segment, with no window of the panels under the mouse, give its tool.
        // Finding V120 of the codebase review of 2026-11-07 (TASK-0264): a chip is drawn over the ring, so a release on a
        // chip is the tap of the chip only; the chips of the last frame are the chips that the person sees. Until
        // TASK-0264 one tap gave both tools.
        var io = ImGui.GetIO();
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow) && ChipAt(io.MousePos) is null && RowAt(io.MousePos) is null
            && ring.At(labels, io.MousePos) is { } released && ring.At(labels, io.MouseClickedPos[0]) == released)
            Use(released, body);

        var list = ImGui.GetBackgroundDrawList();
        var colours = ImGui.GetStyle().Colors;
        foreach (var segment in ring.Segments(labels))
        {
            var hovered = ring.At(labels, io.MousePos) == segment.Label && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
            var ground = ImGui.GetColorU32(colours[(int)(hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button)]);
            // A segment is not convex, so it is filled as a row of small quads.
            const int steps = 12;
            for (var i = 0; i < steps; i++)
            {
                var (a, b) = (segment.From + ((segment.To - segment.From) * i / steps), segment.From + ((segment.To - segment.From) * (i + 1) / steps));
                Vector2 Point(float angle, float radius) => ring.Centre + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
                list.AddQuadFilled(Point(a, ring.Inner), Point(a, ring.Outer), Point(b, ring.Outer), Point(b, ring.Inner), ground);
            }

            list.PathArcTo(ring.Centre, ring.Outer, segment.From, segment.To);
            list.PathArcTo(ring.Centre, ring.Inner, segment.To, segment.From);
            list.PathStroke(ImGui.GetColorU32(colours[(int)ImGuiCol.Border]), ImDrawFlags.Closed, 1.5f);

            var middle = ring.Middle(segment);
            var text = ImGui.CalcTextSize(segment.Label);
            // The review of Codex of v0.251 (TASK-0271): a label at the edge of a portrait display stays 4 pixels inside the
            // safe area; until TASK-0271 the label "Sketch on face" of the fourth segment went past the right edge.
            var place = middle - (text / 2f);
            place.X = Math.Clamp(place.X, 4f + _inset.X, Math.Max(4f + _inset.X, displaySize.X - _inset.Z - 4f - text.X));
            list.AddText(place, ImGui.GetColorU32(colours[(int)ImGuiCol.Text]), segment.Label);
            _items[RingTextItem(segment.Label)] = new Vector4(place.X, place.Y, place.X + text.X, place.Y + text.Y);
            _items[RingItem(segment.Label)] = new Vector4(middle.X - 4f, middle.Y - 4f, middle.X + 4f, middle.Y + 4f);
        }
    }

    // The tool of a segment, as its button of the panel does.
    private void Use(string label, LiveBody body)
    {
        if (label == MoveLabel)
            OpenPad(new NumericPad(MoveLabel, _unit, new("dx", "0"), new("dy", "0"), new("dz", "0")), values => new MoveBodyAction(body.Id, values[0], values[1], values[2]), scope: PadScope.Body);
        else if (label == SetDistanceLabel)
            _actions.Add(new EditDistanceAction(body.Id));
        else if (label == HideLabel || label == ShowLabel)
            _actions.Add(new SetVisibleAction(body.Id, !body.Visible));
        else if (label == SketchOnFaceButtonLabel)
            _actions.Add(new SketchOnSelectedFaceAction());
    }
}
