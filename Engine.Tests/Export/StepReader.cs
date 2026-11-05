using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Engine.Tests.Export;

// A small reader of ISO 10303-21 for the tests of the STEP export (ADR-0070, TASK-0200). It reads the instances of the
// section DATA, one on each line, and checks the topology: each reference names an instance, each loop is closed, and
// each edge has two uses in opposite senses inside its solid. It is a check of the writer, not an import: the charter
// refuses an exact STEP import (anti-objective 14).
internal sealed partial class StepReader
{
    // One instance: its name ("" for a complex instance) and its attributes, each one a token or a list of attributes.
    internal sealed record Instance(string Name, IReadOnlyList<object> Attributes);

    public IReadOnlyDictionary<int, Instance> Instances { get; }

    public string Header { get; }

    private StepReader(IReadOnlyDictionary<int, Instance> instances, string header) => (Instances, Header) = (instances, header);

    [GeneratedRegex(@"^#(\d+)=([A-Z0-9_]*)(\(.*\));$")]
    private static partial Regex Line();

    public static StepReader Parse(byte[] file)
    {
        Assert.All(file, b => Assert.True(b is >= 0x20 and < 0x7F || b == (byte)'\n', $"The byte {b} is not printable ASCII."));
        var text = Encoding.ASCII.GetString(file);
        Assert.StartsWith("ISO-10303-21;\nHEADER;\n", text, StringComparison.Ordinal);
        Assert.EndsWith("ENDSEC;\nEND-ISO-10303-21;\n", text, StringComparison.Ordinal);
        var data = text.IndexOf("\nDATA;\n", StringComparison.Ordinal);
        Assert.True(data > 0, "The file has no section DATA.");
        var header = text[..data];
        var body = text[(data + "\nDATA;\n".Length)..^"ENDSEC;\nEND-ISO-10303-21;\n".Length];

        var instances = new Dictionary<int, Instance>();
        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = Line().Match(line);
            Assert.True(match.Success, $"The line '{line}' is not an instance.");
            var id = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var at = 0;
            var attributes = match.Groups[2].Value.Length == 0 ? [] : List(match.Groups[3].Value, ref at);
            Assert.True(match.Groups[2].Value.Length == 0 || at == match.Groups[3].Value.Length, $"The line '{line}' has text after its attributes.");
            Assert.True(instances.TryAdd(id, new Instance(match.Groups[2].Value, attributes)), $"The instance #{id} is twice in the file.");
        }

        // Each reference names an instance of the file.
        foreach (var (id, instance) in instances)
        {
            foreach (var reference in References(instance.Attributes))
                Assert.True(instances.ContainsKey(reference), $"The instance #{id} names #{reference}, which is not in the file.");
        }

