using System.Numerics;

namespace ThreeDEngine.Core.Rendering;

/// <summary>
/// The list of draws of one frame (ADR-0023 item 4). The host fills it from the scene,
/// and a backend executes it. Each record names a mesh and gives its model-view
/// matrix relative to the eye; the record holds no backend handle, so a later backend
/// can execute the same list in another way, for example with indirect draws.
/// </summary>
public interface IDrawList
{
    /// <summary>Adds one draw of the mesh with the given model-view matrix.</summary>
    void Add(string meshId, Matrix4x4 modelView);
}

/// <summary>One draw of a frame.</summary>
public readonly record struct DrawRecord(string MeshId, Matrix4x4 ModelView);

/// <summary>A draw list that keeps its records in order, for a backend that executes them one by one.</summary>
public sealed class DrawList : IDrawList
{
    private readonly List<DrawRecord> _records = [];

    public IReadOnlyList<DrawRecord> Records => _records;

    public void Add(string meshId, Matrix4x4 modelView) => _records.Add(new DrawRecord(meshId, modelView));

    /// <summary>Removes each record, before the walk of the next frame.</summary>
    public void Clear() => _records.Clear();
}
