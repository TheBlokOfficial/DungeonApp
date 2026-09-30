using System;
using System.Globalization;
using Avalonia.Data.Converters;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries.Converters;

/// <summary>
/// Upper-cases a section header's own display text (krok 10, brief A6: "Nagłówki sekcji po paczce
/// wersalikami"). This is a screen-header style, not an entry's own value - the architect's own
/// clarification during this step settles that distinction and it is not revisited. Never applied to
/// anything an entry actually carries: docs/architecture.md's "Wartości wpisu pokazuje się tak, jak
/// je zapisano" still governs every value coming out of a pack, unchanged.
/// </summary>
public sealed class UpperCaseConverter : IValueConverter
{
    public static readonly UpperCaseConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text ? text.ToUpperInvariant() : value;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
