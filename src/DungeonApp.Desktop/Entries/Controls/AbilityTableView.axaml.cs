using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Entries.Controls;

/// <summary>
/// A reusable card control: a square-celled table of abilities (<see cref="AbilityRow"/>), as many
/// rows as it is given - a card that wants two columns of three lays out two tables. A card view sets
/// <see cref="Rows"/> as a plain property, like <see cref="TraitListView"/>.
/// <para>
/// The cells are built here rather than templated: the frame's <see cref="Table"/> is a grid whose
/// every cell names its own row and column, so their number follows <see cref="Rows"/>.
/// </para>
/// </summary>
public partial class AbilityTableView : UserControl
{
    public static readonly StyledProperty<IReadOnlyList<AbilityRow>> RowsProperty =
        AvaloniaProperty.Register<AbilityTableView, IReadOnlyList<AbilityRow>>(nameof(Rows), defaultValue: []);

    public AbilityTableView()
    {
        InitializeComponent();
    }

    public IReadOnlyList<AbilityRow> Rows
    {
        get => GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == RowsProperty)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        Cells.Children.Clear();
        Cells.RowDefinitions.Clear();

        for (var index = 0; index < Rows.Count; index++)
        {
            var row = Rows[index];
            Cells.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            Cells.Children.Add(Cell(index, 0, row.Label, "label muted"));
            Cells.Children.Add(Cell(index, 1, row.Score, "mono-value"));

            var modifier = Cell(index, 2, row.Modifier, "mono-value");
            modifier.Classes.Set("positive", row.ModifierTone == ValueTone.Positive);
            modifier.Classes.Set("negative", row.ModifierTone == ValueTone.Negative);
            Cells.Children.Add(modifier);
        }
    }

    private static TableCell Cell(int row, int column, string text, string textClasses)
    {
        var block = new SelectableTextBlock { Text = text };
        block.Classes.AddRange(textClasses.Split(' '));

        var cell = new TableCell { Content = block };
        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, column);
        return cell;
    }
}
