using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Shell convention: label trimmed with an ellipsis (tab, segment, button) shows its full
/// text in a tooltip - only when actually trimmed. Enabled in the control template
/// on its content presenter (TrimmedLabelToolTip.IsEnabled="True"), not in views.
/// Checks presenter text after every layout pass (TextBlock created for text content),
/// so window width changes or strip wrapping update the tooltip immediately.
/// An explicit view tooltip (ToolTip.Tip) takes precedence: this behaviour sets
/// a tooltip only when none exists, and removes only the one it set itself.
/// Changes view tooltip only, never application state.
/// </summary>
internal static class TrimmedLabelToolTip
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(TrimmedLabelToolTip));

    // Text this behaviour placed in ToolTip.Tip - distinguishes its own tooltip from an explicit one.
    private static readonly AttachedProperty<string?> OwnTipProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("OwnTip", typeof(TrimmedLabelToolTip));

    static TrimmedLabelToolTip()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);
    }

    public static bool GetIsEnabled(Control presenter) => presenter.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Control presenter, bool value) => presenter.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(Control presenter, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            presenter.LayoutUpdated += OnLayoutUpdated;
        }
        else
        {
            presenter.LayoutUpdated -= OnLayoutUpdated;
        }
    }

    private static void OnLayoutUpdated(object? sender, EventArgs e)
    {
        // Template content presenter: tooltip on the control. Plain text (row in a data template,
        // e.g. DropDownPicker item): tooltip on the text itself.
        var (host, label) = sender switch
        {
            ContentPresenter { TemplatedParent: Control parent } presenter => (parent, presenter.Child as TextBlock),
            TextBlock textBlock => ((Control)textBlock, textBlock),
            _ => (null, null),
        };
        if (host is null)
        {
            return;
        }

        var text = label is not null && IsTrimmed(label) ? label.Text : null;
        var ownTip = host.GetValue(OwnTipProperty);
        var currentTip = ToolTip.GetTip(host);

        if (currentTip is not null && !ReferenceEquals(currentTip, ownTip))
        {
            // Explicit view tooltip - neither overwrite nor clear it.
            return;
        }

        if (text is null)
        {
            if (ownTip is not null)
            {
                host.ClearValue(ToolTip.TipProperty);
                host.ClearValue(OwnTipProperty);
            }

            return;
        }

        if (!string.Equals(text, ownTip, StringComparison.Ordinal))
        {
            host.SetValue(OwnTipProperty, text);
            ToolTip.SetTip(host, text);
        }
    }

    private static bool IsTrimmed(TextBlock textBlock) =>
        !string.IsNullOrEmpty(textBlock.Text) && textBlock.TextLayout.TextLines.Any(line => line.HasCollapsed);
}
