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
/// always there. The current number has two forms in one place: text to read, and on a click (or Tab)
/// a change field that takes "-5", "+3" or "=10" (<see cref="NumberChange"/>). Before Enter the note
/// line shows what the change will do; Enter raises <see cref="ChangeAccepted"/> and runs
/// <see cref="ChangeCommand"/>, Escape or a click elsewhere drops the entry. The control never changes
/// <see cref="Value"/> itself: the arithmetic (floors, ceilings, which pool goes first) belongs to the
/// system, which supplies the preview through <see cref="Preview"/> and sets the new value.
/// </summary>
/// <remarks>
/// The field is always laid out, over the whole value line, and only shown while it has focus, so
/// switching forms changes no size or position. The look belongs to the frame's control theme
/// (Themes/Controls/TrackedValueTile.axaml).
/// </remarks>
[TemplatePart(ValueHitPart, typeof(Control))]
[TemplatePart(ValuePart, typeof(TextBlock))]
[TemplatePart(MaximumPart, typeof(TextBlock))]
[TemplatePart(FieldPart, typeof(TextBox))]
[PseudoClasses(EditingClass, PreviewClass, InvalidClass)]
public sealed class TrackedValueTile : TemplatedControl
{
    public const string EditingClass = ":editing";
    public const string PreviewClass = ":preview";
    public const string InvalidClass = ":invalid";

    private const string ValueHitPart = "PART_ValueHit";
    private const string ValuePart = "PART_Value";
    private const string MaximumPart = "PART_Maximum";
    private const string FieldPart = "PART_Field";
    private const string InvalidMessage = "Wpisz -5, +3 albo =10";

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

    private Control? _valueHit;
    private TextBlock? _value;
    private TextBlock? _maximum;
    private TextBox? _field;
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
    /// The system's text for what a change will do ("−5 → 4"), shown in the note line before Enter.
    /// Without it the line shows the change itself.
    /// </summary>
    public Func<NumberChange, string>? Preview
    {
        get => GetValue(PreviewProperty);
        set => SetValue(PreviewProperty, value);
    }

    /// <summary>Run with the <see cref="NumberChange"/> when Enter confirms a valid entry.</summary>
    public ICommand? ChangeCommand
    {
        get => GetValue(ChangeCommandProperty);
        set => SetValue(ChangeCommandProperty, value);
    }

    /// <summary>The preview or the error shown in the note line while the field is open.</summary>
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

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_valueHit is not null)
        {
            _valueHit.PointerPressed -= OnValuePressed;
        }

        if (_field is not null)
        {
            _field.GotFocus -= OnFieldGotFocus;
            _field.LostFocus -= OnFieldLostFocus;
            _field.TextChanged -= OnFieldTextChanged;
            _field.RemoveHandler(KeyDownEvent, OnFieldKeyDown);
        }

        base.OnApplyTemplate(e);

        _valueHit = e.NameScope.Find<Control>(ValueHitPart);
        _value = e.NameScope.Find<TextBlock>(ValuePart);
        _maximum = e.NameScope.Find<TextBlock>(MaximumPart);
        _field = e.NameScope.Find<TextBox>(FieldPart);

        if (_valueHit is not null)
        {
            _valueHit.PointerPressed += OnValuePressed;
        }

        if (_field is not null)
        {
            _field.Text = Value;
            _field.GotFocus += OnFieldGotFocus;
            _field.LostFocus += OnFieldLostFocus;
            _field.TextChanged += OnFieldTextChanged;
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
        return base.MeasureOverride(availableSize);
    }

    // The two numbers differ in size, so top or bottom alignment would leave their baselines apart;
    // the smaller one is pushed down by the difference between the two baselines.
    private void AlignMaximumToValueBaseline()
    {
        if (_value is null || _maximum is null)
        {
            return;
        }

        var top = Math.Max(0, _value.TextLayout.Baseline - _maximum.TextLayout.Baseline);
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

    private void OnValuePressed(object? sender, PointerPressedEventArgs e)
    {
        if (_field is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        e.Handled = true;
        _field.Focus(NavigationMethod.Pointer);
        _field.SelectAll();
    }

    private void OnPressedInside(object? sender, PointerPressedEventArgs e)
    {
        if (_editing && _field is not null && !(e.Source is Visual source && (source == _field || _field.IsVisualAncestorOf(source))))
        {
            Leave();
        }
    }

    private void OnFieldGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (_editing || _field is null)
        {
            return;
        }

        _textOnEntry = Value ?? "";
        _field.Text = _textOnEntry;
        _field.SelectAll();
        _editing = true;
        PseudoClasses.Set(EditingClass, true);
        ShowEntry(insist: false);
    }

    private void OnFieldLostFocus(object? sender, FocusChangedEventArgs e)
    {
        if (!_editing || _field is null)
        {
            return;
        }

        _editing = false;
        PseudoClasses.Set(EditingClass, false);
        SetMessage(null, preview: false, invalid: false);
        _field.Text = Value;
    }

    private void OnFieldTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_editing)
        {
            ShowEntry(insist: false);
        }
    }

    private void OnFieldKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Return) || _field is null)
        {
            return;
        }

        e.Handled = true;
        var text = _field.Text ?? "";
        if (text == _textOnEntry)
        {
            Leave();
            return;
        }

        if (!NumberChange.TryParse(text, out var change))
        {
            ShowEntry(insist: true);
            return;
        }

        RaiseEvent(new NumberChangeEventArgs(ChangeAcceptedEvent, change));
        if (ChangeCommand is { } command && command.CanExecute(change))
        {
            command.Execute(change);
        }

        Leave();
    }

    // Untouched text and an unfinished entry (empty, a lone sign) stay quiet while typing; Enter on an
    // unfinished entry shows the error too.
    private void ShowEntry(bool insist)
    {
        var text = _field?.Text ?? "";
        if (text == _textOnEntry && !insist)
        {
            SetMessage(null, preview: false, invalid: false);
        }
        else if (NumberChange.TryParse(text, out var change))
        {
            SetMessage(Preview?.Invoke(change) ?? change.ToString(), preview: true, invalid: false);
        }
        else if (!insist && text.Trim() is "" or "-" or "+" or "=" or "−")
        {
            SetMessage(null, preview: false, invalid: false);
        }
        else
        {
            SetMessage(InvalidMessage, preview: false, invalid: true);
        }
    }

    private void SetMessage(string? message, bool preview, bool invalid)
    {
        Message = message;
        PseudoClasses.Set(PreviewClass, preview);
        PseudoClasses.Set(InvalidClass, invalid);
    }

    private void Leave()
    {
        TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
    }
}
