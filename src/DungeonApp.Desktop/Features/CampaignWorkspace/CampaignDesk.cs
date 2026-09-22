using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels;

namespace DungeonApp.Desktop.Features.CampaignWorkspace;

/// <summary>
/// The desk's one public entry point (docs/tasks.md, "Etap 1 - projekt styku"): a system builds its
/// desk tab by handing this exactly a campaign context, a layout store and its own tool list, and
/// gets back a finished <see cref="ITabContent"/> that has already loaded its own saved layout.
/// <para>
/// Nothing upstream of this call - a system's tab factory, the shell that invokes it - reads or
/// writes a layout file directly; this is the only place that does, which is what
/// docs/code-map.md's "Biurko wystawia systemowi jedno publiczne wejście" cashes out to in code.
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
        var layout = await layoutStore.LoadAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        var viewModel = new CampaignWorkspaceViewModel(layoutStore, workspaceId, layout, tools);

        return new CampaignWorkspaceTabContent(viewModel);
    }

    /// <summary>
    /// Wraps the desk view model as an <see cref="ITabContent"/>. Disposing it does what
    /// docs/tasks.md's "zwolnienie zapisuje oczekujący układ" requires: flush whatever gesture is
    /// still pending, then release the view model's own subscriptions - in that order, because a
    /// flush after the session is disposed would write nothing (its debounce timer is already
    /// stopped).
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
