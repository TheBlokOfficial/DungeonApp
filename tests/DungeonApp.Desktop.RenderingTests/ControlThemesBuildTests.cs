using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using DungeonApp.Testing;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Każdy wpis słownika motywów kontrolek ramy daje się zbudować. Wpisy są odroczone: kompilator XAML
/// przepuszcza błąd, który wychodzi dopiero przy pierwszym użyciu kontrolki - na przykład selektor
/// potomka w motywie pola tekstowego wywraca aplikację przy pierwszym polu wyszukiwania, choć build
/// i pozostałe testy przechodzą. Motywy leżą w osobnych plikach, więc test pilnuje też, że spis
/// (Themes/DungeonControls.axaml) scala każdy z nich: plik spoza spisu to kontrolka bez motywu.
/// </summary>
public sealed class ControlThemesBuildTests
{
    private const string IndexUri = "avares://DungeonApp.Desktop/Themes/DungeonControls.axaml";

    [AvaloniaFact]
    public void Every_entry_of_the_frames_control_themes_builds()
    {
        var themes = Load(IndexUri);

        var failures = AllKeys(themes)
            .Select(key => (Key: key, Error: TryBuild(themes, key)))
            .Where(result => result.Error is not null)
            .Select(result => $"{result.Key}: {result.Error!.Message}")
            .ToList();

        Assert.Empty(failures);
    }

    [AvaloniaFact]
    public void Every_control_theme_file_is_merged_into_the_index()
    {
        var indexKeys = AllKeys(Load(IndexUri)).ToHashSet();
        var files = Directory.GetFiles(Path.Combine(RepositoryRoot.DesktopSources, "Themes", "Controls"), "*.axaml");

        Assert.NotEmpty(files);
        var missing = files
            .Select(Path.GetFileName)
            .Where(name => !AllKeys(Load($"avares://DungeonApp.Desktop/Themes/Controls/{name}")).All(indexKeys.Contains))
            .ToList();

        Assert.Empty(missing);
    }

    private static ResourceDictionary Load(string uri) => (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri(uri));

    private static IEnumerable<object> AllKeys(ResourceDictionary dictionary) =>
        dictionary.Keys.Concat(dictionary.MergedDictionaries.OfType<ResourceDictionary>().SelectMany(AllKeys));

    private static Exception? TryBuild(ResourceDictionary themes, object key)
    {
        try
        {
            themes.TryGetResource(key, null, out _);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
