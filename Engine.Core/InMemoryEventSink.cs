using Engine.Contracts;

namespace Engine.Core;

// Bounded ring buffer per ADR-0005 §3 (default capacity 10_000).
// On overflow, evicts the oldest event. Surviving range stays contiguous in Seq
// because the engine never produces gaps.
public sealed class InMemoryEventSink : IEventSink
{
    public const int DefaultCapacity = 10_000;

    private readonly object _lock = new();
    private readonly Queue<EventRecord> _ring;
    private readonly int _capacity;

    public InMemoryEventSink(int capacity = DefaultCapacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

        _capacity = capacity;
        _ring = new Queue<EventRecord>(capacity);
    }

    public int Capacity => _capacity;

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _ring.Count;
            }
        }
    }

    public Task Append(EventRecord record, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_ring.Count >= _capacity)
                _ring.Dequeue();
            _ring.Enqueue(record);
        }
        return Task.CompletedTask;
    }

    public IReadOnlyList<EventRecord> Snapshot()
    {
        lock (_lock)
        {
            return _ring.ToArray();
        }
    }
}
