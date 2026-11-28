using Engine.Core.Preview;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.Desktop;

// ADR-0187 item 4 (phase R25, TASK-0462): the switch of the preview of a proposal. Before: the scene as the Document is. After:
// each body that the proposal removes or gives a new solid does not show, and the new shapes show in dashed amber lines. Both:
// the scene as the Document is, and the new shapes in dashed amber lines on it. A state of the client (ADR-0007).
internal enum PreviewMode
{
    Before,
    After,
    Both,
}

// ADR-0187 item 3 (TASK-0462): the preview of the shown proposal as the window reads it from the host (IProposalDesk.Preview):
// the values of GET /proposals/{id}/preview and the shapes after the commands. Current is false while a later version of the
// Document waits for its preview. Error is the code of a failed preview, or null. SketchNames gives the name of each sketch
// of the frame, for the lines of the state of the solver. ADR-0188 item 6 (phase P16, TASK-0463): Held is the code of the note
// of the last accept that applied nothing because the result changed, or null; the window gives Version to the next accept.
internal sealed record PreviewCard(
    Guid Proposal,
    long Version,
    bool Current,
    string? Error,
    int? FailedIndex,
    string? FailedCode,
    BodyDifferences? Bodies,
    IReadOnlyList<SketchChange> Sketches,
    IReadOnlyList<MeasureChange> Measures,
    IReadOnlyList<PairChange> Checks,
    IReadOnlyList<PreviewShape> Shapes,
    int ShapesLeftOut,
    string? Held = null)
{
    public IReadOnlyDictionary<Guid, string> SketchNames { get; init; } = new Dictionary<Guid, string>();

    // Item 4: the bodies of the Document that "After" does not draw: each removed body, each changed body with a new solid
    // (the rule of the same solid of the fork), and each changed body that the commands hide. Not kept in a field, because a copy "with" of the record
    // would keep the set of the first card.
    public IReadOnlySet<Guid> Replaced => Bodies is not { } bodies
        ? new HashSet<Guid>()
        : [.. bodies.Removed.Select(body => body.Handle.Id),
           // ADR-0191 item 5 (phase P11.0, TASK-0474): a new placement is a new solid too.
           .. bodies.Changed.Where(change => change.Before.GeometryKey != change.After.GeometryKey || !Equals(change.Before.Fault, change.After.Fault)
               || !Equals(change.Before.Placement, change.After.Placement) || !change.After.Visible)
               .Select(change => change.After.Handle.Id)];

    // The name of a body of the preview: the name or the kind of its record after the commands, or before them for a removed
    // body; null for a body that the preview does not name.
    public string? BodyName(Guid id)
    {
        if (Bodies is not { } bodies)
            return null;
        var record = bodies.Added.FirstOrDefault(body => body.Handle.Id == id)
            ?? bodies.Changed.FirstOrDefault(change => change.After.Handle.Id == id)?.After
            ?? bodies.Removed.FirstOrDefault(body => body.Handle.Id == id);
        return record is null ? null : record.Name ?? record.Kind;
    }
}

// ADR-0187 item 5 (TASK-0462): what the renderer draws of the preview in one frame: the card and the mode of the switch.
internal sealed record PreviewDraw(PreviewCard Card, PreviewMode Mode);

// ADR-0187 item 5 (TASK-0462): the projection of the shapes of a preview into the render-side scene (ADR-0009): one entity and
// one mesh with its edge lines for each shape, each mesh narrowed around the centre of its box, as SessionBodies does
// (ADR-0024 item 2). The narrowing and the search of the edges run on the thread pool, so the thread of the frame only puts
// the meshes on the GPU. The meshes of the shown shapes stay until the meshes of the new shapes are ready, and a frame with
// no shape takes them away. The mesh of a shape has the identifier MeshId(body), so it does not meet the mesh of the body.
internal sealed class PreviewMeshes
{
    // The entities of the shown shapes, one for each shape, with no camera.
    public Scene Scene { get; } = new() { Name = "Preview" };

