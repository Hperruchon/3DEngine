using System.Diagnostics;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core.Geometry;

namespace Engine.Core;

// Per ADR-0008 §6: queries are read-only, snapshot-consistent, never logged, never emit events.
// Per ADR-0012 §2: the bus passes the active backend to handlers, same rationale as CommandBus.
public sealed class QueryBus
{
    private readonly Document _document;
    private readonly QueryRegistry _registry;
    private readonly IGeometryBackend _backend;

    public QueryBus(Document document, QueryRegistry registry, IGeometryBackend? backend = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _backend = backend ?? NullGeometryBackend.Instance;
    }

    public async Task<QueryResult<T>> Query<T>(Query query, CancellationToken ct = default)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        var stopwatch = Stopwatch.StartNew();

        if (!_registry.TryFind(query.Name, query.SchemaVersion, out var handler))
        {
            var error = new ErrorDetail(
                DiagnosticCodes.QueryUnknown,
                $"No handler registered for query '{query.Name}'@{query.SchemaVersion}.");
            return new QueryResult<T>(
                QueryName: query.Name,
                AsOfDocumentVersion: _document.Version,
                Result: default,
                Diagnostics: Array.Empty<Diagnostic>(),
                Error: error,
                DurationMs: stopwatch.ElapsedMilliseconds);
        }

        var handlerResult = await handler.Handle(query, _document, _backend, ct).ConfigureAwait(false);

        // Box-to-T cast. Failure here is a programmer error (caller asked for
        // the wrong T); surface as an error rather than throwing through to
        // the transport surface. Default(T) on null Result is intentional
        // when the handler legitimately returned null.
        T? typed = default;
        if (handlerResult.IsSuccess && handlerResult.Result is not null)
        {
            if (handlerResult.Result is T cast)
                typed = cast;
            else
                return new QueryResult<T>(
                    QueryName: query.Name,
                    AsOfDocumentVersion: _document.Version,
                    Result: default,
                    Diagnostics: handlerResult.Diagnostics,
                    Error: new ErrorDetail(
                        DiagnosticCodes.QueryUnknown,
                        $"Query '{query.Name}' returned {handlerResult.Result.GetType().Name}, " +
                        $"but caller requested {typeof(T).Name}."),
                    DurationMs: stopwatch.ElapsedMilliseconds);
        }

        return new QueryResult<T>(
            QueryName: query.Name,
            AsOfDocumentVersion: _document.Version,
            Result: typed,
            Diagnostics: handlerResult.Diagnostics,
            Error: handlerResult.Error,
            DurationMs: stopwatch.ElapsedMilliseconds);
    }
}
