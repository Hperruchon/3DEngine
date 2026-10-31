using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Gizmo;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// The drag of the gizmo in the desktop (ADR-0038 item 2, TASK-0113), with no GPU: the end of a
// drag sends one Translate with the offset and selects the new body, the drawing follows the
// offset during the drag, and a cancel or an offset of zero sends nothing. The native backend
// runs Translate.
public class GizmoActionsTests
{
    private const double Width = 800;
    private const double Height = 600;

    private static Camera Front() => new() { Target = (0, 0, 0), Distance = 10, YawDegrees = 0, PitchDegrees = 0 };

    private static double PerPoint => 2 * 10 * Math.Tan(Math.PI / 6) / Height;

    private static async Task<(EngineHost Host, BodyActions Actions, Guid Box)> OneBox()
    {
        var host = new EngineHost(new ManifoldGeometryBackend());
        var actions = new BodyActions(host.Session);
        Assert.Null(actions.CreateBox(1, 1, 1));
        var box = await host.Session.Read((document, _) => document.Bodies.Single().Handle.Id);
        actions.Select(box);
        return (host, actions, box);
    }

    private static int Count(EngineHost host) => host.Session.Read((document, _) => document.Log.Count).GetAwaiter().GetResult();

    // Each intent through GizmoActions, with the pivot (0, 0, 0) and a record of each preview.
    private static List<(Guid Body, Vector3 Offset)> Drag(GizmoActions gizmo, BodyActions actions, params Intent[] intents)
    {
        var previews = new List<(Guid, Vector3)>();
        foreach (var intent in intents)
            Assert.True(gizmo.Apply(intent, Front(), Width, Height, actions.Selection, _ => (0, 0, 0), (body, offset) => previews.Add((body, offset)), actions.Move, out var rejection) && rejection is null);
        return previews;
    }

    [Fact]
    public async Task The_End_Of_A_Drag_Sends_One_Translate_And_Selects_The_New_Body()
    {
        var (host, actions, box) = await OneBox();
        using var _ = host;
        var gizmo = new GizmoActions();

        var previews = Drag(gizmo, actions, new GizmoStartIntent(GizmoHandle.X, 450, 300), new GizmoMoveIntent(460, 300), new GizmoEndIntent(475, 300));

        var translate = await host.Session.Read((document, _) => (TranslateCommand)document.Log[^1].Command);
        Assert.Equal(box, translate.BodyId);
        Assert.Equal(25 * PerPoint, translate.Dx, 9);
        Assert.Equal((0.0, 0.0), (translate.Dy, translate.Dz));
        Assert.NotEqual(box, actions.Selection);
        Assert.Equal(await host.Session.Read((document, _) => document.Bodies.Single().Handle.Id), actions.Selection);

        // The drawing followed the drag, and went back to zero before the command.
        Assert.Equal((float)(10 * PerPoint), previews[0].Offset.X, 5);
        Assert.Equal(Vector3.Zero, previews[^1].Offset);
        Assert.Null(gizmo.Active);
    }

    // Finding V54 of the codebase review of 2026-10-30 (b) (TASK-0124): an undo during the drag takes
    // the body out of the document. The release then sends no Translate and gives no rejection,
    // as a cancel, and the drawing goes back.
    [Fact]
    public async Task The_End_Of_A_Drag_Of_A_Body_That_Left_The_Document_Sends_Nothing()
    {
        var (host, actions, box) = await OneBox();
        using var _ = host;
        var gizmo = new GizmoActions();
        (double X, double Y, double Z)? PivotOf(Guid body)
            => host.Session.Read((document, _) => document.Bodies.Any(record => record.Handle.Id == body)).GetAwaiter().GetResult() ? (0, 0, 0) : null;
        var previews = new List<(Guid Body, Vector3 Offset)>();
        string? Step(Intent intent)
        {
            Assert.True(gizmo.Apply(intent, Front(), Width, Height, box, PivotOf, (body, offset) => previews.Add((body, offset)), actions.Move, out var rejection));
            return rejection;
        }

        Step(new GizmoStartIntent(GizmoHandle.X, 450, 300));
        Step(new GizmoMoveIntent(470, 300));
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new UndoCommand())).Status);
        var rejection = Step(new GizmoEndIntent(475, 300));

        Assert.Null(rejection);
        Assert.IsType<UndoCommand>(await host.Session.Read((document, _) => document.Log[^1].Command));
        Assert.Equal((box, Vector3.Zero), previews[^1]);
        Assert.Null(gizmo.Active);
    }

    [Fact]
    public async Task A_Cancel_And_An_Offset_Of_Zero_Send_Nothing()
    {
        var (host, actions, box) = await OneBox();
        using var _ = host;
        var gizmo = new GizmoActions();
        var before = Count(host);

        var cancelled = Drag(gizmo, actions, new GizmoStartIntent(GizmoHandle.XY, 420, 280), new GizmoMoveIntent(440, 260), new GizmoCancelIntent());
        Drag(gizmo, actions, new GizmoStartIntent(GizmoHandle.Y, 400, 250), new GizmoEndIntent(400, 250));

        Assert.Equal(before, Count(host));
        Assert.Equal(box, actions.Selection);
        Assert.Equal(Vector3.Zero, cancelled[^1].Offset);
    }

    // ADR-0038 item 7: during a drag the gizmo moves with the offset.
    [Fact]
    public async Task The_Gizmo_Follows_The_Offset_During_A_Drag()
    {
        var (host, actions, _) = await OneBox();
        using var __ = host;
        var gizmo = new GizmoActions();

        Drag(gizmo, actions, new GizmoStartIntent(GizmoHandle.X, 450, 300), new GizmoMoveIntent(475, 300));
        var layout = gizmo.Layout(Front(), Width, Height, (0, 0, 0))!;

        Assert.Equal(GizmoHandle.X, gizmo.Active);
        Assert.Equal(425, layout.Origin.X, 3);
    }

    [Fact]
    public void An_Intent_That_Is_Not_Of_The_Gizmo_Is_Not_Taken()
    {
        var gizmo = new GizmoActions();

        Assert.False(gizmo.Apply(new OrbitIntent(1, 0), Front(), Width, Height, null, _ => null, (_, _) => { }, (_, _, _, _) => null, out _));
    }
}
