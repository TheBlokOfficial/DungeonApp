using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Themes;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Otwierający okienko albo listę po zamknięciu kliknięciem w siebie przechodzi w spoczynek i zostaje
/// w nim, dopóki mysz z niego nie zjedzie (OpenerHoverRest). Motyw pokazuje najechanie tylko przy
/// :pointerover bez klasy hover-suppressed, więc test sprawdza tę parę po każdym zdarzeniu wskaźnika.
/// </summary>
public sealed class OpenerHoverRestTests
{
    public static TheoryData<string> Openers => new() { "button", "combo", "picker" };

    [AvaloniaTheory]
    [MemberData(nameof(Openers))]
    public void Opener_closed_by_second_click_rests_until_the_pointer_leaves(string kind)
    {
        OpenerHoverRest.Register();
        var opener = Create(kind);
        var window = new Window { Width = 400, Height = 400, Content = new StackPanel { Children = { opener } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var over = opener.TranslatePoint(new Point(10, 10), window)!.Value;
        var outside = new Point(390, 390);

        window.MouseMove(over);
        Dispatcher.UIThread.RunJobs();
        window.MouseDown(over, MouseButton.Left);
        window.MouseUp(over, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(IsOpen(opener));

        var states = new List<string>();
        window.MouseDown(over, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        states.Add(Look(opener));
        window.MouseUp(over, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        states.Add(Look(opener));
        window.MouseMove(over + new Point(1, 0));
        Dispatcher.UIThread.RunJobs();
        states.Add(Look(opener));
        window.MouseMove(outside);
        Dispatcher.UIThread.RunJobs();
        states.Add(Look(opener));
        window.MouseMove(over);
        Dispatcher.UIThread.RunJobs();
        states.Add(Look(opener));

        Assert.False(IsOpen(opener));
        Assert.Equal(["rest", "rest", "rest", "rest", "hover"], states);

        window.Close();
    }

    private static Control Create(string kind) => kind switch
    {
        "button" => new Button
        {
            Content = "Długa lista",
            Flyout = new Flyout { Placement = PlacementMode.BottomEdgeAlignedLeft, Content = new TextBlock { Text = "x" } },
        },
        "combo" => new ComboBox { Width = 200, ItemsSource = new[] { "a", "b" } },
        _ => new DropDownPicker { Width = 200, ItemsSource = new[] { "a", "b" } },
    };

    private static bool IsOpen(Control opener) => opener switch
    {
        Button button => button.Flyout!.IsOpen,
        ComboBox combo => combo.IsDropDownOpen,
        DropDownPicker picker => picker.IsDropDownOpen,
        _ => false,
    };

    // Najechanie w motywie ramy: :pointerover bez hover-suppressed, albo wciśnięty, albo otwarty.
    private static string Look(Control opener)
    {
        var classes = opener.Classes;
        var hover = (classes.Contains(":pointerover") && !classes.Contains(OpenerHoverRest.SuppressedClass))
            || classes.Contains(":pressed") || classes.Contains(":flyout-open") || classes.Contains(":dropdownopen");
        return hover ? "hover" : "rest";
    }
}
