using System.Numerics;
using System.Globalization;
using ImGuiNET;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Numeric;
using ThreeDEngine.Core.Sketching;

namespace ThreeDEngine.Desktop.Ui;

// Phase R15 (TASK-0234): the numeric pad (ADR-0032), a part of DesktopUi. Until TASK-0234 it was in DesktopUi.cs; no
// line changed in the split.
internal sealed unsafe partial class DesktopUi
{
    // The open pad, or null.
    public NumericPad? Pad => _pad;

    // The key of the rectangle of a control of the pad, for ItemRectangle.
    public static string PadItem(string label) => $"{PadName} {label}";

    // The host gives the result of the command of an apply: null closes the pad, and a
    // rejection stays in the pad (ADR-0032 item 8).
    public void EndPad(string? rejection)
    {
        _padApplied = false;
        if (rejection is null)
            ClosePad();
        else
            _pad?.Fail(rejection);
    }

    // ADR-0034 item 7: the pad of the next point of a chain, an offset from the last point while a
    // chain is open, else a position. The host opens it again after each apply, so a person types
    // each point of a chain in one pad.
    public void OpenNextPointPad(bool chainOpen)
    {
        var (a, b) = chainOpen ? ("dx", "dy") : ("x", "y");
        // The key comes from the state that names the fields, not from the last frame: the host
        // opens the pad again in the frame of an apply, before the panel sees the new chain.
        OpenPad(new NumericPad(NextPointLabel, _unit, new(a, "0"), new(b, "0")), values => new NextPointAction(values[0], values[1]), bySketch: true,
            scope: PadScope.Chain, key: ChainKey(_activeKey, SketchToolKind.Line, chainOpen));
    }

    // Finding V119 of the codebase review of 2026-11-07 (TASK-0264): a new pad ends the pending state of a chip; the pad of
    // a chip and the pad of the distance set it again after the open (ADR-0082 items 1 and 4).
    private void OpenPad(NumericPad pad, Func<double[], PanelAction> apply, bool bySketch = false, PadScope scope = PadScope.None, string? key = null)
    {
        (_pad, _padApply, _padBySketch, _padApplied) = (pad, apply, bySketch, false);
        (_padScope, _padKey) = (scope, key ?? Key(scope));
        _pendingValue = null;
    }

    private void ClosePad() => (_pad, _padApply, _padScope, _padKey, _padApplied) = (null, null, PadScope.None, null, false);

    // ADR-0049 item 8: the pad of the distance of the selected extrude, which starts with the
    // distance in effect. A change of the selection closes it, as for Move.
    // ADR-0082 item 4 (TASK-0254): the chip of the distance is pending while this pad is open, from the chip, the ring or
    // the panel, because each of them changes the same value.
    public void OpenDistancePad(Guid body, double distance) => OpenDistancePad(body, SketchText.Number(distance));

    // ADR-0128 item 2 (phase R18h, TASK-0357): the same pad with a start text, which a value typed in the command line gives.
    private void OpenDistancePad(Guid body, string start)
    {
        OpenPad(new NumericPad(SetDistanceLabel, _unit, new PadField("distance", start)), values => new SetDistanceAction(body, values[0]), scope: PadScope.Body);
        _pendingValue = EdgeValues.DistanceKey(body);
    }

    // ADR-0032 item 5: the window of the pad. A row selects its field; the line under the fields
    // gives the value of the active field, or its error, and the message of the last apply.
    private void PadWindow(NumericPad pad, Vector2 displaySize)
    {
        // A pad of the sketch opens to the left of the window of the sketch, so that End and Close
        // stay visible (ADR-0034 item 8). The place and the size come from the display, as for the
        // panel, so the pad stays inside a tablet display. Until TASK-0099 Apply was outside the
        // display at 768 x 1024 and at 1024 x 768 (finding V35 of the second codebase review of
        // 2026-10-28).
        // ADR-0097 item 3 (the redesign, slice n, TASK-0308): the rows of the sketch are at the left in the place of the panel,
        // so a pad of the sketch opens to their right. Until TASK-0308 the window was at the right and the pad at its left.
        var width = Math.Min(PadWidth, displaySize.X - 32f);
        var preferred = _padBySketch ? 16f + _inset.X + SketchWidth + 16f : 492f;
        var x = Math.Clamp(preferred, 16f, Math.Max(16f, displaySize.X - width - 16f));

        // Finding V42 of the codebase review of 2026-10-29: on a display with no room beside the
        // window of the sketch, as 768 x 1024, the pad covered End and Close. It now opens
        // below the row of Next point, End and Close.
        var y = 16f;
        if (_padBySketch && preferred + width > displaySize.X - 16f && ItemRectangle(SketchItem(NextPointLabel)) is { } nextPoint)
            y = nextPoint.W + 8f;
        ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new Vector2(width, Math.Clamp(displaySize.Y - y - 16f, 200f, PadHeight)), ImGuiCond.Appearing);
        if (ImGui.Begin(PadName))
        {
            ImGui.TextUnformatted(pad.Title);
            var fields = pad.Fields;
            for (var index = 0; index < fields.Count; index++)
            {
                if (ImGui.Selectable($"{fields[index].Name} = {fields[index].Text}  {pad.Unit}##field{index}", pad.Active == index, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                    pad.Select(index);
                Item(PadItem(fields[index].Name));
            }

            var preview = pad.Preview;
            ImGui.TextUnformatted(preview.IsValue ? $"= {preview.Value.ToString("G15", CultureInfo.InvariantCulture)} {pad.Unit}" : preview.Error!);
            if (pad.Message is { } message)
                ImGui.TextUnformatted(message);
            ImGui.Separator();

            foreach (var row in PadRows)
            {
                for (var column = 0; column < row.Length; column++)
                {
                    if (column > 0)
                        ImGui.SameLine();
                    if (ImGui.Button(row[column], new Vector2(108f, 0f)))
                        Press(pad, row[column]);
                    Item(PadItem(row[column]));
                }
            }
        }

        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        ImGui.End();
    }

    // A button of the pad: an edit, an apply, a cancel, or the text of its label.
    private void Press(NumericPad pad, string label)
    {
        switch (label)
        {
            case "Delete": pad.Delete(); break;
            case "Clear": pad.Clear(); break;
            case "Next": pad.Next(); break;
            case "Apply": ApplyPad(pad); break;
            case "Cancel": ClosePad(); break;
            default: pad.Append(label); break;
        }
    }

    // ADR-0032 item 6: a key or a text of the frame while the pad is open.
    private void PadKey(NumericPad pad, InputSample sample)
    {
        switch (sample)
        {
            case TextSample text: pad.Append(text.Text); break;
            case KeySample { Down: true, Key: InputKey.Backspace }: Press(pad, "Delete"); break;
            case KeySample { Down: true, Key: InputKey.Tab }: Press(pad, "Next"); break;
            case KeySample { Down: true, Key: InputKey.Enter }: Press(pad, "Apply"); break;
            case KeySample { Down: true, Key: InputKey.Escape }: Press(pad, "Cancel"); break;
        }
    }

    // The numbers of an apply go to the host, and the pad stays open until EndPad. A second
    // apply before EndPad does nothing (V34): two presses of Enter in one frame gave two boxes.
    private void ApplyPad(NumericPad pad)
    {
        if (_padApplied)
            return;
        if (pad.Apply() is { } values && _padApply is { } apply)
        {
            _padAction = apply(values);
            _actions.Add(_padAction);
            _padApplied = true;
        }
    }
}
