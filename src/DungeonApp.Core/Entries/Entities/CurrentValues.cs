using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Core.Entries.Entities;

/// <summary>
/// The current numbers of an entity's tracked values, by the key the system gave each one
/// (<see cref="TrackedValueRule.Key"/>). Sparse on purpose: a key that is missing means the system's
/// default (<see cref="TrackedValueRule.DefaultCurrent"/>), so an untouched pool follows its maximum
/// when a pack update changes it. Only a GM's change writes a key.
/// <para>
/// Equal by content, so two entity records with the same numbers are equal records; written as one
/// JSON object with keys in ordinal order, so a save is byte-for-byte repeatable.
/// </para>
/// </summary>
[JsonConverter(typeof(CurrentValuesJsonConverter))]
public sealed class CurrentValues : IReadOnlyDictionary<string, int>, IEquatable<CurrentValues>
{
    private readonly SortedDictionary<string, int> _values;

    private CurrentValues(SortedDictionary<string, int> values) => _values = values;

    public static CurrentValues Empty { get; } = new(new SortedDictionary<string, int>(StringComparer.Ordinal));

    public static CurrentValues From(IEnumerable<KeyValuePair<string, int>> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var sorted = new SortedDictionary<string, int>(StringComparer.Ordinal);

        foreach (var (key, value) in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            sorted.Add(key, value);
        }

        return new CurrentValues(sorted);
    }

    /// <summary>A copy with <paramref name="key"/> set to <paramref name="value"/>.</summary>
    public CurrentValues With(string key, int value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var copy = new SortedDictionary<string, int>(_values, StringComparer.Ordinal) { [key] = value };
        return new CurrentValues(copy);
    }

    public int this[string key] => _values[key];

    public IEnumerable<string> Keys => _values.Keys;

    public IEnumerable<int> Values => _values.Values;

    public int Count => _values.Count;

    public bool ContainsKey(string key) => _values.ContainsKey(key);

    public bool TryGetValue(string key, out int value) => _values.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(CurrentValues? other) =>
        other is not null && (ReferenceEquals(this, other) || _values.SequenceEqual(other._values));

    public override bool Equals(object? obj) => Equals(obj as CurrentValues);

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var (key, value) in _values)
        {
            hash.Add(key, StringComparer.Ordinal);
            hash.Add(value);
        }

        return hash.ToHashCode();
    }
}

/// <summary>
/// Reads and writes <see cref="CurrentValues"/> as a flat object of whole numbers. Strict like every
/// other state record: anything but an object of integers, or a key given twice, is refused.
/// </summary>
public sealed class CurrentValuesJsonConverter : JsonConverter<CurrentValues>
{
    public override CurrentValues Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Bieżące wartości muszą być obiektem.");
        }

        var values = new SortedDictionary<string, int>(StringComparer.Ordinal);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var key = reader.GetString();

            if (string.IsNullOrWhiteSpace(key) || !reader.Read() || reader.TokenType != JsonTokenType.Number
                || !reader.TryGetInt32(out var value))
            {
                throw new JsonException($"Bieżąca wartość „{key}” musi być liczbą całkowitą.");
            }

            if (!values.TryAdd(key, value))
            {
                throw new JsonException($"Bieżąca wartość „{key}” występuje dwa razy.");
            }
        }

        return CurrentValues.From(values);
    }

    public override void Write(Utf8JsonWriter writer, CurrentValues value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var (key, number) in value)
        {
            writer.WriteNumber(key, number);
        }

        writer.WriteEndObject();
    }
}
