using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Icon field: field frame (background, border, radius, height) with a leading icon and the view's
/// text box inside - text box (<see cref="ContentControl.Content"/>, regular
/// <see cref="TextBox"/> with view bindings) starts after the icon and has no border or background of its own.
/// Icon is outside the text box, so clicking it, beside it or on the frame border neither places
/// the caret nor starts selection. <see cref="IsClearable"/> adds a clear button
/// (x) at the frame end, visible only for a non-empty field; clearing keeps field focus. Appearance
/// and states belong to the shell theme (Themes/Controls/IconField.axaml). Changes only text-box
/// text on an explicit clear-button click.
/// </summary>
[TemplatePart(ClearButtonPartName, typeof(Button))]
public sealed class IconField : ContentControl
{
    private const string ClearButtonPartName = "PART_ClearButton";

    /// <summary>Leading field icon (glyph from the icon set, in the theme colour).</summary>
    public static readonly StyledProperty<DrawingImage?> IconProperty =
        AvaloniaProperty.Register<IconField, DrawingImage?>(nameof(Icon));

    /// <summary>Whether the field has a trailing clear button (false by default).</summary>
    public static readonly StyledProperty<bool> IsClearableProperty =
        AvaloniaProperty.Register<IconField, bool>(nameof(IsClearable));

    /// <summary>Whether the clear button is visible: field has one and text is non-empty.</summary>
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
