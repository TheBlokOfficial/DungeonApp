using System;
using System.Windows.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries.ContentTab;

/// <summary>
/// One selectable row in a content tab's list: a resolved entry, a broken entry, a rejected file, or
/// a rejected pack's own header, selectable so its detail can give the reason. The same shape
/// serves all four - a name, an optional badge, whether it draws in the broken-content color, and a
/// command that reports its own <see cref="ContentSelectionKey"/> back to the owning
/// <see cref="ContentTabViewModel"/> - because the view only ever needs to draw and click a row,
/// never to know which of the four it is drawing.
/// </summary>
public sealed partial class ContentRowViewModel : ObservableObject
{
    internal ContentRowViewModel(
        string name,
        string? badgeText,
        IBrush? badgeBrush,
        bool isBroken,
        ContentSelectionKey key,
        Action<ContentSelectionKey> select,
        bool hasPictureSlot = false,
        DrawingImage? picture = null)
    {
        Name = name;
        HasPictureSlot = hasPictureSlot;
        Picture = picture;
        BadgeText = badgeText;
        BadgeBrush = badgeBrush;
        IsBroken = isBroken;
        Key = key;
        SelectCommand = new RelayCommand(() => select(key));
    }

    /// <summary>This row's own identity, so the owning view model can tell whether it is the current selection.</summary>
    internal ContentSelectionKey Key { get; }

    public string Name { get; }

    /// <summary>
    /// Whether the row keeps a place for a small picture before the name - in a tab whose content
    /// types show their entries' pictures in rows (<see cref="IContentTypeProfile.ShowsPictureInRow"/>),
    /// every row keeps it, with a picture or without, so the names stand in one column.
    /// </summary>
    public bool HasPictureSlot { get; }

    /// <summary>
    /// The entry's one-colour (vector) picture for that place; null - the place stays empty, also for
    /// an entry whose picture is a raster image: a row has room for a shape, not for a photograph.
    /// </summary>
    public DrawingImage? Picture { get; }

    /// <summary>Null for a row with nothing to show on its right side (never an empty string).</summary>
    public string? BadgeText { get; }

    public bool HasBadge => BadgeText is not null;

    /// <summary>A badge with text but no color - the plain, muted style (e.g. "Wyzwanie").</summary>
    public bool HasPlainBadge => BadgeText is not null && BadgeBrush is null;

    /// <summary>
    /// Null for a plain, muted text badge - any badge with no <see cref="ContentBadge.ColorKey"/>,
    /// or a key the system did not recognise. Not null for a pill badge in the system's own color.
    /// </summary>
    public IBrush? BadgeBrush { get; }

    public bool HasColoredBadge => BadgeBrush is not null;

    /// <summary>True for a broken row - drawn in the content-broken color, with no badge.</summary>
    public bool IsBroken { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; internal set; }

    public ICommand SelectCommand { get; }
}
