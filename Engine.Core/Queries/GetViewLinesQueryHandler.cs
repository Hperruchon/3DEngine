using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Drawing;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0165 item 1 (phase R30a, TASK-0405). The query reads the Document and the owned kernel with ViewEdges, the code of
// GetSheet, so it needs no capability of the backend. As GetSheet, it has no cache. The direction is nine
// number parameters, as for AddSheetView, because the unit gate reads numbers.
public sealed class GetViewLinesQueryHandler : IQueryHandler
{
    public string QueryName => "GetViewLines";
    public int SchemaVersion => 1;

    // The names of the nine numbers, in the order of GetViewLinesQuery.Direction.
    private static readonly IReadOnlyList<string> DirectionNames = ["rightX", "rightY", "rightZ", "upX", "upY", "upZ", "eyeX", "eyeY", "eyeZ"];

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        DirectionNames.ToDictionary(name => name, _ => new FieldSchema("number", Required: true, Unit: UnitKinds.Direction));

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["direction"] = new("array"),
            ["lines"] = new("array"),
            ["skipped"] = new("array"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetViewLinesQuery
    {
        QueryId = input.QueryId,
        RightX = (double)input.Parameters["rightX"]!,
        RightY = (double)input.Parameters["rightY"]!,
        RightZ = (double)input.Parameters["rightZ"]!,
        UpX = (double)input.Parameters["upX"]!,
        UpY = (double)input.Parameters["upY"]!,
        UpZ = (double)input.Parameters["upZ"]!,
        EyeX = (double)input.Parameters["eyeX"]!,
        EyeY = (double)input.Parameters["eyeY"]!,
        EyeZ = (double)input.Parameters["eyeZ"]!,
    };

    public Task<QueryHandlerResult> Handle(Query query, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var get = (GetViewLinesQuery)query;
        var direction = get.Direction();
        if (SheetLayout.Frame(direction) is not { } frame)
        {
            return Task.FromResult(QueryHandlerResult.Failure(new ErrorDetail(DiagnosticCodes.SheetFrameInvalid,
                "The direction of a view is nine finite numbers, the right, the up and the direction toward the eye: the right and the up have one length at a right angle, and right x up points toward the eye, with no tolerance (ADR-0165 item 1).")));
        }

        ct.ThrowIfCancellationRequested();
        return Task.FromResult(QueryHandlerResult.Success(Read(document, direction, frame)));
    }

    // ADR-0165 item 1: the lines of ViewEdges divided by the length L of the frame. An exact L divides the exact numbers, so a
    // frame of whole numbers gives the doubles of the design; else the doubles divide by the double of L. The code uses
    // + - * / and sqrt only.
    internal static ViewLinesInfo Read(Document document, IReadOnlyList<double> direction, ViewFrame frame)
    {
        var (lines, skipped) = ViewEdges.Of(document, frame);
        var (exact, length) = frame.Length;
        double At(Rational value) => exact is { } l ? (value / l).ToDouble() : value.ToDouble() / length;

        var read = new List<ViewLineInfo>(lines.Count);
        foreach (var line in lines)
        {
            read.Add(new ViewLineInfo(line.Body, At(line.From.X), At(line.From.Y), At(line.To.X), At(line.To.Y), line.Visible,
                line.Kind == ViewLineKind.Outline ? "outline" : "edge")
            {
                Circle = line.Circle is { } c ? new ViewCircleInfo(At(c.Centre.X), At(c.Centre.Y), At(c.Radius)) : null,
                Ellipse = line.Ellipse is { } e
                    ? new ViewEllipseInfo(At(e.Centre.X), At(e.Centre.Y), e.Major.X / length, e.Major.Y / length, e.Minor.X / length, e.Minor.Y / length)
                    : null,
            });
        }

        return new ViewLinesInfo([.. direction], read, [.. skipped.Select(entry => new ViewSkippedBody(entry.Body, entry.Reason))]);
    }
}
