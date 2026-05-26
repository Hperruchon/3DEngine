namespace Engine.Contracts.Schema;

// Per ADR-0013 §2. V1 vocabulary for Type:
// "string", "integer", "number", "boolean", "object", "array", "guid", "datetime".
// Nested object/array shape (item type, properties) is deferred until a
// command needs it.
public sealed record FieldSchema(string Type, bool Required = false);
