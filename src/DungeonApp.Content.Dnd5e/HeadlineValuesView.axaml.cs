using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// One headline value: its label, the theme key of the icon that carries the label's meaning by
/// convention (a shield by KP), the value drawn large, and an optional note under it.
/// </summary>
public sealed record HeadlineValue(string Label, string IconResourceKey, string Value, string? Note = null);

/// <summary>
/// The row of headline values every card of this system lends the detail header - one control for
/// the monster's and the item's alike, so the two blocks cannot drift apart. Built by
/// <see cref="MonsterCardView"/> and <see cref="GearCardView"/>.
/// </summary>
public partial class HeadlineValuesView : UserControl
{
    /// <summary>How many columns the row has, whatever the number of values.</summary>
    public const int ColumnCount = 3;

    public HeadlineValuesView()
    {
        InitializeComponent();
    }

    /// <summary>Places the values left to right, one per column; at most <see cref="ColumnCount"/>.</summary>
    public void Show(IReadOnlyList<HeadlineValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(values.Count, ColumnCount);

        Columns.Children.Clear();
        for (var column = 0; column < values.Count; column++)
        {
            var value = values[column];
            var tile = new StatTile
            {
                Label = value.Label,
                Icon = ThemeResource.Get<DrawingImage>(value.IconResourceKey),
                Value = value.Value,
                Note = value.Note,
            };
            Grid.SetColumn(tile, column);
            Columns.Children.Add(tile);
        }
    }
}
