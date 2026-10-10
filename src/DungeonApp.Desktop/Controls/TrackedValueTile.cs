using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using DungeonApp.Core.State;

namespace DungeonApp.Desktop.Controls;

/// <summary>A change the GM entered in a tracked value's field and confirmed with Enter.</summary>
public sealed class NumberChangeEventArgs(RoutedEvent routedEvent, NumberChange change) : RoutedEventArgs(routedEvent)
{
    public NumberChange Change { get; } = change;
}

/// <summary>
/// A tracked value the GM changes at the table, laid out like <see cref="StatTile"/>: a label with its
/// icon, the current number large with a smaller "/ 13" beside it on one baseline, a note line that is
/// always there. The current number is an <see cref="EditableText"/>: on a click (or Tab) it becomes a
/// change field that takes "12", "-5", "+3" or "=10" (<see cref="NumberChange"/>) and refuses any
/// other key. While a change is typed the note line shows what it will do; Enter raises
/// <see cref="ChangeAccepted"/> and runs <see cref="ChangeCommand"/>, Escape or a click elsewhere drops
/// the entry. The control never changes <see cref="Value"/> itself: the arithmetic (floors, ceilings,
/// which pool goes first) belongs to the system, which supplies the preview through
/// <see cref="Preview"/> and sets the new value.
/// </summary>
/// <remarks>
/// The number's room is the wider of the number and three digits; an entry longer than that spills
/// over "/ 13" (<see cref="EditableText.SpillsOverWhileEditing"/>), so switching forms and typing move
/// nothing. The look belongs to the control theme (Themes/Controls/TrackedValueTile.axaml).
/// </remarks>
[TemplatePart(DigitsPart, typeof(TextBlock))]
[TemplatePart(MaximumPart, typeof(TextBlock))]
[TemplatePart(FieldPart, typeof(EditableText))]
[PseudoClasses(EditingClass, PreviewClass, OverflowingClass)]
public sealed class TrackedValueTile : TemplatedControl
{
    public const string EditingClass = ":editing";
    public const string PreviewClass = ":preview";
    public const string OverflowingClass = ":overflowing";

    private const string DigitsPart = "PART_Digits";
    private const string MaximumPart = "PART_Maximum";
    private const string FieldPart = "PART_Field";

    public static readonly StyledProperty<DrawingImage?> IconProperty =
        AvaloniaProperty.Register<TrackedValueTile, DrawingImage?>(nameof(Icon));

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<TrackedValueTile, string?>(nameof(Label));

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<TrackedValueTile, string?>(nameof(Value));

    public static readonly StyledProperty<string?> MaximumProperty =
        AvaloniaProperty.Register<TrackedValueTile, string?>(nameof(Maximum));

    public static readonly StyledProperty<string?> NoteProperty =
        AvaloniaProperty.Register<TrackedValueTile, string?>(nameof(Note));

    public static readonly StyledProperty<bool> IsUncommittedProperty =
        AvaloniaProperty.Register<TrackedValueTile, bool>(nameof(IsUncommitted));

    public static readonly StyledProperty<Func<NumberChange, string>?> PreviewProperty =
        AvaloniaProperty.Register<TrackedValueTile, Func<NumberChange, string>?>(nameof(Preview));

    public static readonly StyledProperty<ICommand?> ChangeCommandProperty =
        AvaloniaProperty.Register<TrackedValueTile, ICommand?>(nameof(ChangeCommand));

    public static readonly DirectProperty<TrackedValueTile, string?> MessageProperty =
        AvaloniaProperty.RegisterDirect<TrackedValueTile, string?>(nameof(Message), tile => tile.Message);

    public static readonly RoutedEvent<NumberChangeEventArgs> ChangeAcceptedEvent =
        RoutedEvent.Register<TrackedValueTile, NumberChangeEventArgs>(nameof(ChangeAccepted), RoutingStrategies.Bubble);

    private TextBlock? _digits;
    private TextBlock? _maximum;
    private EditableText? _field;
    private string? _message;
    private string _textOnEntry = "";
    private bool _editing;

