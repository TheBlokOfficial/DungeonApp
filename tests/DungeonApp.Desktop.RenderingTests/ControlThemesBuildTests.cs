using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Każdy wpis słownika motywów kontrolek ramy daje się zbudować. Wpisy są odroczone: kompilator XAML
/// przepuszcza błąd, który wychodzi dopiero przy pierwszym użyciu kontrolki - 2026-09-25 selektor
/// potomka w motywie pola tekstowego wywracał aplikację przy wejściu w system (rozgrzewka budowała
/// zakładkę z polem wyszukiwania), a build i pozostałe testy przechodziły.
/// </summary>
public sealed class ControlThemesBuildTests
{
    [AvaloniaFact]
    public void Every_entry_of_the_frames_control_themes_builds()
    {
        var themes = (ResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://DungeonApp.Desktop/Themes/DungeonControls.axaml"));

        var failures = themes.Keys
            .Select(key => (Key: key, Error: TryBuild(themes, key)))
            .Where(result => result.Error is not null)
            .Select(result => $"{result.Key}: {result.Error!.Message}")
            .ToList();

        Assert.Empty(failures);
    }

    private static Exception? TryBuild(ResourceDictionary themes, object key)
    {
        try
        {
            themes.TryGetValue(key, out _);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
