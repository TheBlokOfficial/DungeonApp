using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using DungeonApp.Desktop.Themes;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// List sort control: standalone "Sortuj: {pole}" link with arrow opening a field menu
/// (checkmark beside the selected field, direction in the adjacent muted column), followed by a direction button
/// with an icon that reverses direction. Sorts nothing - holds the view's selection: field
/// (<see cref="SelectedOption"/>) and direction (<see cref="IsDescending"/>), both bound two-way.
/// Changes them only on an explicit menu-item or direction-button click.
/// </summary>
/// <remarks>
/// Direction labels depend on selected field type: text - "A–Z" / "Z–A", numeric (listed
/// in <see cref="NumericOptions"/>) - "rosnąco" / "malejąco". When <see cref="NumericOptions"/> is
/// empty (null), field types are unknown: menu shows no direction, button tooltip says
/// "rosnąco" / "malejąco". Direction change flips the icon across the horizontal axis (flattening to zero
/// and expanding inverted, DungeonMotionDuration, easing out); direction changes immediately, motion
/// catches up; with system animations disabled (<see cref="SystemMotion.IsReduced"/>) - no
/// motion. Appearance belongs to the shell theme (Themes/Controls/SortPicker.axaml).
/// </remarks>
[TemplatePart(FieldButtonPartName, typeof(Button))]
[TemplatePart(DirectionButtonPartName, typeof(Button))]
[TemplatePart(DirectionGlyphPartName, typeof(Control))]
public sealed class SortPicker : TemplatedControl
{
    private const string FieldButtonPartName = "PART_FieldButton";
    private const string DirectionButtonPartName = "PART_DirectionButton";
    private const string DirectionGlyphPartName = "PART_DirectionGlyph";

    /// <summary>Sortable field names - menu items, in this order.</summary>
    public static readonly StyledProperty<IEnumerable<string>?> OptionsProperty =
        AvaloniaProperty.Register<SortPicker, IEnumerable<string>?>(nameof(Options));

    /// <summary>Selected field (one of <see cref="Options"/>).</summary>
    public static readonly StyledProperty<string?> SelectedOptionProperty =
        AvaloniaProperty.Register<SortPicker, string?>(nameof(SelectedOption), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Direction: descending (Z–A) rather than ascending (A–Z).</summary>
    public static readonly StyledProperty<bool> IsDescendingProperty =
        AvaloniaProperty.Register<SortPicker, bool>(nameof(IsDescending), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Numeric fields among <see cref="Options"/>; null - field types unknown.</summary>
    public static readonly StyledProperty<IEnumerable<string>?> NumericOptionsProperty =
        AvaloniaProperty.Register<SortPicker, IEnumerable<string>?>(nameof(NumericOptions));

    /// <summary>Whether direction can be reversed (true by default); false - direction button disabled.</summary>
    public static readonly StyledProperty<bool> CanReverseProperty =
        AvaloniaProperty.Register<SortPicker, bool>(nameof(CanReverse), defaultValue: true);

    /// <summary>Link label: "Sortuj: {pole}".</summary>
    public static readonly DirectProperty<SortPicker, string> FieldTextProperty =
        AvaloniaProperty.RegisterDirect<SortPicker, string>(nameof(FieldText), picker => picker.FieldText);

    /// <summary>Direction button tooltip: "Kierunek: A–Z", etc.</summary>
    public static readonly DirectProperty<SortPicker, string> DirectionToolTipProperty =
        AvaloniaProperty.RegisterDirect<SortPicker, string>(nameof(DirectionToolTip), picker => picker.DirectionToolTip);

    private Button? _fieldButton;
    private Button? _directionButton;
    private Control? _directionGlyph;
    private MenuFlyout? _menu;
    private string _fieldText = "Sortuj:";
    private string _directionToolTip = "Kierunek: rosnąco";

    public IEnumerable<string>? Options
    {
        get => GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public string? SelectedOption
    {
        get => GetValue(SelectedOptionProperty);
        set => SetValue(SelectedOptionProperty, value);
    }

    public bool IsDescending
    {
        get => GetValue(IsDescendingProperty);
        set => SetValue(IsDescendingProperty, value);
    }

    public IEnumerable<string>? NumericOptions
    {
        get => GetValue(NumericOptionsProperty);
        set => SetValue(NumericOptionsProperty, value);
    }

    public bool CanReverse
    {
        get => GetValue(CanReverseProperty);
        set => SetValue(CanReverseProperty, value);
    }

    public string FieldText
    {
        get => _fieldText;
        private set => SetAndRaise(FieldTextProperty, ref _fieldText, value);
    }

    public string DirectionToolTip
    {
        get => _directionToolTip;
        private set => SetAndRaise(DirectionToolTipProperty, ref _directionToolTip, value);
    }

    /// <summary>
    /// Direction label for the selected field ("A–Z", "Z–A", "rosnąco", "malejąco"), or null when field
    /// types are unknown.
    /// </summary>
    internal string? DirectionText(string? option)
    {
        if (NumericOptions is null)
        {
            return null;
        }

        var numeric = option is not null && NumericOptions.Contains(option);
        return numeric
            ? (IsDescending ? "malejąco" : "rosnąco")
            : (IsDescending ? "Z–A" : "A–Z");
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_directionButton is not null)
        {
            _directionButton.Click -= OnDirectionClick;
        }

        _fieldButton = e.NameScope.Find<Button>(FieldButtonPartName);
        _directionButton = e.NameScope.Find<Button>(DirectionButtonPartName);
        _directionGlyph = e.NameScope.Find<Control>(DirectionGlyphPartName);

        // Flyout is not a control - the theme cannot reach it; this control sets placement:
        // below the link, left-aligned with it (gap - DungeonFlyoutMargin from the menu theme).
        _menu = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedLeft };
        if (_fieldButton is not null)
        {
            _fieldButton.Flyout = _menu;
        }

        if (_directionButton is not null)
        {
            _directionButton.Click += OnDirectionClick;
        }

        UpdateTexts();
        RebuildMenu();
        SetGlyphScale(animate: false);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OptionsProperty)
        {
            RebuildMenu();
        }
        else if (change.Property == SelectedOptionProperty || change.Property == NumericOptionsProperty)
        {
            UpdateTexts();
            RefreshMenu();
        }
        else if (change.Property == IsDescendingProperty)
        {
            UpdateTexts();
            RefreshMenu();
            SetGlyphScale(animate: true);
        }
    }

