using System;
using System.Linq;

namespace DungeonApp.Core.Modules;

/// <summary>
/// A module's permanent identifier, such as <c>core.clock</c>.
/// <para>
/// The character set is deliberately narrow because this value becomes a file name inside the
/// campaign directory. Lowercase only: a case-insensitive filesystem would let <c>Core.Clock</c>
/// and <c>core.clock</c> collide on disk while looking distinct in code.
/// </para>
/// </summary>
public readonly record struct ModuleId
{
    public const int MaxLength = 64;

    private ModuleId(string value) => Value = value;

    public string Value { get; }

    public static bool IsValid(string? candidate) =>
        !string.IsNullOrEmpty(candidate)
        && candidate.Length <= MaxLength
        && candidate.All(IsAllowed)
        && !candidate.StartsWith('.')
        && !candidate.EndsWith('.');

    public static bool TryCreate(string? candidate, out ModuleId id)
    {
        id = IsValid(candidate) ? new ModuleId(candidate!) : default;

        return id.Value is not null;
    }

    public static ModuleId Create(string? candidate) =>
        TryCreate(candidate, out var id)
            ? id
            : throw new ArgumentException($"Invalid module id: '{candidate}'.", nameof(candidate));

    public override string ToString() => Value ?? string.Empty;

    private static bool IsAllowed(char character) =>
        character is >= 'a' and <= 'z'
        || character is >= '0' and <= '9'
        || character is '.' or '-';
}
