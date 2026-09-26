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
/// Klocek sortowania listy: odnośnik samodzielny "Sortuj: {pole}" ze strzałką, który otwiera menu pól
/// (ptaszek przy wybranym, przy nim w przygaszonej kolumnie kierunek), i tuż za nim przycisk kierunku
/// z ikoną, która odwraca kierunek. Nic nie sortuje - trzyma wybór widoku: pole
/// (<see cref="SelectedOption"/>) i kierunek (<see cref="IsDescending"/>), oba wiązane w obie strony.
/// Zmienia je wyłącznie na jawne kliknięcie pozycji menu albo przycisku kierunku.
/// </summary>
/// <remarks>
/// Napisy kierunku zależą od rodzaju wybranego pola: tekstowe - "A–Z" / "Z–A", liczbowe (wymienione
/// w <see cref="NumericOptions"/>) - "rosnąco" / "malejąco". Gdy <see cref="NumericOptions"/> jest
/// puste (null), rodzaj pól jest nieznany: menu nie pokazuje kierunku, a podpowiedź przycisku mówi
/// "rosnąco" / "malejąco". Zmiana kierunku przewraca ikonę przez oś poziomą (spłaszczenie do zera
/// i rozłożenie odwróconej, DungeonMotionDuration, z wyhamowaniem); kierunek zmienia się od razu, ruch
/// go dogania; przy wyłączonych animacjach w systemie (<see cref="SystemMotion.IsReduced"/>) - bez
/// ruchu. Wygląd należy do motywu ramy (Themes/DungeonControls.axaml).
/// </remarks>
[TemplatePart(FieldButtonPartName, typeof(Button))]
[TemplatePart(DirectionButtonPartName, typeof(Button))]
[TemplatePart(DirectionGlyphPartName, typeof(Control))]
public sealed class SortPicker : TemplatedControl
{
    private const string FieldButtonPartName = "PART_FieldButton";
    private const string DirectionButtonPartName = "PART_DirectionButton";
    private const string DirectionGlyphPartName = "PART_DirectionGlyph";

    /// <summary>Nazwy pól, po których można sortować - pozycje menu, w tej kolejności.</summary>
    public static readonly StyledProperty<IEnumerable<string>?> OptionsProperty =
        AvaloniaProperty.Register<SortPicker, IEnumerable<string>?>(nameof(Options));

    /// <summary>Wybrane pole (jedna z <see cref="Options"/>).</summary>
    public static readonly StyledProperty<string?> SelectedOptionProperty =
        AvaloniaProperty.Register<SortPicker, string?>(nameof(SelectedOption), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Kierunek: malejąco (Z–A) zamiast rosnąco (A–Z).</summary>
    public static readonly StyledProperty<bool> IsDescendingProperty =
        AvaloniaProperty.Register<SortPicker, bool>(nameof(IsDescending), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Pola liczbowe spośród <see cref="Options"/>; null - rodzaj pól nieznany.</summary>
    public static readonly StyledProperty<IEnumerable<string>?> NumericOptionsProperty =
        AvaloniaProperty.Register<SortPicker, IEnumerable<string>?>(nameof(NumericOptions));

    /// <summary>Czy kierunek da się odwrócić (domyślnie tak); nie - przycisk kierunku wygaszony.</summary>
    public static readonly StyledProperty<bool> CanReverseProperty =
        AvaloniaProperty.Register<SortPicker, bool>(nameof(CanReverse), defaultValue: true);

    /// <summary>Napis odnośnika: "Sortuj: {pole}".</summary>
    public static readonly DirectProperty<SortPicker, string> FieldTextProperty =
        AvaloniaProperty.RegisterDirect<SortPicker, string>(nameof(FieldText), picker => picker.FieldText);

    /// <summary>Podpowiedź przycisku kierunku: "Kierunek: A–Z" itd.</summary>
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
    /// Napis kierunku dla wybranego pola ("A–Z", "Z–A", "rosnąco", "malejąco"), albo null, gdy rodzaj
    /// pól jest nieznany.
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

        // Okienko wysuwane nie jest kontrolką - motyw go nie dosięga; położenie ustala klocek:
        // pod odnośnikiem, wyrównane do jego lewej krawędzi (odstęp - DungeonFlyoutMargin z motywu menu).
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

    // Pozycje menu - jedna na pole; budowane od nowa tylko przy zmianie listy pól. Wybór i kierunek
    // odświeżają istniejące pozycje (RefreshMenu), więc kliknięta pozycja nie znika spod myszy.
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
            item.Click += (_, _) => SelectedOption = option;
            _menu.Items.Add(item);
        }

        RefreshMenu();
    }

    // Ptaszek i napis kierunku przy wybranym polu; pozostałe pozycje bez nich.
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

    // Rysunek ikony znaczy "malejąco"; "rosnąco" to ten sam rysunek przewrócony przez oś poziomą.
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
    /// Przewrócenie ikony: skala w pionie z <paramref name="from"/> przez zero do <paramref name="to"/>
    /// w DungeonMotionDuration, z wyhamowaniem. Stan (wartość bazowa skali) jest już ustawiony - ruch
    /// go tylko dogania. Przy <paramref name="reduced"/> nic się nie rusza.
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
