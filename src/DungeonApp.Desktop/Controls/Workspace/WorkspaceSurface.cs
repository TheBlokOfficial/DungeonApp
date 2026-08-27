using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace DungeonApp.Desktop.Controls.Workspace;

/// <summary>
/// The campaign desk: an <see cref="ItemsControl"/> that lays its items out on a <see cref="Canvas"/>
/// as floating <see cref="PanelWindow"/>s.
/// <para>
/// The container <em>is</em> the panel, rather than the usual <see cref="ContentPresenter"/> wrapping
/// one. That is what lets the gesture code write <see cref="Canvas.LeftProperty"/> on the same object
/// the placement is bound to. The declarative alternative — an <c>ItemContainerTheme</c> whose setters
/// bind the placement — looks tidier but is a trap: setter values sit at Style priority, so the first
/// local write from a drag outranks them permanently and the view-model-to-view direction dies after
/// the first gesture. Bindings created here are at LocalValue priority and survive.
/// </para>
/// </summary>
public class WorkspaceSurface : ItemsControl
{
    private readonly Dictionary<Control, List<IDisposable>> _containerBindings = [];

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
            panel.Bind(PanelWindow.IsActiveProperty, OneWay(nameof(IWorkspacePanel.IsActive), viewModel)),

            // OneWay on purpose: state only ever changes through the commands below, which mutate the
            // view model and flow back down. A TwoWay binding here would add a loop for no gain.
            panel.Bind(PanelWindow.PanelStateProperty, OneWay(nameof(IWorkspacePanel.State), viewModel)),

            panel.Bind(PanelWindow.ActivateCommandProperty, OneWay(nameof(IWorkspacePanel.ActivateCommand), viewModel)),
            panel.Bind(PanelWindow.CloseCommandProperty, OneWay(nameof(IWorkspacePanel.CloseCommand), viewModel)),
            panel.Bind(PanelWindow.MinimizeCommandProperty, OneWay(nameof(IWorkspacePanel.MinimizeCommand), viewModel)),
            panel.Bind(PanelWindow.ToggleMaximizeCommandProperty, OneWay(nameof(IWorkspacePanel.ToggleMaximizeCommand), viewModel))
        ];
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
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
