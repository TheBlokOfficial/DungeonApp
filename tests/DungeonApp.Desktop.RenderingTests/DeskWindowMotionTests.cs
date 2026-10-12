using System;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Desktop.Workspace.Controls;
using DungeonApp.Desktop.Workspace.Panels;
using System.Collections.Generic;
using System.Windows.Input;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Windows of the desk in a surface in a window: one restored with the layout does not start faded,
/// and a closed one leaves a picture behind that takes no input.
/// </summary>
public sealed partial class DeskWindowMotionTests
{
    [AvaloniaFact]
    public void A_window_that_was_not_opened_just_now_does_not_start_faded()
    {
        var (window, surface, panels) = Build(new StubPanel("A", opensWithMotion: false));

        var container = surface.GetVisualDescendants().OfType<PanelWindow>().Single();
        Assert.DoesNotContain(":appearing", container.Classes);
        Assert.Equal(1.0, container.Opacity);

        window.Close();
        Assert.Single(panels);
    }

    [AvaloniaFact]
    public void A_closed_window_leaves_a_ghost_that_takes_no_input_and_goes_away()
    {
        var (window, surface, panels) = Build(new StubPanel("A", false), new StubPanel("B", false));
        var closing = panels[1];
        var container = surface.GetVisualDescendants().OfType<PanelWindow>().Single(panel => ReferenceEquals(panel.DataContext, closing));
        var ghosts = surface.GetVisualDescendants().OfType<Canvas>().Single(canvas => canvas.Name == "PART_Ghosts");
        var centre = new Point(closing.X + closing.Width / 2, closing.Y + closing.Height / 2);

        closing.IsClosing = true;
        panels.Remove(closing);
        Settle();

        // The model forgot the window at once; only a picture of it is left.
        Assert.Single(panels);
        Assert.DoesNotContain(container, surface.GetVisualDescendants());
        var ghost = Assert.Single(ghosts.Children);
        Assert.False(ghost.IsHitTestVisible);
        Assert.False(ghosts.IsHitTestVisible);
        // Asked the way the pointer asks: input hit testing, which honours IsHitTestVisible.
        Assert.False(surface.InputHitTest(centre) is Visual hit && hit.GetSelfAndVisualAncestors().Contains(ghost));

        // The timers that fade the ghost and remove it do not tick in a window without a screen;
        // only the application shows that part.
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    public void The_ghost_covers_the_window_exactly_and_carries_the_fade(double scaling)
    {
        var (window, surface, panels) = Build(new StubPanel("A", false), new StubPanel("B", false));
        window.SetRenderScaling(scaling);
        Settle();
        var closing = panels[1];
        var ghosts = surface.GetVisualDescendants().OfType<Canvas>().Single(canvas => canvas.Name == "PART_Ghosts");

        closing.IsClosing = true;
        panels.Remove(closing);
        Settle();

        var ghost = Assert.IsType<Image>(Assert.Single(ghosts.Children));
        Assert.Equal(new Rect(closing.X, closing.Y, closing.Width, closing.Height),
            new Rect(Canvas.GetLeft(ghost), Canvas.GetTop(ghost), ghost.Width, ghost.Height));

        // The picture has the screen's pixel density, but is a 96-DPI bitmap: its size in DIPs is its
        // size in pixels, so the image reads the whole of it and does not enlarge a corner.
        var picture = Assert.IsAssignableFrom<Bitmap>(ghost.Source);
        Assert.Equal(new Vector(96, 96), picture.Dpi);
        Assert.Equal(new PixelSize((int)Math.Ceiling(closing.Width * scaling), (int)Math.Ceiling(closing.Height * scaling)), picture.PixelSize);

        // The ghost theme reaches it (a template style would not: the ghost is made in code).
        Assert.NotNull(ghost.Transitions);
        Assert.Equal(RelativePoint.Center, ghost.RenderTransformOrigin);
        Assert.Equal(1.0, ghost.Opacity);

        // Where the fade goes: the base value, under the running transition.
        ghost.Classes.Add("leaving");
        Assert.Equal(0.0, ghost.GetBaseValue(Visual.OpacityProperty).Value);
        Assert.Equal(Matrix.CreateScale(0.98, 0.98), ghost.GetBaseValue(Visual.RenderTransformProperty).Value!.Value);

        window.Close();
    }

    private static (Window Window, WorkspaceSurface Surface, ObservableCollection<StubPanel> Panels) Build(params StubPanel[] stubs)
    {
        var panels = new ObservableCollection<StubPanel>(stubs);
        var surface = new WorkspaceSurface { ItemsSource = panels };
        var window = new Window { Width = 1000, Height = 700, Content = surface };

        // The desk's themes, merged the way the workspace view merges them.
        var theme = (ResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://DungeonApp.Desktop/Workspace/WorkspaceControls.axaml"));
        window.Resources.MergedDictionaries.Add(theme);
        Assert.True(theme.TryGetResource(typeof(WorkspaceSurface), null, out var surfaceTheme));
        surface.Theme = (ControlTheme)surfaceTheme!;
        window.Show();
        Settle();

        return (window, surface, panels);
    }

    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    private sealed partial class StubPanel : ObservableObject, IWorkspacePanel
    {
        public StubPanel(string title, bool opensWithMotion)
        {
            Title = title;
            OpensWithMotion = opensWithMotion;
            Body = new TextBlock { Text = title };
            Width = 300;
            Height = 200;
            X = title == "A" ? 20 : 400;
            Y = 20;
        }

        public string Title { get; }

        public string IconResourceKey => "DungeonIconClear";

        public IReadOnlyList<PanelHeaderAction> HeaderActions => [];

        public bool CanMinimize => true;

        public object Body { get; }

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public double MinWidth => 100;

        public double MinHeight => 100;

        public double MaxWidth => double.PositiveInfinity;

        public double MaxHeight => double.PositiveInfinity;

        public bool HasFixedWidth => false;

        public bool CanMaximize => true;

        public int ZOrder => 0;

        public bool IsActive => false;

        public bool OpensWithMotion { get; }

        [ObservableProperty]
        public partial bool IsClosing { get; set; }

        public PanelDisplayState State { get; set; }

        public ICommand ActivateCommand { get; } = new RelayCommand(() => { });

        public ICommand MinimizeCommand { get; } = new RelayCommand(() => { });

        public ICommand ToggleMaximizeCommand { get; } = new RelayCommand(() => { });
    }
}
