using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DungeonApp.Desktop.Controls.Workspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Deck;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignWorkspace;

/// <summary>
/// The campaign desk: which panels are open, how they are stacked, and where they sit.
/// <para>
/// Owns the desired-versus-effective placement split. Gestures write the desired placement (through
/// <see cref="CommitGesture"/>); every surface resize recomputes the effective one from it. Nothing
/// here knows about pixels, pointers or <c>Canvas</c> - it works in logical workspace coordinates.
/// </para>
/// </summary>
public sealed class CampaignWorkspaceViewModel : ObservableObject
{
    /// <summary>
    /// A constant while campaigns do not exist yet. It becomes the campaign identifier later, which
    /// is why the layout file is already keyed by it rather than being a single global file.
    /// </summary>
    private const string WorkspaceId = "default";

    private readonly WorkspaceLayoutSession _session;

    /// <summary>Placements of panels that are closed, so reopening one puts it back where it was.</summary>
    private readonly Dictionary<string, WorkspacePanelLayout> _remembered = new(StringComparer.Ordinal);

    private WorkspaceMetrics _metrics = WorkspaceMetrics.Fallback;
    private double _surfaceWidth;
    private double _surfaceHeight;
    private bool _hasSurface;
    private bool _isFitting;

    public CampaignWorkspaceViewModel(WorkspaceLayoutStore store)
    {
        _session = new WorkspaceLayoutSession(store, WorkspaceId, CreateSnapshot);

        LauncherGroups = BuildLauncherGroups();

        ResetLayoutCommand = new AsyncCommand(() =>
        {
            Restore(WorkspaceLayout.Empty);
            _session.MarkDirty();
            return Task.CompletedTask;
        });

        Restore(_session.Load());
    }

    public ObservableCollection<WorkspacePanelViewModel> Panels { get; } = [];

    /// <summary>Maintained explicitly rather than derived, so no collection-filtering plumbing is needed.</summary>
    public ObservableCollection<WorkspacePanelViewModel> MinimizedPanels { get; } = [];

    public IReadOnlyList<PanelLauncherGroupViewModel> LauncherGroups { get; }

    public ICommand ResetLayoutCommand { get; }

    /// <summary>
    /// Called from the view's SizeChanged. The caller is responsible for filtering out the degenerate
    /// sizes Avalonia reports during the first layout passes and while the window is minimized -
    /// clamping against those would collapse a freshly restored layout.
    /// </summary>
    public void SetSurfaceSize(double width, double height)
    {
        _surfaceWidth = width;
        _surfaceHeight = height;
        _hasSurface = true;

        // Re-read the profile metrics here too: changing the UI scale profile changes the panel size
        // floor, and that has to take effect even when the desk itself did not change size.
        _metrics = WorkspaceMetricsResolver.Resolve();

        FitPanels();
    }

    public void OpenPanel(WorkspacePanelDescriptor descriptor)
    {
        if (!descriptor.AllowsMultipleInstances &&
            Panels.FirstOrDefault(panel => panel.Descriptor.Id == descriptor.Id) is { } existing)
        {
            Activate(existing);
            return;
        }

        var instanceKey = descriptor.AllowsMultipleInstances
            ? $"{descriptor.Id}#{Panels.Count(panel => panel.Descriptor.Id == descriptor.Id) + 1}"
            : descriptor.Id;

        var desired = _remembered.TryGetValue(instanceKey, out var remembered)
            ? new PanelPlacement(remembered.X, remembered.Y, remembered.Width, remembered.Height)
            : descriptor.DefaultPlacement;

        _remembered.Remove(instanceKey);

        var panel = new WorkspacePanelViewModel(this, descriptor, instanceKey, desired);
        Panels.Add(panel);
        Fit(panel);
        Activate(panel);
        RefreshLauncherState();
        _session.MarkDirty();
    }

    /// <summary>
    /// Brings a panel to the front. A minimized panel is restored first, which is what makes a click
    /// on its deck card do the obvious thing without needing a separate command.
    /// </summary>
    public void Activate(WorkspacePanelViewModel panel)
    {
        if (panel.State == PanelDisplayState.Minimized)
        {
            panel.State = PanelDisplayState.Normal;
            MinimizedPanels.Remove(panel);
            Fit(panel);
        }

        BringToFront(panel);
        _session.MarkDirty();
    }

    public void Close(WorkspacePanelViewModel panel)
    {
        Remember(panel, isOpen: false);

        Panels.Remove(panel);
        MinimizedPanels.Remove(panel);

        if (panel.IsActive)
        {
            ActivateTopmost();
        }

        RefreshLauncherState();
        _session.MarkDirty();
    }

    public void Minimize(WorkspacePanelViewModel panel)
    {
        if (panel.State == PanelDisplayState.Minimized)
        {
            return;
        }

        panel.State = PanelDisplayState.Minimized;
        panel.IsActive = false;

        if (!MinimizedPanels.Contains(panel))
        {
            MinimizedPanels.Add(panel);
        }

        ActivateTopmost();
        _session.MarkDirty();
    }

    public void ToggleMaximize(WorkspacePanelViewModel panel)
    {
        // The desired placement is left alone in both directions, which is exactly why it doubles as
        // the restore geometry and no separate restore fields exist.
        panel.State = panel.State == PanelDisplayState.Maximized
            ? PanelDisplayState.Normal
            : PanelDisplayState.Maximized;

        Fit(panel);
        Activate(panel);
    }

    /// <summary>Called once when a drag or resize finishes; promotes the on-screen geometry to desired.</summary>
    public void CommitGesture(WorkspacePanelViewModel panel)
    {
        panel.CommitGesture();
        _session.MarkDirty();
    }

