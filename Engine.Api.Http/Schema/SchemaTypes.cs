using Engine.Contracts.Schema;

namespace Engine.Api.Http.Schema;

// Wire shapes for the /schema/* discovery endpoints.
// Per ADR-0008 §9, TASK-0009 §1–7, and ADR-0013 §2.
//
// FieldSchema moved to Engine.Contracts/Schema/FieldSchema.cs per ADR-0013 §2
// so handlers can declare schemas. The wire types here project from the
// handler's declarations.

internal sealed record CommandSchemaIndexEntry(string Name, int SchemaVersion);

internal sealed record CommandSchemaItem(
    string Name,
    int SchemaVersion,
    IReadOnlyDictionary<string, FieldSchema> Parameters,
    IReadOnlyDictionary<string, FieldSchema> Outputs);

internal sealed record QuerySchemaIndexEntry(string Name, int SchemaVersion);

internal sealed record QuerySchemaItem(
    string Name,
    int SchemaVersion,
    IReadOnlyDictionary<string, FieldSchema> Parameters,
    IReadOnlyDictionary<string, FieldSchema> Result);

internal sealed record EventKindEntry(string Kind);

internal sealed record DiagnosticCodeEntry(string Code, string Severity, string Subsystem);
