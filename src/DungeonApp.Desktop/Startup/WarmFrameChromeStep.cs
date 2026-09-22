using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.Shell.SystemSelection;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Rozgrzewa ramę - wszystko, co GM widzi niezależnie od tego, jaki system wybierze, i zanim wybierze
/// jakikolwiek: ekran wyboru systemu, półkę, pasek boczny w obu stanach zwinięcia (dla każdego
/// wkompilowanego systemu - to jego deklaracje zakładek nadają paskowi kształt) i stronę kampanii,
/// jeśli na półce jest choć jedna kampania. Zawartość poszczególnych systemów - ich zakładki, karty
/// wpisów, biurko - rozgrzewa osobno <see cref="WarmSystemContentStep"/>; ten krok nigdy nie buduje
/// niczego przez tymczasową sesję kampanii.
/// <para>
/// Każda rozgrzewana kontrolka jest egzemplarzem rzucanym - powiązanym z prawdziwym modelem tam, gdzie
/// jeden już istnieje (półka), albo z tymczasowym, wyrzucanym zaraz po rozgrzewce (ekran wyboru, pasek,
/// strona kampanii) - nigdy nie trafia do żadnej pamięci podręcznej, którą później czytałby
/// <c>AppShellViewModel</c>.
/// </para>
/// </summary>
public sealed class WarmFrameChromeStep(
    IReadOnlyList<IGameSystem> systems,
    CampaignLibraryViewModel campaignLibrary,
    CampaignPreparationCache preparations,
    WarmCampaignDataStep dataStep) : IStartupStep
{
    public string Describe() => "Rozgrzewanie interfejsu…";

    public Task PrepareAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken)
    {
        await WarmAsync(
            ui,
            new SystemSelectionView { DataContext = new SystemSelectionViewModel(systems, _ => Task.CompletedTask) },
            cancellationToken);

        await WarmAsync(ui, new CampaignLibraryView { DataContext = campaignLibrary }, cancellationToken);

        foreach (var system in systems)
        {
            await WarmSidebarAsync(ui, system, startCollapsed: false, cancellationToken);
            await WarmSidebarAsync(ui, system, startCollapsed: true, cancellationToken);
        }

        if (dataStep.WarmupCampaignSummary is { } summary &&
            await preparations.PeekAsync(summary, cancellationToken) is { } campaign)
        {
            var pageViewModel = new CampaignPageViewModel(campaign, closeCampaign: () => Task.CompletedTask);
            await WarmAsync(ui, new CampaignPageView { DataContext = pageViewModel }, cancellationToken);
        }
    }

    private static Task WarmSidebarAsync(
        StartupUiContext ui, IGameSystem system, bool startCollapsed, CancellationToken cancellationToken)
    {
        // No-op callbacks: this sidebar is never clicked, only measured, styled and laid out once.
        var sidebar = new GlobalSidebarViewModel(
            system.SystemTabs,
            system.CampaignTabs,
            selectCampaignPosition: () => Task.CompletedTask,
            selectCampaignTab: _ => Task.CompletedTask,
            selectSystemTab: _ => { },
            changeSystem: () => Task.CompletedTask,
            startCollapsed: startCollapsed);

        return WarmAsync(ui, new GlobalSidebarView { DataContext = sidebar }, cancellationToken);
    }

    private static async Task WarmAsync(StartupUiContext ui, Control control, CancellationToken cancellationToken)
    {
        try
        {
            await VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, control, cancellationToken);
        }
        catch (Exception)
        {
            // Warmup is an optimization - the real control still gets a full chance to build once shown.
        }
    }
}
