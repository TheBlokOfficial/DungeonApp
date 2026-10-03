using System.Globalization;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// A number as a card writes it: Polish digit grouping and decimal comma, no trailing zeros -
/// "1 500", "15", "0,5". The one place the cards' decimal numbers get their written form.
/// </summary>
internal static class PolishNumber
{
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    public static string Format(decimal number) => number.ToString("#,##0.############################", Polish);
}
