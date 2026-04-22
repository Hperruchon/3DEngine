using ThreeDEngine.Core.Abstractions;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Core.Services;

/// <summary>
/// Basic in-memory resource catalog shared by hosts and tooling.
/// </summary>
public sealed class ResourceCatalog : IResourceCatalog
{
    private readonly Dictionary<string, MeshDefinition> _meshes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MaterialDefinition> _materials = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TextureDefinition> _textures = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, MeshDefinition> Meshes => _meshes;

    public IReadOnlyDictionary<string, MaterialDefinition> Materials => _materials;

    public IReadOnlyDictionary<string, TextureDefinition> Textures => _textures;

    public void Register(MeshDefinition mesh) => _meshes[mesh.Id] = mesh;

    public void Register(MaterialDefinition material) => _materials[material.Id] = material;

    public void Register(TextureDefinition texture) => _textures[texture.Id] = texture;
}
