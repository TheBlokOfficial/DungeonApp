using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries.Converters;

/// <summary>
/// Turns a solid color badge brush into its own dim, pill-background variant - the tier's text
/// color and its background are the same hue at two opacities. The skeleton draws every colored
/// badge as a pill this way rather than asking a system for two brushes per tier:
/// <c>IContentPresentation.ResolveBadgeBrush</c> only ever names one color per key - the system
/// owns the color, not the chip shape.
/// </summary>
public sealed class DimBrushConverter : IValueConverter
{
    public static readonly DimBrushConverter Instance = new();

    /// <summary>
    /// The opacity of every dim background - one shared constant, not a per-call magic number.
    /// </summary>
    private const double DimOpacity = 0.16;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ISolidColorBrush solid ? new SolidColorBrush(solid.Color, DimOpacity) : value;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
