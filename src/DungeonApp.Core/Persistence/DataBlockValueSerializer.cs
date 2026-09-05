using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using DungeonApp.Core.DataBlocks;

namespace DungeonApp.Core.Persistence;

/// <summary>
/// Turns a data block's in-memory value into JSON and back, strictly following the block's declared
/// <see cref="DataBlockShape"/> in both directions.
/// <para>
/// The shape is the only source of truth here - not what a value happens to be in memory when
/// writing, and not what a JSON token happens to look like when reading. That second half is the
/// point: JSON has no wire-level distinction between an integer and a fractional number, so reading a
/// value back "as whatever the parser produced" would silently turn a stored integer into a double.
/// Reading it back "as whatever the shape says this field is" is what keeps a value's CLR type stable
/// across a save and a reload.
/// </para>
/// <para>
/// Lives in <c>Core.Persistence</c>, not <c>Core.DataBlocks</c>, so the data block types themselves
/// stay unaware that JSON exists.
/// </para>
/// </summary>
internal static class DataBlockValueSerializer
{
    public static JsonNode? ToNode(object value, DataBlockShape shape) => shape switch
    {
        PrimitiveShape { Kind: PrimitiveKind.Integer } => JsonValue.Create(Convert.ToInt64(value)),
        PrimitiveShape { Kind: PrimitiveKind.Fractional } => JsonValue.Create(Convert.ToDouble(value)),
        PrimitiveShape { Kind: PrimitiveKind.Text } => JsonValue.Create((string)value),
        PrimitiveShape { Kind: PrimitiveKind.Boolean } => JsonValue.Create((bool)value),
        ObjectShape objectShape => ToObjectNode(value, objectShape),
        _ => throw new NotSupportedException($"Data block shape '{shape.GetType().Name}' has no serializer."),
    };

    public static object FromNode(JsonNode? node, DataBlockShape shape) => shape switch
    {
        PrimitiveShape { Kind: PrimitiveKind.Integer } => RequireNode(node, shape).GetValue<long>(),
        PrimitiveShape { Kind: PrimitiveKind.Fractional } => RequireNode(node, shape).GetValue<double>(),
        PrimitiveShape { Kind: PrimitiveKind.Text } => RequireNode(node, shape).GetValue<string>(),
        PrimitiveShape { Kind: PrimitiveKind.Boolean } => RequireNode(node, shape).GetValue<bool>(),
        ObjectShape objectShape => FromObjectNode(node, objectShape),
        _ => throw new NotSupportedException($"Data block shape '{shape.GetType().Name}' has no serializer."),
    };

    private static JsonObject ToObjectNode(object value, ObjectShape shape)
    {
        var dictionary = (IReadOnlyDictionary<string, object>)value;
        var node = new JsonObject();

        foreach (var field in shape.Fields)
        {
            node[field.Name] = ToNode(dictionary[field.Name], field.Type);
        }

        return node;
    }

    private static object FromObjectNode(JsonNode? node, ObjectShape shape)
    {
        var obj = node as JsonObject
            ?? throw new FormatException("Expected a JSON object for an object-shaped data block value.");

        var result = new Dictionary<string, object>();

        foreach (var field in shape.Fields)
        {
            if (!obj.TryGetPropertyValue(field.Name, out var fieldNode))
            {
                throw new FormatException($"Stored value is missing field '{field.Name}'.");
            }

            result[field.Name] = FromNode(fieldNode, field.Type);
        }

        return result;
    }

    private static JsonNode RequireNode(JsonNode? node, DataBlockShape shape) =>
        node ?? throw new FormatException($"Stored value for shape '{shape.GetType().Name}' is null.");
}
