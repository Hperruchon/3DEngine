using ThreeDEngine.Vulkan;

namespace ThreeDEngine.Desktop;

// ADR-0184 item 1 (phase R26, TASK-0447): the plane of a section view in the scene: a point on it, and the direction of the
// side that goes. The window gives it to the renderer; no command, event or Document holds it (ADR-0007).
internal readonly record struct SectionCut((double X, double Y, double Z) Point, (double X, double Y, double Z) Away);

// ADR-0184 item 2: a plane that a section can take: an origin plane or a named datum, with its origin and the direction of
// its offset. With no flip, the side that goes is the side of this direction. TASK-0449 (finding V232 of the third codebase
// review of 2026-11-21): Datum is the identity of the datum of the plane, and null for an origin plane, so a section follows its
// datum after a move, a rename or an undo.
internal sealed record SectionPlane(string Name, (double X, double Y, double Z) Origin, (double X, double Y, double Z) Direction, Guid? Datum = null);

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

    // The datums of a Document as planes: each one with a name and no fault, with its origin, its normal and its identity.
    public static IReadOnlyList<SectionPlane> Of(IEnumerable<Engine.Contracts.Sketch.DatumRecord> datums)
        => [.. datums.Where(datum => datum.Name is not null && datum.Fault is null).Select(datum => new SectionPlane(datum.Name!,
            (datum.Frame.Origin.X, datum.Frame.Origin.Y, datum.Frame.Origin.Z), (datum.Frame.Normal.X, datum.Frame.Normal.Y, datum.Frame.Normal.Z), datum.Id))];
}

// One live body with a mesh, as the section cuts it: its identifier, the geometry key of its mesh, true for a solid of the
// kernel (false for a mesh body), the centre of its box, which is the origin of its entity, and its triangles.
internal sealed record SectionBody(Guid Id, Guid Geometry, bool Solid, (double X, double Y, double Z) Centre, double[] Positions, uint[] Indices);

// ADR-0184 items 3 to 6: the meshes of a section view on the GPU. For each visible live body the cut of SectionMesh runs on the
// thread pool, off the thread of the frame, and a later frame puts its kept part and its cap on the GPU around the centre of
// the body, so the model-view matrix of the entity of the body draws them. Until the cut of a body is complete the body
// keeps its last cut, or shows whole. A body that the plane misses shows whole; a body on the side that goes does not show.
// The renderer draws a cap with the shading of a body and its hatch with the edge lines, in the accent colour.
//
// TASK-0449 (findings V229 and V230 of the third codebase review of 2026-11-21): the task of the cut also narrows the
// positions and finds the edge lines of the kept part and the hatch of the cap, so the thread of the frame only puts the
// meshes on the GPU (ADR-0121). A body runs one cut at a time: a new plane or a new mesh while a cut runs waits until that cut
// ends, and then the newest plane and mesh start, so quick taps on "Flip" do not pile up cuts.
internal sealed class SectionView
{
    // The cut of a body, ready for the GPU: the narrowed positions, the indices and the edge lines of the kept part, and of the
    // cap with its hatch. Each array is empty, and each line null, for a body that is whole or gone.
    internal sealed record Prepared(SectionMesh.Side Side, float[] Kept, uint[] KeptIndices, float[]? KeptLines, float[] Cap, uint[] CapIndices, float[]? CapLines);

    private sealed class Entry
    {
        // The mesh and the plane that the body wants now.
        public required Guid Geometry { get; set; }

        public required SectionCut Cut { get; set; }

        // The cut that runs or ran last, with the mesh and the plane that it cuts.
        public required Guid RunningGeometry { get; set; }

        public required SectionCut RunningCut { get; set; }

        public required Task<Prepared> Running { get; set; }

        // True when the GPU holds the result of Running, or when that result is stale and dropped.
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
    internal Func<Func<Prepared>, Task<Prepared>> RunCut { get; init; } = cut => Task.Run(cut);

    // A test can see the thread of each search of the edge lines and of the hatch.
    internal Action? Searched { get; init; }

    // Complete when each cut that is in progress is complete.
    internal Task Cuts => Task.WhenAll(_entries.Values.Select(entry => entry.Running));

    // The count of the cuts that the view started, for a test.
    internal int Started { get; private set; }

    // The mesh of the kept part and of the cap of a body in the dictionary of the meshes.
    internal static string KeptId(Guid body) => $"{body}:section";

    internal static string CapId(Guid body) => $"{body}:cap";

    // ADR-0184 item 5: the spacing of the hatch of a cap is 1/40 of the diagonal of the box of the visible bodies at the start of
    // the cut, about 6 pixels when Frame shows the whole part. The first form, 1/120, filled the caps of the bookcase.
    internal const double HatchParts = 40;

