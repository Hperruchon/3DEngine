namespace ThreeDEngine.Desktop;

// The visibility of each live body, keyed by the identifier of its mesh, and the count of
// the visible bodies that have a mesh (finding V27 of the codebase review of 2026-10-28,
// TASK-0090). Until TASK-0090 the desktop searched the list of live bodies for each mesh
// and made a string from each identifier, at each frame: 90.7 ms for 1,000 bodies. A read
// of the visibility is now one lookup, and the count is kept until the next change.
internal sealed class BodyVisibility
{
    private readonly Dictionary<string, bool> _visible = new(StringComparer.Ordinal);
    private readonly HashSet<string> _meshes = new(StringComparer.Ordinal);
    private int? _count;

    // The number of times the count was computed, for a test.
    public int Counts { get; private set; }

    public bool IsVisible(string meshId) => _visible.TryGetValue(meshId, out var visible) && visible;

    public void Set(string meshId, bool visible)
    {
        _visible[meshId] = visible;
        _count = null;
    }

    // A body that has a mesh counts; a body with no triangles does not.
    public void SetMesh(string meshId)
    {
        _meshes.Add(meshId);
        _count = null;
    }

    // A body whose mesh goes away and keeps its visibility, as in a rebuild (ADR-0037 item 8).
    public void RemoveMesh(string meshId)
    {
        _meshes.Remove(meshId);
        _count = null;
    }

    public void Remove(string meshId)
    {
        _visible.Remove(meshId);
        _meshes.Remove(meshId);
        _count = null;
    }

    // The count of live bodies that are visible and have a mesh.
    public int VisibleCount
    {
        get
        {
            if (_count is null)
            {
                Counts++;
                _count = _meshes.Count(IsVisible);
            }

            return _count.Value;
        }
    }
}
