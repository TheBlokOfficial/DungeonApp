using System;
using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Small card table (e.g. abilities: name, value, modifier): grid (<see cref="Grid"/> -
/// view knows columns and rows at compile time), with a <see cref="TableCell"/> at each position.
/// Cells draw
/// the 1 px outer border and horizontal row lines; before measurement, table sets
/// each cell's border thickness from its position: top only in the first row, left only
/// in the first column, bottom always, right in the last column - and between columns when
/// <see cref="ShowColumnLines"/>. Each line is therefore drawn once, highlighted cell background reaches the line
/// but never lies beneath it. Square corners. Non-interactive. Appearance belongs to the shell theme
/// (Themes/Controls/Table.axaml).
/// </summary>
public sealed class Table : Grid
{
    public static readonly StyledProperty<bool> ShowColumnLinesProperty =
        AvaloniaProperty.Register<Table, bool>(nameof(ShowColumnLines));

    static Table()
    {
        AffectsMeasure<Table>(ShowColumnLinesProperty);
    }

    /// <summary>Vertical lines between columns; off by default.</summary>
    public bool ShowColumnLines
    {
        get => GetValue(ShowColumnLinesProperty);
        set => SetValue(ShowColumnLinesProperty, value);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var lastColumn = Math.Max(ColumnDefinitions.Count, 1) - 1;
        foreach (var child in Children)
        {
            if (child is not TableCell cell)
            {
                continue;
            }

            var column = GetColumn(cell);
            var endColumn = column + GetColumnSpan(cell) - 1;
            var edges = new Thickness(
                column == 0 ? 1 : 0,
                GetRow(cell) == 0 ? 1 : 0,
                endColumn >= lastColumn || ShowColumnLines ? 1 : 0,
                1);
            if (cell.BorderThickness != edges)
            {
                cell.BorderThickness = edges;
            }
        }

        return base.MeasureOverride(constraint);
    }
}
