using System;
using System.Collections.Generic;
using System.Globalization;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// Orders challenge rating text ("1/2 (100 PD)", "1/4 (50 PD)", "5") by the numeric value of its
/// leading fraction or whole number, and puts anything that does not parse after every value that
/// does, alphabetically - backs the "Wyzwanie" filter's option order and the "Wyzwanie" sort.
/// </summary>
internal sealed class ChallengeOrderComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        var hasX = TryParseLeadingNumber(x, out var valueX);
        var hasY = TryParseLeadingNumber(y, out var valueY);

        if (hasX && hasY)
        {
            return valueX.CompareTo(valueY);
        }

        if (hasX != hasY)
        {
            return hasX ? -1 : 1;
        }

        return string.CompareOrdinal(x, y);
    }

    private static bool TryParseLeadingNumber(string? value, out double parsed)
    {
        parsed = 0;

        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var token = value.Split(' ', 2)[0];
        var slash = token.IndexOf('/');

        if (slash > 0)
        {
            var numeratorOk = double.TryParse(token[..slash], NumberStyles.Integer, CultureInfo.InvariantCulture, out var numerator);
            var denominatorOk = double.TryParse(token[(slash + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var denominator);

            if (numeratorOk && denominatorOk && denominator != 0)
            {
                parsed = numerator / denominator;
                return true;
            }

            return false;
        }

        if (double.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole))
        {
            parsed = whole;
            return true;
        }

        return false;
    }
}
