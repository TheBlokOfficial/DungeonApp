using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Core.Entries;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Workspace.Controls;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Desktop.Workspace.Leaf;
using DungeonApp.Desktop.Workspace.Panels;
using DungeonApp.Desktop.Workspace.World;

namespace DungeonApp.Desktop.Workspace;

/// <summary>
/// The campaign desk: which panels are visible, how they are stacked, and where they sit.
/// <para>
/// Owns the desired-versus-effective placement split. Gestures write the desired placement (through
/// <see cref="CommitGesture"/>); every surface resize recomputes the effective one from it. Nothing
/// here knows about pixels, pointers or <c>Canvas</c> - it works in logical workspace coordinates.
/// </para>
/// <para>
/// The windows know nothing of a campaign - see <see cref="CampaignDesk"/> for the one public entry
/// point that builds one of these for the open campaign. It takes a bare <c>workspaceId</c> string,
/// an already-loaded <see cref="WorkspaceLayout"/> and a tool list. The world catalog lying on the
/// desk (<see cref="Catalog"/>) is the one part that reads the campaign, through the entries context.
/// </para>
/// </summary>
public sealed partial class CampaignWorkspaceViewModel : ObservableObject, IDisposable
{
    /// <summary>The preview window: one on the desk, follows the catalog's last clicked entity.</summary>
    public const string PreviewDescriptorId = "frame.world-preview";

    /// <summary>A window with one entity's card, kept open by the GM; its instance key is the entity's id.</summary>
    public const string PinnedDescriptorId = "frame.world-pinned";

    // 560 wide card, its margin and the scrollbar zone.
    private static readonly PanelConstraints CardConstraints = new(640, 280, double.PositiveInfinity, double.PositiveInfinity);

    private readonly PanelCatalog _catalog;
    private readonly WorkspaceLayoutSession _session;
    private readonly WorkspacePanelDescriptor _previewDescriptor;
    private readonly WorkspacePanelDescriptor _pinnedDescriptor;

    private WorkspaceMetrics _metrics = WorkspaceMetrics.Fallback;
    private double _surfaceWidth;
    private double _surfaceHeight;
    private bool _hasSurface;
    private bool _isFitting;
    private bool _isDisposed;

    public CampaignWorkspaceViewModel(
        WorkspaceLayoutStore store,
        string workspaceId,
        WorkspaceLayout layout,
        IReadOnlyList<WorkspacePanelDescriptor> tools,
        Func<Task> closeCampaign,
        CampaignEntriesContext entries,
        IGameSystem system)
    {
        Leaf = new DeskLeafViewModel(closeCampaign);
        _catalog = PanelCatalog.For(tools);

        // Keyed by the campaign, so each one keeps its own desk: the arrangement a GM settles on for
        // one campaign has no business following them into another.
        _session = new WorkspaceLayoutSession(store, workspaceId, CreateSnapshot);

        Catalog = new WorldCatalogViewModel(entries, system, layout.Catalog);
        Catalog.LayoutChanged += _session.MarkDirty;

        var cards = new EntityCardBuilder(system.GetWorldCatalogSource());
        _previewDescriptor = new WorkspacePanelDescriptor(
            PreviewDescriptorId,
            "Podgląd",
            "DungeonIconBookOpen",
            WorkspacePanelGroup.World,
            new PanelPlacement(64, 72, 660, 640),
            CardConstraints,
            () => new EntityCardViewModel(Catalog, cards, null, followsSelection: true, entity => PinEntity(entity)))
        {
            ClosesPermanently = true,
        };
        _pinnedDescriptor = new WorkspacePanelDescriptor(
            PinnedDescriptorId,
            "Karta",
            "DungeonIconBookOpen",
            WorkspacePanelGroup.World,
            new PanelPlacement(96, 96, 660, 640),
            CardConstraints,
            () => throw new InvalidOperationException("A pinned window is built for an entity."))
        {
            AllowsMultipleInstances = true,
            ClosesPermanently = true,
            CreateInstance = key => new EntityCardViewModel(Catalog, cards, ParseEntity(key), followsSelection: false, pin: null),
            CanRestore = key => ParseEntity(key) is { } id && Catalog.Tree.Entity(id) is not null,
        };

        Catalog.OpenEntityRequested += OnOpenEntityRequested;
        Catalog.TreeChanged += ClosePinnedOfRemovedEntities;

        // Storage was already read by the caller (see CampaignDesk.CreateAsync). Construction is a
        // pure, bounded UI-model operation, so mounting this view cannot consume its own transition.
        Restore(layout);
    }

    /// <summary>The desk's command strip, above every window; its close is the frame's, handed in.</summary>
    public DeskLeafViewModel Leaf { get; }

    /// <summary>The world catalog lying on the desk, under every window.</summary>
    public WorldCatalogViewModel Catalog { get; }

