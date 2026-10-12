using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// The heading of a section of a card's text ("Akcje", "Reakcje"): a short accent bar, the
/// section's title and a chevron. It marks where a section starts so the eye can jump between them;
/// a click anywhere on it flips <see cref="IsExpanded"/>, and the view that owns the section's
/// content shows or hides it. The state lives only in the control - nothing remembers it. The look
/// belongs to the frame's control theme (Themes/Controls/SectionHeading.axaml).
/// </summary>
public sealed class SectionHeading : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<SectionHeading, string?>(nameof(Text));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<SectionHeading, bool>(nameof(IsExpanded), defaultValue: true);

    public SectionHeading()
    {
        UpdateCollapsedClass();
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>True while the section's content is shown.</summary>
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsExpandedProperty)
            UpdateCollapsedClass();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            IsExpanded = !IsExpanded;
            e.Handled = true;
        }
    }

    private void UpdateCollapsedClass() => PseudoClasses.Set(":collapsed", !IsExpanded);
}
