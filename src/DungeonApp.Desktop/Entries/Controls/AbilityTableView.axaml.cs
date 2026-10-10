using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Entries.Controls;

/// <summary>
/// A reusable card control: a square-celled table of abilities (<see cref="AbilityRow"/>), one column
/// per ability, as many as it is given - a card that wants two groups of three lays out two tables.
/// A card view sets <see cref="Rows"/> as a plain property, like <see cref="TraitListView"/>.
/// <para>
/// The labels and cells are built here rather than templated: the frame's <see cref="Table"/> is a
/// grid whose every cell names its own row and column, so their number follows <see cref="Rows"/>.
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
        Labels.Children.Clear();
        Labels.ColumnDefinitions.Clear();
        Cells.Children.Clear();
        Cells.ColumnDefinitions.Clear();

        for (var index = 0; index < Rows.Count; index++)
        {
            var row = Rows[index];
            Labels.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            Cells.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var label = Text(row.Label, "label muted ability-label");
            Grid.SetColumn(label, index);
            Labels.Children.Add(label);

            var score = Cell(0, index, row.Score);
            var scoreText = (SelectableTextBlock)score.Content!;
            scoreText.Classes.Set("above-base", row.ScoreDeviation == BaseDeviation.Above);
            scoreText.Classes.Set("below-base", row.ScoreDeviation == BaseDeviation.Below);
            Cells.Children.Add(score);

            var modifier = Cell(1, index, row.Modifier);
            modifier.Classes.Set("positive", row.ModifierTone == ValueTone.Positive);
            modifier.Classes.Set("negative", row.ModifierTone == ValueTone.Negative);
            Cells.Children.Add(modifier);
        }
    }

    private static SelectableTextBlock Text(string text, string classes)
    {
        var block = new SelectableTextBlock { Text = text };
        block.Classes.AddRange(classes.Split(' '));
        return block;
    }

    private static TableCell Cell(int row, int column, string text)
    {
        var cell = new TableCell { Content = Text(text, "stat-value") };
        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, column);
        return cell;
    }
}
