using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DungeonApp.Converters;

public class ScoreToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int score)
        {
            if (score > 10)
                return new SolidColorBrush(Color.Parse("#4ade80")); // Green
            if (score < 10)
                return new SolidColorBrush(Color.Parse("#f87171")); // Red
            
            return new SolidColorBrush(Colors.White); // White for exactly 10
        }
        return new SolidColorBrush(Colors.White);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
