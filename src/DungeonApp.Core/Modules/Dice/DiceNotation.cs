using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DungeonApp.Core.Modules.Dice;

/// <summary>
/// A roll written the way a GM writes it: <c>k20</c>, <c>2k6+3</c>, <c>4k6-1</c>.
/// <para>
/// Deliberately the whole grammar. It knows dice and one flat modifier, and nothing about what the
/// modifier means - no attributes, no bonuses, no advantage. That line is where the ruleset starts,
/// and the ruleset is not here yet; keeping the notation this narrow is what stops it arriving
/// through the back door.
/// </para>
/// <para>
/// Both the Polish <c>k</c> (kość) and the international <c>d</c> are accepted, because a GM who has
/// read English books will type both and neither is a mistake worth a refusal.
/// </para>
/// </summary>
public readonly record struct DiceNotation(int Count, int Sides, int Modifier)
{
    /// <summary>Enough for a fistful of dice; past this the GM means a table, not a roll.</summary>
    public const int MaxCount = 100;

    public const int MinSides = 2;

    public const int MaxSides = 1000;

    public const int MaxModifier = 1000;

    private static readonly Regex Grammar = new(
        @"^\s*(?<count>\d{1,3})?\s*[kKdD]\s*(?<sides>\d{1,4})\s*(?:(?<sign>[+-])\s*(?<modifier>\d{1,4}))?\s*$",
        RegexOptions.CultureInvariant);

    public static bool TryParse(string? value, out DiceNotation notation)
    {
        notation = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = Grammar.Match(value);

        if (!match.Success)
        {
            return false;
        }

        var count = match.Groups["count"].Success
            ? int.Parse(match.Groups["count"].Value, CultureInfo.InvariantCulture)
            : 1;
        var sides = int.Parse(match.Groups["sides"].Value, CultureInfo.InvariantCulture);
        var modifier = match.Groups["modifier"].Success
            ? int.Parse(match.Groups["modifier"].Value, CultureInfo.InvariantCulture)
            : 0;

        if (match.Groups["sign"].Value == "-")
        {
            modifier = -modifier;
        }

        if (count is < 1 or > MaxCount ||
            sides is < MinSides or > MaxSides ||
            Math.Abs(modifier) > MaxModifier)
        {
            return false;
        }

        notation = new DiceNotation(count, sides, modifier);
        return true;
    }

    /// <summary>
    /// The refusal names the shape it wanted rather than the rule that was broken. A GM who mistyped
    /// a roll needs an example, not a diagnosis.
    /// </summary>
    public static DiceNotation Parse(string? value) =>
        TryParse(value, out var notation)
            ? notation
            : throw new CampaignRuleException(
                $"Nie rozumiem zapisu rzutu. Poprawny to na przykład k20, 2k6+3 albo 4k6-1 " +
                $"(do {MaxCount} kości po najwyżej {MaxSides} ścianek).");

    /// <summary>Back to how it is written, so the chronicle quotes the roll rather than a struct.</summary>
    public override string ToString()
    {
        var dice = Count == 1
            ? $"k{Sides}"
            : $"{Count}k{Sides}";

        return Modifier switch
        {
            0 => dice,
            > 0 => $"{dice}+{Modifier}",
            _ => $"{dice}{Modifier}"
        };
    }
}
