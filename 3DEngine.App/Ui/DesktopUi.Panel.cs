using System.Numerics;
using ImGuiNET;
using ThreeDEngine.Core.Numeric;

namespace ThreeDEngine.Desktop.Ui;

// Phase R15 (TASK-0234): the panel of the bodies, of the selected body and of the document file, a part of
// DesktopUi. Until TASK-0234 it was in DesktopUi.cs; no line changed in the split.
internal sealed unsafe partial class DesktopUi
{
    // ADR-0174 item 3 (phase R31a, TASK-0419): the structure view, in the place of the panel on each display. Until TASK-0419
    // the panel held the file, the list "New", "Sketch on", the toggles of the dimensions, the list of the bodies and "Show
    // all"; the ring of the part, the file menu and the net of the views hold them now.
    private void Panel(IReadOnlyList<LiveBody> bodies, Vector2 displaySize, DocumentLine? document)
    {
        // Finding V114 of the codebase review of 2026-11-06 (e) (TASK-0252): a constraint ends the height of the panel above
        // the anchors, and each frame keeps its place between the top of the safe area and the row of the anchors (see
        // KeepAboveAnchors), so a drag of the panel or a smaller display does not cover Undo and Redo. Finding V150 of the
        // codebase review of 2026-11-08 (c) (TASK-0311): the person does not move the panel (NoMove, and the place of each
        // frame); FocusClickTests holds that a drag at its top moves nothing.
        // Finding V118 (TASK-0252): the panel starts inside the safe area of the display.
        ImGui.SetNextWindowPos(new Vector2(16f + _inset.X, PanelTop), ImGuiCond.Always);
        // ADR-0174 item 3: the structure view is at most as wide as the rows of a body. Finding V142 of the codebase review of
        // 2026-11-08 (b) (TASK-0302): and at most as wide as the display less its margins and the safe area.
        var width = Math.Min(RowsWidth, Math.Max(displaySize.X - 32f - _inset.X - _inset.Z, 160f));
        // ADR-0113 item 4 (phase R19, TASK-0336): on a landscape display the panel ends 12 pixels left of the command line, so
        // that the two do not overlap on a narrow display. Until TASK-0336 they overlapped at 640 by 360.
        if (!_portrait)
            width = Math.Min(width, Math.Max(CommandLeft(displaySize) - 12f - 16f - _inset.X, 160f));
        // ADR-0080 (TASK-0248): the panel ends above the round anchors of Undo and Redo at the bottom left.
        var room = Math.Max(displaySize.Y - PanelTop - 16f - AnchorRow - _inset.W, 0f);
        // ADR-0100 item 5 (phase R17a, TASK-0314): on a portrait display the panel ends 8 pixels above the ring of the last
        // frame, as the sheet of a proposal does, when one row of 44 pixels still fits; so each tool of the ring takes its tap.
        if (_portrait && RingTop() is { } ringTop && ringTop - 8f - PanelTop >= PathHeight)
            room = Math.Min(room, ringTop - 8f - PanelTop);
        ImGui.SetNextWindowSizeConstraints(Vector2.Zero, new Vector2(width, room));
        // ADR-0093 item 2: the panel is as high as its content in the last frame. A window that sizes itself hides its items in
        // its first frame, which a tap in that frame misses.
        ImGui.SetNextWindowSize(new Vector2(width, Math.Min(_panelHeight ?? room, room)), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        var open = ImGui.Begin($"###{PanelName}", PanelFlags);
        ImGui.PopStyleVar();
        // Finding V123 of the codebase review of 2026-11-07 (TASK-0264): the clamp runs for each state of the window.
        KeepAboveAnchors(displaySize);
        if (open)
            StructureView(bodies, document);

        _panelHeight = ImGui.GetCursorPosY() + ImGui.GetStyle().WindowPadding.Y;
        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        ImGui.End();
    }

    // ADR-0174 item 3: true while the structure view is open; it starts folded.
    private bool _structureOpen;

    internal bool StructureOpen => _structureOpen;

    // ADR-0174 item 3: open or fold the structure view, as a tap on its first row does; for a test too.
    internal void OpenStructure(bool open = true) => _structureOpen = open;

    // ADR-0174 item 3: the filter shows from this count of live bodies.
    internal const int FilterFrom = 8;

    // ADR-0174 item 3 (phase R31a, TASK-0419): the rows of the structure view, in the style of the rows of a selected body.
    // The first row gives the kind on its first line, and the unit, the count and the state of the bodies on its
    // second line (ADR-0100 item 4), with a mark at its right; a tap on it opens or folds the view. A message of the document
    // follows it. Open, the view gives the filter from eight bodies, one row for each body that the filter keeps (its name at
    // the left; "correct", or "failed" and the code of its fault in red, and "hidden" at the right), and "Show all" while a
    // body is hidden. A tap on a body row selects the body. The key of a body row is its row text, so a test finds it by the
    // same name as before R31a.
    private void StructureView(IReadOnlyList<LiveBody> bodies, DocumentLine? document)
    {
        var colours = ImGui.GetStyle().Colors;
        var text = ImGui.GetColorU32(colours[(int)ImGuiCol.Text]);
        var quiet = ImGui.GetColorU32(colours[(int)ImGuiCol.TextDisabled]);
        var line = ImGui.GetColorU32(colours[(int)ImGuiCol.Separator]);
        var list = ImGui.GetWindowDrawList();
        var lineHeight = ImGui.GetTextLineHeight();
        var width = ImGui.GetContentRegionAvail().X;

        if (ImGui.InvisibleButton("##structure", new Vector2(width, RowHeight)))
            _structureOpen = !_structureOpen;
        Item(DocumentRowItem);
        var (min, max) = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
        if (ImGui.IsItemHovered())
            list.AddRectFilled(min, max, ImGui.GetColorU32(colours[(int)ImGuiCol.ButtonHovered]), 4f);
        var first = min.Y + ((RowHeight - (2f * lineHeight)) / 2f);
        list.AddText(new Vector2(min.X, first), text, DocumentKind(document));
        var x = min.X;
        foreach (var (part, red) in DocumentState(bodies))
        {
            list.AddText(new Vector2(x, first + lineHeight), red ? FailedColour : quiet, part);
            x += ImGui.CalcTextSize(part).X;
        }

        // The mark: a chevron down while folded, up while open.
        var mark = new Vector2(max.X - 12f, (min.Y + max.Y) / 2f);
        var turn = _structureOpen ? -1f : 1f;
        list.AddLine(mark + new Vector2(-6f, -3f * turn), mark + new Vector2(0f, 3f * turn), text, 2f);
        list.AddLine(mark + new Vector2(0f, 3f * turn), mark + new Vector2(6f, -3f * turn), text, 2f);
        list.AddLine(new Vector2(min.X, max.Y), max, line, 1f);

        // V99 (TASK-0232): a host with no files shows its message too.
        if (document?.Message is { } message)
        {
            // ADR-0167 item 2 (TASK-0407): a message of a sheet is longer than a line, so the message wraps.
            ImGui.PushTextWrapPos(0f);
            ImGui.TextUnformatted(message);
            ImGui.PopTextWrapPos();
            Item(DocumentMessageLabel);
        }

        if (!_structureOpen)
            return;

        if (bodies.Count >= FilterFrom)
            FilterLine();
        var middle = (RowHeight - lineHeight) / 2f;
        foreach (var body in bodies)
        {
            var row = RowText(body);
            if (bodies.Count >= FilterFrom && _filter.Length > 0 && !row.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                continue;

            if (ImGui.Selectable($"##document{body.Id}", false, ImGuiSelectableFlags.None, new Vector2(0f, RowHeight)))
                _actions.Add(new SelectBodyAction(body.Id));
            Item(row);
            _shown.Add(row);
            // A selectable is half the space between two items wider on each side than the column, so the texts start and end
            // in the column, under the first row.
            var inside = ImGui.GetStyle().ItemSpacing.X / 2f;
            (min, max) = (ImGui.GetItemRectMin() + new Vector2(inside, 0f), ImGui.GetItemRectMax() - new Vector2(inside, 0f));
            var state = body.Fault is { } fault ? $"failed {fault.Code}" : "correct";
            var hidden = body.Visible ? string.Empty : " · hidden";
            var stateWidth = ImGui.CalcTextSize(state + hidden).X;
            list.AddText(new Vector2(min.X, min.Y + middle), body.Visible ? text : quiet, Fit(body.Name ?? $"{body.Kind}  {body.Id.ToString()[..8]}", Math.Max(max.X - min.X - stateWidth - 12f, 48f)));
            list.AddText(new Vector2(max.X - stateWidth, min.Y + middle), body.Fault is null ? quiet : FailedColour, state);
            if (hidden.Length > 0)
                list.AddText(new Vector2(max.X - ImGui.CalcTextSize(hidden).X, min.Y + middle), quiet, hidden);
            list.AddLine(new Vector2(min.X, max.Y), max, line, 1f);
        }

        // ADR-0174 item 3: "Show all" only while a body is hidden.
        if (bodies.Any(body => !body.Visible))
        {
            if (ImGui.Button(ShowAllLabel))
                _actions.Add(new ShowAllAction());
            Item(ShowAllLabel);
        }
    }

    // The filter of the list of the bodies and its button Clear.
    private void FilterLine()
    {
        ImGui.SetNextItemWidth(FieldWidth(220f, FilterLabel, ClearLabel));
        TextField(FilterLabel, ref _filter, 64);
        Item(FilterLabel);
        SameLineIfFits(ButtonWidth(ClearLabel));
        if (ImGui.Button(ClearLabel))
            _filter = string.Empty;
        Item(ClearLabel);
    }

    // ADR-0100 item 4: the item of the first row of the document.
    public const string DocumentRowItem = "Rows:document";

    // ADR-0100 item 4: the kind of the document with a capital, "Part" or "Assembly".
    internal static string DocumentKind(DocumentLine? document)
        => (document?.Kind ?? Engine.Contracts.Document.Part) switch
        {
            Engine.Contracts.Document.Assembly => "Assembly",
            _ => "Part",
        };

    // ADR-0100 item 4: the parts of the second line of the first row: the unit and the count of the live bodies, then "all
    // correct", or the count that failed with the word "failed" in red.
    internal IReadOnlyList<(string Text, bool Red)> DocumentState(IReadOnlyList<LiveBody> bodies)
    {
        var failed = bodies.Count(body => body.Fault is not null);
        var count = bodies.Count == 1 ? "1 body" : $"{bodies.Count} bodies";
        return failed == 0 ? [($"{_unit} · {count} · all correct", false)] : [($"{_unit} · {count} · ", false), ($"{failed} failed", true)];
    }

    // The rectangle of the open list of a cut in this frame, or null.
    private Vector4? _cutList;

    // ADR-0093 item 2: the panel takes the height of its content, up to the room above the anchors, and scrolls past it.
    // It has no title bar and no border, and the person does not move or size it. ADR-0174 item 3 (TASK-0419): it has the
    // ground of a window, so its text does not draw over the part; until TASK-0419 it had no ground.
    private const ImGuiWindowFlags PanelFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings;

    // ADR-0093 item 2: the height of the content of the panel in the last frame, or null before its first frame.
    private float? _panelHeight;

    // The pad of an entry of "New", for the ring of the part and for the command line (ADR-0098 item 3, TASK-0312; ADR-0174
    // item 2, TASK-0419). Phase K5e
    // (ADR-0089 item 5, TASK-0298): the cone, the sphere and the torus each have a pad; the defaults of the torus keep the
    // tube inside the ring.
    // ADR-0172 item 2 (TASK-0418): internal, so a test opens a pad of three fields.
    internal void OpenNew(string label)
    {
        switch (label)
        {
            case NewBoxLabel:
                OpenPad(new NumericPad(NewBoxLabel, _unit, new("x", "10"), new("y", "10"), new("z", "10")), values => new CreateBoxAction(values[0], values[1], values[2]));
                break;
            case NewCylinderLabel:
                OpenPad(new NumericPad(NewCylinderLabel, _unit, new("radius", "5"), new("height", "10")), values => new CreateCylinderAction(values[0], values[1]));
                break;
            case NewConeLabel:
                OpenPad(new NumericPad(NewConeLabel, _unit, new("radius", "5"), new("height", "10")), values => new CreateConeAction(values[0], values[1]));
                break;
            case NewSphereLabel:
                OpenPad(new NumericPad(NewSphereLabel, _unit, new PadField("radius", "5")), values => new CreateSphereAction(values[0]));
                break;
            case NewTorusLabel:
                OpenPad(new NumericPad(NewTorusLabel, _unit, new("radius", "10"), new("tube radius", "3")), values => new CreateTorusAction(values[0], values[1]));
                break;
        }
    }

    // ADR-0093 item 2: the top of the panel, under the top bar. ADR-0100 item 5 (phase R17a, TASK-0314): on a portrait
    // display, under the path line.
    // ADR-0113 item 3 (phase R19, TASK-0336): and under "AGENT IS WAITING" when it is a line of its own there.
    private float PanelTop => 16f + _inset.Y + TopBarHeight + 8f + (_portrait ? PathHeight + _waitingLine : 0f);

    // The width of the open images on a display that is wide enough (V142, TASK-0302). Until TASK-0419 the panel had it too.
    private const float PanelWidth = 460f;

    // The width of a button with this label.
    private static float ButtonWidth(string label)
        => ImGui.CalcTextSize(label, true).X + 2f * ImGui.GetStyle().FramePadding.X;

    // The width of a check box with this label: the box, the inner spacing and the text.
    private static float CheckboxWidth(string label)
        => ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X + ImGui.CalcTextSize(label, true).X;

    // ADR-0113 item 2 (phase R19, TASK-0336): the next item stays on the line when it fits, else it starts a new line, so no
    // item of the panel goes past its right edge on a narrow display.
    private static void SameLineIfFits(float width)
    {
        ImGui.SameLine();
        if (ImGui.GetContentRegionAvail().X < width)
            ImGui.NewLine();
    }

    // ADR-0113 item 2: the width of a field that its label and a button follow on its line: the preferred width when the line
    // has room, else the room less the label and the button, and at least the narrowest field; the button then goes on a new
    // line.
    private static float FieldWidth(float preferred, string label, string button)
    {
        var style = ImGui.GetStyle();
        var text = ImGui.CalcTextSize(label, true).X;
        var after = (text > 0f ? style.ItemInnerSpacing.X + text : 0f) + style.ItemSpacing.X + ButtonWidth(button);
        return Math.Clamp(ImGui.GetContentRegionAvail().X - after, NarrowestField, preferred);
    }

    // ADR-0113 item 2: the narrowest field of a line of the panel.
    private const float NarrowestField = 120f;

    // V114 and V118 (TASK-0252): the top of the panel is at least 16 pixels inside the top inset, and its bottom at most 16
    // pixels above the anchors. V123 (TASK-0264): its left side is at least 16 pixels inside the left inset too; until
    // TASK-0264 the code kept it inside the inset only, and the comment said 16 pixels.
    private void KeepAboveAnchors(Vector2 displaySize)
    {
        var (at, extent) = (ImGui.GetWindowPos(), ImGui.GetWindowSize());
        var top = 16f + _inset.Y;
        var bottom = displaySize.Y - 32f - _inset.W - AnchorSize;
        var place = new Vector2(Math.Max(at.X, 16f + _inset.X), Math.Clamp(at.Y, top, Math.Max(top, bottom - extent.Y)));
        if (place != at)
            ImGui.SetWindowPos(place);
    }

    // ADR-0080 item 1 (TASK-0248): the title of the document; a host with no files gives no name. ADR-0093 item 1 (TASK-0304):
    // the parts of the top bar, in the order of the render: the program, the file or "unsaved", then the unit; the top bar
    // cuts a long name, so the unit stays (finding V115). Until TASK-0304 the title was one text in the title bar of the panel.
    internal IReadOnlyList<string> TitleParts(DocumentLine? document)
        => document is { Files: true } files
            ? ["3DEngine", files.FileName ?? "unsaved", _unit]
            : ["3DEngine", _unit];
}
