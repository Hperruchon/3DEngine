using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0083 D1, D6 and the demonstration "Open the same STL on desktop and Android" (phase P9c, TASK-0259): the home screen
// with "Open file", "New part" and the recent files, in the theme of ADR-0078; the controls "Home" and "Frame"; and the
// message of a file that does not open, with "Choose another file". Each window is a rectangle of the panels, so a tap on
// it does not reach the scene.
internal sealed unsafe partial class DesktopUi
{
    public const string OpenFileLabel = "Open file";
    public const string NewPartLabel = "New part";
    public const string HomeLabel = "Home";
    public const string FrameLabel = "Frame";
    public const string ChooseAnotherLabel = "Choose another file";
    public const string CloseMessageLabel = "Close";

    public static string RecentItem(string name) => $"Recent:{name}";

    private IReadOnlyList<string>? _home;
    private (string Code, string Text)? _message;

    // True while the home screen shows.
    internal bool HomeShown => _home is not null;

    // Shows the home screen with the names of the recent documents, or hides it with null.
    public void ShowHome(IReadOnlyList<string>? recent) => _home = recent;

    // Shows the message of a file that does not open, or hides it with null.
    public void ShowMessage((string Code, string Text)? message) => _message = message;

    internal (string Code, string Text)? Message => _message;

    private void Home(Vector2 displaySize, IReadOnlyList<string> recent)
    {
        var width = Math.Min(420f, displaySize.X - 32f - _inset.X - _inset.Z);
        ImGui.SetNextWindowPos(new Vector2(displaySize.X / 2f, displaySize.Y / 2f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(width, 0f), ImGuiCond.Always);
        if (ImGui.Begin("3DEngine###Home", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings))
        {
            var full = new Vector2(-1f, MinimumControlHeight);
            if (ImGui.Button(OpenFileLabel, full))
                _actions.Add(new OpenFileAction());
            Item(OpenFileLabel);
            if (ImGui.Button(NewPartLabel, full))
                _actions.Add(new NewPartAction());
            Item(NewPartLabel);

            ImGui.Spacing();
            ImGui.TextDisabled(recent.Count == 0 ? "No recent file" : "Recent files");
            foreach (var name in recent)
            {
                if (ImGui.Selectable($"{name}##{RecentItem(name)}", false, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                    _actions.Add(new OpenDocumentAction(name));
                Item(RecentItem(name));
            }

            AddPanelRectangle();
        }

        ImGui.End();
    }

    private void MessageWindow(Vector2 displaySize, (string Code, string Text) message)
    {
        var width = Math.Min(480f, displaySize.X - 32f - _inset.X - _inset.Z);
        ImGui.SetNextWindowPos(new Vector2(displaySize.X / 2f, displaySize.Y / 2f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(width, 0f), ImGuiCond.Always);
        if (ImGui.Begin("The file did not open###Message", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.PushTextWrapPos(0f);
            ImGui.TextUnformatted(message.Text);
            ImGui.PopTextWrapPos();
            ImGui.TextDisabled(message.Code);
            var full = new Vector2(-1f, MinimumControlHeight);
            if (ImGui.Button(ChooseAnotherLabel, full))
            {
                _message = null;
                _actions.Add(new OpenFileAction());
            }

            Item(ChooseAnotherLabel);
            if (ImGui.Button(CloseMessageLabel, full))
                _message = null;
            Item(CloseMessageLabel);
            AddPanelRectangle();
        }

        ImGui.End();
    }

    // "Frame" and "Home" after Redo, in the window of the anchors (see FixedControls), each as high as an anchor.
    private void ViewButtons()
    {
        var size = new Vector2(0f, AnchorSize);
        ImGui.SameLine(0f, 16f);
        if (ImGui.Button(FrameLabel, size))
            _actions.Add(new FrameAction());
        Item(FrameLabel);
        ImGui.SameLine();
        if (ImGui.Button(HomeLabel, size))
            _actions.Add(new HomeAction());
        Item(HomeLabel);
    }

    private void AddPanelRectangle()
    {
        var (position, size) = (ImGui.GetWindowPos(), ImGui.GetWindowSize());
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
    }
}
