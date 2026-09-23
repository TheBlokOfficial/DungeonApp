using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Persistence;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Rozgrzewa każdą zakładkę każdego wkompilowanego systemu - to, co "wybór systemu" dawniej budował na
/// wątku UI w chwili kliknięcia (docs/tasks.md, objaw 1). Dla każdego systemu: jego zakładki kategorii
/// System (rama nie wie, co która buduje - tylko że każda jest deklaracją bez parametru, więc nie może
/// sięgnąć po kampanię), i jego zakładki kategorii Kampania wobec pierwszej kampanii z półki, jeśli
/// jakaś istnieje.
/// <para>
/// Rozgrzewka kart i wczytywanie paczek treści nie są już tutaj - odkąd każdy system sam wczytuje
/// własne paczki i ma własny rejestr (docs/architecture.md, "Rama, biblioteka, system"), to jego
/// własne kroki startowe (<see cref="IGameSystem.StartupSteps"/>), nie coś rama umiałaby zrobić za
/// niego bez znajomości treści.
/// </para>
/// <para>
/// Każda zawartość jest egzemplarzem rzucanym, budowanym przez tymczasowy kontekst - nigdy przez
/// <see cref="ActiveSystemSession"/>, która przy wyborze systemu zbuduje swój własny, prawdziwy
/// egzemplarz przy pierwszym pokazaniu (etap 1). Nic zbudowane tutaj nie zostaje w żadnej pamięci
/// podręcznej, którą później czytałby wybór systemu.
/// </para>
/// </summary>
public sealed class WarmSystemTabsStep(
    IReadOnlyList<IGameSystem> systems,
    ICampaignRepository campaigns,
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

            // The sampled campaign belongs to exactly one system (docs/architecture.md, "Kampania
            // należy do jednego systemu"); warming another compiled system's campaign tabs against it
            // would hand that system's tab factories a session built from a stranger's declarations.
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
            catch (Exception)
            {
                // Warmup is an optimization - a real chance to build still exists on first click.
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
        var warmupSession = new CampaignSession(campaign, campaigns, system.StateModels);
        var warmupContext = new CampaignTabContext(warmupSession);

        foreach (var declaration in system.CampaignTabs)
        {
            ITabContent? tab = null;

            try
            {
                tab = await declaration.CreateContentAsync(warmupContext);
                await VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, tab.Content, cancellationToken);
            }
            catch (Exception)
            {
                // Warmup is an optimization - a real chance to build still exists on first click.
            }
            finally
            {
                tab?.Dispose();
            }
        }
    }
}
