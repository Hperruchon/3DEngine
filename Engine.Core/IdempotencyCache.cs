using Engine.Contracts;

namespace Engine.Core;

// FIFO-evicting cache of CommandResult keyed on CommandId.
// Per ADR-0006 §7: V1 default capacity 1024. A duplicate CommandId returns
// the cached CommandResult without re-executing. Purpose is transport-retry
// safety (ADR-0006 §7), not intentional repeats — clients reuse a CommandId
// only when they want the prior answer.
//
// Thread-safety: not required. CommandBus.Apply is serial via SemaphoreSlim
// (ADR-0006 §2); this cache is accessed only from inside that serial section.
public sealed class IdempotencyCache
{
    public const int DefaultCapacity = 1024;

    private readonly int _capacity;
    private readonly Dictionary<Guid, CommandResult> _byId;
    private readonly Queue<Guid> _insertionOrder;

    public IdempotencyCache(int capacity = DefaultCapacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

        _capacity = capacity;
        _byId = new Dictionary<Guid, CommandResult>(capacity);
        _insertionOrder = new Queue<Guid>(capacity);
    }

    public int Capacity => _capacity;

    public int Count => _byId.Count;

    public bool TryGet(Guid commandId, out CommandResult result)
    {
        if (_byId.TryGetValue(commandId, out var cached))
        {
            result = cached;
            return true;
        }

        result = null!;
        return false;
    }

    public void Store(Guid commandId, CommandResult result)
    {
        if (_byId.ContainsKey(commandId))
            return;

        if (_insertionOrder.Count >= _capacity)
        {
            var evicted = _insertionOrder.Dequeue();
            _byId.Remove(evicted);
        }

        _byId[commandId] = result;
        _insertionOrder.Enqueue(commandId);
    }
}
