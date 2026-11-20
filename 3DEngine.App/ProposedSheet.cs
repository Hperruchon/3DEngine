using System.Globalization;
using System.Text.Json;
using Engine.Contracts.Drawing;
using Engine.Core.Export;
using Engine.Core.Queries;

namespace ThreeDEngine.Desktop;

// ADR-0177 item 1 (phase R30f, TASK-0422): a command of a proposal that the sheet page shows: a new sheet, a view or a dimension.
// Id is the identifier of the command, so it is the identifier of the sheet, the view or the dimension after the accept
// (CreateSheetCommand, AddSheetViewCommand, PlaceSheetDimensionCommand). Sheet is the sheet that the command changes.
internal abstract record ProposedSheetItem(Guid Id, Guid Sheet);

// CreateSheet: the sheet is the command itself.
internal sealed record ProposedNewSheet(Guid Id, string Name, string Paper, bool Landscape, bool ThirdAngle) : ProposedSheetItem(Id, Id);

// AddSheetView: the nine numbers of the frame, the scale and the place of the centre of the view on the paper.
internal sealed record ProposedView(Guid Id, Guid Sheet, IReadOnlyList<double> Direction, long ScaleNumerator, long ScaleDenominator, double X, double Y)
    : ProposedSheetItem(Id, Sheet);

// PlaceSheetDimension: the view, the source and its kind, and the place of the value on the paper.
internal sealed record ProposedDimension(Guid Id, Guid Sheet, Guid View, Guid Source, string SourceKind, double X, double Y) : ProposedSheetItem(Id, Sheet);

// ADR-0177 item 1: the sheet items of one proposal, for one sheet: the sheet of the first sheet item. Items for another sheet
// do not show. New is the CreateSheet of that sheet, or null for a live sheet.
internal sealed record ProposedSheet(Guid SheetId, ProposedNewSheet? New, IReadOnlyList<ProposedView> Views, IReadOnlyList<ProposedDimension> Dimensions)
{
    public const string CreateSheet = "CreateSheet";
    public const string AddSheetView = "AddSheetView";
    public const string PlaceSheetDimension = "PlaceSheetDimension";

    // The frame of AddSheetView in the order of its nine numbers (SheetView.Direction).
    private static readonly IReadOnlyList<string> DirectionNames = ["rightX", "rightY", "rightZ", "upX", "upY", "upZ", "eyeX", "eyeY", "eyeZ"];

