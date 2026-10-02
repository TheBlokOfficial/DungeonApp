using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using DungeonApp.Desktop.Diagnostics;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell.Settings;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.Shell.SystemSelection;
using DungeonApp.Desktop.Shell.TopBar;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Rozgrzewa ramę - wszystko, co GM widzi niezależnie od tego, jaki system wybierze, i zanim wybierze
/// jakikolwiek: ekran wyboru systemu, półkę, pasek boczny w obu stanach zwinięcia (dla każdego
/// wkompilowanego systemu - to jego deklaracje zakładek nadają paskowi kształt), pasek górny i pustą
/// zakładkę "Ustawienia" (żadne z nich nie zależy od zwinięcia paska bocznego, więc każde rozgrzewa
/// się raz) oraz stronę kampanii, jeśli na półce jest choć jedna kampania. Zawartość poszczególnych
/// systemów rozgrzewają osobno <see cref="WarmSystemTabsStep"/> (zakładki, w tym biurko) i kroki
/// startowe samych systemów (karty wpisów);
/// ten krok nigdy nie buduje niczego przez tymczasową sesję kampanii.
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

        // Neither varies with sidebar collapse (the top bar does not mirror the sidebar's width; the
        // empty settings tab has no layout to speak of at all), so each warms once.
        await WarmAsync(
            ui,
            new TopBarView { DataContext = new TopBarViewModel(() => Task.CompletedTask) },
            cancellationToken);

        await WarmAsync(ui, new SettingsView { DataContext = new SettingsViewModel() }, cancellationToken);

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
            selectGallery: () => Task.CompletedTask,
            selectSettings: () => Task.CompletedTask,
            startCollapsed: startCollapsed);

        return WarmAsync(ui, new GlobalSidebarView { DataContext = sidebar }, cancellationToken);
    }

    private static async Task WarmAsync(StartupUiContext ui, Control control, CancellationToken cancellationToken)
    {
        try
        {
            await VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, control, cancellationToken);
        }
        catch (Exception ex)
        {
            // Warmup is an optimization - the real control still gets a full chance to build once shown.
            AppLog.Error($"Rozgrzewka widoku {control.GetType().Name} nie powiodła się.", ex);
        }
    }
}
