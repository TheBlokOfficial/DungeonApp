using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DungeonApp.Desktop.Themes;
using DungeonApp.Desktop.Workspace.Panels;

namespace DungeonApp.Desktop.Workspace.Controls;

/// <summary>
/// The campaign desk: an <see cref="ItemsControl"/> that lays its items out on a <see cref="Canvas"/>
/// as floating <see cref="PanelWindow"/>s.
/// <para>
/// The container <em>is</em> the panel, rather than the usual <see cref="Avalonia.Controls.Presenters.ContentPresenter"/> wrapping
/// one. That is what lets the gesture code write <see cref="Canvas.LeftProperty"/> on the same object
/// the placement is bound to. The declarative alternative — an <c>ItemContainerTheme</c> whose setters
/// bind the placement — looks tidier but is a trap: setter values sit at Style priority, so the first
/// local write from a drag outranks them permanently and the view-model-to-view direction dies after
/// the first gesture. Bindings created here are at LocalValue priority and survive.
/// </para>
/// </summary>
public class WorkspaceSurface : ItemsControl
{
    /// <summary>
    /// What lies on the desk itself, above the grid lines and below every window (the world catalog).
    /// Not a panel: it has no chrome, no stacking and never covers a window.
    /// </summary>
    public static readonly StyledProperty<Control?> UnderlayProperty =
        AvaloniaProperty.Register<WorkspaceSurface, Control?>(nameof(Underlay));

    private readonly Dictionary<Control, List<IDisposable>> _containerBindings = [];
    private readonly Dictionary<Control, (IWorkspacePanel Panel, PropertyChangedEventHandler Handler)> _closeWatchers = [];
    private Canvas? _ghosts;

    public Control? Underlay
    {
        get => GetValue(UnderlayProperty);
        set => SetValue(UnderlayProperty, value);
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        // A null recycle key means "never recycle". Panels are few and long-lived, and recycling
        // would mean rebinding placement on a container that is mid-gesture.
        recycleKey = null;
        return item is not PanelWindow;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new PanelWindow();

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        if (container is not PanelWindow panel || item is not IWorkspacePanel viewModel)
        {
            base.PrepareContainerForItemOverride(container, item, index);
            return;
        }

        panel.DataContext = viewModel;
        panel.Content = viewModel.Body;

        if (viewModel.OpensWithMotion && !SystemMotion.IsReduced)
        {
            panel.BeginAppear();
        }

        PropertyChangedEventHandler closing = (_, e) =>
        {
            if (e.PropertyName == nameof(IWorkspacePanel.IsClosing) && viewModel.IsClosing)
            {
                LeaveWithMotion(panel);
            }
        };
        viewModel.PropertyChanged += closing;
        _closeWatchers[container] = (viewModel, closing);

        _containerBindings[container] =
        [
            panel.Bind(Canvas.LeftProperty, TwoWay(nameof(IWorkspacePanel.X), viewModel)),
            panel.Bind(Canvas.TopProperty, TwoWay(nameof(IWorkspacePanel.Y), viewModel)),
            panel.Bind(WidthProperty, TwoWay(nameof(IWorkspacePanel.Width), viewModel)),
            panel.Bind(HeightProperty, TwoWay(nameof(IWorkspacePanel.Height), viewModel)),
            panel.Bind(MinWidthProperty, OneWay(nameof(IWorkspacePanel.MinWidth), viewModel)),
            panel.Bind(MinHeightProperty, OneWay(nameof(IWorkspacePanel.MinHeight), viewModel)),
            panel.Bind(MaxWidthProperty, OneWay(nameof(IWorkspacePanel.MaxWidth), viewModel)),
            panel.Bind(MaxHeightProperty, OneWay(nameof(IWorkspacePanel.MaxHeight), viewModel)),
            panel.Bind(ZIndexProperty, OneWay(nameof(IWorkspacePanel.ZOrder), viewModel)),
            panel.Bind(PanelWindow.TitleProperty, OneWay(nameof(IWorkspacePanel.Title), viewModel)),
            panel.Bind(PanelWindow.IconResourceKeyProperty, OneWay(nameof(IWorkspacePanel.IconResourceKey), viewModel)),
            panel.Bind(PanelWindow.HeaderActionsProperty, OneWay(nameof(IWorkspacePanel.HeaderActions), viewModel)),
            panel.Bind(PanelWindow.CanMinimizeProperty, OneWay(nameof(IWorkspacePanel.CanMinimize), viewModel)),
            panel.Bind(PanelWindow.HasFixedWidthProperty, OneWay(nameof(IWorkspacePanel.HasFixedWidth), viewModel)),
            panel.Bind(PanelWindow.CanMaximizeProperty, OneWay(nameof(IWorkspacePanel.CanMaximize), viewModel)),
            panel.Bind(PanelWindow.IsActiveProperty, OneWay(nameof(IWorkspacePanel.IsActive), viewModel)),

            // OneWay on purpose: state only ever changes through the commands below, which mutate the
            // view model and flow back down. A TwoWay binding here would add a loop for no gain.
            panel.Bind(PanelWindow.PanelStateProperty, OneWay(nameof(IWorkspacePanel.State), viewModel)),

            panel.Bind(PanelWindow.ActivateCommandProperty, OneWay(nameof(IWorkspacePanel.ActivateCommand), viewModel)),
            panel.Bind(PanelWindow.MinimizeCommandProperty, OneWay(nameof(IWorkspacePanel.MinimizeCommand), viewModel)),
            panel.Bind(PanelWindow.ToggleMaximizeCommandProperty, OneWay(nameof(IWorkspacePanel.ToggleMaximizeCommand), viewModel))
        ];
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _ghosts = e.NameScope.Find<Canvas>("PART_Ghosts");
    }

