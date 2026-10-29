namespace Engine.Contracts;

// ADR-0037 item 6: the reason that the last rebuild of a body failed. The body keeps its
// last solid. Code is a W- code of docs/diagnostics.md; Message names the reason.
public sealed record BodyFault(string Code, string Message);
