namespace ThreeDEngine.Desktop.Ui;

// What a control of the panel asks for (ADR-0029 item 7, TASK-0086). The panel changes nothing
// itself; the host applies each action as a command of the session or as a change of the
// selection, as it applies an intent.
internal abstract record PanelAction;

internal sealed record SelectBodyAction(Guid Body) : PanelAction;

internal sealed record RenameBodyAction(Guid Body, string Name) : PanelAction;

internal sealed record SetVisibleAction(Guid Body, bool Visible) : PanelAction;

internal sealed record ShowAllAction : PanelAction;

// ADR-0030 item 3: a save to a name and an open of a name in the document folder.
internal sealed record SaveDocumentAction(string Name) : PanelAction;

internal sealed record OpenDocumentAction(string Name) : PanelAction;

// The line of the document file in the panel: the name of the file, or null for an
// unsaved document, and the result of the last save or open.
internal sealed record DocumentLine(string? FileName, string? Message);
