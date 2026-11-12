using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0080 (the redesign, slice b1, TASK-0248): the fixed controls of the "mix" direction of the designer brief. Undo
// and Redo are two round anchors at the bottom left of the display. Each window is a rectangle of the panels, so a tap
// on it does not reach the scene.
// ADR-0093 (the redesign, slice j, TASK-0304): the title of the document is a top bar at the upper left with no ground,
// and the status line is at the lower right, in the mono font, as in the render of the brief.
internal sealed unsafe partial class DesktopUi
{
    // size-anchor of the design system: the round fixed controls, 48 pixels, at least the 44 of ADR-0027 item 6.
    public const float AnchorSize = 48f;

    // The space that the anchors take under the panel: their size and the margin above them.
    internal const float AnchorRow = AnchorSize + 16f;

    private const ImGuiWindowFlags FixedFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground
        | ImGuiWindowFlags.NoFocusOnAppearing;

    public const string StatusLabel = "Status";

    // The top of the row of the anchors in this frame, which the portrait ring stays above (V136). ADR-0093 (TASK-0304): the
    // top bar is a fixed window too, so the ring reads this field and not the lowest top of the fixed windows.
    private float _anchorsTop = float.MaxValue;

    // ADR-0093 item 1: the item of the top bar, and the height that it takes above the panel.
    public const string TitleItem = "Title";
    internal const float TopBarHeight = 32f;

    // ADR-0102 item 7 (TASK-0316): the item of the folder of the file in the top bar.
    public const string TitleFolderItem = "Title:folder";

