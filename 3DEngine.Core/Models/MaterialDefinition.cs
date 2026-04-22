namespace ThreeDEngine.Core.Models;

/// <summary>
/// Describes a material resource used by a renderer.
/// </summary>
public sealed record MaterialDefinition(string Id, string Shader, string? BaseColorTextureId);
