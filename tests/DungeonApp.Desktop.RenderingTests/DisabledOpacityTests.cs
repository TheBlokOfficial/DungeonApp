using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Przygaszenie wyłączenia w motywie ramy idzie po :disabled na najwyższej wyłączonej kontrolce.
/// Przycisk wyłączony komendą (CanExecute fałsz) ma IsEnabled true - przygaszenie po
/// [IsEnabled=False] zostawiłoby go wyglądającym jak włączony. Kontrolka w wyłączonym pojemniku nie
/// przygasa drugi raz: przygasza ją pojemnik.
/// </summary>
public sealed class DisabledOpacityTests
{
    [AvaloniaFact]
    public void Button_disabled_by_its_command_is_dimmed()
    {
        var button = new Button
        {
            Content = "Wyczyść",
            Command = new RelayCommand(() => { }, () => false),
        };
        Show(button);

        Assert.True(button.IsEnabled);
        Assert.Equal(DisabledOpacity(button), button.Opacity);
    }

    [AvaloniaFact]
    public void Button_in_a_disabled_section_is_dimmed_once_by_the_section()
    {
        var button = new Button { Content = "Zapisz" };
        var expander = new Expander { Header = "Sekcja", IsExpanded = true, IsEnabled = false, Content = button };
        Show(expander);

        Assert.Equal(DisabledOpacity(expander), expander.Opacity);
        Assert.Equal(1.0, button.Opacity);
    }

    private static void Show(Control content)
    {
        var window = new Window { Width = 400, Height = 300, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    private static double DisabledOpacity(Control control) =>
        (double)control.FindResource("DungeonDisabledOpacity")!;
}
