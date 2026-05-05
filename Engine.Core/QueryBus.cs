using System.Diagnostics;
using Engine.Contracts;

namespace Engine.Core;

// Per ADR-0008 §6: queries are read-only, snapshot-consistent, never logged, never emit events.
// In P0 the registry is empty by design; every query is rejected with E-QRY-UNKNOWN.
public sealed class QueryBus
{
    private readonly Document _document;
    private readonly QueryRegistry _registry;

    public QueryBus(Document document, QueryRegistry registry)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public Task<QueryResult<T>> Query<T>(Query query, CancellationToken ct = default)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        var stopwatch = Stopwatch.StartNew();

        if (!_registry.TryFind(query.Name, query.SchemaVersion, out _))
        {
            var error = new ErrorDetail(
                DiagnosticCodes.QueryUnknown,
                $"No handler registered for query '{query.Name}'@{query.SchemaVersion}.");
            return Task.FromResult(new QueryResult<T>(
                QueryName: query.Name,
                AsOfDocumentVersion: _document.Version,
                Result: default,
                Diagnostics: Array.Empty<Diagnostic>(),
                Error: error,
                DurationMs: stopwatch.ElapsedMilliseconds));
        }

        // No concrete query in P0 — handler dispatch lands with the first concrete query (post-P0).
        throw new NotImplementedException(
            "Concrete query dispatch is deferred. Empty registry per TASK-0001 §Scope.");
    }
}
