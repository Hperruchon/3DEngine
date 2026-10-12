using System.Numerics;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Core.Rendering;

/// <summary>
/// The walk of the scene graph at each frame (ADR-0023 item 3). Each entity that has a
/// mesh gives one record of the draw list. A child is placed relative to its parent.
/// </summary>
public static class SceneWalker
{
    /// <summary>
    /// Adds one record for each entity of the scene that has a mesh, with its model-view
    /// matrix relative to the eye of the camera. The position of each entity is moved by
    /// minus the eye in double precision before the matrix is narrowed to float.
    /// </summary>
    public static void Fill(Scene scene, Camera camera, IDrawList list)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(list);

        var eye = camera.Eye();
        var view = camera.View();

        foreach (var entity in scene.Entities)
            Walk(entity, Matrix4x4.Identity, (0.0, 0.0, 0.0), eye, view, list);
    }

    private static void Walk(
        Entity entity,
        Matrix4x4 parentTurn,
        (double X, double Y, double Z) parentPosition,
        (double X, double Y, double Z) eye,
        Matrix4x4 view,
        IDrawList list)
    {
        var local = entity.Transform;
        var turn = Matrix4x4.CreateScale(local.Scale) * Matrix4x4.CreateFromQuaternion(local.Rotation) * parentTurn;
        var offset = Vector3.Transform(local.Position, parentTurn);
        var position = (parentPosition.X + offset.X, parentPosition.Y + offset.Y, parentPosition.Z + offset.Z);

        if (entity.MeshId is { } meshId)
        {
            var relative = new Vector3((float)(position.Item1 - eye.X), (float)(position.Item2 - eye.Y), (float)(position.Item3 - eye.Z));
            list.Add(meshId, turn * Matrix4x4.CreateTranslation(relative) * view);
        }

        foreach (var child in entity.Children)
            Walk(child, turn, position, eye, view, list);
    }
}
