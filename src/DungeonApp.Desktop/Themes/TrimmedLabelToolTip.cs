using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Konwencja ramy: etykieta przycięta wielokropkiem (zakładka, segment, przycisk) pokazuje pełny
/// napis w podpowiedzi - wyłącznie wtedy, gdy jest rzeczywiście przycięta. Włączane w szablonie
/// kontrolki na jej prezenterze treści (TrimmedLabelToolTip.IsEnabled="True"), nie w widokach.
/// Po każdym przebiegu układu sprawdza napis prezentera (TextBlock tworzony dla treści tekstowej),
/// więc zmiana szerokości okna albo zawinięcie paska przelicza podpowiedź na bieżąco.
/// Podpowiedź ustawiona jawnie przez widok (ToolTip.Tip) ma pierwszeństwo: zachowanie ustawia
/// podpowiedź tylko wtedy, gdy żadnej nie ma, i zdejmuje wyłącznie tę, którą samo ustawiło.
/// Zmienia wyłącznie podpowiedź w widoku, nigdy stan aplikacji.
/// </summary>
internal static class TrimmedLabelToolTip
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(TrimmedLabelToolTip));

    // Napis, który zachowanie samo wstawiło w ToolTip.Tip - odróżnia własną podpowiedź od jawnej.
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
        // Prezenter treści w szablonie: podpowiedź na kontrolce. Sam napis (wiersz w szablonie danych,
        // np. pozycja DropDownPicker): podpowiedź na nim.
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
            // Podpowiedź jawna z widoku - nie nadpisujemy jej ani nie czyścimy.
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
