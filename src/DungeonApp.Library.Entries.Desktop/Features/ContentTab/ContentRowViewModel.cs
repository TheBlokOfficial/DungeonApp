using System;
using System.Windows.Input;
using Avalonia.Media;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Library.Entries.Desktop.Features.ContentTab;

/// <summary>
/// One selectable row in a content tab's list: a resolved entry, a broken entry, a rejected file, or
/// a rejected pack's own header (docs/architecture.md, "Zakładki treści": "Odrzucona paczka...
/// wybieralny"). The same shape serves all four - a name, an optional badge, whether it draws in the
/// broken-content color, and a command that reports its own <see cref="ContentSelectionKey"/> back to
/// the owning <see cref="ContentTabViewModel"/> - because the view only ever needs to draw and click
/// a row, never to know which of the four it is drawing.
/// </summary>
public sealed class ContentRowViewModel : ObservableObject
{
    private bool _isSelected;

    internal ContentRowViewModel(
        string name,
        string? badgeText,
        IBrush? badgeBrush,
        bool isBroken,
        ContentSelectionKey key,
        Action<ContentSelectionKey> select)
    {
        Name = name;
        BadgeText = badgeText;
        BadgeBrush = badgeBrush;
        IsBroken = isBroken;
        Key = key;
        SelectCommand = new RelayCommand(() => select(key));
    }

    /// <summary>This row's own identity, so the owning view model can tell whether it is the current selection.</summary>
    internal ContentSelectionKey Key { get; }

    public string Name { get; }

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

    public bool IsSelected
    {
        get => _isSelected;
        internal set => SetField(ref _isSelected, value);
    }

    public ICommand SelectCommand { get; }
}