    private void OnDirectionClick(object? sender, RoutedEventArgs e) => IsDescending = !IsDescending;

    // Menu items - one per field; rebuilt only when the field list changes. Selection and direction
    // refresh existing items (RefreshMenu), so a clicked item does not disappear under the pointer.
    private void RebuildMenu()
    {
        if (_menu is null)
        {
            return;
        }

        _menu.Items.Clear();
        foreach (var option in Options ?? [])
        {
            var item = new MenuItem { Header = option, ToggleType = MenuItemToggleType.CheckBox };

            // A checkable item toggles its own checkmark before the click reaches here - clicking
            // the selected field again would remove the checkmark without changing selection. Checkmark belongs to selection,
            // so item state is reapplied after clicking.
            item.Click += (_, _) =>
            {
                SelectedOption = option;
                RefreshMenu();
            };
            _menu.Items.Add(item);
        }

        RefreshMenu();
    }

    // Checkmark and direction label beside the selected field; other items have neither.
    private void RefreshMenu()
    {
        if (_menu is null)
        {
            return;
        }

        foreach (var item in _menu.Items.OfType<MenuItem>())
        {
            var option = item.Header as string;
            var selected = option is not null && option == SelectedOption;
            item.IsChecked = selected;
            MenuItemTrailing.SetText(item, selected ? DirectionText(option) : null);
        }
    }

    private void UpdateTexts()
    {
        FieldText = $"Sortuj: {SelectedOption}";
        var numeric = NumericOptions is null
                      || (SelectedOption is not null && NumericOptions.Contains(SelectedOption));
        var word = numeric
            ? (IsDescending ? "malejąco" : "rosnąco")
            : (IsDescending ? "Z–A" : "A–Z");
        DirectionToolTip = $"Kierunek: {word}";
    }

    // Icon drawing means "malejąco"; "rosnąco" uses the same drawing flipped across the horizontal axis.
    private void SetGlyphScale(bool animate)
    {
        if (_directionGlyph is null)
        {
            return;
        }

        if (_directionGlyph.RenderTransform is not ScaleTransform scale)
        {
            scale = new ScaleTransform();
            _directionGlyph.RenderTransform = scale;
        }

        var from = scale.ScaleY;
        var to = IsDescending ? 1d : -1d;
        scale.ScaleY = to;
        if (animate && from != to)
        {
            Flip(_directionGlyph, from, to, SystemMotion.IsReduced);
        }
    }

    /// <summary>
    /// Icon flip: vertical scale from <paramref name="from"/> through zero to <paramref name="to"/>
    /// over DungeonMotionDuration, easing out. State (base scale value) is already set - motion
    /// only catches up. With <paramref name="reduced"/>, nothing moves.
    /// </summary>
    internal static void Flip(Control glyph, double from, double to, bool reduced)
    {
        if (reduced)
        {
            return;
        }

        var duration = glyph.TryFindResource("DungeonMotionDuration", out var d) && d is TimeSpan span
            ? span
            : TimeSpan.FromMilliseconds(150);

        var animation = new Animation
        {
            Duration = duration,
            Easing = new CubicEaseOut(),
            FillMode = FillMode.None,
            Children =
            {
                new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(ScaleTransform.ScaleYProperty, from) } },
                new KeyFrame { Cue = new Cue(0.5d), Setters = { new Setter(ScaleTransform.ScaleYProperty, 0d) } },
                new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(ScaleTransform.ScaleYProperty, to) } },
            },
        };

        _ = animation.RunAsync(glyph);
    }
}
