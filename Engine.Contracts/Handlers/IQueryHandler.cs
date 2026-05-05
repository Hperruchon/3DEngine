namespace Engine.Contracts.Handlers;

public interface IQueryHandler
{
    string QueryName { get; }
    int SchemaVersion { get; }
}

public interface IQueryHandler<in TQuery, TResult> : IQueryHandler
    where TQuery : Query
{
    Task<QueryHandlerResult<TResult>> Handle(TQuery query, Document document, CancellationToken ct);
}

public sealed record QueryHandlerResult<T>(
    T? Result,
    IReadOnlyList<Diagnostic> Diagnostics,
    ErrorDetail? Error);