    public ObservableCollection<WorkspacePanelViewModel> Panels { get; } = [];

    /// <summary>Maintained explicitly rather than derived, so no collection-filtering plumbing is needed.</summary>
    public ObservableCollection<WorkspacePanelViewModel> MinimizedPanels { get; } = [];

    [RelayCommand]
    private void ResetLayout()
    {
        Restore(WorkspaceLayout.Empty);
        Catalog.ResetPlacement();
        _session.MarkDirty();
    }

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

        Catalog.SetSurface(width, height, _metrics);
        FitPanels();
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

    /// <summary>
    /// Opens a window of <paramref name="descriptor"/> or, if the desk already has it, brings it to
    /// the front. A multi-instance descriptor is told apart by <paramref name="instanceKey"/>; a
    /// singleton ignores the key. The window is part of the saved layout from now on.
    /// </summary>
    public WorkspacePanelViewModel Open(WorkspacePanelDescriptor descriptor, string instanceKey)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (Find(descriptor, instanceKey) is { } existing)
        {
            Activate(existing);
            return existing;
        }

        var panel = new WorkspacePanelViewModel(this, descriptor, instanceKey, NextPlacement(descriptor));

        panel.ZOrder = Panels.Count;
        Panels.Add(panel);
        Fit(panel);
        Activate(panel);

