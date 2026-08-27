using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace DungeonApp.Desktop.Controls;

public sealed class ResourceKeyToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key &&
            Avalonia.Application.Current is { } application &&
            application.TryGetResource(key, application.ActualThemeVariant, out var resource))
        {
            return resource;
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
