using System.Numerics;
using ImGuiNET;
using ThreeDEngine.Core.Numeric;
using ThreeDEngine.Core.Sketching;

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
    // Finding V136 of the codebase review of 2026-11-07 (c) (TASK-0281): the portrait corner is 8 pixels above the top of the
    // anchors (anchorsTop), so no segment is under Frame; until TASK-0281 the first segment, Line or Move, was under it at 390
    // by 844.
    internal static RingLayout Layout(Vector2 display, Vector2 centre, int count, float body = 0f, Vector4 inset = default, float anchorsTop = float.MaxValue)
    {
        var span = (count * RingSegment) + ((count - 1) * RingGap);
        if (display.X < display.Y)
        {
            var segment = Math.Min(RingSegment, ((MathF.PI / 2f) - ((count - 1) * RingGap)) / Math.Max(count, 1));
            // ADR-0086 item 3 (TASK-0275): the inner radius grows when a narrow segment would be less than 44 pixels at it, as
            // the five tools of a sketch need (0.28 radians).
            var portraitInner = Math.Max(150f, (ValueSize + 2f) / segment);
            return new RingLayout(new Vector2(display.X - 16f - inset.Z, Math.Min(display.Y - 16f - inset.W, anchorsTop - 8f)), portraitInner, MathF.PI, 1f, segment);
        }

        // The ring is outside the body on the screen: at least the half of the diagonal of its box, and 24 pixels more.
        var inner = Math.Clamp(Math.Max(display.Y / 3f, body + 24f), 160f, 375f);
        var reach = inner + RingBand + 16f;
        var clamped = new Vector2(Math.Clamp(centre.X, reach, Math.Max(reach, display.X - reach)), Math.Clamp(centre.Y, reach, Math.Max(reach, display.Y - reach)));
        return new RingLayout(clamped, inner, -span / 2f, 1f);
    }

    // ADR-0086 item 1 (TASK-0275): the ring of an active sketch on a landscape display. Its centre is the centre of the sketch
    // on the screen, its inner radius is 24 pixels more than the half of the diagonal of the sketch, at least 100, and its
    // segments go down on the left side, so the band does not cover the sketch. The centre moves only as far as the band
    // needs to stay 16 pixels to the right of the panel and inside the display.
    // Finding V134 of the codebase review of 2026-11-07 (c) (TASK-0280): the centre stays at or left of the window of the
    // sketch, so the band, which is on the left of the centre, is not under that window; and the inner radius is at most the
    // half of the display less the band and 16 pixels, so the band fits the height. Until TASK-0280 a sketch at the right
    // put segments under the window, and a large sketch put Finish below the display.
    internal static RingLayout SketchLayout(Vector2 display, Vector2 centre, int count, float sketch, float panelRight, float windowLeft)
    {
        var span = (count * RingSegment) + ((count - 1) * RingGap);
        var inner = Math.Min(Math.Max(100f, sketch + 24f), Math.Max(100f, (display.Y / 2f) - 16f - RingBand));
        var reach = inner + RingBand + 16f;
        var x = Math.Min(Math.Min(Math.Max(centre.X, panelRight + reach), windowLeft), display.X - 16f);
        var y = Math.Clamp(centre.Y, reach, Math.Max(reach, display.Y - reach));
        return new RingLayout(new Vector2(x, y), inner, MathF.PI + (span / 2f), -1f);
    }

    // Finding V135 of the codebase review of 2026-11-07 (c) (TASK-0281): a release over a window belongs to the window. While an
    // item of a window is active, ImGui counts the window as not hovered unless the flag says so, so until TASK-0281 the ring,
    // the rows and the chips took a release over a row of a window too, and one tap gave two actions.
    private static bool OverWindowAtRelease()
        => ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

    public static string RingItem(string label) => $"Ring:{label}";

    // The rectangle of the text of the label of a segment.
    public static string RingTextItem(string label) => $"RingText:{label}";

    private RingLayout? _ring;
    private IReadOnlyList<string> _ringLabels = [];

    // True when the point is on a segment of the ring of this frame.
    private bool OverRing(double x, double y)
        => _ring is { } ring && ring.At(_ringLabels, new Vector2((float)x, (float)y)) is not null;

    // ADR-0086 (the redesign, slice f, TASK-0275): the tools of the active sketch, in the order of the window of the sketch,
    // and the kind of each one; Finish ends the sketch.
    internal static readonly IReadOnlyList<(string Label, SketchToolKind? Kind)> SketchRingTools =
    [
        (LineToolLabel, SketchToolKind.Line),
        (SelectToolLabel, SketchToolKind.Select),
        (CircleToolLabel, SketchToolKind.Circle),
        (ArcToolLabel, SketchToolKind.Arc),
        (FinishLabel, null),
    ];

    // The tool of the active sketch in this frame, or null with no active sketch.
    private SketchToolKind? _sketchTool;

    // ADR-0087 (the redesign, slice g, TASK-0276): the segment that opens the second ring of the constraints that fit the
    // selection of the sketch, and the segment that goes back to the tools.
    public const string ConstrainLabel = "Constrain";
    public const string BackLabel = "Back";

    // The panel of the sketch of this frame; true while the second ring is open, and the selection that it belongs to.
    private SketchPanel? _sketchPanel;
    private bool _constrainOpen;
    private string _constrainKey = string.Empty;
    private List<(string Label, Action Press)> _fitting = [];

    // V134 (TASK-0280): the window of the sketch of the last frame, which the ring of this frame must not be under, and true
    // when the middle of each segment of the ring of this frame is on the display and under no window and no anchor. The
    // window of the sketch drops the buttons of the ring only then (ADR-0088 item 1).
    private Vector4? _sketchWindow;
    private bool _ringReachable;

    // The ring of the selected body, or of the active sketch (ADR-0086), or none while a pad is open or with no centre.
    private void Ring(Vector2 displaySize, LiveBody? body, Vector2? centre, float size)
    {
        _ring = null;
        _ringLabels = [];
        _ringReachable = false;
        if (centre is not { } at || _pad is not null || (body is null && !_sketchActive))
            return;

        var labels = new List<string>();
        if (_sketchActive)
        {
            // ADR-0086 items 1 and 2: while a sketch is active, the ring holds its tools in the place of the tools of a body.
            // ADR-0087 items 1 and 3: "Constrain" comes before Finish when a constraint fits the selection, and opens the
            // second ring of those constraints with "Back"; a change of the selection closes the second ring.
            _fitting = _sketchPanel?.Active is { } active ? FittingConstraints(active, _sketchPanel.Selection) : [];
            var key = string.Join(",", _sketchPanel?.Selection ?? []);
            if (key != _constrainKey)
                (_constrainOpen, _constrainKey) = (false, key);
            if (_constrainOpen && _fitting.Count > 0)
            {
                labels.AddRange(_fitting.Select(fit => fit.Label));
                labels.Add(BackLabel);
            }
            else
            {
                _constrainOpen = false;
                labels.AddRange(SketchRingTools.Where(tool => tool.Kind is not null).Select(tool => tool.Label));
                if (_fitting.Count > 0)
                    labels.Add(ConstrainLabel);
                labels.Add(FinishLabel);
            }
        }
        else if (body is not null)
        {
            // Finding V130 of the codebase review of 2026-11-07 (b) (TASK-0271): a mesh body takes no move (ADR-0083 item 4).
            if (body.Kind != Engine.Core.Commands.ImportStlCommandHandler.MeshKind)
                labels.Add(MoveLabel);
            if (body.Kind == "Extrude")
                labels.Add(SetDistanceLabel);
            labels.Add(body.Visible ? HideLabel : ShowLabel);
            if (_face is { CanSketch: true })
                labels.Add(SketchOnFaceButtonLabel);
        }

        // ADR-0086 item 1 (TASK-0275): on a landscape display the ring of a sketch is on the left of the sketch, between the
        // panel and the sketch, because the window of the sketch is on the right. The first form took the room left of that
        // window and moved the ring onto the points of the sketch.
        var panelRight = _panels.Count > 0 ? _panels[0].Z : 0f;
        var sketchRing = _sketchActive && displaySize.X >= displaySize.Y;
        var ring = sketchRing
            ? SketchLayout(displaySize, at, labels.Count, size, panelRight, _sketchWindow?.X ?? displaySize.X)
            : Layout(displaySize, at, labels.Count, size, _inset, _fixed.Count > 0 ? _fixed.Min(anchor => anchor.Y) : float.MaxValue);
        _ring = ring;
        _ringLabels = labels;
        var covers = _panels.Concat(_fixed).Concat(_sketchActive && _sketchWindow is { } window ? [window] : []).ToList();
        _ringReachable = ring.Segments(labels).Select(ring.Middle).All(middle =>
            middle.X >= 0f && middle.Y >= 0f && middle.X < displaySize.X && middle.Y < displaySize.Y
            && !covers.Any(cover => middle.X >= cover.X && middle.X < cover.Z && middle.Y >= cover.Y && middle.Y < cover.W));

        // The press and the release on one segment, with no window of the panels under the mouse, give its tool.
        // Finding V120 of the codebase review of 2026-11-07 (TASK-0264): a chip is drawn over the ring, so a release on a
        // chip is the tap of the chip only; the chips of the last frame are the chips that the person sees. Until
        // TASK-0264 one tap gave both tools.
        var io = ImGui.GetIO();
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !OverWindowAtRelease() && ChipAt(io.MousePos) is null && RowAt(io.MousePos) is null
            && ring.At(labels, io.MousePos) is { } released && ring.At(labels, io.MouseClickedPos[0]) == released)
        {
            if (_sketchActive)
                UseSketchTool(released);
            else if (body is not null)
                Use(released, body);
        }

        var list = ImGui.GetBackgroundDrawList();
        var colours = ImGui.GetStyle().Colors;
        foreach (var segment in ring.Segments(labels))
        {
            var hovered = ring.At(labels, io.MousePos) == segment.Label && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
            // ADR-0086 item 2: the tool in use has the colour of an active control.
            var inUse = _sketchActive && SketchRingTools.FirstOrDefault(tool => tool.Label == segment.Label).Kind is { } kind && kind == _sketchTool;
            var ground = ImGui.GetColorU32(colours[(int)(inUse ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button)]);
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
            // ADR-0087 item 5 (TASK-0276): on the left of a sketch, a label wider than the band ends 6 pixels outside the
            // inner edge, so it covers less of the sketch, and stays 4 pixels to the right of the panel; a plate of the colour
            // of its segment keeps it readable over the lines. The first form centred "Horizontal" over the points.
            if (sketchRing && text.X > RingBand - 12f)
            {
                var inner = ring.Centre.X + (MathF.Cos((segment.From + segment.To) / 2f) * ring.Inner);
                place.X = Math.Max(panelRight + 4f, inner - 6f - text.X);
                list.AddRectFilled(place - new Vector2(4f, 2f), place + text + new Vector2(4f, 2f), ground, 4f);
            }

            list.AddText(place, ImGui.GetColorU32(colours[(int)ImGuiCol.Text]), segment.Label);
            _items[RingTextItem(segment.Label)] = new Vector4(place.X, place.Y, place.X + text.X, place.Y + text.Y);
            _items[RingItem(segment.Label)] = new Vector4(middle.X - 4f, middle.Y - 4f, middle.X + 4f, middle.Y + 4f);
        }
    }

    // ADR-0086 item 2: the tool of a segment of the ring of a sketch, as its button of the window of the sketch does.
    private void UseSketchTool(string label)
    {
        // ADR-0087 item 3: the second ring opens and closes, and a constraint does what its button of the window does.
        if (_constrainOpen)
        {
            if (label != BackLabel && _fitting.FirstOrDefault(fit => fit.Label == label).Press is { } press)
                press();
            _constrainOpen = false;
            return;
        }

        if (label == ConstrainLabel)
        {
            _constrainOpen = true;
            return;
        }

        if (SketchRingTools.FirstOrDefault(tool => tool.Label == label) is not ({ } _, var kind))
            return;
        _actions.Add(kind is { } tool ? new SketchToolAction(tool) : new FinishSketchAction());
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
