using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Workspace.Controls;
using CommunityToolkit.Mvvm.Input;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The header of a maximized panel: no frame, no minimize / maximize buttons, Esc restores. A panel
/// that cannot be maximized has no maximize button at all.
/// </summary>
public sealed class PanelWindowMaximizeTests
{
    [AvaloniaFact]
    public void Maximized_panel_has_no_frame_and_no_header_buttons()
    {
        var (window, panel, _) = Show(canMaximize: true);

        Assert.True(Visible(panel, "PART_MinimizeButton"));
        Assert.True(Visible(panel, "PART_MaximizeButton"));
        Assert.Equal(1, panel.BorderThickness.Left);

        panel.PanelState = PanelDisplayState.Maximized;
        Dispatcher.UIThread.RunJobs();

        Assert.False(Visible(panel, "PART_MinimizeButton"));
        Assert.False(Visible(panel, "PART_MaximizeButton"));
        Assert.Equal(0, panel.BorderThickness.Left);
        Assert.Equal(0, panel.CornerRadius.TopLeft);
        window.Close();
    }

    [AvaloniaFact]
    public void Panel_that_cannot_be_maximized_has_no_maximize_button()
    {
        var (window, panel, _) = Show(canMaximize: false);

        Assert.True(Visible(panel, "PART_MinimizeButton"));
        Assert.False(Visible(panel, "PART_MaximizeButton"));
        window.Close();
    }

    [AvaloniaFact]
    public void Escape_restores_a_maximized_panel()
    {
        var (window, panel, toggles) = Show(canMaximize: true);
        panel.PanelState = PanelDisplayState.Maximized;
        Dispatcher.UIThread.RunJobs();

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, toggles.Count);
        window.Close();
    }

    private static bool Visible(PanelWindow panel, string name) =>
        panel.GetVisualDescendants().OfType<Button>().FirstOrDefault(button => button.Name == name)?.IsVisible
        ?? throw new Exception($"theme={panel.Theme?.GetType().Name} tmpl={panel.Template is null} n={string.Join(",", panel.GetVisualDescendants().Select(v => v.GetType().Name))}");

    private static (Window Window, PanelWindow Panel, ToggleCounter Toggles) Show(bool canMaximize)
    {
        var toggles = new ToggleCounter();
        var panel = new PanelWindow
        {
            Title = "Test",
            CanMaximize = canMaximize,
            ToggleMaximizeCommand = toggles.Command,
            Content = new TextBlock { Text = "treść" }
        };
        // The panel theme is merged by the desk view, not by the application.
        var window = new Window { Width = 600, Height = 400, Content = panel };
        var theme = (ResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://DungeonApp.Desktop/Workspace/WorkspaceControls.axaml"));
        window.Resources.MergedDictionaries.Add(theme);
        Assert.True(theme.TryGetResource(typeof(PanelWindow), null, out var panelTheme));
        panel.Theme = (ControlTheme)panelTheme!;
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, panel, toggles);
    }

    private sealed class ToggleCounter
    {
        public ToggleCounter() => Command = new RelayCommand(() => Count++);

        public ICommand Command { get; }

        public int Count { get; private set; }
    }
}
