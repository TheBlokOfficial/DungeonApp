using System;
using System.Linq;

namespace DungeonApp.Core.Content;

/// <summary>
/// The permanent identifier of a pack, a template, or an entry, such as <c>dnd5e</c> or
/// <c>fifth-edition</c>.
/// <para>
/// The character set mirrors <see cref="DataBlocks.DataBlockId"/> exactly, and for the same two
/// reasons. First, an id is a document's declared identity, not the name of the directory it
/// happens to sit in - the loader never trusts a folder name - so the same narrow,
/// filesystem-safe charset keeps that identity safe to use as a key everywhere an id-derived path
/// might still show up later (a registry cache, an exported bundle). Second, and just as load
/// bearing here: a colon is not in this charset, which is exactly what lets
/// <see cref="TemplateReference"/> and <see cref="EntryAddress"/> split <c>"pack:id"</c> on the
/// first colon without ambiguity.
/// </para>
/// </summary>
public readonly record struct ContentId
{
    public const int MaxLength = 64;

    private ContentId(string value) => Value = value;

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
