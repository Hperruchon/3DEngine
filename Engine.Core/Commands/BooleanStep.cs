using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Kernel;

namespace Engine.Core.Commands;

// ADR-0181 item 2 (phase K16, TASK-0433): the steps of SubtractCommandHandler, in the same order and with the same codes,
// for the union and the intersection. Subtract keeps its own handler. The verb names the operation in each message.
internal static class BooleanStep
{
    // The two operands of a union or an intersection, as a command gives them.
    public readonly record struct Operands(Guid CommandId, Guid First, Guid Second);

    public static Task<CommandHandlerResult> Handle(
        Document document,
        IGeometryBackend backend,
        Operands operands,
        string verb,
        Action<IBooleanOps, BodyHandle, BodyHandle, BodyHandle> backendStep,
        Func<KernelSolid, KernelSolid, KernelSolid?> kernelStep)
    {
        // 1. Both operands must be live in the Document projection, before the capability check, so a bad reference has
        // the same code on each backend. A consumed body is not live (ADR-0021 item 4).
        var first = document.Bodies.FirstOrDefault(b => b.Handle.Id == operands.First);
        if (first is null)
            return Failure(DiagnosticCodes.GeomBodyNotFound, $"No body with id '{operands.First}' exists in the Document.");
        var second = document.Bodies.FirstOrDefault(b => b.Handle.Id == operands.Second);
        if (second is null)
            return Failure(DiagnosticCodes.GeomBodyNotFound, $"No body with id '{operands.Second}' exists in the Document.");

        // ADR-0181 item 2: a mesh body of an STL file takes no part in a boolean, as it takes no part in a cut.
        if (new[] { first, second }.FirstOrDefault(body => body.Kind == ImportStlCommandHandler.MeshKind) is { } meshBody)
            return Failure(DiagnosticCodes.MeshUnsupported, $"The body '{meshBody.Handle.Id}' is a mesh body of an STL file, which takes no part in a boolean (ADR-0181 item 2).");

        // The command consumes both operands, and ADR-0021 item 2 permits a handle one time in the consumed list, so a body
        // with itself is refused before the backend, and the body stays live.
        if (operands.First == operands.Second)
            return Failure(DiagnosticCodes.GeomInvalidParam, $"The first and the second body are the same body '{operands.First}'. Give two different bodies.");

        // 2. The backend must support booleans (the managed backend does not).
        var boolean = backend.TryGet<IBooleanOps>();
        if (boolean is null)
            return Failure(DiagnosticCodes.GeomCapMissing, $"Active backend does not implement IBooleanOps; cannot {verb} bodies.");

        // 3. ADR-0181 item 3: the exact result of the owned kernel. When the kernel holds both operands and the result has no
        // volume, the two bodies share no volume, and the command is refused before the backend. Manifold gives a flat solid
        // of no volume for two bodies that touch on a face, so its own check of an empty result is not enough. A body that
        // the kernel cannot build leaves the check to the backend.
        var a = Features.KernelProjection.OfRecord(document, first).Solid;
        var b = Features.KernelProjection.OfRecord(document, second).Solid;
        var exact = a is not null && b is not null ? kernelStep(a, b) : null;
        if (a is not null && b is not null && exact is null)
            return Failure(DiagnosticCodes.GeomNativeOp, $"The bodies '{operands.First}' and '{operands.Second}' share no volume, so the owned kernel gives no solid to {verb}.");

        // 4. The new handle comes from the CommandId (ADR-0012 §4), and the solids of the operands from their geometry keys
        // (ADR-0037 item 2). The commit consumes both operands (ADR-0021 item 3).
        var handle = new BodyHandle(operands.CommandId);
        try
        {
            backendStep(boolean, handle, new BodyHandle(first.GeometryKey), new BodyHandle(second.GeometryKey));
        }
        catch (Exception ex)
        {
            // The operands and the capability are valid, so a throw is a fault of the backend or an empty result.
            return Failure(DiagnosticCodes.GeomNativeOp, $"Geometry backend failed to {verb} the bodies: {ex.Message}");
        }

        var outputs = new Outputs(new Dictionary<string, object?> { ["bodyId"] = handle.Id });
        var (datums, warnings) = exact is null ? ([], []) : GoneFaces(document, first, second, handle.Id, exact);
        return Task.FromResult(CommandHandlerResult.Success(
            outputs,
            diagnostics: warnings,
            // ADR-0181 item 2: the result takes the name and the visibility of the first body, as a cut takes those of the
            // minuend (ADR-0028 item 5).
            createdBodies: new[] { new BodyRecord(handle, "Solid", first.Name, first.Visible) },
            consumedBodies: new[] { new BodyHandle(operands.First), new BodyHandle(operands.Second) },
            changedDatums: datums));
    }

    // ADR-0181 item 6: a datum on a face of either operand that the boolean removes gets a fault, as after a Subtract
    // (ADR-0059 item 3). When the kernel cannot build an operand or the result, no datum has a check.
    private static (IReadOnlyList<Engine.Contracts.Sketch.DatumRecord> Datums, IReadOnlyList<Diagnostic> Warnings) GoneFaces(
        Document document, BodyRecord first, BodyRecord second, Guid body, KernelSolid result)
    {
        var graph = Sketch.FeatureGraph.Of(document);
        var tags = Features.KernelProjection.Tags(result);
        var changed = new List<Engine.Contracts.Sketch.DatumRecord>();
        var warnings = new List<Diagnostic>();
        foreach (var datum in document.Datums.OrderBy(datum => graph.Order(datum.Id)))
        {
            var end = Features.KernelProjection.End(graph, datum.Reference.FeatureId).End;
            if (datum.Fault is not null || (end != first.Handle.Id && end != second.Handle.Id) || Sketch.FaceGone.Holds(tags, datum.Reference))
                continue;
            var fault = Sketch.FaceGone.Fault(datum, body);
            changed.Add(datum with { Fault = fault });
            warnings.Add(Sketch.FaceGone.Warning(datum, fault));
        }

        return (changed, warnings);
    }

    private static Task<CommandHandlerResult> Failure(string code, string message)
        => Task.FromResult(CommandHandlerResult.Failure(new ErrorDetail(code, message)));
}
