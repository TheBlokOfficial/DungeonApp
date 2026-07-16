using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Models.Campaigns.Engine;
using DungeonApp.Models.Campaigns.Engine.Events;
using DungeonApp.Models.Campaigns.Engine.Modules;
using DungeonApp.Models.Campaigns.Engine.Modules.Core;
using DungeonApp.Models.Campaigns.Engine.Modules.Test;
using DungeonApp.Models.Campaigns.Engine.Modules.Timekeeper;
using DungeonApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DungeonApp.ViewModels.Dashboard;

/// <summary>
/// ViewModel Środowiska Testowego (Sandbox / Workbench).
/// Tworzy własną, w pełni izolowaną instancję CampaignEngine z fikcyjną kampanią in-memory.
/// Visible tylko gdy AppSettings.IsDeveloperModeEnabled == true.
/// </summary>
/// <remarks>
/// DLACZEGO fikcyjna kampania, a nie prawdziwa:
/// Sandbox ma testować moduły w sterylnych warunkach bez ryzyka uszkodzenia danych.
/// CampaignId="sandbox" kieruje zapis stanu do folderu tymczasowego (_sandbox/modules/),
/// który nie koliduje z żadną prawdziwą kampanią użytkownika.
/// </remarks>
public partial class SandboxTabViewModel : ViewModelBase
{
    private readonly CampaignEngine _engine;

    /// <summary>
    /// Hermetyczny moduł czasu — bezpośrednio bindowany do TimekeeperView jako DataContext.
    /// </summary>
    public TimekeeperModule TimekeeperModule { get; }

    /// <summary>
    /// Moduł Konsoli
    /// </summary>
    public ConsoleModule ConsoleModule { get; }

    public SandboxTabViewModel(IStorageService storageService)
    {
        var consoleModule = new ConsoleModule();
        TimekeeperModule = new TimekeeperModule();

        _engine = new CampaignEngine(
            new ICampaignModule[] { consoleModule, TimekeeperModule, new TestCommandsModule() },
            storageService
        );

        // Fikcyjna kampania in-memory — ID "_sandbox" kieruje zapis do dedykowowanego folderu
        var sandboxCampaign = new Campaign
        {
            Id   = "_sandbox",
            Name = "Sandbox"
        };

        _engine.StartEngine(sandboxCampaign);

        ConsoleModule = _engine.GetModule<ConsoleModule>()!;
        
        // 5. Opcjonalnie: Załaduj testowe dane / wykonaj komendy rozgrzewkowe
        ConsoleModule.ConsoleInputText = "/time +8h";
        ConsoleModule.ExecuteConsoleCommand();
    }
}