    // The sheet item of one command, or null. A parameter that is absent, or that does not bind to its type of the schema of the
    // handler, gives no item, as for a proposed value (ProposedValues.Of). The names are those of the schema.
    public static ProposedSheetItem? ItemOf(string name, IEnumerable<KeyValuePair<string, JsonElement>> parameters, Guid command)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(parameters);
        var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var (key, value) in parameters)
            values[key] = value;

        switch (name)
        {
            case CreateSheet when Text(values, "name") is { } sheetName:
                var paper = values.ContainsKey("paper") ? Text(values, "paper") : "A4";
                var landscape = values.ContainsKey("landscape") ? Flag(values, "landscape") : true;
                var thirdAngle = values.ContainsKey("thirdAngle") ? Flag(values, "thirdAngle") : false;
                return paper is not null && SheetRecord.Papers.Contains(paper) && landscape is { } wide && thirdAngle is { } third
                    ? new ProposedNewSheet(command, sheetName, paper, wide, third)
                    : null;
            case AddSheetView when Id(values, "sheetId") is { } sheet && Whole(values, "scaleNumerator") is { } numerator and > 0
                && Whole(values, "scaleDenominator") is { } denominator and > 0 && Number(values, "x") is { } x && Number(values, "y") is { } y:
                var direction = DirectionNames.Select(direction => Number(values, direction)).ToList();
                return direction.All(value => value is not null)
                    ? new ProposedView(command, sheet, [.. direction.Select(value => value!.Value)], numerator, denominator, x, y)
                    : null;
            case PlaceSheetDimension when Id(values, "sheetId") is { } sheet && Id(values, "viewId") is { } view && Id(values, "source") is { } source
                && Text(values, "sourceKind") is { } kind && Number(values, "x") is { } x && Number(values, "y") is { } y:
                return new ProposedDimension(command, sheet, view, source, kind, x, y);
            default:
                return null;
        }
    }

    // ADR-0177 item 1: the sheet of the first sheet item, with the items of that sheet in the order of the commands; null with no
    // sheet item.
    public static ProposedSheet? Of(IEnumerable<ProposedSheetItem?>? items)
    {
        var list = items?.OfType<ProposedSheetItem>().ToList() ?? [];
        if (list.Count == 0)
            return null;
        var sheet = list[0].Sheet;
        var mine = list.Where(item => item.Sheet == sheet).ToList();
        return new ProposedSheet(sheet, mine.OfType<ProposedNewSheet>().FirstOrDefault(), [.. mine.OfType<ProposedView>()], [.. mine.OfType<ProposedDimension>()]);
    }

    // ADR-0177 item 2: the box of a proposed view on the paper in millimetres (left, bottom, right, top), as GetSheet places the
    // view after the accept (SheetLayout.Place): the box of its edges and its outlines, with each whole curve, around the centre
    // of that box at the place of the view, times the scale. The lines of GetViewLines are in units of the design already, so
    // the factor is the scale alone. A view with no line, or with no read yet, is a square of 10 mm at its place, as
    // SheetPage.Box gives it.
    public static (double X0, double Y0, double X1, double Y1) Box(ProposedView view, ViewLinesInfo? lines)
    {
        ArgumentNullException.ThrowIfNull(view);
        var (u0, v0, u1, v1) = (double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity);
        void Add(double u, double v) => (u0, v0, u1, v1) = (Math.Min(u0, u), Math.Min(v0, v), Math.Max(u1, u), Math.Max(v1, v));
        foreach (var line in lines?.Lines ?? [])
        {
            // A centre line does not give the box or the centre of a view (SheetLayout.Place).
            if (line.Kind == "centre")
                continue;
            Add(line.FromX, line.FromY);
            Add(line.ToX, line.ToY);
            if (line.Circle is { } c)
            {
                Add(c.X - c.Radius, c.Y - c.Radius);
                Add(c.X + c.Radius, c.Y + c.Radius);
            }

            if (line.Ellipse is { } e)
            {
                var (across, up) = (Math.Sqrt((e.MajorX * e.MajorX) + (e.MinorX * e.MinorX)), Math.Sqrt((e.MajorY * e.MajorY) + (e.MinorY * e.MinorY)));
                Add(e.X - across, e.Y - up);
                Add(e.X + across, e.Y + up);
            }
        }

        if (!double.IsFinite(u0))
            return (view.X - 5, view.Y - 5, view.X + 5, view.Y + 5);
        var factor = (double)view.ScaleNumerator / view.ScaleDenominator;
        var (cu, cv) = ((u0 + u1) / 2, (v0 + v1) / 2);
        return (view.X + (factor * (u0 - cu)), view.Y + (factor * (v0 - cv)), view.X + (factor * (u1 - cu)), view.Y + (factor * (v1 - cv)));
    }

    // ADR-0177 item 3: the name of a proposed view: the name of its flat view, or "View" for each other frame, and its scale.
    public static string Name(ProposedView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        var flat = SheetPage.FlatOf(view.Direction);
        return $"{(flat is { } named ? named.ToString() : "View")} {view.ScaleNumerator}:{view.ScaleDenominator}";
    }

    // ADR-0177 item 2: the text of a proposed dimension, as the sheet page prints the value of a dimension (SvgExport.Value), from
    // the design now: a constraint of a live sketch, or the distance of an extrude. A radius on a whole circle of its sketch is a
    // diameter, as GetSheet gives it. A source that the design does not hold gives "?".
    public static string Text(ProposedDimension dimension, IEnumerable<SketchInfo> sketches, double? distance)
    {
        ArgumentNullException.ThrowIfNull(dimension);
        ArgumentNullException.ThrowIfNull(sketches);
        (string Label, double Value)? value = null;
        if (dimension.SourceKind == "constraint")
        {
            foreach (var sketch in sketches)
            {
                if (sketch.Constraints.FirstOrDefault(constraint => constraint.Id == dimension.Source) is not { Values.Count: > 0 } constraint)
                    continue;
                value = constraint.Kind switch
                {
                    "distance" => ("length", constraint.Values[0]),
                    "radius" when constraint.Items.Count == 1 && sketch.Circles.Any(circle => circle.Id == constraint.Items[0]) => ("diameter", 2 * constraint.Values[0]),
                    "radius" => ("radius", constraint.Values[0]),
                    _ => null,
                };
                break;
            }
        }
        else if (dimension.SourceKind == "distance" && distance is { } length)
            value = ("distance", length);

        return value is { } known
            ? SvgExport.Value(new SheetDimensionInfo(dimension.Id, dimension.View, dimension.Source, dimension.SourceKind, dimension.X, dimension.Y, false, known.Label, known.Value), "Ø")
            : "?";
    }

    private static Guid? Id(Dictionary<string, JsonElement> values, string name)
        => values.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var id) ? id : null;

    private static string? Text(Dictionary<string, JsonElement> values, string name)
        => values.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool? Flag(Dictionary<string, JsonElement> values, string name)
        => values.TryGetValue(name, out var value)
            ? value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(value.GetString(), out var flag) => flag,
                _ => null,
            }
            : null;

    // A number of JSON, or a text that the binder reads as a number, when it is finite.
    private static double? Number(Dictionary<string, JsonElement> values, string name)
    {
        if (!values.TryGetValue(name, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) && double.IsFinite(number))
            return number;
        return value.ValueKind == JsonValueKind.String
            && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && double.IsFinite(parsed)
            ? parsed
            : null;
    }

    private static long? Whole(Dictionary<string, JsonElement> values, string name)
    {
        if (!values.TryGetValue(name, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
            return number;
        return value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }
}

// ADR-0177 items 2 and 3: what the sheet page draws for a proposal: the paper of a new sheet (null for a live sheet), and the
// box, the name and the place of each view, and the text and the place of each value.
internal sealed record SheetProposal(Guid Proposal, Guid SheetId, SheetInfo? NewSheet, IReadOnlyList<SheetProposal.Box> Boxes, IReadOnlyList<SheetProposal.Value> Values)
{
    public sealed record Box(Guid View, string Name, double X0, double Y0, double X1, double Y1);

    public sealed record Value(Guid Dimension, string Text, double X, double Y);

    // ADR-0177 item 2: the read of a proposal, off the frame thread: GetViewLines once for each frame, and GetBodyInfo once for
    // each extrude of a distance. A query that fails gives the square of 10 mm or "?". Nothing is sent.
    public static async Task<SheetProposal> Read(Guid proposal, ProposedSheet sheet, IReadOnlyList<SketchInfo> sketches,
        Func<GetViewLinesQuery, Task<ViewLinesInfo?>> viewLines, Func<Guid, Task<double?>> distance)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(sketches);
        ArgumentNullException.ThrowIfNull(viewLines);
        ArgumentNullException.ThrowIfNull(distance);
        var lines = new Dictionary<string, ViewLinesInfo?>(StringComparer.Ordinal);
        var boxes = new List<Box>(sheet.Views.Count);
        foreach (var view in sheet.Views)
        {
            var key = string.Join(",", view.Direction.Select(value => value.ToString("R", CultureInfo.InvariantCulture)));
            if (!lines.TryGetValue(key, out var read))
            {
                var d = view.Direction;
                read = await viewLines(new GetViewLinesQuery
                {
                    RightX = d[0], RightY = d[1], RightZ = d[2], UpX = d[3], UpY = d[4], UpZ = d[5], EyeX = d[6], EyeY = d[7], EyeZ = d[8],
                }).ConfigureAwait(false);
                lines[key] = read;
            }

            var (x0, y0, x1, y1) = ProposedSheet.Box(view, read);
            boxes.Add(new Box(view.Id, ProposedSheet.Name(view), x0, y0, x1, y1));
        }

        var distances = new Dictionary<Guid, double?>();
        var values = new List<Value>(sheet.Dimensions.Count);
        foreach (var dimension in sheet.Dimensions)
        {
            double? length = null;
            if (dimension.SourceKind == "distance")
            {
                if (!distances.TryGetValue(dimension.Source, out length))
                    distances[dimension.Source] = length = await distance(dimension.Source).ConfigureAwait(false);
            }

            values.Add(new Value(dimension.Id, ProposedSheet.Text(dimension, sketches, length), dimension.X, dimension.Y));
        }

        SheetInfo? paper = null;
        if (sheet.New is { } created)
        {
            var (width, height) = SheetPlacement.Paper(created.Paper, created.Landscape);
            paper = new SheetInfo(created.Id, created.Name, created.Paper, created.Landscape, created.ThirdAngle, width, height, [], []);
        }

        return new SheetProposal(proposal, sheet.SheetId, paper, boxes, values);
    }
}

// ADR-0177 item 2: the read of the proposal of the page, off the thread of the frame, one read at a time, for a new proposal or
// a new version of the Document; until it ends, the page keeps the last read of the same proposal.
internal sealed class SheetProposalCache
{
    private string? _asked;
    private Task<SheetProposal>? _reading;
    private SheetProposal? _last;

    public int Computed { get; private set; }

    public SheetProposal? Read(long version, Guid proposal, Func<Task<SheetProposal>> read)
    {
        ArgumentNullException.ThrowIfNull(read);
        if (_reading is { IsCompleted: true } done)
        {
            if (done.IsCompletedSuccessfully)
                _last = done.Result;
            _ = done.Exception;
            _reading = null;
        }

        var key = $"{version}:{proposal}";
        if (_asked != key && _reading is null)
        {
            _asked = key;
            _reading = read();
            Computed++;
        }

        return _last?.Proposal == proposal ? _last : null;
    }
}