    /// <summary>
    /// A picture of the window that was just closed, lying where it was and fading out. It is not the
    /// window: no input reaches it, and the model has already forgotten the window.
    /// </summary>
    private void LeaveWithMotion(PanelWindow panel)
    {
        var ghosts = _ghosts;
        if (ghosts is null || SystemMotion.IsReduced || panel.PanelState == PanelDisplayState.Minimized
            || panel.Bounds.Width < 1 || panel.Bounds.Height < 1)
        {
            return;
        }

        var bitmap = Snapshot(panel, TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);

        var ghost = new Image
        {
            Source = bitmap,
            Width = panel.Bounds.Width,
            Height = panel.Bounds.Height,
            Stretch = Stretch.Fill,
            IsHitTestVisible = false,
            ZIndex = panel.ZIndex,
        };

        // A theme, not a template style: the ghost is added in code, so it has no templated parent
        // and a "/template/" selector in the surface's theme would never reach it.
        if (this.TryFindResource("DungeonDeskGhostTheme", out var theme) && theme is Avalonia.Styling.ControlTheme ghostTheme)
        {
            ghost.Theme = ghostTheme;
        }

        ghost.Classes.Add("desk-ghost");
        Canvas.SetLeft(ghost, panel.Bounds.X);
        Canvas.SetTop(ghost, panel.Bounds.Y);
        ghosts.Children.Add(ghost);

        // Start after the ghost has been in the tree for a moment (so the fade is a transition), and
        // remove it once the fade is over. One timer after the other, not two started together: the
        // fade must have begun before the removal can come due.
        After(TimeSpan.FromMilliseconds(30), () =>
        {
            ghost.Classes.Add("leaving");
            After(TimeSpan.FromMilliseconds(230), () =>
            {
                ghosts.Children.Remove(ghost);
                bitmap.Dispose();
            });
        });
    }

    /// <summary>
    /// The window drawn at the screen's pixel density, returned as a bitmap of 96 DPI whose pixel size
    /// equals its size. A bitmap of higher DPI cannot be shown directly: the image control asks for
    /// its source rectangle in DIPs, the renderer reads it in pixels, and the ghost comes out
    /// enlarged by the scaling and cut to the window's edges.
    /// </summary>
    internal static Bitmap Snapshot(Visual visual, double scaling)
    {
        var size = new PixelSize(
            Math.Max(1, (int)Math.Ceiling(visual.Bounds.Width * scaling)),
            Math.Max(1, (int)Math.Ceiling(visual.Bounds.Height * scaling)));
        using var rendered = new RenderTargetBitmap(size, new Vector(96 * scaling, 96 * scaling));
        rendered.Render(visual);

        // Same pixel format as the rendering, so the copy is byte for byte.
        var picture = new WriteableBitmap(size, new Vector(96, 96), rendered.Format, rendered.AlphaFormat);
        using (var buffer = picture.Lock())
        {
            rendered.CopyPixels(new PixelRect(size), buffer.Address, buffer.RowBytes * size.Height, buffer.RowBytes);
        }

        return picture;
    }

    private static void After(TimeSpan delay, Action action)
    {
        var timer = new DispatcherTimer { Interval = delay };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            action();
        };
        timer.Start();
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        if (_closeWatchers.Remove(container, out var watcher))
        {
            watcher.Panel.PropertyChanged -= watcher.Handler;
        }

        // Mandatory: every binding above holds the view model as its Source, so the view model keeps
        // the container — and its whole visual tree — alive. Skipping this leaks each closed panel.
        if (_containerBindings.Remove(container, out var bindings))
        {
            foreach (var binding in bindings)
            {
                binding.Dispose();
            }
        }

        if (container is PanelWindow panel)
        {
            panel.Content = null;
            panel.DataContext = null;
        }

        base.ClearContainerForItemOverride(container);
    }

    private static Binding TwoWay(string path, object source) =>
        new(path) { Source = source, Mode = BindingMode.TwoWay };

    private static Binding OneWay(string path, object source) =>
        new(path) { Source = source, Mode = BindingMode.OneWay };
}
