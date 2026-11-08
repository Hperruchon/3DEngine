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
        // place and the size took the first frame only, and a panel over the anchors took each tap on them. The person can
        // still move the panel, which the focus click of ADR-0027 needs (FocusClickTests).
        // Finding V118 (TASK-0252): the panel starts inside the safe area of the display.
        // ADR-0093 item 2 (the redesign, slice j, TASK-0304): the panel has no title bar, no ground and no border, and it stays
        // under the top bar; the person does not move it or fold it. Until TASK-0304 it was a window with a title bar.
        ImGui.SetNextWindowPos(new Vector2(16f + _inset.X, PanelTop), ImGuiCond.Always);
        // Finding V142 of the codebase review of 2026-11-08 (b) (TASK-0302): the panel is at most as wide as the display less its
        // margins and the safe area, as the window of a sketch (T176). Until TASK-0302 it was 460 wide on a display of 390.
        var width = Math.Min(PanelWidth, Math.Max(displaySize.X - 32f - _inset.X - _inset.Z, 160f));
        // ADR-0080 (TASK-0248): the panel ends above the round anchors of Undo and Redo at the bottom left.
        var room = Math.Max(displaySize.Y - PanelTop - 16f - AnchorRow - _inset.W, 0f);
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
        if (open)
        {
            if (document is not null)
                File(document);

            // Phase K5b (ADR-0076 item 7, TASK-0244): the list "New" takes the place of the button "New box" of ADR-0032
            // item 4, because the line has no room for a second button and the panel has no room for a new line (finding
            // V50). Each entry opens its pad.
            ImGui.SetNextItemWidth(NewListWidth);
            if (ImGui.BeginCombo(NewLabel, string.Empty))
            {
                if (ImGui.Selectable($"Box##{NewBoxLabel}", false, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                    OpenPad(new NumericPad(NewBoxLabel, _unit, new("x", "10"), new("y", "10"), new("z", "10")), values => new CreateBoxAction(values[0], values[1], values[2]));
                Item(NewBoxLabel);
                if (ImGui.Selectable($"Cylinder##{NewCylinderLabel}", false, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                    OpenPad(new NumericPad(NewCylinderLabel, _unit, new("radius", "5"), new("height", "10")), values => new CreateCylinderAction(values[0], values[1]));
                Item(NewCylinderLabel);

                // Phase K5e (ADR-0089 item 5, TASK-0298): the entries of the cone, the sphere and the torus, each with its pad.
                // The defaults of the torus keep the tube inside the ring.
                if (ImGui.Selectable($"Cone##{NewConeLabel}", false, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                    OpenPad(new NumericPad(NewConeLabel, _unit, new("radius", "5"), new("height", "10")), values => new CreateConeAction(values[0], values[1]));
                Item(NewConeLabel);
                if (ImGui.Selectable($"Sphere##{NewSphereLabel}", false, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                    OpenPad(new NumericPad(NewSphereLabel, _unit, new PadField("radius", "5")), values => new CreateSphereAction(values[0]));
                Item(NewSphereLabel);
                if (ImGui.Selectable($"Torus##{NewTorusLabel}", false, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                    OpenPad(new NumericPad(NewTorusLabel, _unit, new("radius", "10"), new("tube radius", "3")), values => new CreateTorusAction(values[0], values[1]));
                Item(NewTorusLabel);

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
                ImGui.SameLine();
                if (ImGui.Button($"{label[^2..]}##{label}"))
                    _actions.Add(new NewSketchAction(datum));
                Item(label);
            }

            ImGui.SameLine();
            if (ImGui.Button($"Face##{SketchOnFaceLabel}"))
                _actions.Add(new SketchOnFaceAction());
            Item(SketchOnFaceLabel);

            // ADR-0056 item 2: the toggles of the dimension overlay, on a line of their own before the list of the bodies,
            // so that a long list does not push them out of the panel.
            ImGui.Checkbox(DimensionsLabel, ref _dimensions);
            Item(DimensionsLabel);
            ImGui.SameLine();
            ImGui.BeginDisabled(!_dimensions);
            ImGui.Checkbox(HiddenTooLabel, ref _hiddenToo);
            ImGui.EndDisabled();
            Item(HiddenTooLabel);

            ImGui.TextUnformatted(bodies.Count == 1 ? "1 live body" : $"{bodies.Count} live bodies");

            ImGui.SetNextItemWidth(220f);
            TextField(FilterLabel, ref _filter, 64);
            Item(FilterLabel);
            ImGui.SameLine();
            if (ImGui.Button(ClearLabel))
                _filter = string.Empty;
            Item(ClearLabel);

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

    // ADR-0093 item 2: the top of the panel, under the top bar.
    private float PanelTop => 16f + _inset.Y + TopBarHeight + 8f;

    // The width of the panel on a display that is wide enough (V142, TASK-0302).
    private const float PanelWidth = 460f;

    // The width of a button with this label.
    private static float ButtonWidth(string label)
        => ImGui.CalcTextSize(label, true).X + 2f * ImGui.GetStyle().FramePadding.X;

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
                ImGui.SameLine();
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

        ImGui.SetNextItemWidth(220f);
        TextField(NameLabel, ref _name, 128);
        Item(NameLabel);
        ImGui.SameLine();
        if (ImGui.Button(RenameLabel))
            _actions.Add(new RenameBodyAction(body.Id, _name));
        Item(RenameLabel);

        var visibility = body.Visible ? HideLabel : ShowLabel;
        if (ImGui.Button(visibility))
            _actions.Add(new SetVisibleAction(body.Id, !body.Visible));
        Item(visibility);
        ImGui.SameLine();

        // ADR-0032 item 4: a move of the selected body by the offsets of the pad.
        if (ImGui.Button(MoveLabel))
            OpenPad(new NumericPad(MoveLabel, _unit, new("dx", "0"), new("dy", "0"), new("dz", "0")), values => new MoveBodyAction(body.Id, values[0], values[1], values[2]), scope: PadScope.Body);
        Item(MoveLabel);
        ImGui.SameLine();

        // ADR-0049 item 8: the host reads the distance in effect and opens the pad with it.
        if (body.Kind == "Extrude")
        {
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

    // ADR-0030 item 3: the name of the file, a name field, Save as and Open.
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

        ImGui.SetNextItemWidth(200f);
        TextField(FileNameLabel, ref _fileName, 128);
        Item(FileNameLabel);
        // The buttons have their own line, so that they stay inside the panel.
        if (ImGui.Button(SaveAsLabel))
            _actions.Add(new SaveDocumentAction(_fileName));
        Item(SaveAsLabel);
        ImGui.SameLine();
        if (ImGui.Button(OpenLabel))
            _actions.Add(new OpenDocumentAction(_fileName));
        Item(OpenLabel);
        ImGui.SameLine();
        if (ImGui.Button(ExportStlLabel))
            _actions.Add(new ExportStlAction(_fileName));
        Item(ExportStlLabel);
        if (document.Message is { } message)
        {
            ImGui.TextUnformatted(message);
            Item(DocumentMessageLabel);
        }

        ImGui.Separator();
    }
}
