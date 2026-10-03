using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// One unit of a <see cref="UnitScale"/>: its symbol and how many base units it is worth.
/// </summary>
public sealed record ScaleUnit(string Symbol, decimal Multiplier);

/// <summary>
/// A scale a quantity is shown in: units, each a multiple of the scale's base unit. A quantity is
/// written in the largest unit it is a whole number of; when it is whole in none, in the smallest
/// unit with its fraction. Another set of units - pounds, say - is another list here, not another
/// formatter.
/// </summary>
public sealed class UnitScale
{
    // A non-breaking space keeps a number and its unit on one line.
    private const char UnitSeparator = ' ';

    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    private readonly IReadOnlyList<ScaleUnit> _units;

    public UnitScale(IEnumerable<ScaleUnit> units)
    {
        ArgumentNullException.ThrowIfNull(units);

        _units = [.. units.OrderByDescending(unit => unit.Multiplier)];
        if (_units.Count == 0 || _units.Any(unit => unit.Multiplier <= 0))
        {
            throw new ArgumentException("A scale needs at least one unit, each worth more than nothing.", nameof(units));
        }
    }

    /// <summary>An item's weight, its base unit the kilogram.</summary>
    public static UnitScale Weight { get; } = new([new("kg", 1m)]);

    /// <summary>"1,5 kg", "0,25 kg", "27,5 kg" - Polish digits and separators, no trailing zeros.</summary>
    public string Format(decimal quantity)
    {
        var unit = _units.FirstOrDefault(unit => decimal.Remainder(quantity, unit.Multiplier) == 0) ?? _units[^1];
        var amount = quantity / unit.Multiplier;

        return $"{amount.ToString("#,##0.############################", Polish)}{UnitSeparator}{unit.Symbol}";
    }
}