    public TrackedValueTile()
    {
        // A click on the label or the note while the field is open drops the entry, like a click
        // anywhere else: the shell's focus release treats the whole template as the field.
        AddHandler(PointerPressedEvent, OnPressedInside, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    public DrawingImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>The current number as shown ("9", "—"); the field opens with it, selected.</summary>
    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Shown after the current number as "/ 13" when set.</summary>
    public string? Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Shown under the value when set; its line is reserved either way.</summary>
    public string? Note
    {
        get => GetValue(NoteProperty);
        set => SetValue(NoteProperty, value);
    }

    /// <summary>The value differs from the entity's last commit: the blot sits behind the number.</summary>
    public bool IsUncommitted
    {
        get => GetValue(IsUncommittedProperty);
        set => SetValue(IsUncommittedProperty, value);
    }

    /// <summary>
    /// The system's text for what a change will do ("9 → 4"), shown in the note line before Enter.
    /// Without it the line shows the change itself.
    /// </summary>
    public Func<NumberChange, string>? Preview
    {
        get => GetValue(PreviewProperty);
        set => SetValue(PreviewProperty, value);
    }

    /// <summary>Run with the <see cref="NumberChange"/> when Enter confirms an entry.</summary>
    public ICommand? ChangeCommand
    {
        get => GetValue(ChangeCommandProperty);
        set => SetValue(ChangeCommandProperty, value);
    }

    /// <summary>The preview shown in the note line while the field holds a change.</summary>
    public string? Message
    {
        get => _message;
        private set => SetAndRaise(MessageProperty, ref _message, value);
    }

    public event EventHandler<NumberChangeEventArgs>? ChangeAccepted
    {
        add => AddHandler(ChangeAcceptedEvent, value);
        remove => RemoveHandler(ChangeAcceptedEvent, value);
    }

    /// <summary>
    /// What the field lets the GM type: digits after at most one leading sign ("+", "-", "=", or the
    /// typographic minus the preview shows). Anything else could only end in an entry Enter refuses.
    /// </summary>
    public static bool IsChangeText(string text)
    {
        var digits = text.AsSpan();
        if (digits.Length > 0 && digits[0] is '+' or '-' or '=' or '−')
        {
            digits = digits[1..];
        }

        foreach (var digit in digits)
        {
            if (digit is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_field is not null)
        {
            _field.GotFocus -= OnFieldGotFocus;
            _field.LostFocus -= OnFieldLostFocus;
            _field.TextChanged -= OnFieldTextChanged;
            _field.PropertyChanged -= OnFieldPropertyChanged;
            _field.RemoveHandler(KeyDownEvent, OnFieldKeyDown);
        }

        base.OnApplyTemplate(e);

        _digits = e.NameScope.Find<TextBlock>(DigitsPart);
        _maximum = e.NameScope.Find<TextBlock>(MaximumPart);
        _field = e.NameScope.Find<EditableText>(FieldPart);

        if (_field is not null)
        {
            _field.Text = Value;
            _field.TextFilter = IsChangeText;
            _field.GotFocus += OnFieldGotFocus;
            _field.LostFocus += OnFieldLostFocus;
            _field.TextChanged += OnFieldTextChanged;
            _field.PropertyChanged += OnFieldPropertyChanged;
            _field.AddHandler(KeyDownEvent, OnFieldKeyDown, RoutingStrategies.Tunnel);
        }

        UpdateMaximumText();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MaximumProperty)
        {
            UpdateMaximumText();
        }
        else if (change.Property == ValueProperty && !_editing && _field is not null)
        {
            _field.Text = Value;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        AlignMaximumToValueBaseline();
        var size = base.MeasureOverride(availableSize);

        // Three digits in the number's own font, known once the hidden sample is measured; setting the
        // minimum asks for one more measure pass.
        if (_digits is not null && _field is not null && !_digits.DesiredSize.Width.Equals(_field.MinWidth))
        {
            _field.MinWidth = _digits.DesiredSize.Width;
        }

        return size;
    }

    // The two numbers differ in size, so top or bottom alignment would leave their baselines apart;
    // the smaller one is pushed down by the difference between the two baselines.
    private void AlignMaximumToValueBaseline()
    {
        if (_digits is null || _maximum is null)
        {
            return;
        }

        var top = Math.Max(0, _digits.TextLayout.Baseline - _maximum.TextLayout.Baseline);
        if (!top.Equals(_maximum.Margin.Top))
        {
            _maximum.Margin = new Thickness(0, top, 0, 0);
        }
    }

    private void UpdateMaximumText()
    {
        if (_maximum is null)
        {
            return;
        }

        _maximum.Text = string.IsNullOrEmpty(Maximum) ? null : $"/ {Maximum}";
        _maximum.IsVisible = !string.IsNullOrEmpty(Maximum);
    }

    private void OnPressedInside(object? sender, PointerPressedEventArgs e)
    {
        if (_editing && _field is not null && !(e.Source is Visual source && (source == _field || _field.IsVisualAncestorOf(source))))
        {
            Leave();
        }
    }

    private void OnFieldPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == EditableText.IsOverflowingProperty)
        {
            PseudoClasses.Set(OverflowingClass, e.GetNewValue<bool>());
        }
    }

    private void OnFieldGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (_editing || _field is null)
        {
            return;
        }

        _textOnEntry = Value ?? "";
        _editing = true;
        PseudoClasses.Set(EditingClass, true);
        ShowEntry();
    }

    private void OnFieldLostFocus(object? sender, FocusChangedEventArgs e)
    {
        if (!_editing || _field is null)
        {
            return;
        }

        _editing = false;
        PseudoClasses.Set(EditingClass, false);
        SetMessage(null);
        _field.Text = Value;
    }

    private void OnFieldTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_editing)
        {
            ShowEntry();
        }
    }

    // Enter on the untouched number or an unfinished entry (empty, a lone sign) changes nothing and
    // leaves the field; the filter lets nothing else through.
    private void OnFieldKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Return) || _field is null)
        {
            return;
        }

        e.Handled = true;
        var text = _field.Text ?? "";
        if (text != _textOnEntry && NumberChange.TryParse(text, out var change))
        {
            RaiseEvent(new NumberChangeEventArgs(ChangeAcceptedEvent, change));
            if (ChangeCommand is { } command && command.CanExecute(change))
            {
                command.Execute(change);
            }
        }

        Leave();
    }

    private void ShowEntry()
    {
        var text = _field?.Text ?? "";
        SetMessage(text != _textOnEntry && NumberChange.TryParse(text, out var change)
            ? Preview?.Invoke(change) ?? change.ToString()
            : null);
    }

    private void SetMessage(string? message)
    {
        Message = message;
        PseudoClasses.Set(PreviewClass, message is not null);
    }

    private void Leave()
    {
        TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
    }
}
