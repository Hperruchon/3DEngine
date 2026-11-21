using Engine.Kernel;

namespace Engine.Core.Drawing;

// ADR-0155 item 7, amended by TASK-0446 (register entry R-0069): the B-Rep of the solid of each body for the lines of a view
// with no section. The B-Rep is a function of the polygons of the solid only, so a read keeps it under the exact content of
// the solid and gives each view of the read and each later read the same B-Rep. The lines of each view are still computed
// at each read. Until TASK-0446 each view of each read built the B-Rep again: a sphere of 3,968 triangles took about 1 s
// for each build, and a read of three views of it took 4.5 s.
//
// A kept B-Rep is the B-Rep that a new build gives, in each document of the process. A build that throws is not kept. The
// read keeps one B-Rep for each pair of a document and a body; Keep drops the bodies that the last read of the document did
// not use, and the B-Reps of the last DocumentLimit documents stay. The cut of a section keeps its own B-Rep (SectionCut).
internal static class ViewBreps
{
    public const int DocumentLimit = 64;

    private static readonly object Gate = new();
    private static readonly LinkedList<(Guid Document, Dictionary<Guid, (KernelSolid Solid, Brep Brep)> Bodies)> Kept = new();
    private static readonly Dictionary<Guid, Dictionary<Guid, int>> Built = [];

    // The count of the B-Reps that the reads built for a body of a document, for a test of the cache. The counts of a
    // document leave with its kept B-Reps.
    internal static int BuiltOf(Guid document, Guid body)
    {
        lock (Gate)
            return Built.TryGetValue(document, out var counts) ? counts.GetValueOrDefault(body) : 0;
    }

    // The count of the kept B-Reps of a document, for a test.
    internal static int KeptOf(Guid document)
    {
        lock (Gate)
            return Kept.FirstOrDefault(entry => entry.Document == document).Bodies?.Count ?? 0;
    }

    // The B-Rep of the solid, from the kept B-Rep of the document and the body when its solid has the same polygons.
    public static Brep Of(Guid document, Guid body, KernelSolid solid)
    {
        ArgumentNullException.ThrowIfNull(solid);
        lock (Gate)
        {
            if (Bodies(document).TryGetValue(body, out var kept) && Same(kept.Solid, solid))
                return kept.Brep;
        }

        var brep = Brep.Of(solid);
        lock (Gate)
        {
            Bodies(document)[body] = (solid, brep);
            if (!Built.TryGetValue(document, out var counts))
                Built[document] = counts = [];
            counts[body] = counts.GetValueOrDefault(body) + 1;
        }

        return brep;
    }

    // After a read, the B-Reps of the bodies of the document that the read did not use leave: a body that is gone, a mesh
    // body, or a body with a fault.
    public static void Keep(Guid document, IReadOnlyCollection<Guid> bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        lock (Gate)
        {
            var kept = Bodies(document);
            foreach (var body in kept.Keys.Where(body => !bodies.Contains(body)).ToList())
                kept.Remove(body);
        }
    }

    // The kept B-Reps of a document, the document first in the list; the document of the oldest use leaves past
    // DocumentLimit documents. The caller holds the gate.
    private static Dictionary<Guid, (KernelSolid Solid, Brep Brep)> Bodies(Guid document)
    {
        for (var node = Kept.First; node is not null; node = node.Next)
        {
            if (node.Value.Document != document)
                continue;
            Kept.Remove(node);
            Kept.AddFirst(node);
            return node.Value.Bodies;
        }

        var bodies = new Dictionary<Guid, (KernelSolid Solid, Brep Brep)>();
        Kept.AddFirst((document, bodies));
        while (Kept.Count > DocumentLimit)
        {
            Built.Remove(Kept.Last!.Value.Document);
            Kept.RemoveLast();
        }

        return bodies;
    }

    // The exact content of two solids: each polygon with its vertices, its plane and its tag, in order. The projection of the
    // kernel keeps the solid of each geometry key (ADR-0059 item 2), so a body that did not change gives the same instance.
    private static bool Same(KernelSolid kept, KernelSolid solid)
    {
        if (ReferenceEquals(kept, solid))
            return true;
        var (a, b) = (kept.Polygons, solid.Polygons);
        if (a.Count != b.Count)
            return false;
        for (var i = 0; i < a.Count; i++)
        {
            if (!ReferenceEquals(a[i], b[i]) && (a[i].Plane != b[i].Plane || a[i].Tag != b[i].Tag || !a[i].Vertices.SequenceEqual(b[i].Vertices)))
                return false;
        }

        return true;
    }
}
