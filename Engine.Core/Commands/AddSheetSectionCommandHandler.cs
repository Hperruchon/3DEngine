using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Drawing;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0169 item 2 (phase R30h, TASK-0411). The handler needs no capability of the backend.
public sealed class AddSheetSectionCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "AddSheetSection";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid", Required: true),
            ["viewId"] = new("guid", Required: true),
            ["look"] = new("string", Required: true),
            ["pointX"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["pointY"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["pointZ"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["x"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["y"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["letter"] = new("string", Required: false),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["viewId"] = new("guid"),
            ["letter"] = new("string"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (AddSheetSectionCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sheetId"] = typed.SheetId,
            ["viewId"] = typed.ViewId,
            ["look"] = typed.Look,
            ["pointX"] = typed.PointX,
            ["pointY"] = typed.PointY,
            ["pointZ"] = typed.PointZ,
            ["x"] = typed.X,
            ["y"] = typed.Y,
            ["letter"] = typed.Letter,
        };
    }

    public Command Create(CommandInput input) => new AddSheetSectionCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SheetId = (Guid)input.Parameters["sheetId"]!,
        ViewId = (Guid)input.Parameters["viewId"]!,
        Look = (string)input.Parameters["look"]!,
        PointX = (double)input.Parameters["pointX"]!,
        PointY = (double)input.Parameters["pointY"]!,
        PointZ = (double)input.Parameters["pointZ"]!,
        X = (double)input.Parameters["x"]!,
        Y = (double)input.Parameters["y"]!,
        Letter = input.Parameters.GetValueOrDefault("letter") as string,
    };

    // ADR-0169 item 2: the checks in the order of the record.
    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var add = (AddSheetSectionCommand)command;
        if (SheetResults.Find(document, add.SheetId) is not { } sheet)
            return Task.FromResult(SheetResults.NotFound(add.SheetId));
        if (SheetResults.InUse(document, command, "a section") is { } inUse)
            return Task.FromResult(inUse);
        if (sheet.Views.FirstOrDefault(view => view.Id == add.ViewId) is not { } parent)
            return Task.FromResult(SheetResults.ViewNotFound(add.SheetId, add.ViewId));
        if (!SectionCut.Looks.Contains(add.Look, StringComparer.Ordinal))
            return Task.FromResult(SheetResults.ViewInvalid(
                $"The look of a section is one of {string.Join(", ", SectionCut.Looks)}, and the command gives '{add.Look}' (ADR-0169 item 2)."));
        if (!double.IsFinite(add.PointX) || !double.IsFinite(add.PointY) || !double.IsFinite(add.PointZ))
            return Task.FromResult(SheetResults.ViewInvalid("The point of a section is three finite numbers (ADR-0169 item 2)."));
        if (SheetLayout.Frame(parent.Direction) is not { } parentFrame || SectionCut.Frame(parentFrame, add.Look) is not { } frame)
            return Task.FromResult(SheetResults.ViewInvalid(
                "A section needs a parent whose right has a length that is a rational number, for example a frame of whole numbers whose length is a whole number (ADR-0169 item 2)."));
        if (SectionCut.Direction(frame) is not { } direction)
            return Task.FromResult(SheetResults.ViewInvalid(
                "The frame of the section has a number that a double does not hold exactly (ADR-0169 item 2)."));
        var used = sheet.Views.Where(view => view.Section is not null).Select(view => view.Section!.Letter).ToHashSet(StringComparer.Ordinal);
        if (add.Letter is { } given && (given.Length != 1 || given[0] is < 'A' or > 'Z' || used.Contains(given)))
            return Task.FromResult(SheetResults.ViewInvalid(
                $"The letter of a section is one capital letter from A to Z that no other section of the sheet has, and the command gives '{given}' (ADR-0169 item 2)."));
        if ((add.Letter ?? SectionCut.FreeLetter(sheet)) is not { } letter)
            return Task.FromResult(SheetResults.ViewInvalid("Each default letter of a section, A to Z without I, O and Q, is in use on the sheet, so the command must give a free letter (ADR-0169 item 2)."));
        if (SheetResults.PlaceRefused(sheet, add.X, add.Y) is { } place)
            return Task.FromResult(place);

        var section = new SheetSection(parent.Id, [add.PointX, add.PointY, add.PointZ], letter);
        var view = new SheetView(add.CommandId, direction, parent.ScaleNumerator, parent.ScaleDenominator, add.X, add.Y,
            HiddenLines: false, CentreLines: true, Section: section);
        var outputs = new Outputs(new Dictionary<string, object?> { ["viewId"] = view.Id, ["letter"] = letter });
        return Task.FromResult(CommandHandlerResult.Success(outputs, changedSheets: [sheet with { Views = [.. sheet.Views, view] }]));
    }
}
