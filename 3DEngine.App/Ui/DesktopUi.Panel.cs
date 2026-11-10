using System.Numerics;
using ImGuiNET;
using ThreeDEngine.Core.Numeric;

namespace ThreeDEngine.Desktop.Ui;

// Phase R15 (TASK-0234): the panel of the bodies, of the selected body and of the document file, a part of
// DesktopUi. Until TASK-0234 it was in DesktopUi.cs; no line changed in the split.
internal sealed unsafe partial class DesktopUi
{
    private void Panel(IReadOnlyList<LiveBody> bodies, Guid? selection, Vector2 displaySize, DocumentLine? document)
    {
        // Finding V114 of the codebase review of 2026-11-06 (e) (TASK-0252): a constraint ends the height of the panel above
        // the anchors, and each frame keeps its place between the top of the safe area and the row of the anchors (see
        // KeepAboveAnchors), so a drag of the panel or a smaller display does not cover Undo and Redo. Until TASK-0252 the
        // place and the size took the first frame only, and a panel over the anchors took each tap on them. Finding V150 of
        // the codebase review of 2026-11-08 (c) (TASK-0311): since TASK-0304 the person does not move the panel (NoMove, and
        // the place of each frame); FocusClickTests holds that a drag at its top moves nothing.
        // Finding V118 (TASK-0252): the panel starts inside the safe area of the display.
        // ADR-0093 item 2 (the redesign, slice j, TASK-0304): the panel has no title bar, no ground and no border, and it stays
        // under the top bar; the person does not move it or fold it. Until TASK-0304 it was a window with a title bar.
        ImGui.SetNextWindowPos(new Vector2(16f + _inset.X, PanelTop), ImGuiCond.Always);
        // Finding V142 of the codebase review of 2026-11-08 (b) (TASK-0302): the panel is at most as wide as the display less its
        // margins and the safe area, as the window of a sketch (T176). Until TASK-0302 it was 460 wide on a display of 390.
        var width = Math.Min(PanelWidth, Math.Max(displaySize.X - 32f - _inset.X - _inset.Z, 160f));
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
        // ADR-0093 item 2: the panel is as high as its content in the last frame, so the room under it, with no ground, stays
        // the scene. A window that sizes itself hides its items in its first frame, which a tap in that frame misses.
        ImGui.SetNextWindowSize(new Vector2(width, Math.Min(_panelHeight ?? room, room)), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        var open = ImGui.Begin($"###{PanelName}", PanelFlags);
        ImGui.PopStyleVar();
        // Finding V123 of the codebase review of 2026-11-07 (TASK-0264): Begin gives false for a collapsed panel, so the
        // clamp ran only for an open panel, and a collapsed panel covered Undo. The clamp runs for each state now.
        KeepAboveAnchors(displaySize);
        // ADR-0100 item 4 (phase R17a, TASK-0314): on a landscape display with no selected body, the rows of the document are
        // first, in the place of the rows of a body, and the list of the bodies is in them.
        var documentRows = !_portrait && bodies.All(body => body.Id != selection);
        if (open)
        {
            if (documentRows)
            {
                DocumentRows(bodies, document);
                ImGui.Separator();
            }

            if (document is not null)
                File(document);

            // Phase K5b (ADR-0076 item 7, TASK-0244): the list "New" takes the place of the button "New box" of ADR-0032
            // item 4, because the line has no room for a second button and the panel has no room for a new line (finding
            // V50). Each entry opens its pad.
            ImGui.SetNextItemWidth(NewListWidth);
            if (ImGui.BeginCombo(NewLabel, string.Empty))
            {
                foreach (var (text, label) in new[] { ("Box", NewBoxLabel), ("Cylinder", NewCylinderLabel), ("Cone", NewConeLabel), ("Sphere", NewSphereLabel), ("Torus", NewTorusLabel) })
                {
                    if (ImGui.Selectable($"{text}##{label}", false, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                        OpenNew(label);
                    Item(label);
                }

                var (at, extent) = (ImGui.GetWindowPos(), ImGui.GetWindowSize());
                _newList = new Vector4(at.X, at.Y, at.X + extent.X, at.Y + extent.Y);
                ImGui.EndCombo();
            }

            Item(NewLabel);

            // ADR-0034 item 6: a new sketch on each origin plane.
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(SketchLineLabel);
            foreach (var (label, datum) in NewSketchButtons)
            {
                SameLineIfFits(ButtonWidth(label[^2..]));
                if (ImGui.Button($"{label[^2..]}##{label}"))
                    _actions.Add(new NewSketchAction(datum));
                Item(label);
            }

            SameLineIfFits(ButtonWidth("Face"));
            if (ImGui.Button($"Face##{SketchOnFaceLabel}"))
                _actions.Add(new SketchOnFaceAction());
            Item(SketchOnFaceLabel);

            // ADR-0056 item 2: the toggles of the dimension overlay, on a line of their own before the list of the bodies,
            // so that a long list does not push them out of the panel.
            ImGui.Checkbox(DimensionsLabel, ref _dimensions);
            Item(DimensionsLabel);
            SameLineIfFits(CheckboxWidth(HiddenTooLabel));
            ImGui.BeginDisabled(!_dimensions);
            ImGui.Checkbox(HiddenTooLabel, ref _hiddenToo);
            ImGui.EndDisabled();
            Item(HiddenTooLabel);

            if (!documentRows)
            {
                ImGui.TextUnformatted(bodies.Count == 1 ? "1 live body" : $"{bodies.Count} live bodies");
                FilterLine();
                ImGui.Separator();
                foreach (var body in bodies)
                {
                    var row = RowText(body);
                    if (_filter.Length > 0 && !row.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // The identifier after ## keeps two rows with one text apart.
                    if (ImGui.Selectable($"{row}##{body.Id}", selection == body.Id, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                        _actions.Add(new SelectBodyAction(body.Id));
                    Item(row);
                    _shown.Add(row);
                }

                ImGui.Separator();
            }
            if (bodies.FirstOrDefault(body => body.Id == selection) is { } selected)
            {
                // Selected ends on its last line. Show all goes on that line when it fits there, so that the panel keeps
                // its height (finding V50); else it starts a new line (finding V98, TASK-0232).
                Selected(selected, bodies);
                ImGui.SameLine();
                if (ImGui.GetContentRegionAvail().X < ButtonWidth(ShowAllLabel))
                    ImGui.NewLine();
            }

            if (ImGui.Button(ShowAllLabel))
                _actions.Add(new ShowAllAction());
            Item(ShowAllLabel);
        }

        _panelHeight = ImGui.GetCursorPosY() + ImGui.GetStyle().WindowPadding.Y;
        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        ImGui.End();

        // The review of Codex of v0.212, finding 1 (TASK-0232): the open list of a cut is a window of its own, and a tap on
        // it must not reach the scene. It follows the panel, so that the panel stays the first rectangle.
        if (_cutList is { } list)
            _panels.Add(list);
        if (_newList is { } created)
            _panels.Add(created);
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

    // ADR-0100 item 4 (phase R17a, TASK-0314): the rows of the document, in the style of the rows of a selected body: the
    // first row gives the kind on its first line, and the unit and the counts on its second line; then the filter; then one
    // row for each body that the filter keeps, with its name at the left and its state at the right: "correct", or "failed"
    // and the code of its fault in red, and "hidden". A tap on a body row selects the body. The key of a body row is its row
    // text, as in the list of the panel, so a test finds it by the same name.
    private void DocumentRows(IReadOnlyList<LiveBody> bodies, DocumentLine? document)
    {
        var colours = ImGui.GetStyle().Colors;
        var text = ImGui.GetColorU32(colours[(int)ImGuiCol.Text]);
        var quiet = ImGui.GetColorU32(colours[(int)ImGuiCol.TextDisabled]);
        var line = ImGui.GetColorU32(colours[(int)ImGuiCol.Separator]);
        var list = ImGui.GetWindowDrawList();
        var lineHeight = ImGui.GetTextLineHeight();
        var width = ImGui.GetContentRegionAvail().X;

        ImGui.Dummy(new Vector2(width, RowHeight));
        Item(DocumentRowItem);
        var (min, max) = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
        var first = min.Y + ((RowHeight - (2f * lineHeight)) / 2f);
        list.AddText(new Vector2(min.X, first), text, DocumentKind(document));
        var x = min.X;
        foreach (var (part, red) in DocumentState(bodies))
        {
            list.AddText(new Vector2(x, first + lineHeight), red ? FailedColour : quiet, part);
            x += ImGui.CalcTextSize(part).X;
        }

        list.AddLine(new Vector2(min.X, max.Y), max, line, 1f);

        FilterLine();
        var middle = (RowHeight - lineHeight) / 2f;
        foreach (var body in bodies)
        {
            var row = RowText(body);
            if (_filter.Length > 0 && !row.Contains(_filter, StringComparison.OrdinalIgnoreCase))
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
    }

    // The rectangle of the open list of a cut in this frame, or null.
    private Vector4? _cutList;

    // The rectangle of the open list "New" in this frame, or null (TASK-0244).
    private Vector4? _newList;

    // The width of the box of the list "New", as the list of a cut.
    private const float NewListWidth = 80f;

    // ADR-0093 item 2: the panel takes the height of its content, up to the room above the anchors, and scrolls past it.
    // It has no title bar, no ground and no border, and the person does not move, fold or size it.
    private const ImGuiWindowFlags PanelFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings;

    // ADR-0093 item 2: the height of the content of the panel in the last frame, or null before its first frame.
    private float? _panelHeight;

    // The pad of an entry of the list "New", for the list and for the command line (ADR-0098 item 3, TASK-0312). Phase K5e
    // (ADR-0089 item 5, TASK-0298): the cone, the sphere and the torus each have a pad; the defaults of the torus keep the
    // tube inside the ring.
    private void OpenNew(string label)
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

    // The width of the panel on a display that is wide enough (V142, TASK-0302).
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

    // The controls of the selected body. The name field starts with the name of the body each
    // time another body is selected.
    private void Selected(LiveBody body, IReadOnlyList<LiveBody> bodies)
    {
        if (_nameFor != body.Id)
            (_name, _nameFor) = (body.Name ?? string.Empty, body.Id);

        // ADR-0037 item 6 (Q2 A): the body keeps its last solid, and the panel gives the reason.
        // Finding V50 of the codebase review of 2026-10-30 (TASK-0118): the full reason with the
        // identifiers took 208 pixels and pushed Show all out of the panel. One line gives the code,
        // and a hover or a press on it gives the reason.
        if (body.Fault is { } fault)
        {
            ImGui.TextUnformatted($"{FaultLabel}: {fault.Code}");
            Item(FaultLabel);
            if (ImGui.IsItemHovered() || ImGui.IsItemActive())
                ImGui.SetTooltip(fault.Message);
        }

        // ADR-0056 item 6: the size and the thickness of the selected body while the dimensions show.
        if (_dimensions && _size is { } size)
        {
            ImGui.TextUnformatted(size);
            Item(SizeLabel);
        }

        // ADR-0075 item 3 (phase R12a, TASK-0236): the selected face and a sketch on it, on one line, so that the panel
        // grows by one line only while a face is selected.
        if (_face is { } face)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(face.Text);
            Item(FaceLabel);
            if (face.CanSketch)
            {
                SameLineIfFits(ButtonWidth(SketchOnFaceButtonLabel));
                if (ImGui.Button(SketchOnFaceButtonLabel))
                    _actions.Add(new SketchOnSelectedFaceAction());
                Item(SketchOnFaceButtonLabel);
            }

            // ADR-0077 item 4 (phase R12b, TASK-0245): the selected edge, on its own line.
            if (face.EdgeText is { } edgeText)
            {
                ImGui.TextUnformatted(edgeText);
                Item(EdgeLabel);
            }
        }

        ImGui.SetNextItemWidth(FieldWidth(220f, NameLabel, RenameLabel));
        TextField(NameLabel, ref _name, 128);
        Item(NameLabel);
        SameLineIfFits(ButtonWidth(RenameLabel));
        if (ImGui.Button(RenameLabel))
            _actions.Add(new RenameBodyAction(body.Id, _name));
        Item(RenameLabel);

        var visibility = body.Visible ? HideLabel : ShowLabel;
        if (ImGui.Button(visibility))
            _actions.Add(new SetVisibleAction(body.Id, !body.Visible));
        Item(visibility);
        SameLineIfFits(ButtonWidth(MoveLabel));

        // ADR-0032 item 4: a move of the selected body by the offsets of the pad.
        if (ImGui.Button(MoveLabel))
            OpenPad(new NumericPad(MoveLabel, _unit, new("dx", "0"), new("dy", "0"), new("dz", "0")), values => new MoveBodyAction(body.Id, values[0], values[1], values[2]), scope: PadScope.Body);
        Item(MoveLabel);
        ImGui.SameLine();

        // ADR-0049 item 8: the host reads the distance in effect and opens the pad with it.
        if (body.Kind == "Extrude")
        {
            if (ImGui.GetContentRegionAvail().X < ButtonWidth(SetDistanceLabel))
                ImGui.NewLine();
            if (ImGui.Button(SetDistanceLabel))
                _actions.Add(new EditDistanceAction(body.Id));
            Item(SetDistanceLabel);
            ImGui.SameLine();
        }

        // ADR-0073 item 1 (phase R14e3, TASK-0227): a cut of the selected body with another visible body, the command
        // Subtract, so that a person cuts a body with no agent, as on the phone, which has no surface (ADR-0061 item 4).
        var tools = bodies.Where(other => other.Id != body.Id && other.Visible).ToList();
        if (tools.Count == 0)
            return;

        // The list stays on the line of Move when it fits there, so that Show all stays in the panel (finding V50). For an
        // extrude the line holds Set distance too, and the list starts a new line (finding V98, TASK-0232).
        const float listWidth = 120f;
        if (ImGui.GetContentRegionAvail().X < listWidth + ImGui.GetStyle().ItemInnerSpacing.X + ImGui.CalcTextSize(CutLabel, true).X)
            ImGui.NewLine();
        ImGui.SetNextItemWidth(listWidth);
        if (ImGui.BeginCombo(CutLabel, string.Empty))
        {
            foreach (var tool in tools)
            {
                if (ImGui.Selectable($"{RowText(tool)}##cut{tool.Id}", false, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                    _actions.Add(new CutBodyAction(body.Id, tool.Id));
                Item(CutItem(tool.Id));
            }

            var (at, extent) = (ImGui.GetWindowPos(), ImGui.GetWindowSize());
            _cutList = new Vector4(at.X, at.Y, at.X + extent.X, at.Y + extent.Y);
            ImGui.EndCombo();
        }

        Item(CutLabel);
    }

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

    // ADR-0030 item 3: the name of the file, a name field, Save as and Open. ADR-0102 item 1 (phase P9g, TASK-0316): four
    // buttons that each open a dialog of the system, on two lines: "Open project..." and "Save as...", then "Export STL..."
    // and "Import STL...". Until TASK-0316 a field took a name in the document folder.
    private void File(DocumentLine document)
    {
        // V99 (TASK-0232): a host with no files shows the message only.
        if (!document.Files)
        {
            if (document.Message is { } only)
            {
                ImGui.TextUnformatted(only);
                Item(DocumentMessageLabel);
                ImGui.Separator();
            }

            return;
        }

        foreach (var (label, action, same) in new (string, PanelAction, bool)[]
        {
            (OpenProjectLabel, new OpenProjectAction(), false), (SaveAsLabel, new SaveAsAction(), true),
            (ExportStlLabel, new ExportStlAction(), false), (ImportStlLabel, new ImportStlAction(), true),
        })
        {
            if (same)
                ImGui.SameLine();
            if (ImGui.Button(label))
                _actions.Add(action);
            Item(label);
        }

        if (document.Message is { } message)
        {
            ImGui.TextUnformatted(message);
            Item(DocumentMessageLabel);
        }

        ImGui.Separator();
    }
}
