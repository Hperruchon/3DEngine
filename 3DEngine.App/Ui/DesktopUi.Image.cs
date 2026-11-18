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
    // ADR-0046 items 3 and 4: the controls of the images.
    // ADR-0172 item 4 (TASK-0418): the images are a fixed part of the layout. Folded, they are the button "Images..." in the
    // file group of the panel and the command "Images". Open, they take the place of the panel, or of the rows of a selected
    // body, under the top bar, with "Close"; the person does not move them. On a landscape display they end 12 pixels left of
    // the command line, and on a portrait display 8 pixels above the ring. They do not show while a sketch is active or a pad is open, and they
    // come back after. Until TASK-0418 they were a window that the person moved, which started folded at x 492: on a narrow
    // display its bar covered the buttons of the panel, and open it covered the ring.
    private void ImageWindow(ImagePanel panel, Vector2 displaySize)
    {
        _imagesBox = null;
        if (!_imagesShown)
            return;

        var left = 16f + _inset.X;
        var top = PanelTop;
        var width = Math.Min(PanelWidth, Math.Max(displaySize.X - 32f - _inset.X - _inset.Z, 160f));
        if (!_portrait)
            width = Math.Min(width, Math.Max(CommandLeft(displaySize) - 12f - 16f - _inset.X, 160f));
        var bottom = displaySize.Y - 16f - AnchorRow - _inset.W;
        if (_portrait && RingTop() is { } ringTop && ringTop - 8f - top >= PathHeight)
            bottom = Math.Min(bottom, ringTop - 8f);
        ImGui.SetNextWindowPos(new Vector2(left, top), ImGuiCond.Always);
        ImGui.SetNextWindowSizeConstraints(Vector2.Zero, new Vector2(width, Math.Max(bottom - top, MinimumControlHeight)));
        ImGui.SetNextWindowSize(new Vector2(width, Math.Min(_imagesHeight ?? (bottom - top), Math.Max(bottom - top, MinimumControlHeight))), ImGuiCond.Always);
        if (ImGui.Begin($"###{ImagesName}", PadFlags))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(ImagesName);
            Item(ImagesName);
            ImGui.SameLine();
            if (ImGui.GetContentRegionAvail().X - ButtonWidth(CloseImagesLabel) is var room and > 0f)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + room);
            if (ImGui.Button(CloseImagesLabel))
                _imagesOpen = false;
            Item(CloseImagesLabel);
            ImGui.TextUnformatted(ImageFileLabel);
            ImGui.SetNextItemWidth(-1f);
            TextField($"##{ImageFileLabel}", ref _imagePath, 260);
            Item(ImageFileLabel);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(PlaceLineLabel);
            foreach (var (label, datum) in PlaceImageButtons)
            {
                SameLineIfFits(ButtonWidth(label[^2..]));
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
                SameLineIfFits(ButtonWidth(ProposeLabel));
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
                SameLineIfFits(ButtonWidth(RejectLabel));
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
                SameLineIfFits(ButtonWidth(ApplyCalibrationLabel));
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
            _imagesHeight = ImGui.GetCursorPosY() + ImGui.GetStyle().WindowPadding.Y;
        }

        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        _imagesBox = _panels[^1];
        ImGui.End();
    }

    // ADR-0172 item 4 (TASK-0418): the label of the button that opens the images and of the button that closes them.
    public const string OpenImagesLabel = "Images...";
    public const string CloseImagesLabel = "Close##Images";

    // ADR-0172 item 4: true while the person keeps the images open; true while the host has images (an image panel).
    private bool _imagesOpen;
    private bool _imagesAvailable;

    // ADR-0172 item 4: true when the images show in this frame: open, with no active sketch and no open pad.
    private bool _imagesShown;

    // ADR-0172 item 4: the height of the content of the images in the last frame, so the view is as high as its content.
    private float? _imagesHeight;

    // ADR-0172 item 4: open the images, as the button "Images..." and the command "Images" do; for a test too.
    internal void OpenImages() => _imagesOpen = true;

    // ADR-0172 item 4: the view of the images in this frame, as left, top, right and bottom; null when it does not show.
    private Vector4? _imagesBox;

    public Vector4? ImagesRectangle => _imagesBox;

    // The short name of the asset of an image, its size in pixels, its datum and its scale.
    public static string ImageTitle(ImageRecord image)
        => $"{image.Asset[..8]} {image.Width}x{image.Height} on {Datums.Find(image.DatumId)?.Name ?? "?"}, "
            + $"{image.Scale.ToString("G4", CultureInfo.InvariantCulture)} mm/px";
}
