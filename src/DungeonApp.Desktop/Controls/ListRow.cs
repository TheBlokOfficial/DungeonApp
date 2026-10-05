using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Command list row: button styled as a shell list row (ListBoxItem) -
/// for ItemsControl lists where a view-model command handles selection or opening,
/// rather than control selection. View model supplies <see cref="IsSelected"/> (:selected pseudo-class);
/// the row does not select itself. <see cref="ShowsSelectionStripe"/> enables a selection stripe in the
/// left gutter. Appearance, states, height and stripe gutter belong to the shell theme
/// (Themes/Controls/ListRow.axaml); the list supplies gutter width and stripe insets through
/// DungeonListRowStripeGutter and DungeonListRowStripeInset resources at list level.
/// </summary>
public sealed class ListRow : Button
{
    /// <summary>Whether the row is selected (:selected pseudo-class).</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<ListRow, bool>(nameof(IsSelected));

    /// <summary>Whether the row has a gutter with a selection stripe (false by default).</summary>
    public static readonly StyledProperty<bool> ShowsSelectionStripeProperty =
        AvaloniaProperty.Register<ListRow, bool>(nameof(ShowsSelectionStripe));

    static ListRow()
    {
        IsSelectedProperty.Changed.AddClassHandler<ListRow>((row, e) =>
            row.PseudoClasses.Set(":selected", e.GetNewValue<bool>()));
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool ShowsSelectionStripe
    {
        get => GetValue(ShowsSelectionStripeProperty);
        set => SetValue(ShowsSelectionStripeProperty, value);
    }
}
