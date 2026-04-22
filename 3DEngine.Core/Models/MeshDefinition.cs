namespace ThreeDEngine.Core.Models;

/// <summary>
/// Describes mesh data without binding it to a rendering backend.
/// </summary>
public sealed record MeshDefinition(string Id, int VertexCount, int IndexCount);
