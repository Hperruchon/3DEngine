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

    // Item 6: the outcome of the last accept or reject, which the status line gives until the next one.
    private string? _proposalOutcome;

    // The text of each item of the proposal in this frame, for a test.
    private readonly Dictionary<string, string> _proposalTexts = new(StringComparer.Ordinal);

    // The targets of Reject and Accept of a landscape arc in this frame: the centre of the circle and the rectangle of its word.
    private List<(bool Accept, Vector2 Centre, Vector4 Word)> _proposalTargets = [];

    internal IReadOnlyDictionary<string, string> ProposalTexts => _proposalTexts;

    // The ring of the arc of this frame, or null.
    internal RingLayout? ProposalRing { get; private set; }

    // Item 6: the host gives the outcome of an accept or a reject.
    public void ShowProposalOutcome(string? text) => _proposalOutcome = text;

    // Item 7: Reject and Accept are a part of the panels for a tap, as the nodes of the history arc.
    private bool OverProposal(double x, double y) => ProposalTargetAt(new Vector2((float)x, (float)y)) is not null;

    private bool? ProposalTargetAt(Vector2 point)
    {
        foreach (var (accept, centre, word) in _proposalTargets)
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
        _proposalTexts.Clear();
        _proposalTargets = [];
        ProposalRing = null;
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
        if (_proposals.Count == 0)
            return;

        var card = _proposals[0];
        AgentWaiting(displaySize);
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
            lines.Add((ProposalLineItem(i), card.Commands[i], false));
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

    // A count and its word: "1 command", "3 commands".
    internal static string Counted(int count, string word) => count == 1 ? $"1 {word}" : $"{count} {word}s";

    private static string Caption(ProposalCard card) => $"AGENT · {card.Summary}";

    // Item 7: the arc on a landscape display.
    private void ProposalArc(Vector2 displaySize, LiveBody? body, ProposalCard card, List<(string Item, string Text, bool Amber)> lines)
    {
        // The ring of the selected body, or with no body ring a ring at the centre of the display with the inner radius of a
        // ring of a body.
        var ring = body is not null && !_sketchActive && _ring is { } bodyRing
            ? bodyRing
            : Layout(displaySize, displaySize / 2f, 1, 0f, _inset, _anchorsTop);
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
        if (captionAt.Y >= top)
        {
            list.AddText(captionAt, DimensionColour, caption);
            Shown(ProposalCaptionItem, caption, new Vector4(captionAt.X, captionAt.Y, captionAt.X + ImGui.CalcTextSize(caption).X, captionAt.Y + lineHeight));
        }
        else
            inside.Insert(0, (ProposalCaptionItem, Caption(card), true));

        // The lines inside the ring, under the arc, from 0.6 of the inner radius above the centre, each one cut to the chord of
        // the inner circle at its top, on a plate of the colour of the windows.
        var font = ImGui.GetFont();
        var step = ProposalTextSize + 4f;
        var y = Math.Max(ring.Centre.Y - (ring.Inner * 0.6f), Math.Max(reject.Y, accept.Y) + half + 8f);
        var placed = new List<(string Item, string Text, bool Amber, Vector2 At, float Width)>();
        foreach (var (item, text, amber) in inside)
        {
            var d = Math.Abs(y - ring.Centre.Y);
            var chord = d < ring.Inner ? 2f * MathF.Sqrt((ring.Inner * ring.Inner) - (d * d)) - 16f : 0f;
            var shown = FitAt(text, Math.Max(chord, 48f));
            var width = font.CalcTextSizeA(ProposalTextSize, float.MaxValue, 0f, shown).X;
            placed.Add((item, shown, amber, new Vector2(ring.Centre.X - (width / 2f), y), width));
            y += step;
        }

        if (placed.Count == 0)
            return;
        var widest = placed.Max(line => line.Width);
        var plateTop = placed[0].At.Y - 6f;
        list.AddRectFilled(new Vector2(ring.Centre.X - (widest / 2f) - 10f, plateTop), new Vector2(ring.Centre.X + (widest / 2f) + 10f, y + 2f),
            ImGui.GetColorU32(colours[(int)ImGuiCol.WindowBg] with { W = 0.8f }), 6f);
        foreach (var (item, text, amber, at, width) in placed)
        {
            list.AddText(font, ProposalTextSize, at, amber ? DimensionColour : ImGui.GetColorU32(colours[(int)ImGuiCol.Text]), text);
            Shown(item, text, new Vector4(at.X, at.Y, at.X + width, at.Y + ProposalTextSize));
        }
    }

    // Item 7: the sheet on a portrait display, above the anchors, with the same lines, Reject and Accept.
    private void ProposalSheet(Vector2 displaySize, ProposalCard card, List<(string Item, string Text, bool Amber)> lines)
    {
        var width = displaySize.X - 32f - _inset.X - _inset.Z;
        var bottom = Math.Min(_anchorsTop, displaySize.Y - 16f - _inset.W) - 8f;
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
                var shown = Fit(text, room);
                if (isAmber)
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
        }

        ImGui.End();
    }

    // Item 7: "AGENT IS WAITING" in amber in the top bar, right of the title, or under it when the command line leaves no room.
    private void AgentWaiting(Vector2 displaySize)
    {
        var size = ImGui.CalcTextSize(AgentWaitingText);
        var title = _items.TryGetValue(TitleItem, out var bar) ? bar : new Vector4(16f + _inset.X, 16f + _inset.Y, 16f + _inset.X, 16f + _inset.Y + TopBarHeight);
        var right = displaySize.X - 16f - _inset.Z - (displaySize.X >= displaySize.Y ? CommandWidth + 12f : 0f);
        var at = new Vector2(title.Z + 16f, title.Y);
        if (at.X + size.X > right)
            at = new Vector2(title.X, title.W + 4f);
        ImGui.GetBackgroundDrawList().AddText(at, DimensionColour, AgentWaitingText);
        Shown(ProposalWaitingItem, AgentWaitingText, new Vector4(at.X, at.Y, at.X + size.X, at.Y + size.Y));
    }

    // Item 6: the outcome of the last accept or reject, at the lower right above the line of the status, in its mono font.
    private void ProposalOutcomeLine(Vector2 displaySize)
    {
        if (_proposalOutcome is not { } outcome)
            return;
        var right = displaySize.X - 16f - _inset.Z;
        ImGui.PushFont(_monoFont);
        var text = Fit(outcome, displaySize.X - 32f - _inset.X - _inset.Z);
        var size = ImGui.CalcTextSize(text);
        ImGui.PopFont();
        var statusTop = _items.TryGetValue(StatusLabel, out var status) ? status.Y : displaySize.Y - 16f - _inset.W - AnchorSize;
        ImGui.SetNextWindowPos(new Vector2(right - size.X, statusTop - 6f), ImGuiCond.Always, new Vector2(0f, 1f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if (ImGui.Begin("##ProposalOutcome", FixedFlags))
        {
            ImGui.PushFont(_monoFont);
            ImGui.TextUnformatted(text);
            ImGui.PopFont();
            Item(ProposalOutcomeItem);
            _proposalTexts[ProposalOutcomeItem] = text;
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
    private static string FitAt(string text, float width)
    {
        var font = ImGui.GetFont();
        float Width(string shown) => font.CalcTextSizeA(ProposalTextSize, float.MaxValue, 0f, shown).X;
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
internal sealed record ProposalCard(Guid Id, string Summary, IReadOnlyList<string> Commands, bool Stale = false, bool Applying = false)
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
}

// ADR-0092 item 6: Reject and Accept of the waiting proposal. The host applies them; no route of the surface does.
internal sealed record AcceptProposalAction(Guid Proposal) : PanelAction;

internal sealed record RejectProposalAction(Guid Proposal) : PanelAction;
