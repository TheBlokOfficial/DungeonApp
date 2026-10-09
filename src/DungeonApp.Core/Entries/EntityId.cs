using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Core.Entries;

/// <summary>
/// One entity's permanent identity. Deliberately separate from both the entry it points at and
/// the label the GM gives it: two entities of the same entry are still two different things, and
/// a relabelled entity is still the same one. It has to survive every rename and every edit to
/// the patch, so it is never derived from either.
/// <para>
/// <see cref="EntityIdJsonConverter"/> is what lets this round-trip as the plain GUID string a
/// state file stores - <c>"id": "…"</c>, not <c>{"value":"…"}</c> - with no hand-written DTO.
/// </para>
/// </summary>
[JsonConverter(typeof(EntityIdJsonConverter))]
public readonly record struct EntityId(Guid Value)
{
    public static EntityId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

/// <summary>Reads and writes an <see cref="EntityId"/> as the plain GUID string it wraps, never as an object.</summary>
public sealed class EntityIdJsonConverter : JsonConverter<EntityId>
{
    public override EntityId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var candidate = reader.GetString();

        return Guid.TryParse(candidate, out var value)
            ? new EntityId(value)
            : throw new JsonException($"Invalid entity id: '{candidate}'.");
    }

    public override void Write(Utf8JsonWriter writer, EntityId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
