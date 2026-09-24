using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DungeonApp.Library.Entries.Desktop.Converters;

/// <summary>
/// Turns a solid color badge brush into its own dim, pill-background variant - the same relationship
/// the mockup's own rarity chips carry between a tier's text color and its dim background (e.g.
/// <c>--success</c> next to <c>--success-dim</c>, both the same hue at two opacities). The skeleton
/// draws every colored badge as a pill this way rather than asking a system for two brushes per
/// tier: <c>IContentPresentation.ResolveBadgeBrush</c> only ever names one color per key
/// (docs/architecture.md, "Niezmiennik interfejsu" - the system owns the color, not the chip shape).
/// </summary>
public sealed class DimBrushConverter : IValueConverter
{
    public static readonly DimBrushConverter Instance = new();

    /// <summary>
    /// The opacity every dim token in the mockup shares (<c>--success-dim</c>, <c>--accent-dim</c>,
    /// <c>--warning-dim</c> are all <c>rgba(...,0.16)</c>) - one shared constant, not a per-call
    /// magic number.
    /// </summary>
    private const double DimOpacity = 0.16;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ISolidColorBrush solid ? new SolidColorBrush(solid.Color, DimOpacity) : value;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
