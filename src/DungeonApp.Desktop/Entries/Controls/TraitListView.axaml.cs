using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries.Controls;

/// <summary>
/// A reusable card control: an optional title over a list of <see cref="TraitRow"/> pairs, laid out
/// either one per row or in a compact three-column grid. A card view composes it directly in XAML,
/// setting <see cref="Title"/>, <see cref="Rows"/> and <see cref="Compact"/> as plain properties -
/// there is no data-driven element catalog choosing this control or its layout.
/// <para>
/// <see cref="Compact"/> is set by whoever composes a card in XAML, never by a pack file: a layout
/// decision belongs to the person designing the card, not to data.
/// </para>
/// <para>
/// The control is its own <see cref="Avalonia.StyledElement.DataContext"/> (set once in the
/// constructor): a card view never binds anything to this control, it sets these properties
/// directly, so the control's internal markup can bind to itself with compiled bindings without
/// requiring the card view to hand it a view model.
/// </para>
/// </summary>
public partial class TraitListView : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<TraitListView, string?>(nameof(Title));

    public static readonly StyledProperty<IEnumerable<TraitRow>> RowsProperty =
        AvaloniaProperty.Register<TraitListView, IEnumerable<TraitRow>>(nameof(Rows), defaultValue: []);

    public static readonly StyledProperty<bool> CompactProperty =
        AvaloniaProperty.Register<TraitListView, bool>(nameof(Compact));

    public static readonly StyledProperty<double> LabelWidthProperty =
        AvaloniaProperty.Register<TraitListView, double>(nameof(LabelWidth), defaultValue: double.NaN);

    public static readonly DirectProperty<TraitListView, bool> HasTitleProperty =
        AvaloniaProperty.RegisterDirect<TraitListView, bool>(nameof(HasTitle), o => o.HasTitle);

    private bool _hasTitle;

    public TraitListView()
    {
        InitializeComponent();

        // See the type remarks: this control binds to itself, not to whatever DataContext its
        // parent happens to be flowing down.
        DataContext = this;
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IEnumerable<TraitRow> Rows
    {
        get => GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    public bool Compact
    {
        get => GetValue(CompactProperty);
        set => SetValue(CompactProperty, value);
    }

    /// <summary>
    /// The label column's fixed width, a longer label wrapping inside it; unset (NaN), the column is
    /// as wide as the longest label. Lets a card stand the values on a line it draws elsewhere (a
    /// column above the list) whatever the labels say - a layout decision of the card's.
    /// </summary>
    public double LabelWidth
    {
        get => GetValue(LabelWidthProperty);
        set => SetValue(LabelWidthProperty, value);
    }

    public bool HasTitle
    {
        get => _hasTitle;
        private set => SetAndRaise(HasTitleProperty, ref _hasTitle, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TitleProperty)
        {
            HasTitle = !string.IsNullOrEmpty(Title);
        }
    }
}
