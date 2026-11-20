using System.Numerics;
using Engine.Contracts.Drawing;
using ImGuiNET;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0167 (phase R30b, TASK-0407): "Add to sheet" and "Standard views" in the net of the views and in the command line, and
// the pad of the paper and the scale of a new sheet, which also changes the scale of each view of a sheet.
internal sealed unsafe partial class DesktopUi
{
    public const string AddToSheetLabel = "Add to sheet";
    public const string StandardViewsLabel = "Standard views";
    public const string AddViewToSheetLabel = "Add view to sheet";
    public const string ScaleSheetLabel = "Scale the sheet views";
    public const string SheetPadName = "Sheet";
    public const string SheetPadAdd = "Add";
    public const string SheetPadApply = "Apply";
    public const string SheetPadCancel = "Cancel";
    public const string SheetPadLandscape = "Landscape";
    public const string SheetPadPortrait = "Portrait";
    public const string SheetPadRatio = "Ratio";

    // The key of the rectangle of a control of the pad of a sheet, for ItemRectangle.
    public static string SheetPadItem(string label) => $"{SheetPadName} {label}";

    // ADR-0167 item 2: the first live sheet, or null; the host gives it before each frame.
    private SheetRecord? _sheet;

    public void ShowSheet(SheetRecord? sheet) => (_sheet, _liveSheets) = (sheet, sheet is null ? [] : [sheet]);

    // ADR-0168 item 1 (TASK-0408): each live sheet, for the choice of a sheet on the sheet page; the first one is the sheet of
    // "Add to sheet".
    private IReadOnlyList<SheetRecord> _liveSheets = [];

    public void ShowSheets(IReadOnlyList<SheetRecord> sheets)
    {
        ArgumentNullException.ThrowIfNull(sheets);
        (_sheet, _liveSheets) = (sheets.FirstOrDefault(), sheets);
        // ADR-0177 item 3 (phase R30f, TASK-0422): the new sheet of the waiting proposal stays the chosen sheet, so after the accept
        // the page stays on it. Until TASK-0422 each sheet that was not live went, and the page went to the first live sheet.
        if (_pageSheetId is { } chosen && sheets.All(sheet => sheet.Id != chosen) && _sheetProposal?.SheetId != chosen)
            _pageSheetId = null;
    }

    // ADR-0167 item 5: the state of the pad of a sheet. Views is the list of the views of a new sheet, or empty for the scale
    // of each view of the sheet. Until a person chooses a scale, the scale follows the paper: the largest scale of ISO 5455 at
    // which the standard views fit (D6 A).
    internal sealed class SheetPad(IReadOnlyList<FlatView> views, SheetPlacement.Box? box, (long Numerator, long Denominator) scale, Guid? sheet = null, Guid? view = null)
    {
        public IReadOnlyList<FlatView> Views { get; } = views;

        // ADR-0168 item 2 (TASK-0408): the sheet and the one view of "Scale" on the sheet page, or null for each view of the first
        // live sheet.
        public Guid? Sheet { get; } = sheet;
        public Guid? View { get; } = view;
        public bool NewSheet => Views.Count > 0;
        public string Paper { get; private set; } = "A3";
        public bool Landscape { get; private set; } = true;
        public (long Numerator, long Denominator) Scale { get; private set; } = box is { } b ? SheetPlacement.FitScale("A3", true, false, b) : scale;
        public bool Chosen { get; private set; }
        public string Ratio = string.Empty;

        public void SetPaper(string paper, bool landscape)
        {
            (Paper, Landscape) = (paper, landscape);
            if (!Chosen && box is { } b)
                Scale = SheetPlacement.FitScale(paper, landscape, false, b);
        }

        public void Choose((long Numerator, long Denominator) chosen) => (Scale, Chosen, Ratio) = (chosen, true, string.Empty);

        // A free ratio that is valid takes effect as it is typed.
        public void TypeRatio(string text)
        {
            Ratio = text;
            if (SheetPlacement.Ratio(text) is { } ratio)
                (Scale, Chosen) = (ratio, true);
        }