        return new StepReader(instances, header);
    }

    // The attributes of one list, from its opening parenthesis to its closing one.
    private static List<object> List(string text, ref int at)
    {
        Assert.Equal('(', text[at]);
        at++;
        var items = new List<object>();
        while (text[at] != ')')
        {
            if (text[at] == '(')
            {
                items.Add(List(text, ref at));
            }
            else if (text[at] == '\'')
            {
                var end = text.IndexOf('\'', at + 1);
                items.Add(text[at..(end + 1)]);
                at = end + 1;
            }
            else
            {
                var start = at;
                while (text[at] != ',' && text[at] != ')')
                {
                    if (text[at] == '(')
                    {
                        // A typed value, as LENGTH_MEASURE(1.E-07): the name and its list are one token.
                        var name = text[start..at];
                        items.Add((name, List(text, ref at)));
                        start = -1;
                        break;
                    }
                    at++;
                }
                if (start >= 0)
                    items.Add(text[start..at]);
            }

            if (text[at] == ',')
                at++;
        }

        at++;
        return items;
    }

    private static IEnumerable<int> References(IEnumerable<object> attributes)
    {
        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case string token when token.StartsWith('#'):
                    yield return int.Parse(token[1..], CultureInfo.InvariantCulture);
                    break;
                case List<object> list:
                    foreach (var reference in References(list))
                        yield return reference;
                    break;
                case ValueTuple<string, List<object>> typed:
                    foreach (var reference in References(typed.Item2))
                        yield return reference;
                    break;
            }
        }
    }

    public IEnumerable<int> Of(string name) => Instances.Where(pair => pair.Value.Name == name).Select(pair => pair.Key).Order();

    public int Count(string name) => Of(name).Count();

    public static int Id(object attribute) => int.Parse(((string)attribute)[1..], CultureInfo.InvariantCulture);

    public static IReadOnlyList<object> Items(object attribute) => (List<object>)attribute;

    public static double Real(object attribute)
    {
        var token = (string)attribute;
        Assert.Matches(@"^-?\d+\.\d*(E[+-]?\d+)?$", token);
        return double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    public (double X, double Y, double Z) Point(int id)
    {
        var point = Instances[id];
        Assert.True(point.Name is "CARTESIAN_POINT" or "DIRECTION", $"#{id} is a {point.Name}.");
        var values = Items(point.Attributes[1]);
        Assert.Equal(3, values.Count);
        return (Real(values[0]), Real(values[1]), Real(values[2]));
    }

    // The point of a VERTEX_POINT.
    public (double X, double Y, double Z) Vertex(int id)
    {
        Assert.Equal("VERTEX_POINT", Instances[id].Name);
        return Point(Id(Instances[id].Attributes[1]));
    }

    // The start and the end vertex of an ORIENTED_EDGE, in the sense of its use.
    public (int Start, int End) Ends(int orientedEdge)
    {
        var use = Instances[orientedEdge];
        Assert.Equal("ORIENTED_EDGE", use.Name);
        var curve = Instances[Id(use.Attributes[3])];
        Assert.Equal("EDGE_CURVE", curve.Name);
        var (start, end) = (Id(curve.Attributes[1]), Id(curve.Attributes[2]));
        return (string)use.Attributes[4] == ".T." ? (start, end) : (end, start);
    }

    // The solids of the file, MANIFOLD_SOLID_BREP and BREP_WITH_VOIDS, in the order of the file.
    public IReadOnlyList<int> Solids() => [.. Instances.Where(pair => pair.Value.Name is "MANIFOLD_SOLID_BREP" or "BREP_WITH_VOIDS").Select(pair => pair.Key).Order()];

    // The faces of the outer shell of a solid, from MANIFOLD_SOLID_BREP or BREP_WITH_VOIDS through CLOSED_SHELL.
    public IReadOnlyList<int> Faces(int solid)
    {
        Assert.True(Instances[solid].Name is "MANIFOLD_SOLID_BREP" or "BREP_WITH_VOIDS", $"#{solid} is a {Instances[solid].Name}.");
        return Shell(Id(Instances[solid].Attributes[1]));
    }

    // The faces of each void of a solid, through ORIENTED_CLOSED_SHELL with the orientation .F.; none for a
    // MANIFOLD_SOLID_BREP.
    public IReadOnlyList<IReadOnlyList<int>> Voids(int solid)
    {
        if (Instances[solid].Name == "MANIFOLD_SOLID_BREP")
            return [];
        Assert.Equal("BREP_WITH_VOIDS", Instances[solid].Name);
        return [.. Items(Instances[solid].Attributes[2]).Select(Id).Select(id =>
        {
            var oriented = Instances[id];
            Assert.Equal("ORIENTED_CLOSED_SHELL", oriented.Name);
            Assert.Equal(("*", ".F."), ((string)oriented.Attributes[1], (string)oriented.Attributes[3]));
            return Shell(Id(oriented.Attributes[2]));
        })];
    }

    private IReadOnlyList<int> Shell(int id)
    {
        Assert.Equal("CLOSED_SHELL", Instances[id].Name);
        return [.. Items(Instances[id].Attributes[1]).Select(Id)];
    }

    // The loops of a face, each one the list of its ORIENTED_EDGE, and true for the outer bound.
    public IReadOnlyList<(bool Outer, IReadOnlyList<int> Uses)> Loops(int face)
    {
        Assert.Equal("ADVANCED_FACE", Instances[face].Name);
        return [.. Items(Instances[face].Attributes[1]).Select(Id).Select(bound =>
        {
            var instance = Instances[bound];
            Assert.True(instance.Name is "FACE_OUTER_BOUND" or "FACE_BOUND", $"#{bound} is a {instance.Name}.");
            Assert.Equal(".T.", instance.Attributes[2]);
            var loop = Instances[Id(instance.Attributes[1])];
            Assert.Equal("EDGE_LOOP", loop.Name);
            return (instance.Name == "FACE_OUTER_BOUND", (IReadOnlyList<int>)[.. Items(loop.Attributes[1]).Select(Id)]);
        })];
    }

    // The origin and the normal of the plane of a face.
    public ((double X, double Y, double Z) Origin, (double X, double Y, double Z) Normal) Plane(int face)
    {
        var plane = Instances[Id(Instances[face].Attributes[2])];
        Assert.Equal("PLANE", plane.Name);
        var frame = Instances[Id(plane.Attributes[1])];
        Assert.Equal("AXIS2_PLACEMENT_3D", frame.Name);
        return (Point(Id(frame.Attributes[1])), Point(Id(frame.Attributes[2])));
    }

    // The checks of a closed solid: each loop is a closed chain of edges, and each EDGE_CURVE of each shell has one use in
    // each sense. The result is the counts of its faces, its edges and its vertices, with those of its voids.
    public (int Faces, int Edges, int Vertices) Solid(int solid)
    {
        var (faces, edges, vertices) = Closed(Faces(solid));
        foreach (var hollow in Voids(solid))
        {
            var counts = Closed(hollow);
            (faces, edges, vertices) = (faces + counts.Faces, edges + counts.Edges, vertices + counts.Vertices);
        }

        return (faces, edges, vertices);
    }

    private (int Faces, int Edges, int Vertices) Closed(IReadOnlyList<int> shell)
    {
        var uses = new Dictionary<int, (int Forward, int Backward)>();
        var vertices = new HashSet<int>();
        foreach (var face in shell)
        {
            var loops = Loops(face);
            Assert.Single(loops, loop => loop.Outer);
            Assert.True(loops[0].Outer, $"The first bound of the face #{face} is not its outer bound.");
            foreach (var (_, loop) in loops)
            {
                for (var i = 0; i < loop.Count; i++)
                {
                    var (start, end) = Ends(loop[i]);
                    Assert.Equal(end, Ends(loop[(i + 1) % loop.Count]).Start);
                    vertices.Add(start);
                    var curve = Id(Instances[loop[i]].Attributes[3]);
                    var (forward, backward) = uses.GetValueOrDefault(curve);
                    uses[curve] = (string)Instances[loop[i]].Attributes[4] == ".T." ? (forward + 1, backward) : (forward, backward + 1);
                }
            }
        }

        Assert.All(uses, use => Assert.Equal((1, 1), use.Value));
        return (shell.Count, uses.Count, vertices.Count);
    }
}
