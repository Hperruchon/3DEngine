using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Core.Abstractions;

/// <summary>
/// Defines the lifecycle expected from a 3D engine host.
/// </summary>
public interface IThreeDEngine
{
    string Name { get; }

    Scene? CurrentScene { get; }

    void Initialize();

    void Update(TimeSpan deltaTime);

    void Render();

    void LoadScene(Scene scene);
}
