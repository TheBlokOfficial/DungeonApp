using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The item aspect: what carrying and trading an item reads - its weight and its worth. Data without
/// a look: <see cref="GearCardView"/> writes both at the end of the title's line. Every
/// <see cref="Gear"/> has it.
/// </summary>
public sealed record ItemAspect : IJsonOnDeserialized
{
    /// <summary>
    /// In kilograms; shown through <see cref="UnitScale.Weight"/>. Never negative: 0 is a weight like
    /// any other and the card writes it ("0 kg") rather than hiding it.
    /// </summary>
    public required decimal Weight { get; init; }

    /// <summary>
    /// The item's worth on an abstract, relative scale - a number with no unit, never negative, its
    /// scale not bounded either way. Not a price: a price belongs to a particular slot in an
    /// inventory or a shop. 0 is shown like any other worth.
    /// </summary>
    public required decimal Value { get; init; }

    void IJsonOnDeserialized.OnDeserialized()
    {
        // A negative worth or weight means nothing, so the entry is refused rather than drawn.
        if (Value < 0)
        {
            throw new JsonException("\"value\" cannot be negative.");
        }

        if (Weight < 0)
        {
            throw new JsonException("\"weight\" cannot be negative.");
        }
    }
}
