using System;
using System.Linq;

namespace DungeonApp.Core.Content;

/// <summary>
/// The name of one field inside a template, such as <c>kpZrodlo</c> or <c>cechySzczegolne</c>.
/// <para>
/// Deliberately a different charset than <see cref="ContentId"/>. A field name never becomes a
/// file name or half of a colon-separated reference - it only ever addresses a slot inside a JSON
/// object - so the restriction here is about keeping it a plain identifier, not about path safety.
/// Mixed case is allowed because pack authors write field names as camelCase words, and a leading
/// digit is refused only so a field name can never be confused with a bare number.
/// </para>
/// </summary>
public readonly record struct FieldName
{
    public const int MaxLength = 64;

    private FieldName(string value) => Value = value;

    public string Value { get; }

    public static bool IsValid(string? candidate) =>
        !string.IsNullOrEmpty(candidate)
        && candidate.Length <= MaxLength
        && IsLetter(candidate[0])
        && candidate.All(IsAllowed);

    public static bool TryCreate(string? candidate, out FieldName name)
    {
        name = IsValid(candidate) ? new FieldName(candidate!) : default;

        return name.Value is not null;
    }

    public static FieldName Create(string? candidate) =>
        TryCreate(candidate, out var name)
            ? name
            : throw new ArgumentException($"Invalid field name: '{candidate}'.", nameof(candidate));

    public override string ToString() => Value ?? string.Empty;

    private static bool IsAllowed(char character) => IsLetter(character) || IsDigit(character);

    private static bool IsLetter(char character) =>
        character is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

    private static bool IsDigit(char character) => character is >= '0' and <= '9';
}
