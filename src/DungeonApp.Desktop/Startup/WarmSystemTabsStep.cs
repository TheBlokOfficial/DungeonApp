using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Diagnostics;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Workspace.Layout;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Warms every tab of every compiled-in system so choosing a system does not build them on
/// the UI thread at click time. For each system: its System-category
/// tabs (shell does not know what each builds - only that each is a parameterless declaration, so cannot
/// access a campaign), and, using the first shelf campaign if one exists, the desk stocked with the
/// system's tools and its Campaign-category tabs.
/// <para>
/// Card warmup and content-pack loading do not belong here - each system loads its own
/// packs and owns its registry, so these are its own startup steps
/// (<see cref="IGameSystem.StartupSteps"/>), not something the shell could perform without
/// knowing the content.
/// </para>
/// <para>
/// Each content instance is disposable, built through a throwaway <see cref="ActiveSystemSession"/>
/// that is released afterwards - the real one builds its own instance after system selection.
/// Nothing built here remains in any cache later read during system selection.
/// </para>
/// </summary>
public sealed class WarmSystemTabsStep(
    IReadOnlyList<IGameSystem> systems,
    IReadOnlyDictionary<SystemId, ICampaignRepository> repositoriesBySystem,
    CampaignPreparationCache preparations,
    WarmCampaignDataStep dataStep,
    WorkspaceLayoutStore layoutStore) : IStartupStep
{
    public string Describe() => "Rozgrzewanie zakładek systemów…";

    public Task PrepareAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken)
    {
        Campaign? warmupCampaign = dataStep.WarmupCampaignSummary is { } summary
            ? await preparations.PeekAsync(summary, cancellationToken)
            : null;

        foreach (var system in systems)
        {
            await WarmSystemTabsAsync(ui, system, cancellationToken);

            // The sampled campaign belongs to exactly one system; warming another compiled system's
            // campaign tabs against it would hand that system's tab factories a session built from a
            // stranger's declarations.
            if (warmupCampaign is not null && warmupCampaign.SystemId == system.Id)
            {
                await WarmCampaignTabsAsync(ui, system, warmupCampaign, cancellationToken);
            }
        }
    }

    private static async Task WarmSystemTabsAsync(
        StartupUiContext ui, IGameSystem system, CancellationToken cancellationToken)
    {
        foreach (var declaration in system.SystemTabs)
        {
            ITabContent? tab = null;

            try
            {
                tab = declaration.CreateContent();
                await VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, tab.Content, cancellationToken);
            }
            catch (Exception ex)
            {
                // Warmup is an optimization - a real chance to build still exists on first click.
                AppLog.Error($"Rozgrzewka zakładki {declaration.Id} nie powiodła się.", ex);
            }
            finally
            {
                tab?.Dispose();
            }
        }
    }

    private async Task WarmCampaignTabsAsync(
        StartupUiContext ui,
        IGameSystem system,
        Campaign campaign,
        CancellationToken cancellationToken)
    {
        var session = new ActiveSystemSession(system, repositoriesBySystem[system.Id], layoutStore);

        try
        {
            session.OpenCampaign(campaign);

            try
            {
                var desk = await session.GetOrCreateDeskAsync(() => Task.CompletedTask);
                await VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, desk.Content, cancellationToken);
            }
            catch (Exception ex)
            {
                // Warmup is an optimization - a real chance to build still exists on opening.
                AppLog.Error("Rozgrzewka biurka nie powiodła się.", ex);
            }

            foreach (var declaration in system.CampaignTabs)
            {
                try
                {
                    var tab = await session.GetOrCreateCampaignTabAsync(declaration);
                    await VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, tab.Content, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Warmup is an optimization - a real chance to build still exists on first click.
                    AppLog.Error($"Rozgrzewka zakładki {declaration.Id} nie powiodła się.", ex);
                }
            }
        }
        finally
        {
            session.ReleaseAll();
        }
    }
}
