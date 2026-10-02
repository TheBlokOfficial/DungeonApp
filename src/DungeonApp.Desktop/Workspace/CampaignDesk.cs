using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Desktop.Workspace.Panels;

namespace DungeonApp.Desktop.Workspace;

/// <summary>
/// The desk's one public entry point: a system builds its desk tab by handing this exactly a
/// campaign context, a layout store and its own tool list, and gets back a finished
/// <see cref="ITabContent"/> that has already loaded its own saved layout.
/// <para>
/// Nothing upstream of this call - a system's tab factory, the shell that invokes it - reads or
/// writes a layout file directly; this is the only place that does.
/// </para>
/// </summary>
public static class CampaignDesk
{
    public static async Task<ITabContent> CreateAsync(
        CampaignTabContext campaign,
        WorkspaceLayoutStore layoutStore,
        IReadOnlyList<WorkspacePanelDescriptor> tools,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(layoutStore);
        ArgumentNullException.ThrowIfNull(tools);

        var workspaceId = campaign.CampaignId.ToString();

        // No ConfigureAwait(false) here: everything past this point builds an Avalonia control and
        // its view model, which must happen on the UI thread. layoutStore.LoadAsync's own read runs
        // off-thread regardless (see its doc comment) - this await only decides where the *rest of
        // this method* resumes, and that rest is UI-bound.
        var layout = await layoutStore.LoadAsync(workspaceId, cancellationToken);
        var viewModel = new CampaignWorkspaceViewModel(layoutStore, workspaceId, layout, tools);

        return new CampaignWorkspaceTabContent(viewModel);
    }

    /// <summary>
    /// Wraps the desk view model as an <see cref="ITabContent"/>. Releasing the tab saves the
    /// pending layout: disposing it flushes whatever gesture is still pending, then releases the
    /// view model's own subscriptions - in that order, because a flush after the session is
    /// disposed would write nothing (its debounce timer is already stopped).
    /// </summary>
    private sealed class CampaignWorkspaceTabContent(CampaignWorkspaceViewModel viewModel) : ITabContent
    {
        public Avalonia.Controls.Control Content { get; } = new CampaignWorkspaceView { DataContext = viewModel };

        public void Dispose()
        {
            viewModel.FlushLayout();
            viewModel.Dispose();
        }
    }
}