        // The scales of the pad, with the scale in effect when it is not one of them.
        public IReadOnlyList<(long Numerator, long Denominator)> Scales
            => SheetPlacement.PadScales.Contains(Scale) || Ratio.Length > 0 ? SheetPlacement.PadScales : [.. SheetPlacement.PadScales, Scale];
    }

    private SheetPad? _sheetPad;

    internal SheetPad? SheetPadShown => _sheetPad;

    // ADR-0167 items 2 and 5: the host opens the pad of a new sheet when a view goes to a document with no sheet.
    public void OpenSheetPad(IReadOnlyList<FlatView> views, SheetPlacement.Box box)
    {
        ClosePad();
        _sheetPad = new SheetPad(views, box, (1, 1));
    }

    // ADR-0167 item 5: the pad of the scale of each view of the sheet, from the command line.
    private void OpenScalePad()
    {
        if (_sheet is not { Views.Count: > 0 } sheet)
            return;
        ClosePad();
        _sheetPad = new SheetPad([], null, (sheet.Views[0].ScaleNumerator, sheet.Views[0].ScaleDenominator));
    }

    public void CloseSheetPad() => _sheetPad = null;

    // ADR-0168 item 2 (TASK-0408): the pad of the scale of one view, from "Scale" on the sheet page.
    private void OpenScalePad(Guid sheet, Guid view, (long Numerator, long Denominator) scale)
    {
        ClosePad();
        _sheetPad = new SheetPad([], null, scale, sheet, view);
    }

    // ADR-0167 item 2: the row under the net: "Add to sheet", which adds the flat view of the camera and is dim in the 3D view,
    // and "Standard views". Each is two cells wide; a tap closes the net, as a tap on a view does.
    private void SheetRow(Vector2 cell)
    {
        var wide = new Vector2((2f * cell.X) + ViewGap, cell.Y);
        foreach (var (column, label) in new[] { (0, AddToSheetLabel), (2, StandardViewsLabel) })
        {
            ImGui.SetCursorPos(new Vector2(column * (cell.X + ViewGap), 3 * (cell.Y + ViewGap)));
            var dim = label == AddToSheetLabel && _view is null;
            if (dim)
                ImGui.BeginDisabled();
            if (ImGui.Button($"{label}##View", wide))
            {
                _actions.Add(new AddToSheetAction(label == AddToSheetLabel && _view is { } flat ? [flat] : SheetPlacement.Standard));
                _viewsOpen = false;
            }

            if (dim)
                ImGui.EndDisabled();
            Item(SheetItem(label));
            if (_items.TryGetValue(SheetItem(label), out var box))
                ImGui.GetWindowDrawList().AddRect(new Vector2(box.X + 1f, box.Y + 1f), new Vector2(box.Z - 1f, box.W - 1f), ImGui.GetColorU32(ImGuiCol.Border), ImGui.GetStyle().FrameRounding, ImDrawFlags.None, Theme.OutlineWidth);
        }
    }

    // ADR-0167 item 5: Add or Apply gives the action of the pad and closes it.
    private void SheetPadGo(SheetPad pad)
    {
        _actions.Add(pad.NewSheet
            ? new CreateSheetAction(pad.Paper, pad.Landscape, pad.Scale.Numerator, pad.Scale.Denominator, pad.Views)
            : new ScaleSheetAction(pad.Scale.Numerator, pad.Scale.Denominator, pad.Sheet, pad.View));
        _sheetPad = null;
    }

    // ADR-0167 item 5: as for the numeric pad (ADR-0032 item 6), Enter gives Add or Apply and Escape gives Cancel, while the
    // field of the ratio does not take the keys.
    private void WatchSheetPadKeys(IReadOnlyList<ThreeDEngine.Core.Input.InputSample> samples)
    {
        // ADR-0168 item 6 (TASK-0408, finding V195 of the codebase review of 2026-11-18): the keys of the pad rest while the home
        // screen shows.
        if (_sheetPad is not { } pad || _textActive || _home is not null)
            return;
        foreach (var sample in samples)
        {
            if (sample is ThreeDEngine.Core.Input.KeySample { Down: true, Key: ThreeDEngine.Core.Input.InputKey.Enter })
            {
                SheetPadGo(pad);
                return;
            }

            if (sample is ThreeDEngine.Core.Input.KeySample { Down: true, Key: ThreeDEngine.Core.Input.InputKey.Escape })
            {
                _sheetPad = null;
                return;
            }
        }
    }