    // On the render loop: the cut of each body for the section, or no section. A new plane or a new mesh of a body starts a new
    // cut when the cut of the body that runs ends; a complete cut of the plane and the mesh of now goes on the GPU. It returns
    // true when a mesh of the section changed.
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
            if (!_entries.TryGetValue(body.Id, out var entry))
            {
                _entries[body.Id] = new Entry { Geometry = body.Geometry, Cut = plane, RunningGeometry = body.Geometry, RunningCut = plane, Running = Start(body, plane) };
                continue;
            }

            if (entry.Geometry != body.Geometry)
            {
                // The last cut is a cut of the old mesh, so it leaves the GPU, and the body shows whole until its new cut.
                changed |= Remove(body.Id, meshes, device) || entry.Shown is not null;
                (entry.Kept, entry.Cap, entry.Shown) = (false, false, null);
            }

            (entry.Geometry, entry.Cut) = (body.Geometry, plane);
            if (!entry.Running.IsCompleted)
                continue;
            var current = entry.RunningGeometry == body.Geometry && entry.RunningCut == plane;
            if (current && !entry.Uploaded)
            {
                changed |= Upload(body, entry, meshes, device);
            }
            else if (!current)
            {
                // The cut that ended is stale: the body keeps its last cut on the GPU, and the cut of now starts.
                entry.Uploaded = false;
                (entry.RunningGeometry, entry.RunningCut, entry.Running) = (body.Geometry, plane, Start(body, plane));
            }
        }

        return changed;
    }

    // The cut of a body on the thread pool: the cut of SectionMesh, then the narrowed positions, the edge lines of the kept
    // part and the hatch of the cap, with the spacing of now.
    private Task<Prepared> Start(SectionBody body, SectionCut plane)
    {
        Started++;
        var spacing = _spacing;
        var searched = Searched;
        return RunCut(() => Prepare(body, plane, spacing, searched));
    }

    private static Prepared Prepare(SectionBody body, SectionCut plane, double spacing, Action? searched)
    {
        var result = SectionMesh.Cut(body.Positions, body.Indices, plane.Point, plane.Away, body.Solid);
        if (result.Side != SectionMesh.Side.Cut)
            return new Prepared(result.Side, [], [], null, [], [], null);
        float[] kept = [];
        float[]? keptLines = null;
        if (result.Indices.Length > 0)
        {
            kept = MeshVertex.Narrow(result.Positions, body.Centre, 1.0);
            searched?.Invoke();
            keptLines = EdgeLines.Lines(kept, result.Positions, result.Indices);
        }

        float[] cap = [];
        float[]? capLines = null;
        if (result.CapIndices.Length > 0)
        {
            searched?.Invoke();
            var hatch = SectionMesh.Hatch(result.CapPieces, plane.Away, spacing);
            var ends = hatch.SelectMany(segment => new[] { segment.A.X, segment.A.Y, segment.A.Z, segment.B.X, segment.B.Y, segment.B.Z }).ToArray();
            capLines = ends.Length > 0
                ? EdgeLines.Vertices(MeshVertex.Narrow(ends, body.Centre, 1.0), [.. Enumerable.Range(0, hatch.Count).Select(i => (2 * i, (2 * i) + 1))])
                : null;
            cap = MeshVertex.Narrow(result.CapPositions, body.Centre, 1.0);
        }

        return new Prepared(result.Side, kept, result.Indices, keptLines, cap, result.CapIndices, capLines);
    }

    // The meshes of a complete cut on the GPU in the place of the last ones. A cut that failed shows the body whole, as a
    // failure of one body of SessionBodies, and the next body continues.
    private static bool Upload(SectionBody body, Entry entry, IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        entry.Uploaded = true;
        Remove(body.Id, meshes, device);
        (entry.Kept, entry.Cap) = (false, false);
        if (entry.Running.IsFaulted)
        {
            Console.WriteLine($"[WARN] No section of body {body.Id}: {entry.Running.Exception?.InnerException?.Message}");
            entry.Shown = SectionMesh.Side.Whole;
            return true;
        }

        var result = entry.Running.Result;
        entry.Shown = result.Side;
        if (result.Side != SectionMesh.Side.Cut)
            return true;
        try
        {
            if (result.KeptIndices.Length > 0)
            {
                meshes[KeptId(body.Id)] = new GpuMesh(device, result.Kept, result.KeptIndices, result.KeptLines);
                entry.Kept = true;
            }

            if (result.CapIndices.Length > 0)
            {
                meshes[CapId(body.Id)] = new GpuMesh(device, result.Cap, result.CapIndices, result.CapLines);
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

    // A body that goes: its meshes leave the GPU. A cut of it that still runs ends with no effect.
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
