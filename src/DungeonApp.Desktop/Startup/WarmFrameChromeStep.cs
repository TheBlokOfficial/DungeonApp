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
/// Warms the shell - everything the GM sees regardless of the chosen system and before choosing
/// any: system selection, shelf, sidebar in both collapse states (for every
/// compiled-in system - its tab declarations determine the sidebar shape), top bar and empty
/// "Ustawienia" tab (neither depends on sidebar collapse, so each warms
/// once). Individual system
/// content is warmed separately by <see cref="WarmSystemTabsStep"/> (tabs and the desk) and the systems'
/// own startup steps (entry cards);
/// this step never builds anything through a temporary campaign session.
/// <para>
/// Every warmed control is disposable - bound to a real model where
/// one exists (shelf), or a temporary model discarded after warmup (selection screen, sidebar)
/// - never placed in a cache later read by
/// <c>AppShellViewModel</c>.
/// </para>
/// </summary>
public sealed class WarmFrameChromeStep(
    IReadOnlyList<IGameSystem> systems,
    CampaignLibraryViewModel campaignLibrary) : IStartupStep
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
