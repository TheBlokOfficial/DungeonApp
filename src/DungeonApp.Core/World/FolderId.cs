using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Core.World;

/// <summary>
/// One world folder's permanent identity, the same shape as <see cref="Entries.EntityId"/> and for
/// the same reason: a rename or a move never changes it. Round-trips as the plain GUID string a state
/// file stores.
/// </summary>
[JsonConverter(typeof(FolderIdJsonConverter))]
public readonly record struct FolderId(Guid Value)
{
    public static FolderId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

/// <summary>Reads and writes a <see cref="FolderId"/> as the plain GUID string it wraps, never as an object.</summary>
public sealed class FolderIdJsonConverter : JsonConverter<FolderId>
{
    public override FolderId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var candidate = reader.GetString();

        return Guid.TryParse(candidate, out var value)
            ? new FolderId(value)
            : throw new JsonException($"Invalid folder id: '{candidate}'.");
    }

    public override void Write(Utf8JsonWriter writer, FolderId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
