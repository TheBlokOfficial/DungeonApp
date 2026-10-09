using System;
using System.Globalization;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Splits palette input into a quantity and a search query. A leading whole number followed by a space
/// ("4 gob") is the quantity; zero, a number above <see cref="MaxQuantity"/>, a number without a space after it
/// or no number at all leave the whole text as the query with quantity 1.
/// </summary>
public static class PaletteQuery
{
    /// <summary>Largest quantity read from the text; a bigger number is part of the query.</summary>
    public const int MaxQuantity = 99;

    public static (int Quantity, string Query) Parse(string? text, bool allowQuantity)
    {
        var input = text ?? string.Empty;
        if (!allowQuantity)
        {
            return (1, input);
        }

        var trimmed = input.TrimStart();
        var digits = 0;
        while (digits < trimmed.Length && char.IsAsciiDigit(trimmed[digits]))
        {
            digits++;
        }

        if (digits == 0 || digits == trimmed.Length || !char.IsWhiteSpace(trimmed[digits]) || digits > 3)
        {
            return (1, input);
        }

        var quantity = int.Parse(trimmed.AsSpan(0, digits), CultureInfo.InvariantCulture);
        return quantity is >= 1 and <= MaxQuantity
            ? (quantity, trimmed[(digits + 1)..].TrimStart())
            : (1, input);
    }
}