    // The shapes on the GPU, the shapes that the thread pool prepares, and its work.
    private IReadOnlyList<PreviewShape>? _shown;
    private IReadOnlyList<PreviewShape>? _preparingFor;
    private Task<Prepared[]>? _preparing;
    private readonly List<string> _meshIds = [];
    private readonly List<((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)> _boxes = [];

    // A test can hold the preparation to choose when it is complete.
    internal Func<Func<Prepared[]>, Task<Prepared[]>> RunPrepare { get; init; } = work => Task.Run(work);

    // The count of the sets of shapes that went up to the GPU, for a test.
    internal int Uploads { get; private set; }

    // Complete when the preparation in progress is complete, for a test.
    internal Task Preparing => _preparing ?? Task.CompletedTask;

    public static string MeshId(Guid body) => $"preview:{body}";

    // The bodies of the shown shapes, in the order of the shapes.
    public IReadOnlyList<Guid> Shown => _shown?.Select(shape => shape.BodyId).ToList() ?? [];

    // The box of the shown shapes, or null with no shape.
    public ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? Bounds
        => ThreeDEngine.Core.Measuring.Dimensions.Union(_boxes);

    internal sealed record Prepared(Guid Body, float[] Narrowed, uint[] Indices, float[]? Edges, (double X, double Y, double Z) Centre,
        ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) Box);

    // On the thread of the frame: the shapes of the card on the GPU. It returns true when the shown shapes changed.
    public bool Follow(PreviewCard? card, IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        var wanted = card?.Shapes;
        if (wanted is not { Count: > 0 })
        {
            (_preparing, _preparingFor) = (null, null);
            return Clear(meshes, device);
        }

        if (ReferenceEquals(wanted, _shown))
            return false;
        if (!ReferenceEquals(wanted, _preparingFor))
        {
            _preparingFor = wanted;
            _preparing = RunPrepare(() => [.. wanted.Select(Prepare)]);
        }

        if (_preparing is not { IsCompleted: true } done)
            return false;
        _preparing = null;
        _preparingFor = null;
        var changed = Clear(meshes, device);
        _shown = wanted;
        if (!done.IsCompletedSuccessfully)
        {
            Console.WriteLine($"[WARN] The shapes of the preview were not drawn: {done.Exception?.GetBaseException().Message}");
            return changed;
        }

        foreach (var prepared in done.Result)
        {
            var meshId = MeshId(prepared.Body);
            try
            {
                meshes[meshId] = new GpuMesh(device, prepared.Narrowed, prepared.Indices, prepared.Edges);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"[WARN] The shape of the body {prepared.Body} in the preview was not drawn: {exception.GetType().Name}: {exception.Message}");
                continue;
            }

            _meshIds.Add(meshId);
            _boxes.Add(prepared.Box);
            Scene.Entities.Add(new Entity { Name = $"Preview {prepared.Body}", MeshId = meshId, Origin = prepared.Centre });
        }

        Uploads++;
        return true;
    }

    // On the thread pool: the narrowed positions, the edge lines and the box of one shape.
    private static Prepared Prepare(PreviewShape shape)
    {
        var (positions, indices) = (shape.Triangles.Positions, shape.Triangles.Indices);
        var centre = MeshVertex.Centre(positions);
        var narrowed = MeshVertex.Narrow(positions, centre, 1.0);
        (double X, double Y, double Z) min = (double.MaxValue, double.MaxValue, double.MaxValue);
        (double X, double Y, double Z) max = (double.MinValue, double.MinValue, double.MinValue);
        for (var index = 0; index + 2 < positions.Length; index += 3)
        {
            min = (Math.Min(min.X, positions[index]), Math.Min(min.Y, positions[index + 1]), Math.Min(min.Z, positions[index + 2]));
            max = (Math.Max(max.X, positions[index]), Math.Max(max.Y, positions[index + 1]), Math.Max(max.Z, positions[index + 2]));
        }

        return new Prepared(shape.BodyId, narrowed, indices, EdgeLines.Lines(narrowed, positions, indices), centre, (min, max));
    }

    // Takes each mesh of the shown shapes off the GPU. It returns true when a shape was shown.
    public bool Clear(IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        _shown = null;
        _boxes.Clear();
        Scene.Entities.Clear();
        if (_meshIds.Count == 0)
            return false;

        // A frame in flight can still read the buffers of a mesh.
        device.WaitIdle();
        foreach (var meshId in _meshIds)
        {
            if (meshes.Remove(meshId, out var mesh))
                mesh.Dispose();
        }

        _meshIds.Clear();
        return true;
    }
}
