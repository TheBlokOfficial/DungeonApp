using System.Collections.Generic;
using Avalonia;
using Avalonia.Styling;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// A frame theme token read from code, once, when a card is built. A card is not in a window yet
/// at that point, so the application's resources are asked directly; the theme does not change while
/// the app runs, so reading once is enough.
/// </summary>
internal static class ThemeResource
{
    public static T Get<T>(string key)
    {
        if (Application.Current is { } application
            && application.TryGetResource(key, ThemeVariant.Default, out var value)
            && value is T typed)
        {
            return typed;
        }

        throw new KeyNotFoundException($"Theme resource '{key}' is missing or is not a {typeof(T).Name}.");
    }
}
