using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// Orders known values by a fixed rank this system declares, and puts every other value after all
/// of them, alphabetically - backs the "Rzadkość" filter's option order (krok 10, zlecenie 1,
/// część C): a named rarity tier orders by its place in <c>RarityTiers</c>, anything the list does
/// not recognise falls to the end rather than sorting arbitrarily among the known tiers.
/// </summary>
internal sealed class RankedTextComparer(IReadOnlyList<string> knownInOrder) : IComparer<string>
{
    private readonly IReadOnlyDictionary<string, int> _rank = knownInOrder
        .Select((value, index) => (value, index))
        .ToDictionary(pair => pair.value, pair => pair.index, StringComparer.Ordinal);

    public int Compare(string? x, string? y)
    {
        var hasX = _rank.TryGetValue(x ?? string.Empty, out var rankX);
        var hasY = _rank.TryGetValue(y ?? string.Empty, out var rankY);

        if (hasX && hasY)
        {
            return rankX.CompareTo(rankY);
        }

        if (hasX != hasY)
        {
            return hasX ? -1 : 1;
        }

        return string.CompareOrdinal(x, y);
    }
}
