using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Core.Systems;

/// <summary>
/// The frame's own identity for a compiled system - <c>dnd5e</c>, for example - carried by
/// <see cref="Campaigns.Campaign.SystemId"/>, <see cref="Campaigns.CampaignSummary.SystemId"/> and
/// everything that matches a campaign's recorded system against a compiled one.
/// <para>
/// Deliberately its own type rather than a reuse of the entry library's own content id type: the
/// frame is not supposed to reference that namespace at all (docs/architecture.md, "Rama, biblioteka,
/// system" - "system przestaje być dla ramy katalogiem typów"), and a shared type would be exactly
/// that reference kept alive by a back door. The two ids happen to be spelled the same for today's one
/// system (<c>Dnd5eSystem</c> mints both from the same literal), but nothing here assumes they must
/// be - a system is free to pick a different content-set id than its own frame identity.
/// </para>
/// <para>
/// Same charset and same round-trip shape as <c>ContentId</c> (a plain JSON string, via
/// <see cref="SystemIdJsonConverter"/>), for the same reasons: an id is a declared identity, safe to
/// use as a key, and the campaign manifest's own <c>"system"</c> field is a plain string this type
/// must read and write without changing its shape by a single byte.
/// </para>
/// </summary>
[JsonConverter(typeof(SystemIdJsonConverter))]
public readonly record struct SystemId
{
    public const int MaxLength = 64;

    private SystemId(string value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentException($"Invalid system id: '{value}'.", nameof(value));
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

    public static bool TryCreate(string? candidate, out SystemId id)
    {
        id = IsValid(candidate) ? new SystemId(candidate!) : default;

        return id.Value is not null;
    }

    public static SystemId Create(string? candidate) =>
        TryCreate(candidate, out var id)
            ? id
            : throw new ArgumentException($"Invalid system id: '{candidate}'.", nameof(candidate));

    public override string ToString() => Value ?? string.Empty;

    private static bool IsAllowed(char character) =>
        character is >= 'a' and <= 'z'
        || character is >= '0' and <= '9'
        || character is '.' or '-';
}

/// <summary>Reads and writes a <see cref="SystemId"/> as the plain string it wraps, never as an object.</summary>
public sealed class SystemIdJsonConverter : JsonConverter<SystemId>
{
    public override SystemId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var candidate = reader.GetString();

        return SystemId.TryCreate(candidate, out var id)
            ? id
            : throw new JsonException($"Invalid system id: '{candidate}'.");
    }

    public override void Write(Utf8JsonWriter writer, SystemId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
