using Engine.Contracts.Handlers;

namespace Engine.Core;

public sealed class CommandRegistry
{
    private readonly Dictionary<(string Name, int SchemaVersion), ICommandHandler> _handlers = new();

    public void Register(ICommandHandler handler)
    {
        var key = (handler.CommandName, handler.SchemaVersion);
        if (_handlers.ContainsKey(key))
            throw new InvalidOperationException(
                $"Handler for command '{handler.CommandName}'@{handler.SchemaVersion} already registered.");
        _handlers[key] = handler;
    }

    public bool TryFind(string name, int schemaVersion, out ICommandHandler handler)
    {
        if (_handlers.TryGetValue((name, schemaVersion), out var found))
        {
            handler = found;
            return true;
        }
        handler = null!;
        return false;
    }

    public int Count => _handlers.Count;

    public IReadOnlyCollection<(string Name, int SchemaVersion)> Registered
        => _handlers.Keys.ToArray();

    // Per ADR-0013 §3: schema endpoints project from the handler's declared
    // Parameters/Outputs. The endpoint needs the actual handler instance,
    // not just the (name, version) tuple.
    public IReadOnlyCollection<ICommandHandler> Handlers => _handlers.Values.ToArray();
}
