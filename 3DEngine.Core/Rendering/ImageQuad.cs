using System.Numerics;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Core.Rendering;

// ADR-0043 item 1 (phase V1a, TASK-0146): an image on a parallelogram of the scene. Origin is the
// corner at the bottom left of the image; Right and Up are the two edges, in units of the scene.
// It is render state: V2 makes it from the Document.
public sealed record ImageQuad(
    string ImageId,
    (double X, double Y, double Z) Origin,
    (double X, double Y, double Z) Right,
    (double X, double Y, double Z) Up)
{
    // ADR-0043 item 2: the model-view matrix that takes the unit square of the plane z = 0 to the
    // quad, relative to the eye. The corner moves by minus the eye in double precision before the
    // matrix narrows to float, as SceneWalker does. The third row is zero, because each corner of
    // the unit square has z = 0.
    public Matrix4x4 ModelView(Camera camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        var eye = camera.Eye();
        var model = new Matrix4x4(
            (float)Right.X, (float)Right.Y, (float)Right.Z, 0,
            (float)Up.X, (float)Up.Y, (float)Up.Z, 0,
            0, 0, 0, 0,
            (float)(Origin.X - eye.X), (float)(Origin.Y - eye.Y), (float)(Origin.Z - eye.Z), 1);
        return model * camera.View();
    }
}
