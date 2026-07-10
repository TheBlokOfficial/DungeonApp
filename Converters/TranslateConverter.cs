using System;
using System.Globalization;
using Avalonia.Data.Converters;
using DungeonApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DungeonApp.Converters;

/// <summary>
/// Konwerter dla bindowań XAML, który tłumaczy dynamiczne teksty
/// (np. "core:longsword_name" na "Miecz Długi").
/// Jeśli klucz nie istnieje w słowniku, konwerter zwraca oryginalny tekst.
/// </summary>
public class TranslateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key && !string.IsNullOrEmpty(key))
        {
            var ts = App.Current?.Services?.GetService<ITranslationService>();
            if (ts != null)
            {
                return ts.Translate(key);
            }
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
