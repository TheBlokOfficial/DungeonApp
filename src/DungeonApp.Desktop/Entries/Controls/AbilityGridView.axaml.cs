using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries.Controls;

/// <summary>
/// A reusable card control: a fixed three-column grid of bordered ability boxes (krok 10, brief C -
/// mockup's own ".ability-grid"). See <see cref="TraitListView"/>'s remarks - the same reasoning
/// applies: a card view composes this directly in XAML, setting <see cref="Rows"/> as a plain
/// property, and it is its own DataContext.
/// </summary>
public partial class AbilityGridView : UserControl
{
    public static readonly StyledProperty<IEnumerable<AbilityRow>> RowsProperty =
        AvaloniaProperty.Register<AbilityGridView, IEnumerable<AbilityRow>>(nameof(Rows), defaultValue: []);

    public AbilityGridView()
    {
        InitializeComponent();

        DataContext = this;
    }

    public IEnumerable<AbilityRow> Rows
    {
        get => GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }
}
