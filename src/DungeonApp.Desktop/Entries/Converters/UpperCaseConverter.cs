using System;
using System.Globalization;
using Avalonia.Data.Converters;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries.Converters;

/// <summary>
/// Upper-cases a section header's own display text (a pack's section header is set in capitals).
/// This is a screen-header style, not an entry's own value. Never applied to anything an entry
/// actually carries: every value coming out of a pack is shown exactly as it was written.
/// </summary>
public sealed class UpperCaseConverter : IValueConverter
{
    public static readonly UpperCaseConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text ? text.ToUpperInvariant() : value;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
