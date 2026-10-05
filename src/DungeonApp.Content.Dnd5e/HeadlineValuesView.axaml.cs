using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
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
/// the creature's and the item's alike, so the two blocks cannot drift apart. Built by
/// <see cref="CreatureCardView"/> and <see cref="GearCardView"/>.
/// </summary>
public partial class HeadlineValuesView : UserControl
{
    /// <summary>How many columns the row has, whatever the number of values.</summary>
    public const int ColumnCount = 3;

    public HeadlineValuesView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Places the values left to right, one per column; at most <see cref="ColumnCount"/>. A null
    /// leaves its column empty, so a value that has a column of its own keeps it whatever stands
    /// before it.
    /// </summary>
    public void Show(IReadOnlyList<HeadlineValue?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(values.Count, ColumnCount);

        Columns.Children.Clear();
        for (var column = 0; column < values.Count; column++)
        {
            if (values[column] is not { } value)
            {
                continue;
            }

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

    /// <summary>
    /// Gives the columns whole-pixel widths before the row is measured. Star columns would split the
    /// row into fractions (394 wide makes three columns of 120.67): the tiles are measured at the
    /// fraction but arranged at the rounded width, so a note a hair wider than the fraction wraps
    /// while measuring, the tile keeps a second, empty line, and the row no longer stands on the
    /// picture's bottom edge. With whole pixels the tiles are measured exactly as they are drawn;
    /// the pixels the division leaves over go one each to the first columns.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        SetWholePixelColumns(availableSize.Width);
        return base.MeasureOverride(availableSize);
    }

    private void SetWholePixelColumns(double width)
    {
        var definitions = Columns.ColumnDefinitions;
        if (double.IsInfinity(width) || double.IsNaN(width))
        {
            foreach (var definition in definitions)
            {
                definition.Width = GridLength.Star;
            }

            return;
        }

        var scale = LayoutHelper.GetLayoutScale(this);
        var content = Math.Max(0, width - ((definitions.Count - 1) * Columns.ColumnSpacing));
        var pixels = (int)Math.Floor(content * scale);
        var share = pixels / definitions.Count;
        var leftOver = pixels % definitions.Count;
        for (var column = 0; column < definitions.Count; column++)
        {
            var columnPixels = share + (column < leftOver ? 1 : 0);
            definitions[column].Width = new GridLength(columnPixels / scale, GridUnitType.Pixel);
        }
    }
}
