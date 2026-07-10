using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DungeonApp.Converters;

/// <summary>
/// Inteligentny konwerter pędzli (kolorów).
/// 1. Jeśli dostanie Hex (np. "#FF0000"), parsuje go do SolidColorBrush.
/// 2. Jeśli dostanie klucz (np. "TextPrimary"), pobiera pędzel z zasobów.
/// </summary>
public class SmartBrushConverter : IValueConverter
{
    public static readonly SmartBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string colorString && !string.IsNullOrWhiteSpace(colorString))
        {
            // Opcja 1: Zwykły klucz z zasobów aplikacji (np. "TextPrimary")
            if (Application.Current != null && Application.Current.TryFindResource(colorString, out var resource))
            {
                if (resource is IBrush brush)
                    return brush;
                if (resource is Color color)
                    return new SolidColorBrush(color);
            }

            // Opcja 2: Surowy kod koloru (np. "#FF0000" lub "Red")
            try
            {
                if (Color.TryParse(colorString, out var c))
                    return new SolidColorBrush(c);
            }
            catch
            {
                // Zignoruj błędy parsowania
            }
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
