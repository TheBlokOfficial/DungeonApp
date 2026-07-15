using CommunityToolkit.Mvvm.Messaging;
using DungeonApp.Services;

namespace DungeonApp.Models.Campaigns.Engine.Modules;

/// <summary>
/// Interfejs definiujący cykl życia autonomicznego modułu kampanii.
/// </summary>
public interface ICampaignModule
{
    /// <summary>
    /// Unikalny identyfikator modułu (np. "Core.Timekeeper").
    /// </summary>
    string ModuleId { get; }

    /// <summary>
    /// Uruchamia moduł, wstrzykując mu kontekst aktualnej kampanii, szynę zdarzeń
    /// oraz serwis storage z gotową ścieżką do folderu kampanii.
    /// </summary>
    /// <param name="campaign">Obiekt aktualnie załadowanej kampanii.</param>
    /// <param name="messenger">Izolowana szyna komunikatów dla tej kampanii.</param>
    /// <param name="storage">Serwis do zapisu/odczytu plików JSON.</param>
    /// <param name="campaignDataPath">Ścieżka do folderu tej kampanii na dysku.</param>
    void Initialize(Campaign campaign, IMessenger messenger, IStorageService storage, string campaignDataPath);

    /// <summary>
    /// Zatrzymuje moduł (np. podczas zamykania kampanii).
    /// </summary>
    void Shutdown();
}