        return panel;
    }

    /// <summary>The open window of <paramref name="descriptor"/> with this key, null when the desk has none.</summary>
    public WorkspacePanelViewModel? Find(WorkspacePanelDescriptor descriptor, string instanceKey) =>
        Panels.FirstOrDefault(panel => panel.Descriptor.Id == descriptor.Id
            && (!descriptor.AllowsMultipleInstances || panel.InstanceKey == instanceKey));

    /// <summary>
    /// Removes a window for good: it leaves the desk and the saved layout, and is not offered by the
    /// deck. A tool that always lies on the desk is minimized instead (<see cref="Minimize"/>).
    /// </summary>
    public void Close(WorkspacePanelViewModel panel)
    {
        if (!Panels.Remove(panel))
        {
            return;
        }

        MinimizedPanels.Remove(panel);

        if (panel.Body is IDisposable disposable)
        {
            disposable.Dispose();
        }

        var order = 0;
        foreach (var other in Panels.OrderBy(other => other.ZOrder).ToList())
        {
            other.ZOrder = order++;
        }

        ActivateTopmost();
        _session.MarkDirty();
    }

    /// <summary>Opens the preview, or brings it to the front.</summary>
    public WorkspacePanelViewModel OpenPreview() => Open(_previewDescriptor, PreviewDescriptorId);

    /// <summary>
    /// Sets an entity's card aside in a window of its own (the preview keeps following the catalog);
    /// an entity that already has one gets that window brought to the front. Null - the entity does not exist.
    /// </summary>
    public WorkspacePanelViewModel? PinEntity(EntityId entity) =>
        Catalog.Tree.Entity(entity) is null ? null : Open(_pinnedDescriptor, entity.Value.ToString());

    private static EntityId? ParseEntity(string key) =>
        Guid.TryParse(key, out var value) ? new EntityId(value) : null;

    private void OnOpenEntityRequested(EntityId entity) => OpenPreview();

    private void ClosePinnedOfRemovedEntities()
    {
        foreach (var panel in Panels.Where(panel => panel.Descriptor.Id == PinnedDescriptorId).ToList())
        {
            if (ParseEntity(panel.InstanceKey) is not { } id || Catalog.Tree.Entity(id) is null)
            {
                Close(panel);
            }
        }
    }

    // A new window opens beside the preview when there is one (a pinned card goes to its right),
    // each further one a step lower and to the right so none hides another completely.
    private PanelPlacement NextPlacement(WorkspacePanelDescriptor descriptor)
    {
        var placement = descriptor.DefaultPlacement;

        if (Panels.FirstOrDefault(panel => panel.Descriptor.Id == PreviewDescriptorId) is { } preview
            && descriptor.Id == PinnedDescriptorId)
        {
            placement = placement with { X = preview.Desired.Right + 16, Y = preview.Desired.Y };
        }

        var same = Panels.Count(panel => panel.Descriptor.Id == descriptor.Id);

        return placement with { X = placement.X + 28 * same, Y = placement.Y + 28 * same };
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
        // A panel with a size cap cannot fill the desk, so it has nothing to maximize into.
        if (!panel.CanMaximize || panel.State == PanelDisplayState.Minimized)
        {
            return;
        }

        // The desired placement is left alone in both directions, which is exactly why it doubles as
        // the restore geometry and no separate restore fields exist. Maximized is a session-only
        // state: it is neither marked dirty here nor written by the snapshot.
        panel.State = panel.State == PanelDisplayState.Maximized
            ? PanelDisplayState.Normal
            : PanelDisplayState.Maximized;

        Fit(panel);
        Activate(panel);
    }

    /// <summary>Clears the transient selection without changing stacking or persisted layout.</summary>
    public void ClearActivePanel()
    {
        foreach (var panel in Panels)
        {
            panel.IsActive = false;
        }
    }

    /// <summary>Called once when a drag or resize finishes; promotes the on-screen geometry to desired.</summary>
    public void CommitGesture(WorkspacePanelViewModel panel)
    {
        panel.CommitGesture();
        _session.MarkDirty();
    }

    /// <summary>Writes any pending arrangement immediately. Called on shutdown.</summary>
    public void FlushLayout() => _session.Flush();

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _session.Dispose();
        Catalog.LayoutChanged -= _session.MarkDirty;
        Catalog.OpenEntityRequested -= OnOpenEntityRequested;
        Catalog.TreeChanged -= ClosePinnedOfRemovedEntities;
        Catalog.Dispose();

        foreach (var panel in Panels)
        {
            if (panel.Body is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private void Restore(WorkspaceLayout layout)
    {
        // Rebuilding the desk creates fresh bodies, so the outgoing ones have to let go of whatever
        // they were listening to first. Without this, resetting the layout leaves every previous
        // panel subscribed to the session and quietly reacting from off-screen.
        foreach (var panel in Panels)
        {
            if (panel.Body is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        Panels.Clear();
        MinimizedPanels.Clear();

        if (layout.IsEmpty)
        {
            foreach (var descriptor in _catalog.All)
            {
                Panels.Add(new WorkspacePanelViewModel(this, descriptor, descriptor.Id, descriptor.DefaultPlacement));
            }
        }
        else
        {
            var restoredSingletons = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in layout.Panels.OrderBy(entry => entry.ZOrder))
            {
                // A layout naming a panel this build no longer has is skipped, never an error.
                if (FindDescriptor(entry.DescriptorId) is not { } descriptor)
                {
                    continue;
                }

                // A pinned card of an entity the campaign no longer has does not come back.
                if (descriptor.CanRestore is { } canRestore && !canRestore(entry.InstanceKey))
                {
                    continue;
                }

                if (!descriptor.AllowsMultipleInstances && !restoredSingletons.Add(descriptor.Id))
                {
                    continue;
                }

                var panel = new WorkspacePanelViewModel(
                    this,
                    descriptor,
                    entry.InstanceKey,
                    new PanelPlacement(entry.X, entry.Y, entry.Width, entry.Height))
                {
                    // A module stored as closed becomes minimized, because every known module
                    // remains part of the workspace for its lifetime. A stored maximized state
                    // (older files) opens as normal: maximizing is never remembered.
                    State = !descriptor.ClosesPermanently && (!entry.IsOpen || entry.State == PanelDisplayState.Minimized)
                        ? PanelDisplayState.Minimized
                        : PanelDisplayState.Normal
                };

                Panels.Add(panel);

                if (panel.State == PanelDisplayState.Minimized)
                {
                    MinimizedPanels.Add(panel);
                }
            }

            // A module introduced after the layout was saved must not suddenly cover the user's
            // arrangement. Add it to the taskbar in its default geometry instead.
            foreach (var descriptor in _catalog.All.Where(
                         descriptor => Panels.All(panel => panel.Descriptor.Id != descriptor.Id)))
            {
                var panel = new WorkspacePanelViewModel(this, descriptor, descriptor.Id, descriptor.DefaultPlacement)
                {
                    State = PanelDisplayState.Minimized
                };

                Panels.Add(panel);
                MinimizedPanels.Add(panel);
            }
        }

        Normalize();
        ActivateTopmost();
        FitPanels();
    }

    // The tools the system put on the desk, then the frame's own windows (preview, pinned cards),
    // which the deck never offers and which exist only once opened.
    private WorkspacePanelDescriptor? FindDescriptor(string id) =>
        _catalog.Find(id)
        ?? (id == PreviewDescriptorId ? _previewDescriptor : id == PinnedDescriptorId ? _pinnedDescriptor : null);

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

    private WorkspaceLayout CreateSnapshot()
    {
        return new WorkspaceLayout(
            WorkspaceLayout.CurrentVersion,
            _surfaceWidth,
            _surfaceHeight,
            [.. Panels.Select(ToLayout)],
            Catalog.CreateLayout());
    }

    private static WorkspacePanelLayout ToLayout(WorkspacePanelViewModel panel) =>
        new(
            panel.Descriptor.Id,
            panel.InstanceKey,
            // The schema's open flag. The desk never closes a module, so it is always written true.
            true,
            panel.State == PanelDisplayState.Maximized ? PanelDisplayState.Normal : panel.State,
            panel.ZOrder,
            panel.Desired.X,
            panel.Desired.Y,
            panel.Desired.Width,
            panel.Desired.Height);
}
