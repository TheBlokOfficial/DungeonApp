using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.Persistence;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Rozgrzewa zawartość każdego wkompilowanego systemu - to, co "wybór systemu" dawniej budował na
/// wątku UI w chwili kliknięcia (docs/tasks.md, objaw 1). Dla każdego systemu: jego zakładki kategorii
/// System (rejestr, wraz z kartą pierwszego rozwiązanego wpisu każdego typu treści, jeśli paczki
/// dostarczają choć jeden - typ bez ani jednego wpisu nie ma z czego zbudować karty, więc jest
/// pomijany, tak samo jak dotąd pomijana jest rozgrzewka przy pustej półce), i jego zakładki kategorii
/// Kampania (biurko, wraz z oknem każdego zadeklarowanego narzędzia - <see cref="CampaignTabDeclaration.CreateContentAsync"/>
/// buduje cały gotowy widok biurka naraz, więc osobne rozgrzewanie panelu po panelu nie jest tu
/// potrzebne) wobec pierwszej kampanii z półki, jeśli jakaś istnieje.
/// <para>
/// Każda zawartość jest egzemplarzem rzucanym, budowanym przez tymczasowy kontekst - nigdy przez
/// <see cref="ActiveSystemSession"/>, która przy wyborze systemu zbuduje swój własny, prawdziwy
/// egzemplarz przy pierwszym pokazaniu (etap 1). Nic zbudowane tutaj nie zostaje w żadnej pamięci
/// podręcznej, którą później czytałby wybór systemu.
/// </para>
/// </summary>
public sealed class WarmSystemContentStep(
    IReadOnlyList<IGameSystem> systems,
    Func<ContentRegistry> registry,
    ICampaignRepository campaigns,
    CampaignPreparationCache preparations,
    WarmCampaignDataStep dataStep) : IStartupStep
{
    public string Describe() => "Rozgrzewanie zawartości systemów…";

    public Task PrepareAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken)
    {
        var content = registry();

        Campaign? warmupCampaign = dataStep.WarmupCampaignId is { } id
            ? await preparations.PeekAsync(id, cancellationToken)
            : null;

        foreach (var system in systems)
        {
            await WarmSystemTabsAsync(ui, system, content, cancellationToken);
            await WarmCardsAsync(ui, system, content, cancellationToken);

            if (warmupCampaign is not null)
            {
                await WarmCampaignTabsAsync(ui, system, warmupCampaign, content, cancellationToken);
            }
        }
    }

    private static async Task WarmSystemTabsAsync(
        StartupUiContext ui, IGameSystem system, ContentRegistry registry, CancellationToken cancellationToken)
    {
        var context = new SystemTabContext(registry);

        foreach (var declaration in system.SystemTabs)
        {
            ITabContent? tab = null;

            try
            {
                tab = declaration.CreateContent(context);
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

    /// <summary>
    /// One card per distinct, resolved content type this system owns among the loaded entries - the
    /// first entry found for each. A type with no entry in any installed pack has nothing to build a
    /// card from and is skipped, the same way an empty shelf skips campaign warmup below.
    /// </summary>
    private static async Task WarmCardsAsync(
        StartupUiContext ui, IGameSystem system, ContentRegistry registry, CancellationToken cancellationToken)
    {
        var samples = registry.Entries
            .Where(entry => entry.Type is { } type && type.Reference.Set == system.Id)
            .GroupBy(entry => entry.Type!.Value.Reference)
            .Select(group => group.First());

        foreach (var entry in samples)
        {
            try
            {
                var card = system.CreateCard(entry.Entry);
                await VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, card, cancellationToken);
            }
            catch (Exception)
            {
                // Warmup is an optimization - the registry screen still draws the card for real on selection.
            }
        }
    }

    private async Task WarmCampaignTabsAsync(
        StartupUiContext ui,
        IGameSystem system,
        Campaign campaign,
        ContentRegistry registry,
        CancellationToken cancellationToken)
    {
        var warmupSession = new CampaignSession(campaign, campaigns);
        var warmupContext = new CampaignTabContext(warmupSession, registry);

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
