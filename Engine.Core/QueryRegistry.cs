using Engine.Contracts.Handlers;

namespace Engine.Core;

public sealed class QueryRegistry
{
    private readonly Dictionary<(string Name, int SchemaVersion), IQueryHandler> _handlers = new();

    public void Register(IQueryHandler handler)
    {
        var key = (handler.QueryName, handler.SchemaVersion);
        if (_handlers.ContainsKey(key))
            throw new InvalidOperationException(
                $"Handler for query '{handler.QueryName}'@{handler.SchemaVersion} already registered.");
        _handlers[key] = handler;
    }

    public bool TryFind(string name, int schemaVersion, out IQueryHandler handler)
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
}
