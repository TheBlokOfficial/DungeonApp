using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Konwencja ramy: akcja w wierszu listy (motyw DungeonRowAction, np. kosz) ma własne najechanie -
/// gdy mysz jest w jej pasie, wiersz się nie podświetla, żeby otwarcie wiersza i akcja nie wyglądały
/// na jedno. Włączane przez motyw akcji (RowActionHover.IsEnabled="True"), nie w widokach.
/// Wiersz to najbliższy przodek <see cref="ListBoxItem"/> albo kontrolka z klasą
/// <see cref="RowClass"/>; w pasie akcji dostaje klasę <see cref="SuppressedClass"/>, a motyw wiersza
/// pokazuje najechanie tylko bez niej (<c>:pointerover:not(.row-action-hover)</c>). Klasę zdejmuje
/// zjechanie myszy z akcji albo odłączenie akcji od drzewa (usunięty wiersz).
/// Zmienia wyłącznie wygląd widoku, nigdy stan aplikacji.
/// </summary>
internal static class RowActionHover
{
    public const string SuppressedClass = "row-action-hover";
    public const string RowClass = "list-row";

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(RowActionHover));

    // Wiersz, któremu akcja dołożyła klasę - zdejmowana z niego, nawet gdy akcja jest już poza drzewem.
    private static readonly AttachedProperty<Control?> SuppressedRowProperty =
        AvaloniaProperty.RegisterAttached<Control, Control?>("SuppressedRow", typeof(RowActionHover));

    static RowActionHover()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);
    }

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(Control action, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            action.PointerEntered += OnPointerEntered;
            action.PointerExited += OnPointerExited;
            action.DetachedFromVisualTree += OnDetached;
        }
        else
        {
            action.PointerEntered -= OnPointerEntered;
            action.PointerExited -= OnPointerExited;
            action.DetachedFromVisualTree -= OnDetached;
            Release(action);
        }
    }

    private static void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Control action)
        {
            return;
        }

        Release(action);
        var row = FindRow(action);
        if (row is null)
        {
            return;
        }

        row.Classes.Add(SuppressedClass);
        action.SetValue(SuppressedRowProperty, row);
    }

    private static void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Control action)
        {
            Release(action);
        }
    }

    private static void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control action)
        {
            Release(action);
        }
    }

    private static void Release(Control action)
    {
        action.GetValue(SuppressedRowProperty)?.Classes.Remove(SuppressedClass);
        action.ClearValue(SuppressedRowProperty);
    }

    private static Control? FindRow(Control action)
    {
        foreach (var ancestor in action.GetVisualAncestors())
        {
            if (ancestor is ListBoxItem item)
            {
                return item;
            }

            if (ancestor is Control control && control.Classes.Contains(RowClass))
            {
                return control;
            }
        }

        return null;
    }
}
