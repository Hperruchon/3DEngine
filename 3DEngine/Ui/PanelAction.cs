namespace ThreeDEngine.Desktop.Ui;

// What a control of the panel asks for (ADR-0029 item 7, TASK-0086). The panel changes nothing
// itself; the host applies each action as a command of the session or as a change of the
// selection, as it applies an intent.
internal abstract record PanelAction;

internal sealed record SelectBodyAction(Guid Body) : PanelAction;

internal sealed record RenameBodyAction(Guid Body, string Name) : PanelAction;

internal sealed record SetVisibleAction(Guid Body, bool Visible) : PanelAction;

internal sealed record ShowAllAction : PanelAction;
