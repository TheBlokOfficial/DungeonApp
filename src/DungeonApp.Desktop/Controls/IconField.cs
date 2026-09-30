using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Pole z ikoną: ramka pola (tło, krawędź, zaokrąglenie, wysokość) z ikoną na początku i polem
/// tekstowym widoku w środku - pole tekstowe (<see cref="ContentControl.Content"/>, zwykły
/// <see cref="TextBox"/> z wiązaniami widoku) zaczyna się za ikoną i nie ma własnej krawędzi ani tła.
/// Ikona nie leży w polu tekstowym, więc kliknięcie w nią, obok niej ani w krawędź ramki nie stawia
/// karetki i nie zaczyna zaznaczania. <see cref="IsClearable"/> dokłada na końcu ramki przycisk
/// czyszczenia (x), widoczny tylko przy niepustym polu; czyszczenie zostawia fokus w polu. Wygląd
/// i stany należą do motywu ramy (Themes/Controls/IconField.axaml). Zmienia wyłącznie tekst pola
/// tekstowego na jawne kliknięcie przycisku czyszczenia.
/// </summary>
[TemplatePart(ClearButtonPartName, typeof(Button))]
public sealed class IconField : ContentControl
{
    private const string ClearButtonPartName = "PART_ClearButton";

    /// <summary>Ikona na początku pola (rysunek z zestawu ikon, w kolorze z motywu).</summary>
    public static readonly StyledProperty<DrawingImage?> IconProperty =
        AvaloniaProperty.Register<IconField, DrawingImage?>(nameof(Icon));

    /// <summary>Czy pole ma przycisk czyszczenia na końcu (domyślnie nie).</summary>
    public static readonly StyledProperty<bool> IsClearableProperty =
        AvaloniaProperty.Register<IconField, bool>(nameof(IsClearable));

    /// <summary>Czy przycisk czyszczenia jest widoczny: pole go ma i tekst nie jest pusty.</summary>
    public static readonly DirectProperty<IconField, bool> CanClearProperty =
        AvaloniaProperty.RegisterDirect<IconField, bool>(nameof(CanClear), field => field.CanClear);

    private TextBox? _textBox;
    private Button? _clearButton;
    private bool _canClear;

    public DrawingImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsClearable
    {
        get => GetValue(IsClearableProperty);
        set => SetValue(IsClearableProperty, value);
    }

    public bool CanClear
    {
        get => _canClear;
        private set => SetAndRaise(CanClearProperty, ref _canClear, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_clearButton is not null)
        {
            _clearButton.Click -= OnClearClick;
        }

        _clearButton = e.NameScope.Find<Button>(ClearButtonPartName);
        if (_clearButton is not null)
        {
            _clearButton.Click += OnClearClick;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty)
        {
            AttachTextBox(change.NewValue as TextBox);
        }
        else if (change.Property == IsClearableProperty)
        {
            UpdateCanClear();
        }
    }

    private void AttachTextBox(TextBox? textBox)
    {
        if (_textBox is not null)
        {
            _textBox.PropertyChanged -= OnTextBoxPropertyChanged;
        }

        _textBox = textBox;
        if (_textBox is not null)
        {
            _textBox.PropertyChanged += OnTextBoxPropertyChanged;
        }

        UpdateCanClear();
    }

    private void OnTextBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.TextProperty)
        {
            UpdateCanClear();
        }
    }

    private void UpdateCanClear()
    {
        CanClear = IsClearable && !string.IsNullOrEmpty(_textBox?.Text);
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        _textBox?.Clear();
    }
}