    // ADR-0093 item 1: the top bar: the program, then the file or "unsaved", then the unit, with no ground. A long name is
    // cut to the width of the rows, so the unit stays.
    // Finding V149 of the codebase review of 2026-11-08 (c) (TASK-0311): and to the room left of the rows of a selected body,
    // 12 pixels from them, so on a narrow window the top bar does not cover their title; at least 48 pixels.
    // ADR-0099 item 5 (the redesign, slice p, TASK-0313): the rows are at the left now, so the name ends 12 pixels left of the
    // command line at the upper right.
    // ADR-0102 item 7 (phase P9g, TASK-0316): the folder of the file follows its name in the quiet colour, cut at its start so
    // that the last folders stay, where the room left of the command line holds at least 48 pixels of it.
    // ADR-0100 item 1 (phase R17a, TASK-0314): on a landscape display the file of the top bar is the first segment of the path
    // line: the file, the selected body, the selected face and the selected edge. Each segment is 44 pixels high (ADR-0027
    // item 6), so the window of the top bar starts above its text by half the difference, and the text stays in its place. A
    // portrait display gives the program and the unit here, and the path line under them (item 5).
    private void TopBar(Vector2 displaySize, DocumentLine? document, LiveBody? body)
    {
        var portrait = displaySize.X < displaySize.Y;
        var (left, top) = (16f + _inset.X, 16f + _inset.Y);
        var pad = PathPad;
        ImGui.SetNextWindowPos(new Vector2(left, portrait ? top : top - pad), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        if (ImGui.Begin("##TopBar", FixedFlags))
        {
            const string program = "3DEngine";
            if (portrait)
            {
                ImGui.TextUnformatted(program);
                ImGui.SameLine(0f, 12f);
                ImGui.TextDisabled(UnitText);
            }
            else
            {
                // Finding V156 of the codebase review of 2026-11-09 (TASK-0324): the command line shows on a landscape display
                // only, so only there the top bar leaves its width.
                var commandLeft = CommandLeft(displaySize);
                var room = commandLeft - 12f - left - ImGui.CalcTextSize(program).X - ImGui.CalcTextSize(UnitText).X - 24f;
                Label(program, quiet: false, pad);
                ImGui.SameLine(0f, 12f);
                PathRow(document, body, room, pad);
                ImGui.SameLine(0f, 12f);
                Label(UnitText, quiet: true, pad);
            }

            AddWindowRectangle();
            var window = _fixed[^1];
            _items[TitleItem] = portrait ? window : new Vector4(window.X, top, window.Z, top + ImGui.GetTextLineHeight());
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
        if (portrait)
            PathLine(displaySize, document, body);
    }

    // ADR-0100 item 1: the item of the path line, and the item of each of its segments.
    public const string PathItem = "Path";

    public static string PathSegmentItem(PathLevel level) => $"Path:{level}";

    // ADR-0101 item 4 (phase R17b, TASK-0315): the word "Focus" before the name of the body, and the close mark at the end of
    // the path line, while a focus lasts.
    public const string FocusMarkItem = "Path:Focus";
    public const string FocusCloseItem = "Path:Close";

    // ADR-0101 item 4: the room of the word "Focus" and of its space, and the room of the close mark and of its gap.
    private float FocusRoom(LiveBody? body)
        => Focus is not null && body is not null ? ImGui.CalcTextSize(FocusLabel).X + 8f + MinimumControlHeight + 8f : 0f;

    // ADR-0100 item 1: the height of a segment, the width of the place of a separator between two segments, and the space
    // above the text of a segment.
    internal const float PathHeight = MinimumControlHeight;
    internal const float PathSeparator = 24f;

    private static float PathPad => Math.Max((PathHeight - ImGui.GetTextLineHeight()) / 2f, 0f);

    // ADR-0100 item 1: the segments of the path line, each with its level. The document is the file, "unsaved", or "Document"
    // for a host with no files; then the body, by its name or its kind; then the face and the edge, by their short names.
    internal IReadOnlyList<(string Text, PathLevel Level)> PathSegments(DocumentLine? document, LiveBody? body)
    {
        var segments = new List<(string Text, PathLevel Level)>
        {
            (document is { Files: true } files ? files.FileName ?? "unsaved" : "Document", PathLevel.Document),
        };
        if (body is null)
            return segments;
        segments.Add((body.Name ?? body.Kind, PathLevel.Body));
        if (_face?.Name is { } face)
        {
            segments.Add((face, PathLevel.Face));
            if (_face.EdgeName is { } edge)
                segments.Add((edge, PathLevel.Edge));
        }

        return segments;
    }

    // ADR-0100 item 1: the texts of the segments in the room. The file name is at most as wide as the rows (V115). A path that
    // is too wide gives "..." for each middle segment first, from the second one; then it cuts the file name, then the last
    // segment, each to at least 48 pixels.
    internal static IReadOnlyList<string> PathFit(IReadOnlyList<string> texts, float room)
    {
        var fitted = texts.ToArray();
        fitted[0] = Fit(fitted[0], RowsWidth);
        float Width() => fitted.Sum(text => ImGui.CalcTextSize(text).X) + (PathSeparator * (fitted.Length - 1));
        for (var i = 1; i < fitted.Length - 1 && Width() > room; i++)
            fitted[i] = "...";
        foreach (var i in new[] { 0, fitted.Length - 1 }.Distinct())
        {
            if (Width() > room)
                fitted[i] = Fit(fitted[i], Math.Max(48f, room - (Width() - ImGui.CalcTextSize(fitted[i]).X)));
        }

        return fitted;
    }

    // ADR-0100 items 1 and 2: the segments in the current window, with a separator between two of them. A press on a segment
    // that is not the last one selects its level; the last one is the selection, and takes no tap. With nothing selected, the
    // folder of the file follows its name (ADR-0102 item 7).
    private void PathRow(DocumentLine? document, LiveBody? body, float room, float pad)
    {
        var segments = PathSegments(document, body);
        var focus = Focus is not null && body is not null;
        var texts = PathFit([.. segments.Select(segment => segment.Text)], room - FocusRoom(body));
        var colours = ImGui.GetStyle().Colors;
        var list = ImGui.GetWindowDrawList();
        var quiet = ImGui.GetColorU32(colours[(int)ImGuiCol.TextDisabled]);
        for (var i = 0; i < segments.Count; i++)
        {
            if (i > 0)
            {
                ImGui.SameLine(0f, 0f);
                ImGui.Dummy(new Vector2(PathSeparator, PathHeight));
                var middle = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) / 2f;
                list.AddLine(middle + new Vector2(-3f, -6f), middle + new Vector2(3f, 0f), quiet, 2f);
                list.AddLine(middle + new Vector2(3f, 0f), middle + new Vector2(-3f, 6f), quiet, 2f);
                ImGui.SameLine(0f, 0f);
            }

            var size = new Vector2(ImGui.CalcTextSize(texts[i]).X, PathHeight);
            var level = segments[i].Level;
            // ADR-0101 item 4: "Focus" in the quiet colour before the name of the body; it takes no tap.
            if (focus && level == PathLevel.Body)
            {
                Label(FocusLabel, quiet: true, pad);
                _items[FocusMarkItem] = new Vector4(ImGui.GetItemRectMin(), ImGui.GetItemRectMax().X, ImGui.GetItemRectMax().Y);
                ImGui.SameLine(0f, 8f);
            }

            if (i < segments.Count - 1)
            {
                if (ImGui.InvisibleButton($"##path{level}", size))
                    _actions.Add(new PathAction(level));
                if (ImGui.IsItemHovered())
                    list.AddRectFilled(ImGui.GetItemRectMin() - new Vector2(4f, 0f), ImGui.GetItemRectMax() + new Vector2(4f, 0f), ImGui.GetColorU32(colours[(int)ImGuiCol.ButtonHovered]), 6f);
            }
            else
            {
                ImGui.Dummy(size);
            }

            Item(PathSegmentItem(level));
            list.AddText(ImGui.GetItemRectMin() + new Vector2(0f, pad), ImGui.GetColorU32(colours[(int)ImGuiCol.Text]), texts[i]);
        }

        // ADR-0101 item 4: the close mark at the end of the line, 44 by 44 pixels, so it does not move when the person selects a
        // face. A tap on it ends the focus.
        if (focus)
        {
            ImGui.SameLine(0f, 8f);
            if (ImGui.InvisibleButton("##pathClose", new Vector2(MinimumControlHeight, MinimumControlHeight)))
                _actions.Add(new EndFocusAction());
            Item(FocusCloseItem);
            var (min, max) = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
            if (ImGui.IsItemHovered())
                list.AddRectFilled(min, max, ImGui.GetColorU32(colours[(int)ImGuiCol.ButtonHovered]), 6f);
            var middle = (min + max) / 2f;
            var ink = ImGui.GetColorU32(colours[(int)ImGuiCol.Text]);
            list.AddLine(middle + new Vector2(-7f, -7f), middle + new Vector2(7f, 7f), ink, 2f);
            list.AddLine(middle + new Vector2(7f, -7f), middle + new Vector2(-7f, 7f), ink, 2f);
        }

        var folderRoom = room - ImGui.CalcTextSize(texts[0]).X - 12f;
        if (segments.Count == 1 && document?.Folder is { } folder && folderRoom >= 48f)
        {
            ImGui.SameLine(0f, 12f);
            Label(FitStart(folder, folderRoom), quiet: true, pad);
            _items[TitleFolderItem] = new Vector4(ImGui.GetItemRectMin().X, ImGui.GetItemRectMin().Y + pad, ImGui.GetItemRectMax().X, ImGui.GetItemRectMax().Y - pad);
        }
    }

    // A text with no tap, in an item as high as a segment of the path.
    private static void Label(string text, bool quiet, float pad)
    {
        ImGui.Dummy(new Vector2(ImGui.CalcTextSize(text).X, (2f * pad) + ImGui.GetTextLineHeight()));
        var colours = ImGui.GetStyle().Colors;
        ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + new Vector2(0f, pad), ImGui.GetColorU32(colours[(int)(quiet ? ImGuiCol.TextDisabled : ImGuiCol.Text)]), text);
    }

