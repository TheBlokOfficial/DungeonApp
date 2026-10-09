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
/// The desk's one public entry point: the frame builds the open campaign's desk by handing this a
/// campaign context, a layout store, the system's tool list and the way to close the campaign, and
/// gets back a finished <see cref="ITabContent"/> that has already loaded its own saved layout.
/// <para>
/// Nothing upstream of this call reads or writes a layout file directly; this is the only place
/// that does.
/// </para>
/// </summary>
public static class CampaignDesk
{
    public static async Task<ITabContent> CreateAsync(
        CampaignTabContext campaign,
        WorkspaceLayoutStore layoutStore,
        IReadOnlyList<WorkspacePanelDescriptor> tools,
        Func<Task> closeCampaign,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(layoutStore);
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(closeCampaign);

        var workspaceId = campaign.CampaignId.ToString();

        // No ConfigureAwait(false) here: everything past this point builds an Avalonia control and
        // its view model, which must happen on the UI thread. layoutStore.LoadAsync's own read runs
        // off-thread regardless (see its doc comment) - this await only decides where the *rest of
        // this method* resumes, and that rest is UI-bound.
        var layout = await layoutStore.LoadAsync(workspaceId, cancellationToken);
        var viewModel = new CampaignWorkspaceViewModel(layoutStore, workspaceId, layout, tools, closeCampaign);

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
