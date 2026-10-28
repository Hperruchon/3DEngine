using Engine.Contracts;

namespace Engine.Core.Persistence;

// ADR-0015 item 11: a handler turns its own command back into the parameters that
// its Create method reads, with the names and the types of its declaration
// (ADR-0013). The interface belongs to Engine.Core, so ICommandHandler does not
// change. Each handler of HandlerCatalog implements it, and a test holds that
// Create(Encode(c)) gives c.
public interface ICommandEncoder
{
    // The value of each parameter: a double, a long, a string, a bool or a Guid. An
    // optional parameter with no value is absent or null.
    IReadOnlyDictionary<string, object?> Encode(Command command);
}
