using System.Numerics;
using ImGuiNET;
using ThreeDEngine.Core.Input;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0098 (the redesign, slice o, TASK-0312): the command line of the render "mix", at the upper right of a landscape
// display. Control and K give it the keys. A text lists the commands of this state whose name holds it; Enter, or a tap,
// runs the first one or the one that the arrows chose, as its button or its segment does. Escape empties it and gives
// the keys back to the scene. A command that needs a number opens its pad (ADR-0032), so each value of the model still
// goes through the pad and its evaluator.
internal sealed unsafe partial class DesktopUi
{
    // The item of the field, the item of a listed command, and the text of the empty field.
    public const string CommandItem = "Command";
    public const string CommandHint = "Type a command";

    public static string CommandListItem(string name) => $"Command:{name}";

    // Item 2: the width of the field, and the count of commands that the list shows at most.
    internal const float CommandWidth = 300f;
    internal const int CommandListLength = 8;

    private string _command = string.Empty;
    private int _commandIndex;
    private bool _focusCommand;

    // Item 1: Control and K, down, give the field the keys in the next frame.
    private void WatchCommandKey(IReadOnlyList<InputSample> samples)
    {
        if (samples.Any(sample => sample is KeySample { Key: InputKey.K, Down: true } key && (key.Modifiers & KeyModifiers.Control) != 0))
            _focusCommand = true;
    }

    // Item 3: the commands of this state, in the order of the list: the commands of the active sketch, or of the document
    // and of the selected body.
    internal IReadOnlyList<(string Name, Action Run)> Commands(IReadOnlyList<LiveBody> bodies, Guid? selection)
    {
        var commands = new List<(string Name, Action Run)>();
        if (_sketchActive)
        {
            foreach (var (label, _) in SketchRingTools)
                commands.Add((label, () => UseSketchTool(label)));
            commands.Add((NextPointLabel, () => OpenNextPointPad(_sketchPanel?.ChainOpen ?? false)));
        }
        else
        {
            foreach (var label in new[] { NewBoxLabel, NewCylinderLabel, NewConeLabel, NewSphereLabel, NewTorusLabel })
                commands.Add((label, () => OpenNew(label)));
            // ADR-0100 item 1 (phase P9g, TASK-0314): the controls of a file, because the rows of a selected body hide the
            // panel (ADR-0099); a host with no files has none.
            if (_files)
            {
                commands.Add((OpenProjectLabel, () => _actions.Add(new OpenProjectAction())));
                commands.Add((SaveAsLabel, () => _actions.Add(new SaveAsAction())));
                commands.Add((ExportStlLabel, () => _actions.Add(new ExportStlAction())));
            }

            commands.Add((ImportStlLabel, () => _actions.Add(new ImportStlAction())));
            foreach (var (label, datum) in NewSketchButtons)
                commands.Add((label, () => _actions.Add(new NewSketchAction(datum))));
            // ADR-0099 item 4 (the redesign, slice p, TASK-0313): the controls of the panel that the rows of a selected body
            // hide: the armed tap on a face, the toggles of the dimensions, and the name field of the selected body.
            commands.Add((SketchOnFaceLabel, () => _actions.Add(new SketchOnFaceAction())));
            commands.Add((DimensionsLabel, () => _dimensions = !_dimensions));
            if (_dimensions)
                commands.Add((HiddenTooLabel, () => _hiddenToo = !_hiddenToo));
            if (bodies.FirstOrDefault(body => body.Id == selection) is { } body)
            {
                commands.Add((RenameLabel, () => StartRename(body)));
                foreach (var tool in bodies.Where(other => other.Id != body.Id && other.Visible))
                    commands.Add(($"{CutLabel} {tool.Name ?? tool.Kind}", () => _actions.Add(new CutBodyAction(body.Id, tool.Id))));
                commands.Add((MoveLabel, () => Use(MoveLabel, body)));
                if (body.Kind == "Extrude")
                    commands.Add((SetDistanceLabel, () => Use(SetDistanceLabel, body)));
                var visible = body.Visible ? HideLabel : ShowLabel;
                commands.Add((visible, () => Use(visible, body)));
            }

            commands.Add((ShowAllLabel, () => _actions.Add(new ShowAllAction())));
        }

        commands.Add((UndoLabel, () => _actions.Add(new UndoAction())));
        commands.Add((RedoLabel, () => _actions.Add(new RedoAction())));
        commands.Add((FrameLabel, () => _actions.Add(new FrameAction())));
        return commands;
    }

    // Item 3: the commands whose name holds the text, with no regard to the case; a name that starts with it comes first.
    internal static IReadOnlyList<(string Name, Action Run)> Matches(IReadOnlyList<(string Name, Action Run)> commands, string text)
    {
        var query = text.Trim();
        if (query.Length == 0)
            return [];
        return [.. commands
            .Select((command, order) => (command, order, at: command.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase)))
            .Where(match => match.at >= 0)
            .OrderBy(match => match.at == 0 ? 0 : 1)
            .ThenBy(match => match.order)
            .Select(match => match.command)
            .Take(CommandListLength)];
    }

    // Items 1 to 4: the field at the upper right of a landscape display, and the list of the matches under it.
    private void CommandLine(Vector2 displaySize, IReadOnlyList<LiveBody> bodies, Guid? selection)
    {
        if (displaySize.X < displaySize.Y)
            return;

        var right = displaySize.X - 16f - _inset.Z;
        var width = Math.Min(CommandWidth, Math.Max(right - 16f - _inset.X, 0f));
        ImGui.SetNextWindowPos(new Vector2(right - width, 16f + _inset.Y), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if (ImGui.Begin("##Command", FixedFlags))
        {
            if (_focusCommand)
            {
                ImGui.SetKeyboardFocusHere();
                _focusCommand = false;
            }

            ImGui.SetNextItemWidth(width);
            var enter = ImGui.InputTextWithHint("##CommandText", CommandHint, ref _command, 128, ImGuiInputTextFlags.EnterReturnsTrue);
            var active = ImGui.IsItemActive();
            var left = ImGui.IsItemDeactivated();
            Item(CommandItem);

            var matches = Matches(Commands(bodies, selection), _command);
            if (active && ImGui.IsKeyPressed(ImGuiKey.DownArrow))
                _commandIndex++;
            if (active && ImGui.IsKeyPressed(ImGuiKey.UpArrow))
                _commandIndex--;
            _commandIndex = matches.Count == 0 ? 0 : Math.Clamp(_commandIndex, 0, matches.Count - 1);

            (string Name, Action Run)? chosen = enter && matches.Count > 0 ? matches[_commandIndex] : null;
            for (var index = 0; index < matches.Count; index++)
            {
                if (ImGui.Selectable($"{matches[index].Name}##Command", index == _commandIndex, ImGuiSelectableFlags.None, new Vector2(width, MinimumControlHeight)))
                    chosen = matches[index];
                Item(CommandListItem(matches[index].Name));
            }

            if (chosen is { } run)
            {
                run.Run();
                (_command, _commandIndex, _dropFocus) = (string.Empty, 0, true);
            }
            else if ((active || left) && ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                (_command, _commandIndex, _dropFocus) = (string.Empty, 0, true);
            }

            AddWindowRectangle();
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }
}
