using System;
using System.Linq;

namespace DungeonApp.Core.DataBlocks;

/// <summary>
/// A data block's permanent identifier, such as <c>party.roster</c>.
/// <para>
/// The character set mirrors <c>ModuleId</c> deliberately: a data block value is bound to become a
/// file name inside the campaign directory, same as a module's state. Lowercase only: a
/// case-insensitive filesystem would let <c>Party.Roster</c> and <c>party.roster</c> collide on
/// disk while looking distinct in code.
/// </para>
/// </summary>
public readonly record struct DataBlockId
{
    public const int MaxLength = 64;

    private DataBlockId(string value) => Value = value;

    public string Value { get; }

    public static bool IsValid(string? candidate) =>
        !string.IsNullOrEmpty(candidate)
        && candidate.Length <= MaxLength
        && candidate.All(IsAllowed)
        && !candidate.StartsWith('.')
        && !candidate.EndsWith('.');

    public static bool TryCreate(string? candidate, out DataBlockId id)
    {
        id = IsValid(candidate) ? new DataBlockId(candidate!) : default;

        return id.Value is not null;
    }

    public static DataBlockId Create(string? candidate) =>
        TryCreate(candidate, out var id)
            ? id
            : throw new ArgumentException($"Invalid data block id: '{candidate}'.", nameof(candidate));

    public override string ToString() => Value ?? string.Empty;

    private static bool IsAllowed(char character) =>
        character is >= 'a' and <= 'z'
        || character is >= '0' and <= '9'
        || character is '.' or '-';
}