    // The item of a button of the row of the sheet in the net.
    public static string SheetItem(string label) => $"View:{label}";

    // ADR-0167 item 5: the window of the pad of a sheet: the paper and its orientation for a new sheet, the scales of ISO 5455
    // and a free ratio, then Add (or Apply for the scale of each view) and Cancel.
    private void SheetPadWindow(SheetPad pad, Vector2 displaySize)
    {
        var width = Math.Min(PadWidth, displaySize.X - 32f);
        var x = Math.Clamp(492f, 16f, Math.Max(16f, displaySize.X - width - 16f));
        ImGui.SetNextWindowPos(new Vector2(x, 16f + _inset.Y), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new Vector2(width, Math.Clamp(displaySize.Y - 32f - _inset.Y - _inset.W, 200f, PadHeight)), ImGuiCond.Appearing);
        if (ImGui.Begin(SheetPadName))
        {
            ImGui.TextUnformatted(pad.NewSheet
                ? $"A new sheet with the {string.Join(", the ", pad.Views.Select(SheetActions.Name))} view"
                : pad.View is not null ? "The scale of the view" : $"The scale of each view of {_sheet?.Name}");
            var accent = ImGui.ColorConvertU32ToFloat4(Theme.Accent) with { W = 0.3f };
            void Choice(string label, bool chosen, Action press, string? id = null)
            {
                if (chosen)
                    ImGui.PushStyleColor(ImGuiCol.Button, accent);
                if (ImGui.Button($"{label}##{id ?? label}", new Vector2(0f, MinimumControlHeight)))
                    press();
                if (chosen)
                    ImGui.PopStyleColor();
                Item(SheetPadItem(label));
            }

            if (pad.NewSheet)
            {
                ImGui.TextUnformatted("Paper");
                foreach (var paper in SheetRecord.Papers)
                {
                    ImGui.SameLine();
                    Choice(paper, pad.Paper == paper, () => pad.SetPaper(paper, pad.Landscape));
                }

                Choice(SheetPadLandscape, pad.Landscape, () => pad.SetPaper(pad.Paper, true));
                ImGui.SameLine();
                Choice(SheetPadPortrait, !pad.Landscape, () => pad.SetPaper(pad.Paper, false));
            }

            ImGui.TextUnformatted("Scale");
            var first = true;
            foreach (var scale in pad.Scales)
            {
                var label = SheetPlacement.Text(scale);
                if (!first)
                    SameLineIfFits(ButtonWidth(label));
                first = false;
                Choice(label, pad.Scale == scale && pad.Ratio.Length == 0, () => pad.Choose(scale));
            }

            ImGui.SetNextItemWidth(160f);
            var ratio = pad.Ratio;
            if (ImGui.InputTextWithHint($"{SheetPadRatio}##SheetRatio", "n:d", ref ratio, 24))
                pad.TypeRatio(ratio);
            Item(SheetPadItem(SheetPadRatio));
            ImGui.TextUnformatted(pad.Ratio.Length > 0 && SheetPlacement.Ratio(pad.Ratio) is null
                ? "A ratio is two whole numbers greater than zero, as 3:4."
                : $"Scale {SheetPlacement.Text(pad.Scale)}");
            ImGui.Separator();

            var go = pad.NewSheet ? SheetPadAdd : SheetPadApply;
            if (ImGui.Button(go, new Vector2(108f, MinimumControlHeight)))
                SheetPadGo(pad);

            Item(SheetPadItem(go));
            ImGui.SameLine();
            if (ImGui.Button(SheetPadCancel, new Vector2(108f, MinimumControlHeight)))
                _sheetPad = null;
            Item(SheetPadItem(SheetPadCancel));
        }

        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        ImGui.End();
    }
}
