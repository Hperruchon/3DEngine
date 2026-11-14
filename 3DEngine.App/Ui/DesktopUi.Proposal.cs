using System.Numerics;
using System.Text.Json;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0092 item 7 (the agent arc of the render "mix", phase R16r, TASK-0326): a proposal of an agent waits for the person.
// On a landscape display it is an amber arc on the band of a ring, above its centre, from 235 to 305 degrees (y down), with
// Reject at its left end and Accept at its right end. The ring is the ring of the selected body, or with no body ring a ring
// at the centre of the display with the inner radius of a ring of a body. The caption "AGENT · summary" is above the arc,
// and the command lines are under it, inside the ring. On a portrait display the proposal is a sheet above the anchors. The
// tools of the ring dim, and the top bar says "AGENT IS WAITING" in amber. Nothing shows on the home screen.
//
// Theme.cs has no amber token and is not in the write set of TASK-0326, so the amber is the colour of a pending value
// of a chip (DimensionColour).
internal sealed unsafe partial class DesktopUi
{
    // Item 7: the two ends of the arc, in radians from the positive x axis of the screen, y down.
    internal const float ProposalFrom = 235f * MathF.PI / 180f;
    internal const float ProposalTo = 305f * MathF.PI / 180f;

    // Item 7: the diameter of Reject and of Accept, the size of an anchor, at least the 44 pixels of ADR-0027 item 6.
    public const float ProposalButton = AnchorSize;

    // Item 7: the alpha of the tools of the ring while a proposal waits.
    internal const float ProposalDim = 0.35f;

    // Item 5: at most four command lines, then "and N more".
    public const int ProposalLines = 4;

    // The size of the text of the lines inside the ring, smaller than the font of the panels so a command fits the ring.
    internal const float ProposalTextSize = 20f;

    public const string AgentWaitingText = "AGENT IS WAITING";
    public const string ProposalStaleText = "The document changed after the proposal";

    public const string ProposalRejectItem = "Proposal:reject";
    public const string ProposalAcceptItem = "Proposal:accept";
    public const string ProposalCaptionItem = "Proposal:caption";
    public const string ProposalMoreItem = "Proposal:more";
    public const string ProposalCountItem = "Proposal:count";
    public const string ProposalPlaceItem = "Proposal:place";
    public const string ProposalStaleItem = "Proposal:stale";
    public const string ProposalWaitingItem = "Proposal:waiting";
    public const string ProposalOutcomeItem = "Proposal:outcome";
    public const string ProposalSheetName = "Agent proposal";

    public static string ProposalLineItem(int index) => $"Proposal:line:{index}";

    // The waiting proposals of this frame, the oldest first; the window shows the first one.
    private IReadOnlyList<ProposalCard> _proposals = [];

    // ADR-0113 item 3 (phase R19, TASK-0336): the height of "AGENT IS WAITING" and its gap when it is a line of its own under
    // the path line of a portrait display, from the last frame; else zero. The panel starts under it.
    private float _waitingLine;

    // Item 6: the outcome of the last accept or reject, which the status line gives until the next one.
    private string? _proposalOutcome;

    // The text of each item of the proposal in this frame, for a test.
    private readonly Dictionary<string, string> _proposalTexts = new(StringComparer.Ordinal);

    // The targets of Reject and Accept of a landscape arc in this frame: the centre of the circle and the rectangle of its word.
    private List<(bool Accept, Vector2 Centre, Vector4 Word)> _proposalTargets = [];

    // The targets of the last frame, which the person sees while the ring of this frame takes its tap (finding V160).
    private List<(bool Accept, Vector2 Centre, Vector4 Word)> _lastProposalTargets = [];

    // Finding V179 of the codebase review of 2026-11-14 (b) (TASK-0362): the rectangles of a landscape arc in this frame and in
    // the last frame: the box of the circle of Reject and of Accept, their words, the caption and the plate of the lines. The
    // plates of the dimensions draw before the arc, so they read the rectangles of the last frame.
    private List<Vector4> _proposalCovers = [];
    private List<Vector4> _lastProposalCovers = [];

    internal IReadOnlyDictionary<string, string> ProposalTexts => _proposalTexts;

    // ADR-0107 (TASK-0330): the rectangles of the words Reject and Accept of the arc of this frame, for a test.
    internal IReadOnlyList<Vector4> ProposalWords => [.. _proposalTargets.Select(target => target.Word)];

    // The ring of the arc of this frame, or null.
    internal RingLayout? ProposalRing { get; private set; }

    // Item 6: the host gives the outcome of an accept or a reject. ADR-0130 item 4 (phase R18i, TASK-0358): after an accept, the
    // line has a button "Undo" after the text. Finding V178 of the codebase review of 2026-11-14 (b) (TASK-0362): the host gives
    // the count of the log at the end of the accept, and the button shows only while the count of the status is that count, so
    // the newest step is the last step of the proposal. A later step, an Undo of the anchors or another Document takes it away.
    public void ShowProposalOutcome(string? text, int? undoAt = null) => (_proposalOutcome, _proposalUndo, _proposalUndoAt) = (text, undoAt is not null && text is not null, undoAt);

