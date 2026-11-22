using ThreeDEngine.Vulkan;

namespace ThreeDEngine.Desktop;

// ADR-0184 item 1 (phase R26, TASK-0447): the plane of a section view in the scene: a point on it, and the direction of the
// side that goes. The window gives it to the renderer; no command, event or Document holds it (ADR-0007).
internal readonly record struct SectionCut((double X, double Y, double Z) Point, (double X, double Y, double Z) Away);

// ADR-0184 item 2: a plane that a section can take: an origin plane or a named datum, with its origin and the direction of
// its offset. With no flip, the side that goes is the side of this direction.
internal sealed record SectionPlane(string Name, (double X, double Y, double Z) Origin, (double X, double Y, double Z) Direction);

// ADR-0184 items 2 and 3: the state of a section view in the window: its plane, its offset along the direction of the plane,
// and the flip of the side that goes.
internal sealed record SectionState(SectionPlane Plane, double Offset, bool Flip)
{
    public SectionCut Cut
    {
        get
        {
            var (o, d) = (Plane.Origin, Plane.Direction);
            var sign = Flip ? -1.0 : 1.0;
            return new SectionCut((o.X + (Offset * d.X), o.Y + (Offset * d.Y), o.Z + (Offset * d.Z)), (sign * d.X, sign * d.Y, sign * d.Z));
        }
    }
}

// ADR-0184 item 2: the named datums of the document with no fault, as planes of a section, read off the thread of the frame
// after each change of the document while a section lasts, so the frame takes no lock of the session, as for the history of a body.
internal sealed class SectionDatums
{
    private long? _version;
    private Task<IReadOnlyList<SectionPlane>>? _reading;
    private IReadOnlyList<SectionPlane> _planes = [];

    // The planes of the last complete read; a new version starts a read, which the cache does not wait for.
    public IReadOnlyList<SectionPlane> Planes(long version, Func<Task<IReadOnlyList<SectionPlane>>> read)
    {
        ArgumentNullException.ThrowIfNull(read);
        if (_reading is { IsCompleted: true } done)
        {
            _planes = done.IsCompletedSuccessfully ? done.Result : _planes;
            _reading = null;
        }

        if (_version != version && _reading is null)
        {
            _version = version;
            _reading = read();
        }

        return _planes;
    }

    // The datums of a Document as planes: each one with a name and no fault, with its origin and its normal.
    public static IReadOnlyList<SectionPlane> Of(IEnumerable<Engine.Contracts.Sketch.DatumRecord> datums)
        => [.. datums.Where(datum => datum.Name is not null && datum.Fault is null).Select(datum => new SectionPlane(datum.Name!,
            (datum.Frame.Origin.X, datum.Frame.Origin.Y, datum.Frame.Origin.Z), (datum.Frame.Normal.X, datum.Frame.Normal.Y, datum.Frame.Normal.Z)))];
}

// One live body with a mesh, as the section cuts it: its identifier, the geometry key of its mesh, true for a solid of the
// kernel (false for a mesh body), the centre of its box, which is the origin of its entity, and its triangles.
internal sealed record SectionBody(Guid Id, Guid Geometry, bool Solid, (double X, double Y, double Z) Centre, double[] Positions, uint[] Indices);

// ADR-0184 items 3 to 6: the meshes of a section view on the GPU. For each live body the cut of SectionMesh runs on the
// thread pool, off the thread of the frame, and a later frame puts its kept part and its cap on the GPU around the centre of
// the body, so the model-view matrix of the entity of the body draws them. Until the cut of a body is complete the body
// keeps its last cut, or shows whole. A body that the plane misses shows whole; a body on the side that goes does not show.
// The renderer draws a cap with the shading of a body and its hatch with the edge lines, in the accent colour.
internal sealed class SectionView
{
    private sealed class Entry
    {
        public required Guid Geometry { get; init; }

        public required SectionCut Cut { get; init; }

        public required Task<SectionMesh.Result> Pending { get; init; }

        // True when the GPU holds the result of Pending.
        public bool Uploaded { get; set; }

        // The side of the cut that the GPU shows, or null before the first cut of the body is on the GPU.
        public SectionMesh.Side? Shown { get; set; }

        public bool Kept { get; set; }

        public bool Cap { get; set; }
    }

    private readonly Dictionary<Guid, Entry> _entries = new();
    private SectionCut? _cut;
    private double _spacing;

    // A test can hold a cut to choose when it is complete.
    internal Func<Func<SectionMesh.Result>, Task<SectionMesh.Result>> RunCut { get; init; } = cut => Task.Run(cut);

    // Complete when each cut that is in progress is complete.
    internal Task Cuts => Task.WhenAll(_entries.Values.Select(entry => entry.Pending));

    // The count of the cuts that the view started, for a test.
    internal int Started { get; private set; }

    // The mesh of the kept part and of the cap of a body in the dictionary of the meshes.
    internal static string KeptId(Guid body) => $"{body}:section";

    internal static string CapId(Guid body) => $"{body}:cap";

    // ADR-0184 item 5: the spacing of the hatch of a cap is 1/40 of the diagonal of the box of the visible bodies at the start of
    // the cut, about 6 pixels when Frame shows the whole part. The first form, 1/120, filled the caps of the bookcase.
    internal const double HatchParts = 40;

