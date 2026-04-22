using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Core.Abstractions;

/// <summary>
/// Exposes the render resource descriptors known by the engine.
/// </summary>
public interface IResourceCatalog
{
    IReadOnlyDictionary<string, MeshDefinition> Meshes { get; }

    IReadOnlyDictionary<string, MaterialDefinition> Materials { get; }

    IReadOnlyDictionary<string, TextureDefinition> Textures { get; }
}
