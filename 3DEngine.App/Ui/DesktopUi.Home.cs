using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0083 D1, D6 and the demonstration "Open the same STL on desktop and Android" (phase P9c, TASK-0259): the home screen
// with "Open file", "New part" and the recent files, in the theme of ADR-0078; the controls "Home" and "Frame"; and the
// message of a file that does not open, with "Choose another file". Each window is a rectangle of the panels, so a tap on
// it does not reach the scene.
internal sealed unsafe partial class DesktopUi
{
    public const string NewPartLabel = "New part";
    public const string HomeLabel = "Home";
    public const string FrameLabel = "Frame";
    public const string ChooseAnotherLabel = "Choose another file";
    public const string CloseMessageLabel = "Close";

    // ADR-0100 item 5 (Q3 A, TASK-0314): the question before a change of project while the document has no file.
    public const string QuestionItem = "Question";
    public const string DiscardLabel = "Discard";
    public const string CancelLabel = "Cancel";

    // ADR-0100 item 6 (TASK-0314): the item of a recent row is its path.
    public static string RecentItem(string path) => $"Recent:{path}";

    private IReadOnlyList<string>? _home;
    private bool _homeProjects = true;
    private (string Code, string Text)? _message;
    private bool _question;

    // True while the home screen shows.
    internal bool HomeShown => _home is not null;

    // Shows the home screen with the paths of the recent projects, or hides it with null. ADR-0100 item 1: a host with no
    // files (the phone) shows no "Open project..." and no recent rows.
    public void ShowHome(IReadOnlyList<string>? recent, bool projects = true) => (_home, _homeProjects) = (recent, projects);

    // ADR-0100 item 5: shows or hides the question before a change of project.
    public void ShowQuestion(bool shown) => _question = shown;

    internal bool QuestionShown => _question;

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
            // ADR-0100 item 1 (TASK-0314): "Open project..." and "Import STL..." are two actions, so a person cannot take an
            // import for the open of a project.
            if (_homeProjects)
            {
                if (ImGui.Button(OpenProjectLabel, full))
                    _actions.Add(new OpenProjectAction());
                Item(OpenProjectLabel);
            }

            if (ImGui.Button(ImportStlLabel, full))
                _actions.Add(new ImportStlAction());
            Item(ImportStlLabel);
            if (ImGui.Button(NewPartLabel, full))
                _actions.Add(new NewPartAction());
            Item(NewPartLabel);

            // ADR-0100 item 6 (Q2 A): the recent projects of this desktop, in each folder, with the name of the file and its
            // folder in the quiet colour.
            if (_homeProjects)
            {
                ImGui.Spacing();
                ImGui.TextDisabled(recent.Count == 0 ? "No recent project" : "Recent projects");
                foreach (var path in recent)
                {
                    var start = ImGui.GetCursorScreenPos();
                    if (ImGui.Selectable($"{Path.GetFileNameWithoutExtension(path)}##{RecentItem(path)}", false, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                        _actions.Add(new OpenRecentAction(path));
                    Item(RecentItem(path));
                    var name = ImGui.CalcTextSize(Path.GetFileNameWithoutExtension(path)).X + 16f;
                    var room = ImGui.GetItemRectMax().X - start.X - name;
                    if (Path.GetDirectoryName(path) is { Length: > 0 } folder && room >= 48f)
                        ImGui.GetWindowDrawList().AddText(new Vector2(start.X + name, start.Y + ((MinimumControlHeight - ImGui.GetTextLineHeight()) / 2f)),
                            ImGui.GetColorU32(ImGuiCol.TextDisabled), FitStart(folder, room));
                }
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
                _actions.Add(new ImportStlAction());
            }

            Item(ChooseAnotherLabel);
            if (ImGui.Button(CloseMessageLabel, full))
                _message = null;
            Item(CloseMessageLabel);
            AddPanelRectangle();
        }

        ImGui.End();
    }

    // ADR-0100 item 5 (Q3 A, TASK-0314): before a change of project while the document has no file and holds a command,
    // the window asks: "Save as..." saves it first, "Discard" leaves its journal in the folder of unsaved documents (ADR-0030
    // item 6) and changes, and "Cancel" changes nothing.
    private void QuestionWindow(Vector2 displaySize)
    {
        var width = Math.Min(480f, displaySize.X - 32f - _inset.X - _inset.Z);
        ImGui.SetNextWindowPos(new Vector2(displaySize.X / 2f, displaySize.Y / 2f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(width, 0f), ImGuiCond.Always);
        if (ImGui.Begin("The part has no file###Question", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.PushTextWrapPos(0f);
            ImGui.TextUnformatted("This part has no file. Save it as a project before the change?");
            ImGui.PopTextWrapPos();
            Item(QuestionItem);
            var full = new Vector2(-1f, MinimumControlHeight);
            foreach (var (label, answer) in new[] { (SaveAsLabel, ChangeAnswer.SaveAs), (DiscardLabel, ChangeAnswer.Discard), (CancelLabel, ChangeAnswer.Cancel) })
            {
                if (ImGui.Button($"{label}##Question", full))
                {
                    _question = false;
                    _actions.Add(new AnswerAction(answer));
                }

                Item($"{QuestionItem}:{label}");
            }

            AddPanelRectangle();
        }

        ImGui.End();
    }

    // ADR-0100: the end of the text that fits the width, after "...", so the last folders of a path stay.
    internal static string FitStart(string text, float width)
    {
        if (ImGui.CalcTextSize(text).X <= width)
            return text;
        for (var start = 1; start < text.Length; start++)
        {
            var cut = "..." + text[start..];
            if (ImGui.CalcTextSize(cut).X <= width)
                return cut;
        }

        return "...";
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
