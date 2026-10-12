using System;
using System.IO;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Tests;

public sealed class WindowPlacementStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"DungeonApp-window-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private WindowPlacementStore NewStore() => new(Path.Combine(_directory, "window.json"));

    [Fact]
    public void Saved_placement_is_read_back()
    {
        var store = NewStore();

        store.Save(new WindowPlacement(true, 1500, 900));

        Assert.Equal(new WindowPlacement(true, 1500, 900), store.Load());
    }

    [Fact]
    public void Missing_file_means_defaults()
    {
        Assert.Null(NewStore().Load());
    }

    [Fact]
    public void Damaged_file_means_defaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "window.json"), "{ to nie jest json");

        Assert.Null(NewStore().Load());
    }

    [Fact]
    public void Absurd_size_is_clamped()
    {
        var store = NewStore();
        store.Save(new WindowPlacement(false, -5, 1_000_000));

        var loaded = store.Load();

        Assert.Equal(WindowPlacementStore.MinimumSize, loaded!.Width);
        Assert.Equal(WindowPlacementStore.MaximumSize, loaded.Height);
    }
}