    private bool _proposalUndo;

    private int? _proposalUndoAt;

    // Item 7: Reject and Accept are a part of the panels for a tap, as the nodes of the history arc.
    private bool OverProposal(double x, double y) => ProposalTargetAt(new Vector2((float)x, (float)y)) is not null;

    private bool? ProposalTargetAt(Vector2 point) => ProposalTargetAt(_proposalTargets, point);

    // Finding V160 of the codebase review of 2026-11-09 (b) (TASK-0331): the ring draws before the arc, so it reads the targets
    // of the last frame, as it reads the chips of the last frame (finding V120).
    private bool OverLastProposal(Vector2 point) => ProposalTargetAt(_lastProposalTargets, point) is not null;

    private static bool? ProposalTargetAt(List<(bool Accept, Vector2 Centre, Vector4 Word)> targets, Vector2 point)
    {
        foreach (var (accept, centre, word) in targets)
        {
            if (Vector2.Distance(point, centre) <= ProposalButton / 2f || (point.X >= word.X && point.X < word.Z && point.Y >= word.Y && point.Y < word.W))
                return accept;
        }

        return null;
    }

    // The state of the proposal at the start of a frame. The host gives the waiting proposals, read from the queue with no
    // read of the session.
    private void BeginProposal(IReadOnlyList<ProposalCard>? proposals)
    {
        _proposals = _home is null ? proposals ?? [] : [];
        if (_proposals.Count == 0)
            _waitingLine = 0f;
        _proposalTexts.Clear();
        _lastProposalTargets = _proposalTargets;
        _proposalTargets = [];
        _lastProposalCovers = _proposalCovers;
        _proposalCovers = [];
        ProposalRing = null;
        ProposalGlowRectangle = null;
    }

