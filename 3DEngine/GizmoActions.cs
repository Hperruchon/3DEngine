using System.Numerics;
using ThreeDEngine.Core.Gizmo;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop;

// The drag of the translate gizmo in the desktop (ADR-0038 items 2, 7 and 8). It holds the state
// of the client between the start and the end of a drag: the body, its pivot, the handle and the
// offset. The drawing of the body follows the offset, and the end sends one Translate through
// BodyActions.Move, which selects the new body. A cancel and an offset of zero send nothing.
internal sealed class GizmoActions
{
    private Guid? _body;
    private (double X, double Y, double Z) _pivot;
    private (double X, double Y) _from;

    // The handle under the drag, or null.
    public GizmoHandle? Active { get; private set; }

    // The offset of the drag in progress.
    public (double X, double Y, double Z) Offset { get; private set; }

    // ADR-0038 item 7: the layout of the gizmo of the selected body, or null when no gizmo shows.
    // During a drag the gizmo moves with the offset.
    public GizmoLayout? Layout(Camera camera, double width, double height, (double X, double Y, double Z)? pivot)
    {
        if (pivot is not { } p)
            return null;
        var at = Active is null ? p : (_pivot.X + Offset.X, _pivot.Y + Offset.Y, _pivot.Z + Offset.Z);
        return TranslateGizmo.Layout(camera, width, height, at);
    }

    // Applies one intent of the gizmo, and returns true when the intent was one. The preview moves
    // the drawing of a body; the move sends the command and returns its rejection or null.
    public bool Apply(
        Intent intent, Camera camera, double width, double height, Guid? selected,
        Func<Guid, (double X, double Y, double Z)?> pivotOf, Action<Guid, Vector3> preview, Func<Guid, double, double, double, string?> move,
        out string? rejection)
    {
        rejection = null;
        switch (intent)
        {
            case GizmoStartIntent start:
                Reset(preview);
                if (selected is { } body && pivotOf(body) is { } pivot)
                    (_body, _pivot, _from, Active, Offset) = (body, pivot, (start.X, start.Y), start.Handle, (0, 0, 0));
                return true;

            case GizmoMoveIntent motion:
                Follow(camera, width, height, motion.X, motion.Y, preview);
                return true;

            case GizmoEndIntent end:
                Follow(camera, width, height, end.X, end.Y, preview);
                if (_body is { } moved && Offset != (0, 0, 0))
                {
                    var (dx, dy, dz) = Offset;
                    Reset(preview);
                    rejection = move(moved, dx, dy, dz);
                }
                else
                {
                    Reset(preview);
                }
                return true;

            case GizmoCancelIntent:
                Reset(preview);
                return true;

            default:
                return false;
        }
    }

    // The offset for a point of the pointer; a ray parallel to the axis or the plane keeps the
    // last offset (ADR-0038 item 8).
    private void Follow(Camera camera, double width, double height, double x, double y, Action<Guid, Vector3> preview)
    {
        if (_body is not { } body || Active is not { } handle)
            return;
        if (TranslateGizmo.Offset(camera, width, height, _pivot, handle, _from, (x, y)) is { } offset
            && double.IsFinite(offset.X) && double.IsFinite(offset.Y) && double.IsFinite(offset.Z))
            Offset = offset;
        preview(body, new Vector3((float)Offset.X, (float)Offset.Y, (float)Offset.Z));
    }

    private void Reset(Action<Guid, Vector3> preview)
    {
        if (_body is { } body)
            preview(body, Vector3.Zero);
        (_body, Active, Offset) = (null, null, (0, 0, 0));
    }
}
