using System.Globalization;
using DungeonApp.Core.Content;

namespace DungeonApp.Desktop.Features.Registry.Elements;

/// <summary>
/// Turns one <see cref="FieldValue"/> into the text a card element shows. Lives here, not in Core,
/// because how a value is rendered is a presentation concern - Core only knows that a value is text
/// or a whole number, never how either should look on a card.
/// <para>
/// Dispatch on <see cref="FieldValue"/>'s variant is the same kind the content architecture allows
/// for <see cref="CardElement"/>: the catalog of value kinds is closed and compiled (<see
/// cref="TextValue"/>, <see cref="IntegerValue"/>), so switching on which one a given value is stays
/// legal - what stays forbidden is branching on what kind of entry supplied it.
/// </para>
/// </summary>
internal static class FieldValueText
{
    public static string Format(FieldValue value) => value switch
    {
        TextValue text => text.Text,
        IntegerValue integer => integer.Value.ToString(CultureInfo.InvariantCulture),
        _ => string.Empty
    };
}
