using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// The GM's note on an entity's card: multi-line text that reads as the card's prose and is edited
/// in place. It is one text box in both forms - at rest the theme draws no frame, so the same text
/// presenter shows the text to read and, after a click, the field; the form changes only what is
/// drawn around the text, never where the text stands (Themes/Controls/NoteField.axaml).
/// </summary>
/// <remarks>
/// <see cref="TextBox.Text"/> follows every key; whoever keeps the note saves it on
/// <see cref="Committed"/>, raised when the field loses focus (a click elsewhere, Tab, Escape), so a
/// note is never saved per keystroke and never lost on Escape.
/// </remarks>
public sealed class NoteField : TextBox
{
    public static readonly RoutedEvent<NoteCommittedEventArgs> CommittedEvent =
        RoutedEvent.Register<NoteField, NoteCommittedEventArgs>(nameof(Committed), RoutingStrategies.Bubble);

    public event EventHandler<NoteCommittedEventArgs>? Committed
    {
        add => AddHandler(CommittedEvent, value);
        remove => RemoveHandler(CommittedEvent, value);
    }

    protected override Type StyleKeyOverride => typeof(NoteField);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsFocusedProperty && !change.GetNewValue<bool>())
        {
            RaiseEvent(new NoteCommittedEventArgs(CommittedEvent, Text ?? string.Empty));
        }
        else if (change.Property == IsEffectivelyEnabledProperty)
        {
            // A disabled note fades through an opacity layer with nothing behind its text, and
            // subpixel smoothing breaks apart in a transparent layer - greyscale stays whole.
            TextOptions.SetTextRenderingMode(this, change.GetNewValue<bool>() ? TextRenderingMode.Unspecified : TextRenderingMode.Antialias);
        }
    }
}

/// <summary>The note's text at the moment the GM left the field.</summary>
public sealed class NoteCommittedEventArgs(RoutedEvent routedEvent, string text) : RoutedEventArgs(routedEvent)
{
    public string Text { get; } = text;
}
