using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// One block of a creature's rules prose - its special abilities, actions, spellcasting, bonus
/// actions, reactions or legendary actions - and a condition's rules (<see cref="StatusCondition.Rules"/>):
/// an optional paragraph that introduces the block, then its named entries. Every such block has this
/// one shape, so the cards draw them all the same way.
/// <para>
/// The structure is the pack author's, not something the application reads out of the prose: the
/// card never guesses where a name ends or which parenthesis is a usage note. It exists only for
/// reading - nothing computes with, filters or sorts by any part of it.
/// </para>
/// <para>
/// The deserializer validates it, as it validates the rest of the record: a block with neither an
/// introduction nor an entry says nothing and is refused (<see cref="IJsonOnDeserialized"/>), so a
/// pack author who left it half-written sees that entry rejected instead of an empty heading.
/// </para>
/// </summary>
public sealed record StatblockSection : IJsonOnDeserialized
{
    /// <summary>
    /// The paragraph before the entries - a spellcasting preamble, the legendary actions' allowance.
    /// May contain <c>**…**</c> emphasis.
    /// </summary>
    public string? Intro { get; init; }

    /// <summary>
    /// Required even when empty (<c>"entries": []</c> under an introduction alone): a key the reader
    /// filled in by itself would be written back out when an entity's patch is diffed against its
    /// entry, and would show up in the patch as a deviation nobody made.
    /// </summary>
    public required IReadOnlyList<StatblockEntry> Entries { get; init; }

    void IJsonOnDeserialized.OnDeserialized()
    {
        if (Entries is null)
        {
            throw new JsonException("A section's \"entries\" must be a list, not null.");
        }

        if (string.IsNullOrWhiteSpace(Intro) && Entries.Count == 0)
        {
            throw new JsonException("A section needs an \"intro\" or at least one entry in \"entries\".");
        }
    }
}

/// <summary>
/// One named entry of a <see cref="StatblockSection"/>: "Kopyta", "Leczący dotyk" with its note
/// "3 na dzień", "Bez ograniczeń" with a list of spells. <see cref="Note"/> is a short label the card
/// shows muted after the name - text the GM reads.
/// <para>
/// <see cref="Name"/> and <see cref="Text"/> are both required and must say something: an entry with
/// either missing, null or blank is refused when its pack loads.
/// </para>
/// </summary>
public sealed record StatblockEntry : IJsonOnDeserialized
{
    public required string Name { get; init; }

    public string? Note { get; init; }

    /// <summary>The entry's rules text. May contain <c>**…**</c> emphasis.</summary>
    public required string Text { get; init; }

    void IJsonOnDeserialized.OnDeserialized()
    {
        // "required" only demands the key; a null or blank value would slip through it and draw
        // a nameless entry, so the shape check finishes the job here.
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new JsonException("Every entry of a section needs a non-empty \"name\".");
        }

        if (string.IsNullOrWhiteSpace(Text))
        {
            throw new JsonException($"Entry \"{Name}\" needs a non-empty \"text\".");
        }
    }
}