    // ADR-0100 item 5: on a portrait display the path line is the first line under the top bar, and the panel is under it.
    private void PathLine(Vector2 displaySize, DocumentLine? document, LiveBody? body)
    {
        var left = 16f + _inset.X;
        ImGui.SetNextWindowPos(new Vector2(left, 16f + _inset.Y + TopBarHeight), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        if (ImGui.Begin("##Path", FixedFlags))
        {
            PathRow(document, body, displaySize.X - 16f - _inset.Z - 12f - left, PathPad);
            AddWindowRectangle();
            _items[PathItem] = _fixed[^1];
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    // ADR-0093 item 3: the text of the status line: the state of the live bodies and the address of the surface. ADR-0127
    // items 1 and 3 (phase R18g, TASK-0354): the count of the entries of the log first, in place of the count of the bodies,
    // which the rows show, and "agent connected" while a client listens to the event stream of the surface.
    internal string StatusText(IReadOnlyList<LiveBody> bodies, bool address = true, bool agent = true)
    {
        var failed = bodies.Count(body => body.Fault is not null);
        var parts = new List<string>(4);
        if (_status.Log is { } log)
            parts.Add($"log {log}");
        parts.Add(failed == 0 ? "all correct" : $"{failed} failed");
        if (agent && _status.Agent)
            parts.Add(AgentText);
        if (address && _address.Length > 0)
            parts.Add(_address);
        return string.Join(StatusSeparator, parts);
    }

    // ADR-0127 item 3: the part of the status line while a client listens.
    public const string AgentText = "agent connected";

    // ADR-0127 items 1 to 3: the state of the session that the host gives for each frame.
    private SessionStatus _status = SessionStatus.None;

    public void ShowStatus(SessionStatus status) => _status = status ?? SessionStatus.None;

    // ADR-0127 item 5: the unit of the top bar, with "saved" when the document has its own file and "not saved" while it is
    // in the folder of the unsaved documents; the unit alone for a host with no folder of documents.
    internal string UnitText => _status.Saved switch
    {
        true => $"{_unit} · saved",
        false => $"{_unit} · not saved",
        null => _unit,
    };

    // ADR-0124 item 2 (phase R20b2, TASK-0351): the part of the hidden bodies at the end of the status line, or null when no
    // live body is hidden.
    internal static string? HiddenText(IReadOnlyList<LiveBody> bodies)
    {
        var hidden = bodies.Count(body => !body.Visible);
        return hidden == 0 ? null : $"{hidden} hidden · Show";
    }

    // Finding V146 of the codebase review of 2026-11-08 (c) (TASK-0310): the text of the status line that fits in this room
    // in the mono font: the whole line, else the line with no address, else none. Until TASK-0310 the line grew to the
    // left over Frame and Home on a phone, and took each tap on them.
    // ADR-0124 item 2: with the part of the hidden bodies after each, and then the part alone, so a phone keeps the way back.
    internal (string? Status, string? Hidden) StatusFit(IReadOnlyList<LiveBody> bodies, float room)
    {
        var hidden = HiddenText(bodies);
        ImGui.PushFont(_monoFont);
        try
        {
            var tail = hidden is null ? 0f : ImGui.CalcTextSize(StatusSeparator + hidden).X;
            // ADR-0127 item 3: then the line with no address and no agent.
            foreach (var text in new[] { StatusText(bodies), StatusText(bodies, address: false), StatusText(bodies, address: false, agent: false) })
            {
                if (ImGui.CalcTextSize(text).X + tail <= room)
                    return (text, hidden);
            }

            return hidden is not null && ImGui.CalcTextSize(hidden).X <= room ? (null, hidden) : (null, null);
        }
        finally
        {
            ImGui.PopFont();
        }
    }

    private const string StatusSeparator = " · ";

    private void FixedControls(Vector2 displaySize, IReadOnlyList<LiveBody> bodies, DocumentLine? document, LiveBody? body)
    {
        TopBar(displaySize, document, body);

        // Undo and Redo at the bottom left (ADR-0031 item 11, amended by ADR-0080). Finding V118 of the codebase review of
        // 2026-11-06 (e) (TASK-0252): inside the safe area of the display, so that the navigation bar of the phone does not
        // cover them.
        ImGui.SetNextWindowPos(new Vector2(16f + _inset.X, displaySize.Y - 16f - _inset.W - AnchorSize), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        var anchorsRight = 16f + _inset.X;
        if (ImGui.Begin("##Anchors", FixedFlags))
        {
            if (Anchor(UndoLabel, back: true))
                _actions.Add(new UndoAction());
            ImGui.SameLine();
            if (Anchor(RedoLabel, back: false))
                _actions.Add(new RedoAction());
            // ADR-0083 (TASK-0259): "Frame" and "Home" of the demonstration, on the row of the anchors. ADR-0127 item 4
            // (phase R18g, TASK-0354): on a portrait display only; a landscape display has them in the command line, and F
            // frames, so the row holds Undo, Redo and the step of Undo, as the render.
            // ADR-0108 item 2 (phase R18a, TASK-0332): a button as high as this window has its outline on the edge of the
            // window, where the clip of the window cuts it; so the outline is drawn 1 pixel inside the button.
            if (displaySize.X < displaySize.Y)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);
                ViewButtons();
                ImGui.PopStyleVar();
                foreach (var label in new[] { FrameLabel, HomeLabel })
                {
                    if (_items.TryGetValue(label, out var button))
                        ImGui.GetWindowDrawList().AddRect(new Vector2(button.X + 1f, button.Y + 1f), new Vector2(button.Z - 1f, button.W - 1f), ImGui.GetColorU32(ImGuiCol.Border), ImGui.GetStyle().FrameRounding, ImDrawFlags.None, Theme.OutlineWidth);
                }
            }
            else if (_status.Undo is { } step)
            {
                UndoStep(step, displaySize);
            }

            AddWindowRectangle();
            _anchorsTop = _fixed[^1].Y;
            anchorsRight = _fixed[^1].Z;
        }

        ImGui.End();

        // ADR-0093 item 3: the status line at the lower right, on the row of the anchors, in the mono font. Until TASK-0304 it
        // held the address only, right of Redo. V146 (TASK-0310): it starts at least 12 pixels right of the anchors, with the
        // text that fits there, or it does not show. Its left edge comes from the width of that text, so the first frame of a
        // new text is in its place too; a pivot at the right edge used the width of the last frame.
        var right = displaySize.X - 16f - _inset.Z;
        var (status, hidden) = StatusFit(bodies, right - (anchorsRight + 12f));
        StatusShown = status;
        _statusRight = right;
        if (status is not null || hidden is not null)
        {
            // ADR-0108 item 5: the width of the parts, each in its colour, as the window draws them.
            var parts = status is null ? [] : StatusParts(status);
            var separator = status is not null && hidden is not null ? StatusSeparator : string.Empty;
            ImGui.PushFont(_monoFont);
            var width = parts.Sum(part => ImGui.CalcTextSize(part.Text).X) + ImGui.CalcTextSize(separator).X + (hidden is null ? 0f : ImGui.CalcTextSize(hidden).X);
            ImGui.PopFont();
            ImGui.SetNextWindowPos(new Vector2(right - width, displaySize.Y - 16f - _inset.W - (AnchorSize / 2f)), ImGuiCond.Always, new Vector2(0f, 0.5f));
            if (ImGui.Begin("##Status", FixedFlags))
            {
                // The text colour of the panels: the dim colour gives 2.9:1 on the clear, below the 3:1 of large text.
                // ADR-0108 item 5 (phase R18a, TASK-0332): the state in its colour, "all correct" in green and "failed" in red.
                ImGui.PushFont(_monoFont);
                // ADR-0124 item 2: the part of the hidden bodies is as high as the anchors, so the text is in the middle of it.
                var top = hidden is null ? 0f : (AnchorSize - ImGui.GetTextLineHeight()) / 2f;
                if (parts.Count > 0)
                {
                    ImGui.SetCursorPosY(top);
                    ImGui.BeginGroup();
                    for (var i = 0; i < parts.Count; i++)
                    {
                        if (i > 0)
                            ImGui.SameLine(0f, 0f);
                        if (parts[i].Colour is { } ink)
                            ImGui.PushStyleColor(ImGuiCol.Text, ink);
                        ImGui.TextUnformatted(parts[i].Text);
                        if (parts[i].Colour is not null)
                            ImGui.PopStyleColor();
                    }

                    ImGui.EndGroup();
                    Item(StatusLabel);
                }

                if (hidden is not null)
                {
                    if (separator.Length > 0)
                    {
                        ImGui.SameLine(0f, 0f);
                        ImGui.SetCursorPosY(top);
                        ImGui.TextUnformatted(separator);
                        ImGui.SameLine(0f, 0f);
                    }

                    ImGui.SetCursorPosY(0f);
                    var at = ImGui.GetCursorScreenPos();
                    if (ImGui.InvisibleButton(HiddenItem, new Vector2(ImGui.CalcTextSize(hidden).X, AnchorSize)))
                        _hiddenOpen = !_hiddenOpen;
                    Item(HiddenItem);
                    ImGui.GetWindowDrawList().AddText(at + new Vector2(0f, top), Theme.Accent, hidden);
                }

                ImGui.PopFont();
                AddWindowRectangle();
            }

            ImGui.End();
        }

        // ADR-0124 item 3: the list closes when no body is hidden, or when the part does not show.
        if (hidden is null)
            _hiddenOpen = false;
        ImGui.PopStyleVar();
    }

    // ADR-0127 item 2 (phase R18g, TASK-0354): the item of the text of the step that Undo reverses, its word, and its largest
    // width as a part of the width of the display.
    public const string UndoStepItem = "Undo:step";
    public const string UndoStepWord = "Undo:";
    internal const float UndoStepShare = 0.25f;

    // ADR-0127 item 2: "Undo: C3 = 75" right of Redo, in the middle of the height of the anchors. A long text keeps its start.
    private void UndoStep(string step, Vector2 displaySize)
    {
        ImGui.SameLine(0f, 16f);
        ImGui.SetCursorPosY((AnchorSize - ImGui.GetTextLineHeight()) / 2f);
        var text = Fit($"{UndoStepWord} {step}", displaySize.X * UndoStepShare);
        ImGui.TextUnformatted(text);
        Item(UndoStepItem);
    }

    // ADR-0124 items 2 and 3 (phase R20b2, TASK-0351): the part of the hidden bodies in the status line, and the list that it
    // opens, with a row for each hidden body and "Show all".
    public const string HiddenItem = "Status:hidden";
    public const string HiddenListItem = "Status:hidden list";
    public const string HiddenShowAllItem = "Status:show all";

    internal static string HiddenRowItem(Guid body) => $"Status:hidden {body}";

    private bool _hiddenOpen;
    private float _statusRight;

    // ADR-0127 item 3: the text of the status line in the last frame, or null when it did not show, for a test.
    internal string? StatusShown { get; private set; }

    internal bool HiddenListOpen => _hiddenOpen;

    // ADR-0124 item 3: the list above the status line, with its right edge on the right edge of the status line. A tap on a
    // row shows that body; a release outside the list and the part closes it. The list is a rectangle of the panels.
    private void HiddenList(Vector2 displaySize, IReadOnlyList<LiveBody> bodies)
    {
        if (!_hiddenOpen)
            return;
        var hidden = bodies.Where(body => !body.Visible).ToList();
        var labels = hidden.Select(body => body.Name ?? $"{body.Kind}  {body.Id.ToString()[..8]}").ToList();
        if (hidden.Count > 1)
            labels.Add(ShowAllLabel);
        var padding = ImGui.GetStyle().FramePadding.X * 2f;
        var width = Math.Min(labels.Max(label => ImGui.CalcTextSize(label).X) + padding, displaySize.X - 32f - _inset.X - _inset.Z);
        var bottom = _anchorsTop - 8f;
        var top = 16f + _inset.Y + TopBarHeight;
        ImGui.SetNextWindowPos(new Vector2(_statusRight, bottom), ImGuiCond.Always, new Vector2(1f, 1f));
        ImGui.SetNextWindowSizeConstraints(Vector2.Zero, new Vector2(displaySize.X, Math.Max(bottom - top, MinimumControlHeight)));
        if (ImGui.Begin("##HiddenList", FixedFlags & ~ImGuiWindowFlags.NoBackground))
        {
            for (var i = 0; i < hidden.Count; i++)
            {
                if (ImGui.Button($"{labels[i]}##{hidden[i].Id}", new Vector2(width, MinimumControlHeight)))
                    _actions.Add(new SetVisibleAction(hidden[i].Id, true));
                Item(HiddenRowItem(hidden[i].Id));
            }

            if (hidden.Count > 1)
            {
                if (ImGui.Button($"{ShowAllLabel}##hidden", new Vector2(width, MinimumControlHeight)))
                    _actions.Add(new ShowAllAction());
                Item(HiddenShowAllItem);
            }

            AddWindowRectangle();
            _items[HiddenListItem] = _fixed[^1];
        }

        ImGui.End();
    }

    // ADR-0124 item 3: true when a point of the window is on the list of the hidden bodies or on the part of the status line,
    // in the last frame. Route closes the list on a release elsewhere.
    private bool OverHidden(double x, double y)
        => new[] { HiddenListItem, HiddenItem }.Any(item => _items.TryGetValue(item, out var r) && x >= r.X && x < r.Z && y >= r.Y && y < r.W);

    // ADR-0108 item 5: the parts of the status line, and the colour of the part of the state; the other parts keep the text
    // colour of the panels.
    internal static IReadOnlyList<(string Text, uint? Colour)> StatusParts(string status)
    {
        foreach (var (state, colour) in new[] { ("all correct", Theme.Correct), (" failed", FailedColour) })
        {
            var at = status.IndexOf(state, StringComparison.Ordinal);
            if (at < 0)
                continue;
            // "2 failed": the count goes with the word, so the red part is "2 failed".
            var start = state[0] == ' ' ? status.LastIndexOf(' ', Math.Max(at - 1, 0)) + 1 : at;
            var end = at + state.Length;
            return [(status[..start], null), (status[start..end], colour), (status[end..], null)];
        }

        return [(status, null)];
    }

    private void AddWindowRectangle()
    {
        var (position, size) = (ImGui.GetWindowPos(), ImGui.GetWindowSize());
        _fixed.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
    }

    // A round button with a bent arrow: to the left for Undo, to the right for Redo. The bitmap font has no arrow, so the
    // draw list draws it.
    private bool Anchor(string label, bool back)
    {
        var pressed = ImGui.InvisibleButton(label, new Vector2(AnchorSize));
        Item(label);
        var (min, max) = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
        var centre = (min + max) / 2f;
        var colours = ImGui.GetStyle().Colors;
        var ground = ImGui.IsItemActive() ? colours[(int)ImGuiCol.ButtonActive] : ImGui.IsItemHovered() ? colours[(int)ImGuiCol.ButtonHovered] : colours[(int)ImGuiCol.Button];
        var list = ImGui.GetWindowDrawList();
        list.AddCircleFilled(centre, AnchorSize / 2f, ImGui.GetColorU32(ground));
        list.AddCircle(centre, AnchorSize / 2f, ImGui.GetColorU32(colours[(int)ImGuiCol.Border]), 0, 1.5f);

        // The arrow: an arc over the top, and a head at its lower end. ADR-0108 item 2: in the accent, as the render.
        var ink = Theme.Accent;
        var side = back ? -1f : 1f;
        const float radius = 10f;
        list.PathArcTo(centre + new Vector2(0f, 3f), radius, MathF.PI * 1.1f, MathF.PI * 1.9f);
        list.PathStroke(ink, ImDrawFlags.None, 2.5f);
        var tip = centre + new Vector2(side * radius, 3f + 1f);
        list.AddTriangleFilled(tip + new Vector2(0f, 6f), tip + new Vector2(-side * 6f, -2f), tip + new Vector2(side * 6f, -2f), ink);
        return pressed;
    }
}

// ADR-0127 items 1 to 3 and 5 (phase R18g, TASK-0354): the state of the session for the fixed controls: the count of the
// entries of the log (null before the first read), true while a client listens to the event stream of the surface, the
// text of the step that Undo reverses (null when no step is left), and true when the document has its own file, false
// while it is in the folder of the unsaved documents, null for a host with no folder of documents.
internal sealed record SessionStatus(int? Log, bool Agent, string? Undo, bool? Saved)
{
    public static SessionStatus None { get; } = new(null, false, null, null);
}
