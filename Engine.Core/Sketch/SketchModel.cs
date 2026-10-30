using Engine.Contracts;
using Engine.Contracts.Sketch;

namespace Engine.Core.Sketch;

// ADR-0033 items 2, 4 and 5: the rules of a sketch record that each sketch command uses.
// The code uses + - * / and sqrt only (CLAUDE.md, determinism rules), because a
// residual or a check here decides a command of the log.
public static class SketchModel
{
    // The kind of an item of a sketch, as a constraint names it.
    public enum ItemKind
    {
        None,
        Point,
        Line,
        Circle,
        Arc,
        Constraint,
    }

    // ADR-0033 item 2: a new sketch holds its frame, the origin point with the identifier
    // of the sketch at (0, 0).
    public static SketchRecord New(Guid id, Guid datumId, string? name) => new(
        id, datumId, name,
        [new SketchPoint(id, 0, 0)],
        [], [], [], []);

    public static SketchRecord? Find(Document document, Guid sketchId)
        => document.Sketches.FirstOrDefault(sketch => sketch.Id == sketchId);

    public static ItemKind KindOf(SketchRecord sketch, Guid id)
    {
        if (sketch.Points.Any(point => point.Id == id)) return ItemKind.Point;
        if (sketch.Lines.Any(line => line.Id == id)) return ItemKind.Line;
        if (sketch.Circles.Any(circle => circle.Id == id)) return ItemKind.Circle;
        if (sketch.Arcs.Any(arc => arc.Id == id)) return ItemKind.Arc;
        if (sketch.Constraints.Any(constraint => constraint.Id == id)) return ItemKind.Constraint;
        return ItemKind.None;
    }

    public static SketchPoint Point(SketchRecord sketch, Guid id) => sketch.Points.First(point => point.Id == id);

    public static SketchLine Line(SketchRecord sketch, Guid id) => sketch.Lines.First(line => line.Id == id);

    public static SketchCircle Circle(SketchRecord sketch, Guid id) => sketch.Circles.First(circle => circle.Id == id);

    public static SketchArc Arc(SketchRecord sketch, Guid id) => sketch.Arcs.First(arc => arc.Id == id);

    // ADR-0033 item 6: DeleteSketchItem removes a point with each line, circle and arc that
    // references it, a line, a circle, an arc, or a constraint, and each constraint that
    // references a removed item. The origin of the frame is not deletable; the caller checks it.
    public static SketchRecord Without(SketchRecord sketch, Guid id)
    {
        var removed = new HashSet<Guid> { id };
        foreach (var line in sketch.Lines.Where(line => removed.Contains(line.Start) || removed.Contains(line.End)))
            removed.Add(line.Id);
        foreach (var circle in sketch.Circles.Where(circle => removed.Contains(circle.Center)))
            removed.Add(circle.Id);
        foreach (var arc in sketch.Arcs.Where(arc => removed.Contains(arc.Center) || removed.Contains(arc.Start) || removed.Contains(arc.End)))
            removed.Add(arc.Id);
        foreach (var constraint in sketch.Constraints.Where(constraint => constraint.Items.Any(removed.Contains)))
            removed.Add(constraint.Id);

        return sketch with
        {
            // Finding E91 of the codebase review of 2026-10-29: a read-only list, so a client
            // that casts a list of GetSketch cannot change the Document.
            Points = sketch.Points.Where(point => !removed.Contains(point.Id)).ToList().AsReadOnly(),
            Lines = sketch.Lines.Where(line => !removed.Contains(line.Id)).ToList().AsReadOnly(),
            Circles = sketch.Circles.Where(circle => !removed.Contains(circle.Id)).ToList().AsReadOnly(),
            Arcs = sketch.Arcs.Where(arc => !removed.Contains(arc.Id)).ToList().AsReadOnly(),
            Constraints = sketch.Constraints.Where(constraint => !removed.Contains(constraint.Id)).ToList().AsReadOnly(),
        };
    }
}
