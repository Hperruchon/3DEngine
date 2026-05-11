using Engine.Contracts;
using Engine.Core;

namespace Engine.Tests;

public class IdempotencyCacheTests
{
    private static CommandResult StubResult(string name = "NoOp")
        => new(
            CommandId: Guid.NewGuid(),
            CommandName: name,
            Status: CommandStatus.Applied,
            AppliedAtSeq: 1,
            DocumentVersion: 1,
            Outputs: Outputs.Empty,
            Diagnostics: Array.Empty<Diagnostic>(),
            Error: null,
            DurationMs: 0);

    [Fact]
    public void TryGet_Returns_False_For_Unknown_CommandId()
    {
        var cache = new IdempotencyCache();

        Assert.False(cache.TryGet(Guid.NewGuid(), out _));
    }

    [Fact]
    public void Store_Then_TryGet_Returns_Stored_Result()
    {
        var cache = new IdempotencyCache();
        var id = Guid.NewGuid();
        var result = StubResult();

        cache.Store(id, result);

        Assert.True(cache.TryGet(id, out var fetched));
        Assert.Same(result, fetched);
    }

    [Fact]
    public void Capacity_Is_Bounded_And_Evicts_Oldest_When_Full()
    {
        var cache = new IdempotencyCache(capacity: 2);
        var oldest = Guid.NewGuid();
        var middle = Guid.NewGuid();
        var newest = Guid.NewGuid();

        cache.Store(oldest, StubResult());
        cache.Store(middle, StubResult());
        cache.Store(newest, StubResult()); // evicts oldest

        Assert.False(cache.TryGet(oldest, out _));
        Assert.True(cache.TryGet(middle, out _));
        Assert.True(cache.TryGet(newest, out _));
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void Store_Is_Idempotent_For_Same_CommandId()
    {
        var cache = new IdempotencyCache(capacity: 2);
        var id = Guid.NewGuid();
        var first = StubResult();
        var second = StubResult();

        cache.Store(id, first);
        cache.Store(id, second); // no-op; first stays
        cache.Store(Guid.NewGuid(), StubResult());

        Assert.True(cache.TryGet(id, out var fetched));
        Assert.Same(first, fetched);
        Assert.Equal(2, cache.Count);
    }
}
