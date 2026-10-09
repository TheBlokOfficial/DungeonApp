using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// One palette result. <see cref="Key"/> belongs to the caller (the palette never reads it);
/// <see cref="IconKey"/> is a geometry-icon resource key; <see cref="BadgeBrush"/> colours the badge when set,
/// otherwise the badge is plain muted text.
/// </summary>
public sealed record PaletteItem(
    object Key,
    string IconKey,
    string Name,
    string Tags = "",
    string? Badge = null,
    IBrush? BadgeBrush = null);

/// <summary>What a palette shows and does; see <see cref="Palette.ShowAsync"/>.</summary>
public sealed class PaletteOptions
{
    /// <summary>Dimmed first line: what the choice will do ("Dodaj do: Jaskinia").</summary>
    public string TargetText { get; init; } = string.Empty;

    public string Placeholder { get; init; } = string.Empty;

    /// <summary>Optional line under the field.</summary>
    public string? Hint { get; init; }

    /// <summary>Whether "4 gob" reads as quantity 4 and query "gob".</summary>
    public bool AllowQuantity { get; init; }

    /// <summary>Whether Ctrl+Enter chooses and keeps the palette open.</summary>
    public bool AllowKeepOpen { get; init; }

    /// <summary>Results for a query; the palette shows exactly what it returns.</summary>
    public required Func<string, IReadOnlyList<PaletteItem>> Search { get; init; }

    /// <summary>Called with the chosen item and the quantity (1 without a number). Errors reach the UI thread handler.</summary>
    public required Func<PaletteItem, int, Task> Choose { get; init; }
}

/// <summary>Row of the palette list.</summary>
public sealed class PaletteRowModel : ObservableObject
{
    private bool isSelected;

    public PaletteRowModel(PaletteItem item, int quantity, Avalonia.Media.DrawingImage? icon)
    {
        Item = item;
        Icon = icon;
        DisplayName = quantity == 1 ? item.Name : item.Name + " ×" + quantity;
    }

    public PaletteItem Item { get; }

    public Avalonia.Media.DrawingImage? Icon { get; }

    public string DisplayName { get; }

    public string Tags => Item.Tags;

    public string? BadgeText => Item.Badge;

    public IBrush? BadgeBrush => Item.BadgeBrush;

    public bool HasPlainBadge => !string.IsNullOrEmpty(Item.Badge) && Item.BadgeBrush is null;

    public bool HasColoredBadge => !string.IsNullOrEmpty(Item.Badge) && Item.BadgeBrush is not null;

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}