    // Item 7: the tools of the ring dim while a proposal waits; they still take a tap.
    private bool DimRing()
    {
        if (_proposals.Count == 0)
            return false;
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ProposalDim);
        return true;
    }

    // Items 5 to 7: the proposal of this frame, the word in the top bar and the outcome in the status line.
    private void Proposal(Vector2 displaySize, LiveBody? body)
    {
        ProposalOutcomeLine(displaySize);
        _sheetTop = null;
        if (_proposals.Count == 0)
            return;

        var card = _proposals[0];
        AgentWaiting(displaySize);
        ProposalGlow(displaySize);
        var lines = ProposalLinesOf(card);
        if (displaySize.X < displaySize.Y)
            ProposalSheet(displaySize, card, lines);
        else
            ProposalArc(displaySize, body, card, lines);
    }

    // Item 5: at most four command lines and "and N more"; the count and the steps of undo (item 6); "1 of 3" when more
    // proposals wait (item 3); and the line of a stale proposal (item 4).
    private List<(string Item, string Text, bool Amber)> ProposalLinesOf(ProposalCard card)
    {
        var lines = new List<(string Item, string Text, bool Amber)>();
        for (var i = 0; i < Math.Min(ProposalLines, card.Commands.Count); i++)
            lines.Add((ProposalLineItem(i), ProposalLine(card, i, _unit), false));
        if (card.Commands.Count > ProposalLines)
            lines.Add((ProposalMoreItem, $"and {card.Commands.Count - ProposalLines} more", false));
        var count = Counted(card.Commands.Count, "command");
        lines.Add((ProposalCountItem, card.Applying ? $"Applying {count}" : $"{count} · {Counted(card.Commands.Count, "step")} of undo", false));
        if (_proposals.Count > 1)
            lines.Add((ProposalPlaceItem, $"1 of {_proposals.Count}", false));
        if (card.Stale)
            lines.Add((ProposalStaleItem, ProposalStaleText, true));
        return lines;
    }

    // The smallest inner radius of the ring of FreeRing, as the smallest ring of a sketch.
    internal const float ProposalInnerMinimum = 100f;

    // ADR-0107 (phase P0.138, TASK-0330): the ring of the arc with no ring of a body. Its centre is at the middle of the room
    // right of each panel at the left half of the display, at the height of the middle of the display, and its inner radius
    // is that of a ring of a body (ADR-0081), less where the room is narrow, so that each end and its word stay in the room.
    // The panels are those of this frame and of the frame before, because the rows of an active sketch draw after the arc.
    // Finding V159 of the codebase review of 2026-11-09 (b): until TASK-0330 the ring was at the centre of the display, and
    // at 1280 by 720 the window of the live sketches covered Accept and the panel covered the word "Reject".
    private RingLayout FreeRing(Vector2 displaySize)
    {
        var left = 16f + _inset.X;
        foreach (var panel in _panels.Concat(_lastPanels))
        {
            if (panel.X < displaySize.X / 2f)
                left = Math.Max(left, panel.Z + 16f);
        }

        var right = displaySize.X - 16f - _inset.Z;
        // The layout gives the height and the radius of a ring of a body. The arc draws only the top of the ring, so its centre
        // stays at the middle of the room, where the layout would move it to keep the whole ring in the display.
        var ring = Layout(displaySize, new Vector2((left + right) / 2f, displaySize.Y / 2f), 1, 0f, _inset, _anchorsTop);
        var word = Math.Max(ImGui.CalcTextSize(RejectLabel).X, ImGui.CalcTextSize(AcceptLabel).X);
        var room = ((right - left) / 2f) - (ProposalButton / 2f) - 8f - word;
        var fit = (room / MathF.Cos(ProposalTo)) - (RingBand / 2f);
        return ring with { Centre = new Vector2((left + right) / 2f, ring.Centre.Y), Inner = Math.Clamp(fit, ProposalInnerMinimum, ring.Inner) };
    }

    // ADR-0110 item 3 (Q3 A, TASK-0334): the line of a command whose value the frame names is the name of the value and its
    // change, "Plate C3  60 → 75 mm" or "Bracket distance  → 30 mm" with no value now. Each other command keeps its line of
    // ADR-0092 item 5. ADR-0114 item 2 (finding V164, TASK-0339): an angle shows in degrees, "Plate C1  60 → 45 deg".
    internal static string ProposalLine(ProposalCard card, int index, string unit)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.Values is { } values && index < values.Count && values[index] is { Name: { } name, Label: { } label } value
            ? $"{name} {label}  {(value.Now is { } now ? $"{Measured(now)} " : string.Empty)}{Arrow} {Measured(value.Shown ?? value.Number)} {value.Unit ?? unit}"
            : card.Commands[index];
    }

    // ADR-0110 item 2 (TASK-0334): the text of a chip whose value the proposal changes: the text of the chip with the arrow
    // and the proposed value before the unit, "C3  60 → 75 mm". The unit is the last word of the text of the chip.
    internal static string PendingText(string text, double proposed)
    {
        ArgumentNullException.ThrowIfNull(text);
        var unit = text.LastIndexOf(' ');
        return unit < 0
            ? $"{text} {Arrow} {EdgeValues.Exact(proposed)}"
            : $"{text[..unit]} {Arrow} {EdgeValues.Exact(proposed)}{text[unit..]}";
    }

    // A value in a line of a proposal (Dimensions.Format). ADR-0115 item 1 (phase R18c, TASK-0340): a chip shows the proposed
    // value with three decimals (EdgeValues.Exact, in PendingText); a line keeps this form until the proposal of R18i, because
    // the longer form does not fit the sheet of a phone.
    private static string Measured(double value) => ThreeDEngine.Core.Measuring.Dimensions.Format(value);

    // ADR-0110 (TASK-0334): the arrow of a change. The atlas of the fonts holds the glyphs of Latin-1 only, so the window draws
    // the arrow as a line and a head, in the width of the text "->".
    internal const string Arrow = "→";

    private static float ArrowWidth(ImFontPtr font, float size) => font.CalcTextSizeA(size, float.MaxValue, 0f, "->").X;

    // The width of a text with arrows, in the font at the size.
    internal static float ArrowTextWidth(ImFontPtr font, float size, string text)
    {
        var parts = text.Split(Arrow);
        return parts.Sum(part => font.CalcTextSizeA(size, float.MaxValue, 0f, part).X) + ((parts.Length - 1) * ArrowWidth(font, size));
    }

    // A text with arrows on a draw list: each part as a text, and each arrow as a line with a head at the middle of the line.
    internal static void ArrowText(ImDrawListPtr list, ImFontPtr font, float size, Vector2 at, uint colour, string text)
    {
        var parts = text.Split(Arrow);
        var x = at.X;
        var arrow = ArrowWidth(font, size);
        for (var i = 0; i < parts.Length; i++)
        {
            list.AddText(font, size, new Vector2(x, at.Y), colour, parts[i]);
            x += font.CalcTextSizeA(size, float.MaxValue, 0f, parts[i]).X;
            if (i == parts.Length - 1)
                break;
            var (from, to, y) = (x + (arrow * 0.15f), x + (arrow * 0.85f), at.Y + (size * 0.55f));
            var head = size * 0.2f;
            list.AddLine(new Vector2(from, y), new Vector2(to, y), colour, 1.5f);
            list.AddLine(new Vector2(to - head, y - head), new Vector2(to, y), colour, 1.5f);
            list.AddLine(new Vector2(to - head, y + head), new Vector2(to, y), colour, 1.5f);
            x += arrow;
        }
    }

    // A count and its word: "1 command", "3 commands".
    internal static string Counted(int count, string word) => count == 1 ? $"1 {word}" : $"{count} {word}s";

    private static string Caption(ProposalCard card) => $"AGENT · {card.Summary}";

    // Item 7: the arc on a landscape display.
    private void ProposalArc(Vector2 displaySize, LiveBody? body, ProposalCard card, List<(string Item, string Text, bool Amber)> lines)
    {
        // The ring of the selected body, or with no body ring the ring of FreeRing.
        var ring = body is not null && !_sketchActive && _ring is { } bodyRing
            ? bodyRing
            : FreeRing(displaySize);
        ProposalRing = ring;
        var radius = ring.Inner + (RingBand / 2f);
        Vector2 At(float angle, float distance) => ring.Centre + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance);
        var (reject, accept) = (At(ProposalFrom, radius), At(ProposalTo, radius));

        // The word of each end, out of the arc: Reject at the left of its circle, Accept at the right of its circle.
        var lineHeight = ImGui.GetTextLineHeight();
        var rejectWidth = ImGui.CalcTextSize(RejectLabel).X;
        var rejectWord = new Vector4(reject.X - (ProposalButton / 2f) - 8f - rejectWidth, reject.Y - (lineHeight / 2f), reject.X - (ProposalButton / 2f) - 8f, reject.Y + (lineHeight / 2f));
        var acceptWord = new Vector4(accept.X + (ProposalButton / 2f) + 8f, accept.Y - (lineHeight / 2f), accept.X + (ProposalButton / 2f) + 8f + ImGui.CalcTextSize(AcceptLabel).X, accept.Y + (lineHeight / 2f));
        _proposalTargets = [(false, reject, rejectWord), (true, accept, acceptWord)];
        foreach (var (_, centre, word) in _proposalTargets)
        {
            var box = new Vector4(centre.X - (ProposalButton / 2f), centre.Y - (ProposalButton / 2f), centre.X + (ProposalButton / 2f), centre.Y + (ProposalButton / 2f));
            _proposalCovers.AddRange([box, word]);
        }

        // Item 6: a press and a release on one end, with no window, no chip and no row under the pointer, give its action. A
        // proposal that applies takes no tap.
        var io = ImGui.GetIO();
        if (!card.Applying && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !OverWindowAtRelease() && ChipAt(io.MousePos) is null && RowAt(io.MousePos) is null
            && ProposalTargetAt(io.MousePos) is { } released && ProposalTargetAt(io.MouseClickedPos[0]) == released)
            _actions.Add(released ? new AcceptProposalAction(card.Id) : new RejectProposalAction(card.Id));

        // The band of the arc: a light amber ground and an amber outline.
        var list = ImGui.GetBackgroundDrawList();
        const int steps = 24;
        var ground = (DimensionColour & 0x00FFFFFFu) | (0x48u << 24);
        for (var i = 0; i < steps; i++)
        {
            var (a, b) = (ProposalFrom + ((ProposalTo - ProposalFrom) * i / steps), ProposalFrom + ((ProposalTo - ProposalFrom) * (i + 1) / steps));
            list.AddQuadFilled(At(a, ring.Inner), At(a, ring.Outer), At(b, ring.Outer), At(b, ring.Inner), ground);
        }

        list.PathArcTo(ring.Centre, ring.Outer, ProposalFrom, ProposalTo, 32);
        list.PathArcTo(ring.Centre, ring.Inner, ProposalTo, ProposalFrom, 32);
        list.PathStroke(DimensionColour, ImDrawFlags.Closed, 2f);

        // Reject: a ground of a control, an amber outline and a cross. Accept: an amber ground and a tick in the colour of the
        // casing. A proposal that applies shows both at a third of their alpha.
        var colours = ImGui.GetStyle().Colors;
        var hovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow) ? null : ProposalTargetAt(io.MousePos);
        var fade = card.Applying ? 0.35f : 1f;
        var ink = ImGui.GetColorU32(colours[(int)ImGuiCol.Text] with { W = fade });
        var half = ProposalButton / 2f;
        list.AddCircleFilled(reject, half, ImGui.GetColorU32(colours[(int)(hovered == false ? ImGuiCol.ButtonHovered : ImGuiCol.Button)] with { W = 0.95f * fade }), 32);
        list.AddCircle(reject, half, Faded(DimensionColour, fade), 32, 2f);
        list.AddLine(reject - new Vector2(9f, 9f), reject + new Vector2(9f, 9f), ink, 2.5f);
        list.AddLine(reject + new Vector2(-9f, 9f), reject + new Vector2(9f, -9f), ink, 2.5f);
        list.AddCircleFilled(accept, half, Faded(DimensionColour, (hovered == true ? 0.8f : 1f) * fade), 32);
        var tick = Faded(Theme.Casing, fade);
        list.AddLine(accept + new Vector2(-10f, 1f), accept + new Vector2(-3f, 8f), tick, 3f);
        list.AddLine(accept + new Vector2(-3f, 8f), accept + new Vector2(11f, -8f), tick, 3f);
        list.AddText(new Vector2(rejectWord.X, rejectWord.Y), ink, RejectLabel);
        list.AddText(new Vector2(acceptWord.X, acceptWord.Y), Faded(DimensionColour, fade), AcceptLabel);
        _items[ProposalRejectItem] = new Vector4(reject.X - half, reject.Y - half, reject.X + half, reject.Y + half);
        _items[ProposalAcceptItem] = new Vector4(accept.X - half, accept.Y - half, accept.X + half, accept.Y + half);

        // The caption above the arc, under the top bar. When the room above the arc is too small, the caption is the first
        // line inside the ring.
        var caption = Caption(card);
        var top = _items.TryGetValue(TitleItem, out var title) ? title.W + 4f : 16f + _inset.Y;
        var captionWidth = Math.Min(ImGui.CalcTextSize(caption).X, displaySize.X - 32f - _inset.X - _inset.Z);
        caption = Fit(caption, captionWidth);
        var captionAt = new Vector2(
            Math.Clamp(ring.Centre.X - (captionWidth / 2f), 16f + _inset.X, Math.Max(16f + _inset.X, displaySize.X - 16f - _inset.Z - captionWidth)),
            ring.Centre.Y - ring.Outer - 8f - lineHeight);
        var inside = new List<(string Item, string Text, bool Amber)>(lines);
        var captionBox = new Vector4(captionAt.X, captionAt.Y, captionAt.X + ImGui.CalcTextSize(caption).X, captionAt.Y + lineHeight);
        // ADR-0113 item 6 (phase R19, TASK-0336): the caption goes inside the ring when a fixed window, as the rows of the body,
        // covers its place too. Until TASK-0336 the rows covered it at 640 by 360.
        var covered = _fixed.Any(box => box.X < captionBox.Z && captionBox.X < box.Z && box.Y < captionBox.W && captionBox.Y < box.W);
        if (captionAt.Y >= top && !covered)
        {
            list.AddText(captionAt, DimensionColour, caption);
            Shown(ProposalCaptionItem, caption, captionBox);
            _proposalCovers.Add(captionBox);
        }
        else
            inside.Insert(0, (ProposalCaptionItem, Caption(card), true));

        // The lines inside the ring, under the arc, from 0.6 of the inner radius above the centre, each one cut to the chord of
        // the inner circle at its top, on a plate of the colour of the windows.
        var font = ImGui.GetFont();
        var step = ProposalTextSize + 4f;
        var y = Math.Max(ring.Centre.Y - (ring.Inner * 0.6f), Math.Max(reject.Y, accept.Y) + half + 8f);
        // ADR-0113 item 6 (phase R19, TASK-0336): the lines start under a fixed window, as the rows of the body, that covers
        // the place of the first line. Until TASK-0336 the rows covered the first line at 640 by 360.
        foreach (var box in _fixed)
        {
            if (box.X < ring.Centre.X + ring.Inner && ring.Centre.X - ring.Inner < box.Z && box.Y < y + step && y - 6f < box.W)
                y = Math.Max(y, box.W + 6f);
        }

        var placed = new List<(string Item, string Text, bool Amber, Vector2 At, float Width)>();
        foreach (var (item, text, amber) in inside)
        {
            var d = Math.Abs(y - ring.Centre.Y);
            var chord = d < ring.Inner ? 2f * MathF.Sqrt((ring.Inner * ring.Inner) - (d * d)) - 16f : 0f;
            var shown = FitAt(text, Math.Max(chord, 48f));
            var width = ArrowTextWidth(font, ProposalTextSize, shown);
            placed.Add((item, shown, amber, new Vector2(ring.Centre.X - (width / 2f), y), width));
            y += step;
        }

        if (placed.Count == 0)
            return;
        var widest = placed.Max(line => line.Width);
        var plateTop = placed[0].At.Y - 6f;
        list.AddRectFilled(new Vector2(ring.Centre.X - (widest / 2f) - 10f, plateTop), new Vector2(ring.Centre.X + (widest / 2f) + 10f, y + 2f),
            ImGui.GetColorU32(colours[(int)ImGuiCol.WindowBg] with { W = 0.8f }), 6f);
        _proposalCovers.Add(new Vector4(ring.Centre.X - (widest / 2f) - 10f, plateTop, ring.Centre.X + (widest / 2f) + 10f, y + 2f));
        foreach (var (item, text, amber, at, width) in placed)
        {
            ArrowText(list, font, ProposalTextSize, at, amber ? DimensionColour : ImGui.GetColorU32(colours[(int)ImGuiCol.Text]), text);
            Shown(item, text, new Vector4(at.X, at.Y, at.X + width, at.Y + ProposalTextSize));
        }
    }

    // The height of the sheet of the last frame, so the sheet of this frame knows if it fits above the ring.
    private float _sheetHeight;

    // ADR-0131 item 1 (phase R18k, TASK-0360): the top of the sheet of the last frame, or null with no sheet, so the rows under
    // the part end above it.
    private float? _sheetTop;

    // ADR-0111 item 4 (TASK-0331): the highest point of the band of the ring of this frame, or null with no ring. The top of a
    // segment is at an end of its arc, or at the top of the circle when the segment holds that angle.
    internal float? RingTop()
    {
        if (_ring is not { } ring || _ringLabels.Count == 0)
            return null;
        var top = float.MaxValue;
        foreach (var segment in ring.Segments(_ringLabels))
        {
            var angles = new List<float> { segment.From, segment.To };
            angles.AddRange(new[] { -MathF.PI / 2f, 3f * MathF.PI / 2f }.Where(up => up >= segment.From && up <= segment.To));
            foreach (var angle in angles)
            {
                foreach (var radius in new[] { ring.Inner, ring.Outer })
                    top = Math.Min(top, ring.Centre.Y + (radius * MathF.Sin(angle)));
            }
        }

        return top;
    }

    // Item 7: the sheet on a portrait display, above the anchors, with the same lines, Reject and Accept.
    // ADR-0111 item 4 (TASK-0331): the sheet ends 8 pixels above the ring, so each tool of the ring takes its tap, when the
    // sheet of the last frame fits between the top bar and the ring; else it ends above the anchors, as before, and covers the
    // ring. Finding V162 of the codebase review of 2026-11-09 (b): until TASK-0331 the sheet covered each tool of the ring.
    private void ProposalSheet(Vector2 displaySize, ProposalCard card, List<(string Item, string Text, bool Amber)> lines)
    {
        var width = displaySize.X - 32f - _inset.X - _inset.Z;
        var bottom = Math.Min(_anchorsTop, displaySize.Y - 16f - _inset.W) - 8f;
        // The top bar, and the word "AGENT IS WAITING" under it on a narrow display, stay above the sheet.
        var top = Math.Max(_items.TryGetValue(TitleItem, out var title) ? title.W + 4f : 16f + _inset.Y,
            _items.TryGetValue(ProposalWaitingItem, out var waiting) ? waiting.W + 4f : 0f);
        if (RingTop() is { } ringTop && ringTop - 8f < bottom && ringTop - 8f - _sheetHeight >= top)
            bottom = ringTop - 8f;
        ImGui.SetNextWindowPos(new Vector2(16f + _inset.X, bottom), ImGuiCond.Always, new Vector2(0f, 1f));
        ImGui.SetNextWindowSize(new Vector2(width, 0f), ImGuiCond.Always);
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing;
        if (ImGui.Begin($"{ProposalSheetName}###ProposalSheet", flags))
        {
            var room = ImGui.GetContentRegionAvail().X;
            var amber = ImGui.ColorConvertU32ToFloat4(DimensionColour);
            // The caption wraps, so a summary of 120 characters shows whole on a phone; each other line is cut to the width.
            ImGui.PushStyleColor(ImGuiCol.Text, amber);
            ImGui.PushTextWrapPos(0f);
            ImGui.TextUnformatted(Caption(card));
            ImGui.PopTextWrapPos();
            ImGui.PopStyleColor();
            Item(ProposalCaptionItem);
            _proposalTexts[ProposalCaptionItem] = Caption(card);
            foreach (var (item, text, isAmber) in lines)
            {
                // ADR-0110 item 3 (TASK-0334): a line with an arrow draws its arrow as a line and a head.
                var arrow = text.Contains(Arrow, StringComparison.Ordinal);
                var shown = arrow ? FitWith(ImGui.GetFont(), ImGui.GetFontSize(), text, room) : Fit(text, room);
                if (arrow)
                {
                    var at = ImGui.GetCursorScreenPos();
                    ImGui.Dummy(new Vector2(ArrowTextWidth(ImGui.GetFont(), ImGui.GetFontSize(), shown), ImGui.GetTextLineHeight()));
                    ArrowText(ImGui.GetWindowDrawList(), ImGui.GetFont(), ImGui.GetFontSize(), at, ImGui.GetColorU32(isAmber ? amber : ImGui.GetStyle().Colors[(int)ImGuiCol.Text]), shown);
                }
                else if (isAmber)
                    ImGui.TextColored(amber, shown);
                else
                    ImGui.TextUnformatted(shown);
                Item(item);
                _proposalTexts[item] = shown;
            }

            // Item 6: a proposal that applies takes no tap.
            ImGui.BeginDisabled(card.Applying);
            var button = new Vector2((room - ImGui.GetStyle().ItemSpacing.X) / 2f, MinimumControlHeight);
            if (ImGui.Button($"{RejectLabel}##Proposal", button))
                _actions.Add(new RejectProposalAction(card.Id));
            Item(ProposalRejectItem);
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, amber);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, amber with { W = 0.85f });
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, amber with { W = 0.7f });
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertU32ToFloat4(Theme.Casing));
            if (ImGui.Button($"{AcceptLabel}##Proposal", button))
                _actions.Add(new AcceptProposalAction(card.Id));
            ImGui.PopStyleColor(4);
            Item(ProposalAcceptItem);
            ImGui.EndDisabled();
            AddPanelRectangle();
            _sheetHeight = ImGui.GetWindowSize().Y;
        }

        ImGui.End();
        _sheetTop = bottom - _sheetHeight;
    }

    // Item 7: "AGENT IS WAITING" in amber with an amber dot, in the top bar, or under it when the top bar has no room.
    private void AgentWaiting(Vector2 displaySize)
    {
        var size = ImGui.CalcTextSize(AgentWaitingText);
        var title = _items.TryGetValue(TitleItem, out var bar) ? bar : new Vector4(16f + _inset.X, 16f + _inset.Y, 16f + _inset.X, 16f + _inset.Y + TopBarHeight);
        var landscape = displaySize.X >= displaySize.Y;
        var right = displaySize.X - 16f - _inset.Z - (landscape ? CommandLineWidth(displaySize) + 12f : 0f);
        // ADR-0108 item 6 (phase R18a, TASK-0332): on a landscape display the dot and the word end 12 pixels left of the
        // command line, in the middle of its height, as the render; a portrait display has no command line, so they follow
        // the title. Until TASK-0332 the word followed the title on each display.
        var at = landscape
            ? new Vector2(right - size.X, 16f + _inset.Y + ((ImGui.GetFrameHeight() - size.Y) / 2f))
            : new Vector2(title.Z + 16f + WaitingDot, title.Y);
        var dot = WaitingDot;
        _waitingLine = 0f;
        if (at.X - dot < title.Z + 16f || at.X + size.X > right)
        {
            // ADR-0113 item 3 (phase R19, TASK-0336): on a portrait display the word goes under the path line,
            // and the panel of the next frame starts under the word. Until TASK-0336 the path line covered the word.
            var under = _portrait && _items.TryGetValue(PathItem, out var path) ? path.W : title.W;
            at = new Vector2(title.X + dot, under + 4f);
            _waitingLine = _portrait ? size.Y + 4f : 0f;
        }

        // ADR-0108 item 6: the amber dot before the word.
        var list = ImGui.GetBackgroundDrawList();
        list.AddCircleFilled(new Vector2(at.X - dot + 4f, at.Y + (size.Y / 2f)), 4f, DimensionColour);
        list.AddText(at, DimensionColour, AgentWaitingText);
        Shown(ProposalWaitingItem, AgentWaitingText, new Vector4(at.X - dot, at.Y, at.X + size.X, at.Y + size.Y));
        // Finding V179 (TASK-0362): the word is no window either, so it is a cover of the plates of the next frame.
        _proposalCovers.Add(new Vector4(at.X - dot, at.Y, at.X + size.X, at.Y + size.Y));
    }

    // ADR-0108 item 6: the room of the dot before "AGENT IS WAITING": its width of 8 pixels and a gap of 8 pixels.
    private const float WaitingDot = 16f;

    // ADR-0130 item 3 (phase R18i, TASK-0358): while a proposal waits, a glow of amber along the edges of the display, inside
    // the safe area: four frames from the edge inward, each wider and fainter. It takes no tap, so it is not an item of the
    // window; the rectangle of this frame is for a test.
    internal Vector4? ProposalGlowRectangle { get; private set; }

    internal static readonly (float Width, float Alpha)[] GlowFrames = [(2f, 0.9f), (6f, 0.35f), (12f, 0.15f), (20f, 0.06f)];

    private void ProposalGlow(Vector2 displaySize)
    {
        var (min, max) = (new Vector2(_inset.X, _inset.Y), new Vector2(displaySize.X - _inset.Z, displaySize.Y - _inset.W));
        var list = ImGui.GetBackgroundDrawList();
        foreach (var (width, alpha) in GlowFrames)
        {
            var half = new Vector2(width / 2f, width / 2f);
            list.AddRect(min + half, max - half, ImGui.GetColorU32(Faded(DimensionColour, alpha)), 0f, ImDrawFlags.None, width);
        }

        ProposalGlowRectangle = new Vector4(min.X, min.Y, max.X, max.Y);
    }

    // ADR-0130 item 4: the word of the button of the outcome of an accept, which undoes the steps of the proposal one by one.
    public const string ProposalUndoLabel = "Undo";
    public const string ProposalUndoItem = "Proposal:undo";

    // Item 6: the outcome of the last accept or reject, at the lower right above the line of the status, in its mono font.
    // ADR-0130 item 4 (phase R18i, TASK-0358): after an accept, "Applied · Bracket rebuilt" and a button "Undo", as the render;
    // a tap on Undo gives the Undo of the anchors, and the line goes.
    private void ProposalOutcomeLine(Vector2 displaySize)
    {
        if (_proposalOutcome is not { } outcome)
            return;
        if (_proposalUndo && _status.Log != _proposalUndoAt)
            _proposalUndo = false;
        var right = displaySize.X - 16f - _inset.Z;
        ImGui.PushFont(_monoFont);
        var undo = _proposalUndo ? StatusSeparator + ProposalUndoLabel : string.Empty;
        var text = Fit(outcome, displaySize.X - 32f - _inset.X - _inset.Z - ImGui.CalcTextSize(undo).X);
        var size = ImGui.CalcTextSize(text + undo);
        ImGui.PopFont();
        var statusTop = _items.TryGetValue(StatusLabel, out var status) ? status.Y : displaySize.Y - 16f - _inset.W - AnchorSize;
        ImGui.SetNextWindowPos(new Vector2(right - size.X, statusTop - 6f), ImGuiCond.Always, new Vector2(0f, 1f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if (ImGui.Begin("##ProposalOutcome", FixedFlags))
        {
            ImGui.PushFont(_monoFont);
            // With the button the line is as high as a target of ADR-0027 item 6, and each text is in its middle.
            var top = _proposalUndo ? (MinimumControlHeight - ImGui.GetTextLineHeight()) / 2f : 0f;
            ImGui.SetCursorPosY(top);
            ImGui.TextUnformatted(text);
            Item(ProposalOutcomeItem);
            _proposalTexts[ProposalOutcomeItem] = text;
            if (_proposalUndo)
            {
                ImGui.SameLine(0f, 0f);
                ImGui.SetCursorPosY(top);
                ImGui.TextUnformatted(StatusSeparator);
                ImGui.SameLine(0f, 0f);
                ImGui.SetCursorPosY(0f);
                var at = ImGui.GetCursorScreenPos();
                if (ImGui.InvisibleButton(ProposalUndoItem, new Vector2(ImGui.CalcTextSize(ProposalUndoLabel).X, MinimumControlHeight)))
                {
                    _actions.Add(new UndoAction());
                    (_proposalOutcome, _proposalUndo) = (null, false);
                }

                Item(ProposalUndoItem);
                ImGui.GetWindowDrawList().AddText(new Vector2(at.X, at.Y + top), ImGui.GetColorU32(Theme.Accent), ProposalUndoLabel);
            }

            ImGui.PopFont();
            AddWindowRectangle();
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    private void Shown(string item, string text, Vector4 rectangle)
    {
        _items[item] = rectangle;
        _proposalTexts[item] = text;
    }

    // The text, or its start and "...", in the width, at the size of the lines inside the ring.
    private static string FitAt(string text, float width) => FitWith(ImGui.GetFont(), ProposalTextSize, text, width);

    // The text, or its start and "...", in the width, in the font at the size. An arrow has the width that ArrowText gives it.
    private static string FitWith(ImFontPtr font, float size, string text, float width)
    {
        float Width(string shown) => ArrowTextWidth(font, size, shown);
        if (Width(text) <= width)
            return text;
        for (var length = text.Length - 1; length > 0; length--)
        {
            var cut = text[..length] + "...";
            if (Width(cut) <= width)
                return cut;
        }

        return "...";
    }

    // A colour of the draw list (alpha, blue, green, red) with its alpha multiplied.
    private static uint Faded(uint colour, float alpha)
        => (colour & 0x00FFFFFFu) | ((uint)Math.Round(((colour >> 24) & 0xFFu) * Math.Clamp(alpha, 0f, 1f)) << 24);
}

// ADR-0092 items 4 and 5 (TASK-0326): one waiting proposal as the window shows it: its identifier, its summary, the text of
// each command, true when the Document changed after the post, and true while its commands apply.
// ADR-0110 item 1 (TASK-0334): Values holds the proposed value of each command, in the order of Commands, or null for a
// command with no value. A card with no Values shows no pending chip.
// ADR-0129 item 3 (finding V177, TASK-0356): CommandIds holds the identifier of each command, in the order of Commands, so
// the text of the step of an accepted value goes to its own command.
internal sealed record ProposalCard(Guid Id, string Summary, IReadOnlyList<string> Commands, bool Stale = false, bool Applying = false,
    IReadOnlyList<ProposedValue?>? Values = null, IReadOnlyList<Guid>? CommandIds = null)
{
    // Item 5: the text of a command: its name and the value of each parameter, in the order of the envelope. An identifier
    // shows its first 8 characters, as a row of a body does; another value shows its JSON text.
    public static string Line(string name, IEnumerable<KeyValuePair<string, JsonElement>> parameters)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(parameters);
        return string.Join(" ", parameters.Select(parameter => Value(parameter.Value)).Prepend(name));
    }

    private static string Value(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
            return value.GetRawText();
        try
        {
            var text = value.GetString() ?? string.Empty;
            return Guid.TryParse(text, out var id) ? id.ToString()[..8] : text;
        }
        catch (InvalidOperationException)
        {
            return value.GetRawText();
        }
    }
}

// ADR-0092 item 6 (TASK-0326): the outcome of an accept or a reject, for the status line: the state of the proposal, the
// count of the applied commands and of the commands, and for a failed proposal the index and the code of the command that
// stopped it. A proposal that is no longer open has no state.
internal sealed record ProposalOutcome(string? State, int Applied, int Count, int? FailedIndex = null, string? FailedCode = null)
{
    public static readonly ProposalOutcome Gone = new(null, 0, 0);

    public string Text => State switch
    {
        "accepted" => $"Proposal accepted: {DesktopUi.Counted(Applied, "command")} applied, {DesktopUi.Counted(Applied, "step")} of undo",
        "rejected" => "Proposal rejected: no command applied",
        "failed" => $"Proposal failed at command {(FailedIndex ?? Applied) + 1} of {Count}{(FailedCode is { } code ? $" ({code})" : string.Empty)}: {DesktopUi.Counted(Applied, "command")} applied",
        _ => "The proposal is no longer open",
    };

    // ADR-0130 item 4 (phase R18i, TASK-0358): the line after an accept, as the render: "Applied · Bracket rebuilt" with the
    // name of the body that was selected at the accept, or "Applied · 2 commands" with no body.
    public string AppliedText(string? body) => body is not null ? $"Applied · {body} rebuilt" : $"Applied · {DesktopUi.Counted(Applied, "command")}";
}

// ADR-0092 item 6: Reject and Accept of the waiting proposal. The host applies them; no route of the surface does.
internal sealed record AcceptProposalAction(Guid Proposal) : PanelAction;

internal sealed record RejectProposalAction(Guid Proposal) : PanelAction;
