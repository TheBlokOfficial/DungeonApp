using System;
using Avalonia.Controls;
using DungeonApp.Controls;

namespace DungeonApp.Views.Dashboard;

/// <summary>
/// Code-behind dla środowiska testowego Sandbox / Workbench.
/// </summary>
/// <remarks>
/// DLACZEGO Canvas zamiast HorizontalAlignment="Center":
/// Center-alignment przelicza pozycję lewego marginesu przy każdej zmianie Width —
/// panel "walczy" ze swoją pozycją podczas resize, powodując drżenie elementów wewnątrz.
/// Canvas ustawia Left/Top raz (CenterPanel() przy starcie) i nie zmienia ich
/// podczas resize — brak przeliczania pozycji = zero drżenia.
/// </remarks>
public partial class SandboxTabView : UserControl
{
    private bool _isInitialPositionSet = false;

    public SandboxTabView()
    {
        InitializeComponent();

        var canvas = this.FindControl<Canvas>("WorkCanvas");
        if (canvas is not null)
            canvas.SizeChanged += WorkCanvas_SizeChanged;
    }

    private void WorkCanvas_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // Centrujemy panel tylko raz — przy pierwszym poprawnym pomiarze Canvas.
        // Przy kolejnych resize okna panel zostaje tam, gdzie jest (Canvas.Left/Top fixed).
        if (_isInitialPositionSet) return;
        if (e.NewSize.Width <= 0 || e.NewSize.Height <= 0) return;

        CenterPanel();
        _isInitialPositionSet = true;
    }

    /// <summary>
    /// Ustawia FloatingPanel na środku Canvas.
    /// Wywoływana tylko raz przy starcie — później Canvas.Left/Top się nie zmieniają,
    /// co zapobiega drżeniu podczas resize (Width/Height rosną od stałego punktu).
    /// </summary>
    private void CenterPanel()
    {
        var canvas = this.FindControl<Canvas>("WorkCanvas");
        var panel  = this.FindControl<FloatingPanel>("ModulePanel");

        if (canvas is null || panel is null) return;
        if (canvas.Bounds.Width <= 0 || canvas.Bounds.Height <= 0) return;

        var left = (canvas.Bounds.Width  - panel.Width)  / 2.0;
        var top  = (canvas.Bounds.Height - panel.Height) / 2.0;

        Canvas.SetLeft(panel, Math.Max(0, left));
        Canvas.SetTop(panel,  Math.Max(0, top));
    }
}
