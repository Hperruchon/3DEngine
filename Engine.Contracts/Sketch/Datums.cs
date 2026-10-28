namespace Engine.Contracts.Sketch;

// A direction in the space of the Document.
public sealed record DatumAxis(double X, double Y, double Z);

// ADR-0033 item 1: a plane that a sketch lies on. The frame is right-handed:
// Normal = U × V, and the origin is (0, 0, 0).
public sealed record Datum(Guid Id, string Name, DatumAxis U, DatumAxis V, DatumAxis Normal);

// ADR-0033 item 1: the three origin planes, each with a fixed identifier. A sketch
// records the identifier, so a later command that makes a datum plane is additive.
public static class Datums
{
    public static readonly Datum XY = new(
        new Guid("00000000-0000-0000-0000-000000000001"), "XY",
        new DatumAxis(1, 0, 0), new DatumAxis(0, 1, 0), new DatumAxis(0, 0, 1));

    public static readonly Datum XZ = new(
        new Guid("00000000-0000-0000-0000-000000000002"), "XZ",
        new DatumAxis(1, 0, 0), new DatumAxis(0, 0, 1), new DatumAxis(0, -1, 0));

    public static readonly Datum YZ = new(
        new Guid("00000000-0000-0000-0000-000000000003"), "YZ",
        new DatumAxis(0, 1, 0), new DatumAxis(0, 0, 1), new DatumAxis(1, 0, 0));

    public static IReadOnlyList<Datum> All { get; } = [XY, XZ, YZ];

    // The datum with the identifier, or null.
    public static Datum? Find(Guid id) => All.FirstOrDefault(datum => datum.Id == id);
}
