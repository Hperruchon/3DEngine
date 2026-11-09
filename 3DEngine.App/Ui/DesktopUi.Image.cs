using System.Numerics;
using System.Globalization;
using Engine.Contracts.Images;
using Engine.Contracts.Sketch;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// Phase R15 (TASK-0234): the window of the images, a part of DesktopUi. Until TASK-0234 it was in DesktopUi.cs; no
// line changed in the split.
internal sealed unsafe partial class DesktopUi
{
    // ADR-0046 items 3 and 4: the window of the images, to the right of the panel and closed until
    // the person opens it. Under the panel it hid Show all in a window 720 pixels high, and at 280
    // pixels wide it stays left of the window of the sketch in a window 1280 pixels wide.
    private void ImageWindow(ImagePanel panel, Vector2 displaySize)
    {
        // Finding V154 of the codebase review of 2026-11-09 (TASK-0324): the window starts under the top bar, at the top of the
        // panel, so the folder of the file in the top bar has its room. Until TASK-0324 it started at the top of the display,
        // and the folder ran under its folded title bar.
        ImGui.SetNextWindowPos(new Vector2(492f, PanelTop), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(ImagesWidth, Math.Clamp(displaySize.Y - 32f, 120f, ImagesHeight)), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowCollapsed(true, ImGuiCond.FirstUseEver);
        if (ImGui.Begin(ImagesName))
        {
            ImGui.TextUnformatted(ImageFileLabel);
            ImGui.SetNextItemWidth(ImagesWidth - 24f);
            TextField($"##{ImageFileLabel}", ref _imagePath, 260);
            Item(ImageFileLabel);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(PlaceLineLabel);
            foreach (var (label, datum) in PlaceImageButtons)
            {
                ImGui.SameLine();
                if (ImGui.Button($"{label[^2..]}##{label}"))
                    _actions.Add(new PlaceImageAction(_imagePath, datum));
                Item(label);
            }

            ImGui.Separator();
            foreach (var image in panel.Live)
            {
                var label = $"{CalibrateLabel} {ImageTitle(image)}";
                ImGui.TextWrapped(ImageTitle(image));
                if (ImGui.Button($"{CalibrateLabel}##{image.Id}"))
                    _actions.Add(new StartCalibrationAction(image.Id));
                Item(label);
                ImGui.SameLine();
                if (ImGui.Button($"{ProposeLabel}##{image.Id}"))
                    _actions.Add(new ProposeContourAction(image.Id));
                Item($"{ProposeLabel} {ImageTitle(image)}");
            }

            // ADR-0047 item 3: the proposal shows on the scene until the person accepts or rejects it.
            if (panel.ProposalPoints > 0)
            {
                ImGui.TextUnformatted($"Contour of {panel.ProposalPoints} points");
                if (ImGui.Button(AcceptLabel))
                    _actions.Add(new AcceptContourAction());
                Item(AcceptLabel);
                ImGui.SameLine();
                if (ImGui.Button(RejectLabel))
                    _actions.Add(new RejectContourAction());
                Item(RejectLabel);
            }

            if (panel.Calibrating is not null)
            {
                ImGui.TextUnformatted($"Tap two points of the image: {panel.Points} of 2");
                ImGui.SetNextItemWidth(120f);
                TextField(DistanceLabel, ref _distance, 32);
                Item(DistanceLabel);
                ImGui.SameLine();
                ImGui.TextUnformatted(_unit);
                ImGui.SameLine();
                // A text that is not a number gives NaN, and the engine refuses it with its reason.
                if (ImGui.Button(ApplyCalibrationLabel))
                    _actions.Add(new CalibrateImageAction(double.TryParse(_distance, NumberStyles.Float, CultureInfo.InvariantCulture, out var distance) ? distance : double.NaN));
                Item(ApplyCalibrationLabel);
                if (ImGui.Button(CancelCalibrationLabel))
                    _actions.Add(new CancelCalibrationAction());
                Item(CancelCalibrationLabel);
            }

            if (panel.Message is { } message)
                ImGui.TextWrapped(message);
        }

        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        ImGui.End();
    }

    // The short name of the asset of an image, its size in pixels, its datum and its scale.
    public static string ImageTitle(ImageRecord image)
        => $"{image.Asset[..8]} {image.Width}x{image.Height} on {Datums.Find(image.DatumId)?.Name ?? "?"}, "
            + $"{image.Scale.ToString("G4", CultureInfo.InvariantCulture)} mm/px";
}
