using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Controls;

/// <summary>How many rows a <see cref="DropDownPicker"/> selects.</summary>
public enum DropDownPickerMode
{
    /// <summary>One row (<see cref="DropDownPicker.SelectedItem"/>); a choice closes the list.</summary>
    Single,

    /// <summary>Any rows (<see cref="DropDownPicker.SelectedItems"/>); the list stays open.</summary>
    Multiple,
}

/// <summary>
/// The frame's drop-down list for what ComboBox does not do: selecting several rows at once and/or
/// narrowing the rows with a search field. A plain single choice without search stays a ComboBox.
/// Rows show each item's name: <see cref="DisplayMemberBinding"/> evaluated on the item (like
/// ItemsControl.DisplayMemberBinding), or <c>ToString()</c> without it. The look belongs to the frame's control theme
/// (Themes/DungeonControls.axaml). Selection changes only what the user clicked.
/// </summary>
[TemplatePart("PART_SearchBox", typeof(TextBox))]
[PseudoClasses(":dropdownopen", ":has-selection")]
public sealed class DropDownPicker : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<DropDownPicker, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IList?> SelectedItemsProperty =
        AvaloniaProperty.Register<DropDownPicker, IList?>(
            nameof(SelectedItems), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<DropDownPicker, object?>(
            nameof(SelectedItem), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<DropDownPickerMode> SelectionModeProperty =
        AvaloniaProperty.Register<DropDownPicker, DropDownPickerMode>(
            nameof(SelectionMode), DropDownPickerMode.Multiple);

    public static readonly StyledProperty<BindingBase?> DisplayMemberBindingProperty =
        AvaloniaProperty.Register<DropDownPicker, BindingBase?>(nameof(DisplayMemberBinding));

    /// <summary>
    /// Single mode: clicking the selected row again clears the choice (a filter chip, where nothing
    /// chosen means everything). Set by the chip's theme (DungeonChipPicker), not by a view; a plain
    /// single list in a form keeps its choice.
    /// </summary>
    public static readonly StyledProperty<bool> AllowsDeselectProperty =
        AvaloniaProperty.Register<DropDownPicker, bool>(nameof(AllowsDeselect));

    public static readonly StyledProperty<bool> IsSearchEnabledProperty =
        AvaloniaProperty.Register<DropDownPicker, bool>(nameof(IsSearchEnabled));

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<DropDownPicker, string?>(nameof(PlaceholderText));

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<DropDownPicker, bool>(
            nameof(IsDropDownOpen), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> SearchTextProperty =
        AvaloniaProperty.Register<DropDownPicker, string?>(
            nameof(SearchText), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly DirectProperty<DropDownPicker, IReadOnlyList<DropDownPickerRow>> RowsProperty =
        AvaloniaProperty.RegisterDirect<DropDownPicker, IReadOnlyList<DropDownPickerRow>>(
            nameof(Rows), picker => picker.Rows);

    public static readonly DirectProperty<DropDownPicker, bool> HasNoMatchesProperty =
        AvaloniaProperty.RegisterDirect<DropDownPicker, bool>(nameof(HasNoMatches), picker => picker.HasNoMatches);

    public static readonly DirectProperty<DropDownPicker, string> LabelTextProperty =
        AvaloniaProperty.RegisterDirect<DropDownPicker, string>(nameof(LabelText), picker => picker.LabelText);

    public static readonly DirectProperty<DropDownPicker, string> MoreBadgeTextProperty =
        AvaloniaProperty.RegisterDirect<DropDownPicker, string>(nameof(MoreBadgeText), picker => picker.MoreBadgeText);

    public static readonly DirectProperty<DropDownPicker, bool> HasMoreSelectedProperty =
        AvaloniaProperty.RegisterDirect<DropDownPicker, bool>(nameof(HasMoreSelected), picker => picker.HasMoreSelected);

    public static readonly DirectProperty<DropDownPicker, string?> SelectionToolTipTextProperty =
        AvaloniaProperty.RegisterDirect<DropDownPicker, string?>(
            nameof(SelectionToolTipText), picker => picker.SelectionToolTipText);

    public static readonly StyledProperty<IBrush?> PlaceholderForegroundProperty =
        AvaloniaProperty.Register<DropDownPicker, IBrush?>(nameof(PlaceholderForeground));

    public static readonly StyledProperty<IBrush?> GlyphForegroundProperty =
        AvaloniaProperty.Register<DropDownPicker, IBrush?>(nameof(GlyphForeground));

    public static readonly StyledProperty<double> GlyphSpacingProperty =
        AvaloniaProperty.Register<DropDownPicker, double>(nameof(GlyphSpacing));

    public static readonly DirectProperty<DropDownPicker, bool> IsPlaceholderShownProperty =
        AvaloniaProperty.RegisterDirect<DropDownPicker, bool>(
            nameof(IsPlaceholderShown), picker => picker.IsPlaceholderShown);

    private IReadOnlyList<DropDownPickerRow> _rows = [];
    private bool _hasNoMatches;
    private string _labelText = string.Empty;
    private string _moreBadgeText = string.Empty;
    private bool _hasMoreSelected;
    private bool _isPlaceholderShown = true;
    private string? _selectionToolTipText;
    private INotifyCollectionChanged? _observedSelection;
    private TextBox? _searchBox;

    // Evaluates DisplayMemberBinding on one item at a time: the binding is applied once, the item
    // is set as its DataContext and the name read back - the binding itself resolves the path (a
    // compiled binding in XAML; no reflection here).
    private TextBlock? _nameEvaluator;

    public DropDownPicker()
    {
        SetCurrentValue(SelectedItemsProperty, new AvaloniaList<object>());
        AddHandler(PointerReleasedEvent, OnAnyPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Multiple mode: the selected items. The picker adds and removes in this list.</summary>
    public IList? SelectedItems
    {
        get => GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    /// <summary>Single mode: the selected item.</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// The binding that gives an item's name in the rows and the closed text, when items are objects
    /// rather than strings (e.g. <c>{Binding Name}</c>). Without it, rows show <c>ToString()</c>.
    /// </summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public BindingBase? DisplayMemberBinding
    {
        get => GetValue(DisplayMemberBindingProperty);
        set => SetValue(DisplayMemberBindingProperty, value);
    }

    public bool AllowsDeselect
    {
        get => GetValue(AllowsDeselectProperty);
        set => SetValue(AllowsDeselectProperty, value);
    }

    public DropDownPickerMode SelectionMode
    {
        get => GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>A search field on top of the open list narrows its rows.</summary>
    public bool IsSearchEnabled
    {
        get => GetValue(IsSearchEnabledProperty);
        set => SetValue(IsSearchEnabledProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    public string? SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    /// <summary>The rows the open list shows - the items the search keeps.</summary>
    public IReadOnlyList<DropDownPickerRow> Rows
    {
        get => _rows;
        private set => SetAndRaise(RowsProperty, ref _rows, value);
    }

    /// <summary>There are items, but the search keeps none of them.</summary>
    public bool HasNoMatches
    {
        get => _hasNoMatches;
        private set => SetAndRaise(HasNoMatchesProperty, ref _hasNoMatches, value);
    }

    /// <summary>
    /// What the closed picker - also in the filter chip look (DungeonChipPicker) - says (<see cref="DropDownPickerText.Label"/>):
    /// the filter's name (<see cref="PlaceholderText"/>) while nothing is selected, otherwise the first
    /// selected name in the order of the list.
    /// </summary>
    public string LabelText
    {
        get => _labelText;
        private set => SetAndRaise(LabelTextProperty, ref _labelText, value);
    }

    /// <summary>"+N" for the selected items after the first (<see cref="DropDownPickerText.MoreBadge"/>).</summary>
    public string MoreBadgeText
    {
        get => _moreBadgeText;
        private set => SetAndRaise(MoreBadgeTextProperty, ref _moreBadgeText, value);
    }

    /// <summary>More than one item is selected - the chip shows the "+N" badge.</summary>
    public bool HasMoreSelected
    {
        get => _hasMoreSelected;
        private set => SetAndRaise(HasMoreSelectedProperty, ref _hasMoreSelected, value);
    }

    /// <summary>
    /// Several selected: the filter's name and every selected name
    /// (<see cref="DropDownPickerText.SelectionToolTip"/>) - the theme shows it as the closed box's tip,
    /// which wins over a trimmed label's own tip because it says more.
    /// </summary>
    public string? SelectionToolTipText
    {
        get => _selectionToolTipText;
        private set => SetAndRaise(SelectionToolTipTextProperty, ref _selectionToolTipText, value);
    }

    /// <summary>The colour of the placeholder text; set by the control theme.</summary>
    public IBrush? PlaceholderForeground
    {
        get => GetValue(PlaceholderForegroundProperty);
        set => SetValue(PlaceholderForegroundProperty, value);
    }

    /// <summary>The colour of the drop-down arrow; set by the control theme.</summary>
    public IBrush? GlyphForeground
    {
        get => GetValue(GlyphForegroundProperty);
        set => SetValue(GlyphForegroundProperty, value);
    }

    /// <summary>The gap before the drop-down arrow (and before the "+N" badge); set by the control theme.</summary>
    public double GlyphSpacing
    {
        get => GetValue(GlyphSpacingProperty);
        set => SetValue(GlyphSpacingProperty, value);
    }

    /// <summary>The closed picker shows the placeholder: nothing is selected.</summary>
    public bool IsPlaceholderShown
    {
        get => _isPlaceholderShown;
        private set => SetAndRaise(IsPlaceholderShownProperty, ref _isPlaceholderShown, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _searchBox = e.NameScope.Find<TextBox>("PART_SearchBox");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedItemsProperty)
        {
            ObserveSelection(change.GetNewValue<IList?>());
            UpdateSelection();
        }
        else if (change.Property == SelectedItemProperty)
        {
            UpdateSelection();
        }
        else if (change.Property == PlaceholderTextProperty)
        {
            UpdateSummary(Items());
        }
        else if (change.Property == DisplayMemberBindingProperty)
        {
            _nameEvaluator = null;
            RebuildRows();
        }
        else if (change.Property == ItemsSourceProperty
                 || change.Property == SelectionModeProperty
                 || change.Property == SearchTextProperty)
        {
            RebuildRows();
        }
        else if (change.Property == IsDropDownOpenProperty)
        {
            var isOpen = change.GetNewValue<bool>();
            PseudoClasses.Set(":dropdownopen", isOpen);
            if (isOpen)
            {
                if (IsSearchEnabled && _searchBox is { } searchBox)
                {
                    // The popup's content attaches after this change; focus it once it is there.
                    Dispatcher.UIThread.Post(() => searchBox.Focus(), DispatcherPriority.Loaded);
                }
            }
            else
            {
                // Closing forgets the search: the next opening shows every row.
                SetCurrentValue(SearchTextProperty, null);
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && IsDropDownOpen)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
            e.Handled = true;
        }
    }

    private void OnAnyPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left || !IsEnabled || e.Source is not Visual source)
        {
            return;
        }

        if (TopLevel.GetTopLevel(source) == TopLevel.GetTopLevel(this))
        {
            // The closed box itself: open or close.
            SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
            e.Handled = true;
            return;
        }

        // Inside the open list: a row toggles wherever it is clicked.
        var row = source.GetSelfAndVisualAncestors()
            .OfType<ListBoxItem>()
            .Select(container => container.DataContext)
            .OfType<DropDownPickerRow>()
            .FirstOrDefault();
        if (row is not null)
        {
            Toggle(row);
            e.Handled = true;
        }
    }

    private void Toggle(DropDownPickerRow row)
    {
        if (SelectionMode == DropDownPickerMode.Single)
        {
            ChooseSingle(row.Item);
            return;
        }

        if (SelectedItems is not { } selected)
        {
            return;
        }

        if (selected.Contains(row.Item))
        {
            selected.Remove(row.Item);
        }
        else
        {
            selected.Add(row.Item);
        }

        if (_observedSelection is null)
        {
            UpdateSelection();
        }
    }

    /// <summary>
    /// Single mode: the clicked row becomes the choice and the list closes; with
    /// <see cref="AllowsDeselect"/> a click on the row already chosen clears the choice instead.
    /// </summary>
    internal void ChooseSingle(object? item)
    {
        var deselect = AllowsDeselect && SelectedItem is not null && Equals(SelectedItem, item);
        SetCurrentValue(SelectedItemProperty, deselect ? null : item);
        SetCurrentValue(IsDropDownOpenProperty, false);
    }

    private void ObserveSelection(IList? selection)
    {
        if (_observedSelection is not null)
        {
            _observedSelection.CollectionChanged -= OnSelectionCollectionChanged;
        }

        _observedSelection = selection as INotifyCollectionChanged;
        if (_observedSelection is not null)
        {
            _observedSelection.CollectionChanged += OnSelectionCollectionChanged;
        }
    }

    // Notifies the view only: the rows' selection state and the closed text are redrawn, nothing
    // is written.
    private void OnSelectionCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateSelection();

    // The items, the mode or the search changed: new rows. A change of the selection alone never
    // comes here - it keeps the rows (UpdateSelection), so their containers are not rebuilt.
    private void RebuildRows()
    {
        var items = Items();
        var isMultiple = SelectionMode == DropDownPickerMode.Multiple;

        Rows = items
            .Select(item => new DropDownPickerRow(item, NameOf(item), IsSelected(item), isMultiple))
            .Where(row => DropDownPickerText.Matches(row.Text, SearchText))
            .ToList();
        HasNoMatches = items.Count > 0 && Rows.Count == 0;
        UpdateSummary(items);
    }

    // The selection changed: the same rows, each told whether it is selected now.
    private void UpdateSelection()
    {
        foreach (var row in Rows)
        {
            row.IsSelected = IsSelected(row.Item);
        }

        UpdateSummary(Items());
    }

    // In the order of the items, not of clicking - the closed text does not jump. What the closed
    // picker shows follows the selection; nothing is written.
    private void UpdateSummary(List<object> items)
    {
        var selectedNames = items.Where(IsSelected).Select(NameOf).ToList();
        LabelText = DropDownPickerText.Label(PlaceholderText, selectedNames);
        MoreBadgeText = DropDownPickerText.MoreBadge(selectedNames);
        HasMoreSelected = MoreBadgeText.Length > 0;
        IsPlaceholderShown = selectedNames.Count == 0;
        PseudoClasses.Set(":has-selection", selectedNames.Count > 0);
        SelectionToolTipText = DropDownPickerText.SelectionToolTip(PlaceholderText, selectedNames);
    }



    private string NameOf(object item)
    {
        if (DisplayMemberBinding is not { } binding)
        {
            return item.ToString() ?? string.Empty;
        }

        if (_nameEvaluator is null)
        {
            _nameEvaluator = new TextBlock();
            _nameEvaluator.Bind(TextBlock.TextProperty, binding);
        }

        _nameEvaluator.DataContext = item;
        return _nameEvaluator.Text ?? string.Empty;
    }

    /// <summary>The names of all items, whatever the search keeps - the open list's width comes from them.</summary>
    internal IEnumerable<string> ItemNames() => Items().Select(NameOf).ToList();

    private List<object> Items() => ItemsSource?.Cast<object>().ToList() ?? [];

    private bool IsSelected(object item) => SelectionMode == DropDownPickerMode.Multiple
        ? SelectedItems?.Contains(item) == true
        : Equals(SelectedItem, item);
}
