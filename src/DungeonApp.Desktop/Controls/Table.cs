using System;
using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Mała tabela w karcie (np. cechy: nazwa, wartość, modyfikator): siatka (<see cref="Grid"/> -
/// kolumny i wiersze zna widok w czasie kompilacji), której każda pozycja ma <see cref="TableCell"/>.
/// Obramowanie
/// zewnętrzne 1 px i linie poziome między wierszami rysują komórki; tabela przed pomiarem ustala
/// każdej komórce grubość krawędzi z jej położenia: górna tylko w pierwszym wierszu, lewa tylko
/// w pierwszej kolumnie, dolna zawsze, prawa w ostatniej kolumnie - i między kolumnami, gdy
/// <see cref="ShowColumnLines"/>. Każda linia leży więc raz, tło wyróżnionej komórki sięga linii,
/// a linia leży nad nim. Ostre narożniki. Nieinteraktywna. Wygląd należy do motywu ramy
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

    /// <summary>Linie pionowe między kolumnami; domyślnie wyłączone.</summary>
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
