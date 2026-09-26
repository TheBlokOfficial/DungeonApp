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

    public static readonly DirectProperty<DropDownPicker, string> SummaryTextProperty =
        AvaloniaProperty.RegisterDirect<DropDownPicker, string>(nameof(SummaryText), picker => picker.SummaryText);

    public static readonly DirectProperty<DropDownPicker, bool> HasNoMatchesProperty =
        AvaloniaProperty.RegisterDirect<DropDownPicker, bool>(nameof(HasNoMatches), picker => picker.HasNoMatches);

    public static readonly StyledProperty<bool> ShowsSelectionCountProperty =
        AvaloniaProperty.Register<DropDownPicker, bool>(nameof(ShowsSelectionCount));

    public static readonly StyledProperty<IBrush?> PlaceholderForegroundProperty =
        AvaloniaProperty.Register<DropDownPicker, IBrush?>(nameof(PlaceholderForeground));

    public static readonly StyledProperty<IBrush?> GlyphForegroundProperty =
        AvaloniaProperty.Register<DropDownPicker, IBrush?>(nameof(GlyphForeground));

    public static readonly StyledProperty<double> GlyphSpacingProperty =
        AvaloniaProperty.Register<DropDownPicker, double>(nameof(GlyphSpacing));

    public static readonly DirectProperty<DropDownPicker, int> SelectedCountProperty =
        AvaloniaProperty.RegisterDirect<DropDownPicker, int>(nameof(SelectedCount), picker => picker.SelectedCount);

    public static readonly DirectProperty<DropDownPicker, bool> IsPlaceholderShownProperty =
        AvaloniaProperty.RegisterDirect<DropDownPicker, bool>(
            nameof(IsPlaceholderShown), picker => picker.IsPlaceholderShown);

    public static readonly DirectProperty<DropDownPicker, bool> IsSelectionCountShownProperty =
        AvaloniaProperty.RegisterDirect<DropDownPicker, bool>(
            nameof(IsSelectionCountShown), picker => picker.IsSelectionCountShown);

    private IReadOnlyList<DropDownPickerRow> _rows = [];
    private string _summaryText = string.Empty;
    private bool _hasNoMatches;
    private int _selectedCount;
    private bool _isPlaceholderShown = true;
    private bool _isSelectionCountShown;
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

    /// <summary>What the closed picker says (<see cref="DropDownPickerText.Summary"/>).</summary>
    public string SummaryText
    {
        get => _summaryText;
        private set => SetAndRaise(SummaryTextProperty, ref _summaryText, value);
    }

    /// <summary>There are items, but the search keeps none of them.</summary>
    public bool HasNoMatches
    {
        get => _hasNoMatches;
        private set => SetAndRaise(HasNoMatchesProperty, ref _hasNoMatches, value);
    }

    /// <summary>
    /// The look of a filter chip (the frame's DungeonChipPicker theme): the closed picker always says
    /// its <see cref="PlaceholderText"/> - the filter's name - and, when something is selected, the
    /// number of selected items in a badge after it, instead of the selected names.
    /// </summary>
    public bool ShowsSelectionCount
    {
        get => GetValue(ShowsSelectionCountProperty);
        set => SetValue(ShowsSelectionCountProperty, value);
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

    /// <summary>The gap before the drop-down arrow (and before the count badge); set by the control theme.</summary>
    public double GlyphSpacing
    {
        get => GetValue(GlyphSpacingProperty);
        set => SetValue(GlyphSpacingProperty, value);
    }

    /// <summary>How many items are selected.</summary>
    public int SelectedCount
    {
        get => _selectedCount;
        private set => SetAndRaise(SelectedCountProperty, ref _selectedCount, value);
    }

    /// <summary>
    /// The closed picker shows the placeholder: always with <see cref="ShowsSelectionCount"/> (the
    /// filter's name), otherwise while nothing is selected.
    /// </summary>
    public bool IsPlaceholderShown
    {
        get => _isPlaceholderShown;
        private set => SetAndRaise(IsPlaceholderShownProperty, ref _isPlaceholderShown, value);
    }

    /// <summary>The count badge is shown: <see cref="ShowsSelectionCount"/> and something selected.</summary>
    public bool IsSelectionCountShown
    {
        get => _isSelectionCountShown;
        private set => SetAndRaise(IsSelectionCountShownProperty, ref _isSelectionCountShown, value);
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
        else if (change.Property == ShowsSelectionCountProperty)
        {
            UpdateClosedLook();
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
            SetCurrentValue(SelectedItemProperty, row.Item);
            SetCurrentValue(IsDropDownOpenProperty, false);
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

    // In the order of the items, not of clicking - the closed text does not jump.
    private void UpdateSummary(List<object> items)
    {
        var selectedNames = items.Where(IsSelected).Select(NameOf).ToList();
        SummaryText = DropDownPickerText.Summary(selectedNames);
        SelectedCount = selectedNames.Count;
        UpdateClosedLook();
    }

    // What the closed picker shows follows the selection and the chip look; nothing is written.
    private void UpdateClosedLook()
    {
        var hasSelection = SelectedCount > 0;
        PseudoClasses.Set(":has-selection", hasSelection);
        IsPlaceholderShown = ShowsSelectionCount || !hasSelection;
        IsSelectionCountShown = ShowsSelectionCount && hasSelection;
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

    private List<object> Items() => ItemsSource?.Cast<object>().ToList() ?? [];

    private bool IsSelected(object item) => SelectionMode == DropDownPickerMode.Multiple
        ? SelectedItems?.Contains(item) == true
        : Equals(SelectedItem, item);
}
