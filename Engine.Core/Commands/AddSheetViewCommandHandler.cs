using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Drawing;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0155 items 1, 4 and 6 (phase P10.2, TASK-0393). The handler needs no capability of the backend. The direction is nine
// number parameters, because the unit gate reads numbers and no command has a parameter of the type array.
public sealed class AddSheetViewCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "AddSheetView";
    public int SchemaVersion => 1;

    // The names of the nine numbers of the direction, in the order of SheetView.Direction and of the properties of the command.
    internal static readonly IReadOnlyList<string> DirectionNames = ["rightX", "rightY", "rightZ", "upX", "upY", "upZ", "eyeX", "eyeY", "eyeZ"];

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = Schema();

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["viewId"] = new("guid"),
        };

    // ADR-0155 item 1 (Q3 A, Q4 A): the two parts of the scale are whole numbers with no unit, so they take the unit Count; the
    // place is a length on the paper.
    private static Dictionary<string, FieldSchema> Schema()
    {
        var schema = new Dictionary<string, FieldSchema> { ["sheetId"] = new("guid", Required: true) };
        foreach (var name in DirectionNames)
            schema[name] = new("number", Required: true, Unit: UnitKinds.Direction);
        schema["scaleNumerator"] = new("integer", Required: true, Unit: UnitKinds.Count);
        schema["scaleDenominator"] = new("integer", Required: true, Unit: UnitKinds.Count);
        schema["x"] = new("number", Required: true, Unit: UnitKinds.Length);
        schema["y"] = new("number", Required: true, Unit: UnitKinds.Length);
        return schema;
    }

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (AddSheetViewCommand)command;
        var fields = new Dictionary<string, object?>(StringComparer.Ordinal) { ["sheetId"] = typed.SheetId };
        var direction = typed.Direction();
        for (var i = 0; i < DirectionNames.Count; i++)
            fields[DirectionNames[i]] = direction[i];
        fields["scaleNumerator"] = typed.ScaleNumerator;
        fields["scaleDenominator"] = typed.ScaleDenominator;
        fields["x"] = typed.X;
        fields["y"] = typed.Y;
        return fields;
    }

    public Command Create(CommandInput input) => new AddSheetViewCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SheetId = (Guid)input.Parameters["sheetId"]!,
        RightX = (double)input.Parameters["rightX"]!,
        RightY = (double)input.Parameters["rightY"]!,
        RightZ = (double)input.Parameters["rightZ"]!,
        UpX = (double)input.Parameters["upX"]!,
        UpY = (double)input.Parameters["upY"]!,
        UpZ = (double)input.Parameters["upZ"]!,
        EyeX = (double)input.Parameters["eyeX"]!,
        EyeY = (double)input.Parameters["eyeY"]!,
        EyeZ = (double)input.Parameters["eyeZ"]!,
        ScaleNumerator = (long)input.Parameters["scaleNumerator"]!,
        ScaleDenominator = (long)input.Parameters["scaleDenominator"]!,
        X = (double)input.Parameters["x"]!,
        Y = (double)input.Parameters["y"]!,
    };

    // ADR-0155 item 1: the checks in the order of the record.
    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var add = (AddSheetViewCommand)command;
        if (SheetResults.Find(document, add.SheetId) is not { } sheet)
            return Task.FromResult(SheetResults.NotFound(add.SheetId));
        if (SheetResults.InUse(document, command, "a view") is { } inUse)
            return Task.FromResult(inUse);
        var direction = add.Direction();
        if (SheetLayout.Frame(direction) is null)
            return Task.FromResult(SheetResults.ViewInvalid(
                "The direction of a view is nine finite numbers, the right, the up and the direction toward the eye: the right and the up have one length at a right angle, and right x up points toward the eye, with no tolerance (ADR-0155 item 1)."));
        if (add.ScaleNumerator <= 0 || add.ScaleDenominator <= 0)
            return Task.FromResult(SheetResults.ViewInvalid(
                $"The scale of a view is two whole numbers greater than zero, and the command gives {add.ScaleNumerator} : {add.ScaleDenominator} (ADR-0155 item 1)."));
        if (SheetResults.PlaceRefused(sheet, add.X, add.Y) is { } place)
            return Task.FromResult(place);

        var view = new SheetView(add.CommandId, direction, add.ScaleNumerator, add.ScaleDenominator, add.X, add.Y);
        var outputs = new Outputs(new Dictionary<string, object?> { ["viewId"] = view.Id });
        return Task.FromResult(CommandHandlerResult.Success(outputs, changedSheets: [sheet with { Views = [.. sheet.Views, view] }]));
    }
}
