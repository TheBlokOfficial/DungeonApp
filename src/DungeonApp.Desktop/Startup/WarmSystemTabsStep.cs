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

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Warms every tab of every compiled-in system so choosing a system does not build them on
/// the UI thread at click time. For each system: its System-category
/// tabs (shell does not know what each builds - only that each is a parameterless declaration, so cannot
/// access a campaign), and its Campaign-category tabs using the first shelf campaign, if
/// one exists.
/// <para>
/// Card warmup and content-pack loading do not belong here - each system loads its own
/// packs and owns its registry, so these are its own startup steps
/// (<see cref="IGameSystem.StartupSteps"/>), not something the shell could perform without
/// knowing the content.
/// </para>
/// <para>
/// Each content instance is disposable, built through a temporary context - never through
/// <see cref="ActiveSystemSession"/>, which builds its own real instance on first display
/// after system selection. Nothing built here remains in any cache
/// later read during system selection.
/// </para>
/// </summary>
public sealed class WarmSystemTabsStep(
    IReadOnlyList<IGameSystem> systems,
    IReadOnlyDictionary<SystemId, ICampaignRepository> repositoriesBySystem,
    CampaignPreparationCache preparations,
    WarmCampaignDataStep dataStep) : IStartupStep
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
        var warmupSession = new CampaignSession(campaign, repositoriesBySystem[system.Id], system.StateModels);
        var warmupContext = new CampaignTabContext(warmupSession);

        foreach (var declaration in system.CampaignTabs)
        {
            ITabContent? tab = null;

            try
            {
                tab = await declaration.CreateContentAsync(warmupContext);
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
}