    /// <summary>Writes any pending arrangement immediately. Called on shutdown.</summary>
    public void FlushLayout() => _session.Flush();

    private void Restore(WorkspaceLayout layout)
    {
        Panels.Clear();
        MinimizedPanels.Clear();
        _remembered.Clear();

        if (layout.IsEmpty)
        {
            foreach (var descriptor in PanelCatalog.All)
            {
                Panels.Add(new WorkspacePanelViewModel(this, descriptor, descriptor.Id, descriptor.DefaultPlacement));
            }
        }
        else
        {
            foreach (var entry in layout.Panels.OrderBy(entry => entry.ZOrder))
            {
                // A layout naming a panel this build no longer has is skipped, never an error.
                if (PanelCatalog.Find(entry.DescriptorId) is not { } descriptor)
                {
                    continue;
                }

                if (!entry.IsOpen)
                {
                    _remembered[entry.InstanceKey] = entry;
                    continue;
                }

                var panel = new WorkspacePanelViewModel(
                    this,
                    descriptor,
                    entry.InstanceKey,
                    new PanelPlacement(entry.X, entry.Y, entry.Width, entry.Height))
                {
                    State = entry.State == PanelDisplayState.Minimized
                        ? PanelDisplayState.Minimized
                        : entry.State
                };

                Panels.Add(panel);

                if (panel.State == PanelDisplayState.Minimized)
                {
                    MinimizedPanels.Add(panel);
                }
            }
        }

        Normalize();
        ActivateTopmost();
        RefreshLauncherState();
        FitPanels();
    }

    private void FitPanels()
    {
        if (!_hasSurface || _isFitting)
        {
            return;
        }

        // Belt and braces. Panels live on a Canvas and so cannot resize the surface, meaning there is
        // no real feedback path back into SizeChanged - but a re-entrancy guard around a loop that
        // writes bound state is one line and turns a possible hang into a no-op.
        _isFitting = true;
        try
        {
            foreach (var panel in Panels)
            {
                Fit(panel);
            }
        }
        finally
        {
            _isFitting = false;
        }
    }

    private void Fit(WorkspacePanelViewModel panel)
    {
        if (!_hasSurface || panel.State == PanelDisplayState.Minimized)
        {
            return;
        }

        panel.ApplyEffective(panel.State == PanelDisplayState.Maximized
            ? PanelGeometry.Maximize(_surfaceWidth, _surfaceHeight, _metrics)
            : PanelGeometry.FitInto(panel.Desired, _surfaceWidth, _surfaceHeight, panel.Descriptor.Constraints, _metrics));
    }

    private void BringToFront(WorkspacePanelViewModel panel)
    {
        if (panel.ZOrder != Panels.Count - 1)
        {
            var ordered = Panels.Where(other => other != panel).OrderBy(other => other.ZOrder).ToList();
            ordered.Add(panel);

            for (var index = 0; index < ordered.Count; index++)
            {
                ordered[index].ZOrder = index;
            }
        }

        foreach (var other in Panels)
        {
            other.IsActive = other == panel;
        }
    }

    private void ActivateTopmost()
    {
        var topmost = Panels
            .Where(panel => panel.State != PanelDisplayState.Minimized)
            .OrderByDescending(panel => panel.ZOrder)
            .FirstOrDefault();

        foreach (var panel in Panels)
        {
            panel.IsActive = panel == topmost;
        }
    }

    private void Normalize()
    {
        var index = 0;
        foreach (var panel in Panels)
        {
            panel.ZOrder = index++;
        }
    }

    private void Remember(WorkspacePanelViewModel panel, bool isOpen) =>
        _remembered[panel.InstanceKey] = ToLayout(panel, isOpen);

    private WorkspaceLayout CreateSnapshot()
    {
        var entries = new List<WorkspacePanelLayout>(Panels.Count + _remembered.Count);
        entries.AddRange(Panels.Select(panel => ToLayout(panel, isOpen: true)));

        // Only remember closed panels the build still knows about, so the file cannot accumulate
        // entries for descriptors that no longer exist.
        entries.AddRange(_remembered.Values.Where(entry => PanelCatalog.Find(entry.DescriptorId) is not null));

        return new WorkspaceLayout(
            WorkspaceLayout.CurrentVersion,
            _surfaceWidth,
            _surfaceHeight,
            entries);
    }

    private static WorkspacePanelLayout ToLayout(WorkspacePanelViewModel panel, bool isOpen) =>
        new(
            panel.Descriptor.Id,
            panel.InstanceKey,
            isOpen,
            panel.State,
            panel.ZOrder,
            panel.Desired.X,
            panel.Desired.Y,
            panel.Desired.Width,
            panel.Desired.Height);

    private IReadOnlyList<PanelLauncherGroupViewModel> BuildLauncherGroups() =>
        [.. PanelCatalog.All
            .GroupBy(descriptor => descriptor.Group)
            .OrderBy(group => group.Key)
            .Select(group => new PanelLauncherGroupViewModel(
                GroupLabel(group.Key),
                [.. group.Select(descriptor => new PanelLauncherItemViewModel(
                    descriptor,
                    new AsyncCommand(() =>
                    {
                        OpenPanel(descriptor);
                        return Task.CompletedTask;
                    })))]))];

    private void RefreshLauncherState()
    {
        foreach (var item in LauncherGroups.SelectMany(group => group.Items))
        {
            item.IsOpen = Panels.Any(panel => panel.Descriptor.Id == item.Descriptor.Id);
        }
    }

    private static string GroupLabel(WorkspacePanelGroup group) => group switch
    {
        WorkspacePanelGroup.Session => "SESJA",
        WorkspacePanelGroup.World => "ŚWIAT",
        WorkspacePanelGroup.Knowledge => "WIEDZA",
        _ => "KAMPANIA"
    };
}
