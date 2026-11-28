using Engine.Contracts;

namespace Engine.Core;

// ADR-0192 (phase K25, TASK-0478): the live bodies in the order of the log, the place in Document.Log of the command that
// made each one (ADR-0192 item 1). Document.Bodies gives the values of a .NET Dictionary, whose order changes when a command
// removes a record and the next record takes its free place, so each answer that lists the bodies takes this order.
public static class LogOrder
{
    // Each live body in the order of the command that made it. The identifier of a body is the identifier of its command
    // (ADR-0012 §4), and the log holds each command once.
    public static IReadOnlyList<BodyRecord> Bodies(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var live = document.Bodies.ToDictionary(body => body.Handle.Id);
        var bodies = new List<BodyRecord>(live.Count);
        foreach (var entry in document.Log)
        {
            if (live.Remove(entry.Command.CommandId, out var body))
                bodies.Add(body);
        }

        // No body has another handle today; such a body still comes, after the others, in the order of its identifier.
        bodies.AddRange(live.Values.OrderBy(body => body.Handle.Id));
        return bodies;
    }
}
