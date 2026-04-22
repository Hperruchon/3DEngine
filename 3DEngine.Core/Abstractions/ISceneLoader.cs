using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Core.Abstractions;

/// <summary>
/// Loads a scene description from an external source.
/// </summary>
public interface ISceneLoader
{
    Scene Load(string source);
}
