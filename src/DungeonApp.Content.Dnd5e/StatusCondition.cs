using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// A D&amp;D 5e condition's values (Powalony, Oślepiony): knowledge the GM reads at the table. The
/// entry carries no logic - nothing applies, counts or ends a condition by reading it.
/// <para>
/// <see cref="Summary"/> and <see cref="Icon"/> are what a condition shows wherever it stands as one
/// row (an icon, the name, the summary); <see cref="Rules"/> is the full text the card shows under them.
/// </para>
/// <para>
/// Named "StatusCondition", not "Condition": every public type name here becomes a word Core and
/// Desktop may not use (<c>CoreEntryKindIndependenceTests</c>), and "Condition" is a plain word of
/// their plumbing (<c>JsonIgnoreCondition</c>).
/// </para>
/// </summary>
public sealed record StatusCondition : IJsonOnDeserialized
{
    /// <summary>
    /// The condition in one or two sentences, plain text - under the name on the card, and the line a
    /// condition is recognised by at a glance.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// The condition's rules, written out the way a creature's sections are: an introduction and, when
    /// the rules come in named parts (Wyczerpanie's levels), its entries; emphasis marked by the pack
    /// (<c>**…**</c>).
    /// </summary>
    public required StatblockSection Rules { get; init; }

    /// <summary>
    /// The condition's icon: a one-colour picture (its shape on a transparent background), PNG, JPEG
    /// or WebP, as a path relative to this entry's pack. Only its shape is shown, in the theme's icon
    /// color. Declared to the loader as this type's picture (<see cref="Dnd5eSystem"/>).
    /// </summary>
    public string? Icon { get; init; }

    void IJsonOnDeserialized.OnDeserialized()
    {
        // "required" only demands the key; a blank summary would draw a nameless row's worth of
        // nothing under the name, so it is refused like a missing one.
        if (Summary is not null && string.IsNullOrWhiteSpace(Summary))
        {
            throw new JsonException("\"summary\" must not be empty.");
        }
    }
}
