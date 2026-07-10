using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using DungeonApp.Services;

namespace DungeonApp.Converters;

/// <summary>
/// Inteligentny konwerter ikon, który potrafi wyciągnąć ikonę z dwóch źródeł:
/// 1. Z wektorowego stringa SVG podanego bezpośrednio przez moddera (np. "M10,10 L20...").
/// 2. Z globalnych zasobów Avalonia (np. klucz "IconSword").
/// Zwraca gotowy StreamGeometry do przypięcia w PathIcon.Data.
/// </summary>
public class SmartIconConverter : IValueConverter
{
    public static readonly SmartIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string iconString && !string.IsNullOrWhiteSpace(iconString))
        {
            // Opcja 0: Własne ikony modderów z Rejestru (np. "core:sword")
            if (App.Current?.Services != null)
            {
                var registry = App.Current.Services.GetService<IContentRegistry>();
                var customSvg = registry?.ResolveIconPath(iconString);
                if (!string.IsNullOrWhiteSpace(customSvg))
                {
                    try
                    {
                        return StreamGeometry.Parse(customSvg);
                    }
                    catch { } // Ignore parse error and fall back
                }
            }

            // Opcja 1: Zwykły klucz z zasobów aplikacji
            if (Application.Current != null)
            {
                // Próba z pełną nazwą
                if (Application.Current.TryFindResource(iconString, out var resource) && resource is StreamGeometry geometry)
                {
                    return geometry;
                }

                // Próba po obcięciu przedrostka paczki (tzw. fallback do natywnych ikon Avalonia)
                var fallbackKey = iconString.Contains(':') ? iconString.Split(':')[1] : iconString;
                if (Application.Current.TryFindResource(fallbackKey, out var fallbackResource) && fallbackResource is StreamGeometry fallbackGeometry)
                {
                    return fallbackGeometry;
                }
            }

            // Opcja 2: Modder podał surową ścieżkę SVG (Path Data)
            // Używamy natywnego parsera z Avalonia.
            try
            {
                return StreamGeometry.Parse(iconString);
            }
            catch
            {
                // Zignoruj błąd parsowania (np. jeśli string był niepoprawnym SVG i nie było go w zasobach)
            }
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
