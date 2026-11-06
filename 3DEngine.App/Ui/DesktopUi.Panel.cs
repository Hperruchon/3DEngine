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
        ImGui.SetNextWindowPos(new Vector2(16f, 16f), ImGuiCond.FirstUseEver);
        // The first size fits inside a small display too. Since the line of Undo and Redo (TASK-0093)
        // and the line of a new sketch (TASK-0101) the panel is 700 pixels high at most, so that two
        // bodies and Show all fit with no scroll bar.
        ImGui.SetNextWindowSize(new Vector2(460f, Math.Clamp(displaySize.Y - 32f, 200f, 700f)), ImGuiCond.FirstUseEver);
        if (ImGui.Begin(PanelName))
        {
            ImGui.TextUnformatted(_address);
            if (document is not null)
                File(document);

            // ADR-0031 item 11: the history of the document. A rejection shows in the line of the file.
            if (ImGui.Button(UndoLabel))
                _actions.Add(new UndoAction());
            Item(UndoLabel);
            ImGui.SameLine();
            if (ImGui.Button(RedoLabel))
                _actions.Add(new RedoAction());
            Item(RedoLabel);
            ImGui.SameLine();
            if (ImGui.Button(NewBoxLabel))
                OpenPad(new NumericPad(NewBoxLabel, _unit, new("x", "10"), new("y", "10"), new("z", "10")), values => new CreateBoxAction(values[0], values[1], values[2]));
            Item(NewBoxLabel);

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

        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        ImGui.End();

        // The review of Codex of v0.212, finding 1 (TASK-0232): the open list of a cut is a window of its own, and a tap on
        // it must not reach the scene. It follows the panel, so that the panel stays the first rectangle.
        if (_cutList is { } list)
            _panels.Add(list);
    }

    // The rectangle of the open list of a cut in this frame, or null.
    private Vector4? _cutList;

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

        ImGui.TextUnformatted(document.FileName is { } name ? $"File: {name}" : "File: unsaved");
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
