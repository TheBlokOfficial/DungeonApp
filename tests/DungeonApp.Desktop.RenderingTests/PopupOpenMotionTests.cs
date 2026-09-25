using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using DungeonApp.Desktop.Themes;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Lista rozwijana, pod którą brak miejsca, otwiera się nad polem, a pole dostaje klasę opens-up
/// (motyw obraca po niej strzałkę w górę); z miejscem pod spodem - bez klasy.
/// </summary>
public sealed class PopupOpenMotionTests
{
    [AvaloniaTheory]
    [InlineData(VerticalAlignment.Top, false)]
    [InlineData(VerticalAlignment.Bottom, true)]
    public void Opener_knows_whether_its_list_opened_above_it(VerticalAlignment alignment, bool opensUp)
    {
        PopupOpenMotion.Register();
        var combo = new ComboBox { Width = 200, VerticalAlignment = alignment, ItemsSource = new[] { "a", "b", "c", "d", "e" } };
        var window = new Window { Width = 400, Height = 300, Content = combo };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        combo.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(opensUp, combo.Classes.Contains(PopupOpenMotion.OpensUpClass));
        window.Close();
    }
}
