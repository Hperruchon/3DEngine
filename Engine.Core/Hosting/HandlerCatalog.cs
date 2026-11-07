using Engine.Contracts.Handlers;
using Engine.Core.Assets;
using Engine.Core.Commands;
using Engine.Core.Queries;

namespace Engine.Core.Hosting;

// Per ADR-0016: one list of handlers, used by each host and by the canonical
// replay determinism gate.
//
// Before this catalog existed, seven positions in the solution registered
// handlers by hand. The consequence was register entry R-0003: the canonical
// gate registered two handlers and each host registered five, therefore the
// gate did not exercise the set that the hosts use.
//
// The list is explicit. It is not a scan of the assembly. Two reasons:
// replay determinism needs a fixed set and a fixed order, and a scan makes the
// set implicit at the moment when a person most needs to read it.
//
// A narrow unit test registers its own handlers on purpose. Such a test checks
// one behaviour of the bus, and it must not receive each handler. Use this
// catalog for a host and for a gate that must agree with a host.
public static class HandlerCatalog
{
    // ADR-0083 item 2 (TASK-0258): assets is the store of the assets of the host, which the handler of ImportStl reads;
    // a host with no store gives null, and its imports are refused.
    public static IReadOnlyList<ICommandHandler> CommandHandlers(IAssetSource? assets = null) =>
    [
        new NoOpCommandHandler(),
        new CreateBoxCommandHandler(),
        new CreateCylinderCommandHandler(),
        new TranslateCommandHandler(),
        new SubtractCommandHandler(),
        new RenameBodyCommandHandler(),
        new SetBodyVisibilityCommandHandler(),
        new UndoCommandHandler(),
        new RedoCommandHandler(),
        new CreateSketchCommandHandler(),
        new AddSketchPointCommandHandler(),
        new AddSketchLineCommandHandler(),
        new AddSketchCircleCommandHandler(),
        new AddSketchArcCommandHandler(),
        new AddSketchConstraintCommandHandler(),
        new SetSketchConstraintValueCommandHandler(),
        new MoveSketchPointCommandHandler(),
        new DeleteSketchItemCommandHandler(),
        new ExtrudeCommandHandler(),
        new PlaceImageCommandHandler(),
        new CalibrateImageCommandHandler(),
        new SetExtrudeDistanceCommandHandler(),
        new CreateDatumCommandHandler(),
        new AddSketchReferenceCommandHandler(),
        new AddPartReferenceCommandHandler(),
        new ImportStlCommandHandler(assets),
    ];

    public static IReadOnlyList<IQueryHandler> QueryHandlers() =>
    [
        new GetBoundingBoxQueryHandler(),
        new GetTessellationQueryHandler(),
        new GetBodyInfoQueryHandler(),
        new GetSketchQueryHandler(),
        new GetDatumQueryHandler(),
        new GetFaceAtQueryHandler(),
        new GetFacesQueryHandler(),
        new GetFeaturesQueryHandler(),
        new GetPartReferencesQueryHandler(),
    ];

    public static void RegisterAll(CommandRegistry commands, QueryRegistry queries, IAssetSource? assets = null)
    {
        foreach (var handler in CommandHandlers(assets))
            commands.Register(handler);

        foreach (var handler in QueryHandlers())
            queries.Register(handler);
    }
}
