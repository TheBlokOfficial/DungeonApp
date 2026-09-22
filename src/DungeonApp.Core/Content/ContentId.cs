using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Core.Content;

/// <summary>
/// The permanent identifier of a pack, a system, a content type, or an entry, such as
/// <c>dnd5e</c> or <c>fifth-edition</c>.
/// <para>
/// The character set is deliberately narrow, for two reasons. First, an id is a document's declared
/// identity, not the name of the directory it happens to sit in - the loader never trusts a folder
/// name - so a narrow, filesystem-safe charset keeps that identity safe to use as a key everywhere
/// an id-derived path might still show up later (a registry cache, an exported bundle). Second, and
/// just as load bearing here: a colon is not in this charset, which is exactly what lets
/// <see cref="ContentTypeReference"/> and <see cref="EntryAddress"/> split <c>"pack:id"</c> on the
/// first colon without ambiguity.
/// </para>
/// <para>
/// <see cref="ContentIdJsonConverter"/> is what lets a state record such as <c>CampaignInstance</c>
/// carry a <see cref="ContentId"/> (inside an <c>EntryAddress</c>) and still round-trip as a plain
/// JSON string - <c>"dnd5e"</c>, not <c>{"value":"dnd5e"}</c> - with no hand-written DTO standing in
/// for it. It is the only validator that read ever gets: a value the charset rejects fails as a
/// <see cref="JsonException"/>, the same shape every other invalid record already fails as.
/// </para>
/// </summary>
[JsonConverter(typeof(ContentIdJsonConverter))]
public readonly record struct ContentId
{
    public const int MaxLength = 64;

    private ContentId(string value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentException($"Invalid content id: '{value}'.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public static bool IsValid(string? candidate) =>
        !string.IsNullOrEmpty(candidate)
        && candidate.Length <= MaxLength
        && candidate.All(IsAllowed)
        && !candidate.StartsWith('.')
        && !candidate.EndsWith('.');

    public static bool TryCreate(string? candidate, out ContentId id)
    {
        id = IsValid(candidate) ? new ContentId(candidate!) : default;

        return id.Value is not null;
    }

    public static ContentId Create(string? candidate) =>
        TryCreate(candidate, out var id)
            ? id
            : throw new ArgumentException($"Invalid content id: '{candidate}'.", nameof(candidate));

    public override string ToString() => Value ?? string.Empty;

    private static bool IsAllowed(char character) =>
        character is >= 'a' and <= 'z'
        || character is >= '0' and <= '9'
        || character is '.' or '-';
}

/// <summary>Reads and writes a <see cref="ContentId"/> as the plain string it wraps, never as an object.</summary>
public sealed class ContentIdJsonConverter : JsonConverter<ContentId>
{
    public override ContentId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var candidate = reader.GetString();

        return ContentId.TryCreate(candidate, out var id)
            ? id
            : throw new JsonException($"Invalid content id: '{candidate}'.");
    }

    public override void Write(Utf8JsonWriter writer, ContentId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
