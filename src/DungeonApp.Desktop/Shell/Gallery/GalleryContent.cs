using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.Desktop.Shell.Gallery;

/// <summary>
/// The one "Galeria kontrolek" view for the application's whole life. Thousands of controls: even
/// with every style and template already cached, building and styling them takes a noticeable part
/// of a second, which a warmed throwaway instance does not save. So startup warms this very instance
/// and hands it over (<see cref="Adopt"/>), and the shell keeps it in its own hidden workspace layer -
/// opening the gallery only makes that layer visible, already laid out at the end of startup.
/// </summary>
public sealed partial class GalleryContent : ObservableObject
{
    /// <summary>Null until startup hands over the warmed view or the gallery is first opened.</summary>
    [ObservableProperty]
    public partial Control? View { get; private set; }

    /// <summary>A new view, not yet the gallery - for startup to warm before <see cref="Adopt"/>.</summary>
    public static Control Create() => new GalleryView { DataContext = new GalleryViewModel() };

    /// <summary>Makes a warmed view the gallery, unless one already is.</summary>
    public void Adopt(Control view) => View ??= view;

    /// <summary>The gallery, built on the spot when startup did not provide it.</summary>
    public Control EnsureView() => View ??= Create();
}
