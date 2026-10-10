using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Text that reads as part of the card and is edited in place - the GM's note, the current number of
/// a tracked value. It is one text box in both forms: at rest the theme draws no frame, so the same
/// text presenter shows the text to read and, after a click, the field; the form changes only what is
/// drawn around the text, never where the text stands (Themes/Controls/EditableText.axaml). Its uses
/// differ only in font and in the settings below.
/// </summary>
/// <remarks>
/// The control is as wide as its text (its longest line, or the placeholder while empty) and never
/// narrower than <c>MinWidth</c>, so the hover background and the outline cover the
/// text, not the free room beside it. <see cref="TextBox.Text"/> follows every key; whoever keeps the
/// text saves it on <see cref="Committed"/>, raised when the field loses focus (a click elsewhere, Tab,
/// Escape), so the text is never saved per keystroke.
/// </remarks>
public sealed class EditableText : TextBox
{
    public static readonly StyledProperty<bool> SelectAllOnEntryProperty =
        AvaloniaProperty.Register<EditableText, bool>(nameof(SelectAllOnEntry));

    public static readonly StyledProperty<Thickness> FrameOutsetProperty =
        AvaloniaProperty.Register<EditableText, Thickness>(nameof(FrameOutset));

    public static readonly StyledProperty<Func<string, bool>?> TextFilterProperty =
        AvaloniaProperty.Register<EditableText, Func<string, bool>?>(nameof(TextFilter));

    public static readonly RoutedEvent<EditableTextCommittedEventArgs> CommittedEvent =
        RoutedEvent.Register<EditableText, EditableTextCommittedEventArgs>(nameof(Committed), RoutingStrategies.Bubble);

    private bool _entering;
    private string _lastAllowedText = "";

    public event EventHandler<EditableTextCommittedEventArgs>? Committed
    {
        add => AddHandler(CommittedEvent, value);
        remove => RemoveHandler(CommittedEvent, value);
    }

    /// <summary>
    /// Entering the field (click or Tab) selects the whole text, so typing replaces it - right for a
    /// number that is replaced by a change. Off, the caret lands where the click was, because a typed
    /// letter must not replace a whole note.
    /// </summary>
    public bool SelectAllOnEntry
    {
        get => GetValue(SelectAllOnEntryProperty);
        set => SetValue(SelectAllOnEntryProperty, value);
    }

    /// <summary>
    /// How far the hover background and the editing outline stand around the text, as a margin
    /// (negative reaches out of the control's bounds). Set per use, because a large number's line box
    /// has room above and below its digits that a note's line has not.
    /// </summary>
    public Thickness FrameOutset
    {
        get => GetValue(FrameOutsetProperty);
        set => SetValue(FrameOutsetProperty, value);
    }

    /// <summary>
    /// Text the field may hold while the GM types; a key or a paste that would make anything else is
    /// refused, so the field never shows an entry it cannot take. Text set from code is not filtered.
    /// </summary>
    public Func<string, bool>? TextFilter
    {
        get => GetValue(TextFilterProperty);
        set => SetValue(TextFilterProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(EditableText);

    public EditableText()
    {
        // Tunnelling, so the press is taken before the text box places the caret where the click was.
        AddHandler(PointerPressedEvent, OnPressedToEnter, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    // The press that enters the field selects everything; the focus may already have moved here when
    // the press arrives (the window focuses what is clicked), so "entering" lasts until the first
    // release or key.
    private void OnPressedToEnter(object? sender, PointerPressedEventArgs e)
    {
        if (SelectAllOnEntry && (!IsFocused || _entering) && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            Focus(NavigationMethod.Pointer);
            SelectAll();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _entering = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        _entering = false;
        base.OnKeyDown(e);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        _entering = false;
        if (TextFilter is { } filter && !string.IsNullOrEmpty(e.Text) && !filter(TextAfterTyping(e.Text)))
        {
            e.Handled = true;
            return;
        }

        base.OnTextInput(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsFocusedProperty)
        {
            OnFocusChanged(change.GetNewValue<bool>());
        }
        else if (change.Property == TextProperty)
        {
            KeepToFilter();
        }
        else if (change.Property == IsEffectivelyEnabledProperty)
        {
            // Disabled text fades through an opacity layer with nothing behind it, and subpixel
            // smoothing breaks apart in a transparent layer - greyscale stays whole.
            TextOptions.SetTextRenderingMode(this, change.GetNewValue<bool>() ? TextRenderingMode.Unspecified : TextRenderingMode.Antialias);
        }
    }

    private void OnFocusChanged(bool focused)
    {
        _entering = focused;
        if (focused)
        {
            _lastAllowedText = Text ?? "";
            if (SelectAllOnEntry)
            {
                SelectAll();
            }

            return;
        }

        RaiseEvent(new EditableTextCommittedEventArgs(CommittedEvent, Text ?? string.Empty));
    }

    // Typing is stopped before it lands (OnTextInput); this catches what arrives another way - a paste,
    // a drop - and puts back the last text the filter took.
    private void KeepToFilter()
    {
        if (!IsFocused || TextFilter is not { } filter)
        {
            return;
        }

        var text = Text ?? "";
        if (filter(text))
        {
            _lastAllowedText = text;
            return;
        }

        if (text != _lastAllowedText)
        {
            var caret = Math.Min(CaretIndex, _lastAllowedText.Length);
            SetCurrentValue(TextProperty, _lastAllowedText);
            CaretIndex = caret;
        }
    }

    private string TextAfterTyping(string typed)
    {
        var text = Text ?? "";
        var start = Math.Clamp(Math.Min(SelectionStart, SelectionEnd), 0, text.Length);
        var end = Math.Clamp(Math.Max(SelectionStart, SelectionEnd), 0, text.Length);
        if (start == end)
        {
            start = end = Math.Clamp(CaretIndex, 0, text.Length);
        }

        return string.Concat(text.AsSpan(0, start), typed, text.AsSpan(end));
    }
}

/// <summary>The text at the moment the GM left the field.</summary>
public sealed class EditableTextCommittedEventArgs(RoutedEvent routedEvent, string text) : RoutedEventArgs(routedEvent)
{
    public string Text { get; } = text;
}
