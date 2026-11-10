namespace Engine.Contracts.Schema;

// Per ADR-0013 §2. V1 vocabulary for Type:
// "string", "integer", "number", "boolean", "object", "array", "guid", "datetime".
// Nested object/array shape (item type, properties) is deferred until a
// command needs it.
//
// ADR-0060: a number or an integer parameter of a command or of a query declares its unit kind, in Unit or in
// UnitBy and not in both. The unit is metadata only: no host converts a value, and
// nothing new enters the log. The wire does not write an absent member.
public sealed record FieldSchema(string Type, bool Required = false, string? Unit = null, UnitByField? UnitBy = null);

// ADR-0060 item 3: the unit of a number that depends on the value of a string
// parameter of the same command. Units gives the unit for each value of Field; a
// value that is not in the map takes no number.
public sealed record UnitByField(string Field, IReadOnlyDictionary<string, string> Units);

// ADR-0060 item 2: the unit vocabulary. A client shows Length with the length unit
// of the document, and a pair Cosine and Sine as one angle.
public static class UnitKinds
{
    // A length in the length unit of the document.
    public const string Length = "length";

    // A position or a size on an image, in pixels.
    public const string Pixel = "pixel";

    // A length for one pixel of an image.
    public const string Scale = "scale";

    // The cosine of an angle.
    public const string Cosine = "cosine";

    // The sine of an angle.
    public const string Sine = "sine";

    // -1 or 1.
    public const string Sign = "sign";

    // The unit that the kind of the constraint gives, through the map of AddSketchConstraint.
    public const string ConstraintValue = "constraint-value";

    // A component of a direction, with no unit (finding E141 of the codebase review of 2026-11-05 (b), TASK-0192).
    public const string Direction = "direction";

    // A count of items, with no unit, as the count of the entries of a part (ADR-0071 item 3, TASK-0202).
    public const string Count = "count";

    // A component of the quaternion of a turn, with no unit. A client shows the four components of one command as one
    // turn (ADR-0112 item 2, TASK-0335).
    public const string Quaternion = "quaternion";

    public static IReadOnlyList<string> All { get; } =
        [Length, Pixel, Scale, Cosine, Sine, Sign, ConstraintValue, Direction, Count, Quaternion];
}