    // On the render loop: the cut of each body for the section, or no section. A new plane or a new mesh of a body starts a new
    // cut; a complete cut goes on the GPU. It returns true when a mesh of the section changed.
    public bool Follow(SectionCut? cut, IReadOnlyList<SectionBody> bodies, ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? box, IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        if (cut is not { } plane)
        {
            _cut = null;
            return Clear(meshes, device);
        }

        if (_cut != plane)
        {
            _cut = plane;
            var diagonal = box is { } b
                ? Math.Sqrt(Square(b.Max.X - b.Min.X) + Square(b.Max.Y - b.Min.Y) + Square(b.Max.Z - b.Min.Z))
                : 0;
            _spacing = diagonal > 0 && double.IsFinite(diagonal) ? diagonal / HatchParts : 1;
        }

        var changed = false;
        var live = bodies.Select(body => body.Id).ToHashSet();
        foreach (var gone in _entries.Keys.Where(id => !live.Contains(id)).ToList())
            changed |= Drop(gone, meshes, device);

        foreach (var body in bodies)
        {
            if (_entries.TryGetValue(body.Id, out var entry) && entry.Geometry == body.Geometry && entry.Cut == plane)
                continue;
            var start = Start(body, plane);
            if (entry is not null && entry.Geometry == body.Geometry)
            {
                // The body keeps its last cut on the GPU until the new one is complete.
                _entries[body.Id] = new Entry { Geometry = body.Geometry, Cut = plane, Pending = start, Shown = entry.Shown, Kept = entry.Kept, Cap = entry.Cap };
            }
            else
            {
                changed |= Drop(body.Id, meshes, device);
                _entries[body.Id] = new Entry { Geometry = body.Geometry, Cut = plane, Pending = start };
            }
        }

        foreach (var body in bodies)
        {
            var entry = _entries[body.Id];
            if (entry.Pending.IsCompleted && !entry.Uploaded)
                changed |= Upload(body, entry, meshes, device);
        }

        return changed;
    }

    private Task<SectionMesh.Result> Start(SectionBody body, SectionCut plane)
    {
        Started++;
        var (positions, indices, solid) = (body.Positions, body.Indices, body.Solid);
        return RunCut(() => SectionMesh.Cut(positions, indices, plane.Point, plane.Away, solid));
    }

    // The meshes of a complete cut on the GPU in the place of the last ones. A cut that failed shows the body whole, as a
    // failure of one body of SessionBodies, and the next body continues.
    private bool Upload(SectionBody body, Entry entry, IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        entry.Uploaded = true;
        Remove(body.Id, meshes, device);
        (entry.Kept, entry.Cap) = (false, false);
        if (entry.Pending.IsFaulted)
        {
            Console.WriteLine($"[WARN] No section of body {body.Id}: {entry.Pending.Exception?.InnerException?.Message}");
            entry.Shown = SectionMesh.Side.Whole;
            return true;
        }

        var result = entry.Pending.Result;
        entry.Shown = result.Side;
        if (result.Side != SectionMesh.Side.Cut)
            return true;
        try
        {
            if (result.Indices.Length > 0)
            {
                var narrowed = MeshVertex.Narrow(result.Positions, body.Centre, 1.0);
                meshes[KeptId(body.Id)] = new GpuMesh(device, narrowed, result.Indices, EdgeLines.Lines(narrowed, result.Positions, result.Indices));
                entry.Kept = true;
            }

            if (result.CapIndices.Length > 0)
            {
                var hatch = SectionMesh.Hatch(result.CapPieces, entry.Cut.Away, _spacing);
                var ends = hatch.SelectMany(segment => new[] { segment.A.X, segment.A.Y, segment.A.Z, segment.B.X, segment.B.Y, segment.B.Z }).ToArray();
                var lines = ends.Length > 0
                    ? EdgeLines.Vertices(MeshVertex.Narrow(ends, body.Centre, 1.0), [.. Enumerable.Range(0, hatch.Count).Select(i => (2 * i, (2 * i) + 1))])
                    : null;
                meshes[CapId(body.Id)] = new GpuMesh(device, MeshVertex.Narrow(result.CapPositions, body.Centre, 1.0), result.CapIndices, lines);
                entry.Cap = true;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[WARN] No section of body {body.Id}: {exception.GetType().Name}: {exception.Message}");
            Remove(body.Id, meshes, device);
            (entry.Kept, entry.Cap, entry.Shown) = (false, false, SectionMesh.Side.Whole);
        }

        return true;
    }

    // ADR-0184 item 4: the meshes that the draw of a body takes, or null when the body draws whole: no section, no cut on the
    // GPU yet, or a plane that misses the body. A body on the side that goes draws nothing.
    public (string? Kept, string? Cap)? Draws(Guid body)
    {
        if (_cut is null || !_entries.TryGetValue(body, out var entry) || entry.Shown is not { } side || side == SectionMesh.Side.Whole)
            return null;
        return (entry.Kept ? KeptId(body) : null, entry.Cap ? CapId(body) : null);
    }

    // The side of the cut of a body that the GPU shows, or null, for a test.
    internal SectionMesh.Side? ShownOf(Guid body) => _entries.TryGetValue(body, out var entry) ? entry.Shown : null;

    private bool Clear(IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        var changed = false;
        foreach (var id in _entries.Keys.ToList())
            changed |= Drop(id, meshes, device);
        return changed;
    }

    private bool Drop(Guid body, IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        var shown = _entries.Remove(body, out var entry) && entry.Shown is not null;
        return Remove(body, meshes, device) || shown;
    }

    // The meshes of the section of a body leave the GPU. A frame in flight can still read their buffers.
    private static bool Remove(Guid body, IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        var removed = false;
        foreach (var id in new[] { KeptId(body), CapId(body) })
        {
            if (!meshes.Remove(id, out var mesh))
                continue;
            if (!removed)
                device.WaitIdle();
            mesh.Dispose();
            removed = true;
        }

        return removed;
    }

    private static double Square(double value) => value * value;
}
