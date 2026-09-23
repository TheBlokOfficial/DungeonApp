using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Library.Entries;

/// <summary>
/// One instance's permanent identity. Deliberately separate from both the entry it points at and
/// the label the GM gives it: two instances of the same entry are still two different things, and
/// a relabelled instance is still the same one. It has to survive every rename and every edit to
/// the patch, so it is never derived from either.
/// <para>
/// <see cref="InstanceIdJsonConverter"/> is what lets this round-trip as the plain GUID string a
/// state file stores - <c>"id": "…"</c>, not <c>{"value":"…"}</c> - with no hand-written DTO.
/// </para>
/// </summary>
[JsonConverter(typeof(InstanceIdJsonConverter))]
public readonly record struct InstanceId(Guid Value)
{
    public static InstanceId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

/// <summary>Reads and writes an <see cref="InstanceId"/> as the plain GUID string it wraps, never as an object.</summary>
public sealed class InstanceIdJsonConverter : JsonConverter<InstanceId>
{
    public override InstanceId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var candidate = reader.GetString();

        return Guid.TryParse(candidate, out var value)
            ? new InstanceId(value)
            : throw new JsonException($"Invalid instance id: '{candidate}'.");
    }

    public override void Write(Utf8JsonWriter writer, InstanceId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
