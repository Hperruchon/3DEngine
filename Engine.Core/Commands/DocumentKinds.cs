using Engine.Contracts;
using Engine.Contracts.Handlers;

namespace Engine.Core.Commands;

// ADR-0071 item 2 (phase P8b1, TASK-0202): the kinds of document in which a command applies. The interface belongs to
// Engine.Core, as ICommandEncoder does, so ICommandHandler does not change. A handler that does not implement it applies
// in a part only, so each command of a part needs no change, and an assembly refuses it.
public interface IDocumentKinds
{
    IReadOnlySet<string> Kinds { get; }
}

public static class DocumentKinds
{
    // The kinds of a command that applies in each document: Undo, Redo and NoOp.
    public static readonly IReadOnlySet<string> Each = new HashSet<string>(StringComparer.Ordinal) { Document.Part, Document.Assembly };

    public static readonly IReadOnlySet<string> AssemblyOnly = new HashSet<string>(StringComparer.Ordinal) { Document.Assembly };

    // True when the command of the handler applies in a document of the kind.
    public static bool Permits(ICommandHandler handler, string kind)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return handler is IDocumentKinds kinds ? kinds.Kinds.Contains(kind) : kind == Document.Part;
    }
}
