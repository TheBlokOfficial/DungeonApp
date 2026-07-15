using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging;
using DungeonApp.Models.Campaigns.Engine.Modules;

namespace DungeonApp.Models.Campaigns.Engine;

/// <summary>
/// Serce architektury kampanii.
/// Zarządza cyklem życia wszystkich modułów i dostarcza im izolowaną szynę komunikatów.
/// </summary>
public interface ICampaignEngine
{
    /// <summary>
    /// Zwraca izolowany Message Bus (szynę komunikatów) dedykowany tylko dla tej aktywnej kampanii.
    /// </summary>
    IMessenger Messenger { get; }

    /// <summary>
    /// Inicjalizuje silnik dla podanej kampanii, ładując wszystkie moduły.
    /// </summary>
    void StartEngine(Campaign campaign);

    /// <summary>
    /// Zatrzymuje silnik i wszystkie jego moduły.
    /// </summary>
    void StopEngine();

    /// <summary>
    /// Zwraca instancję zarejestrowanego modułu na podstawie typu.
    /// </summary>
    T? GetModule<T>() where T : class, ICampaignModule;
}
