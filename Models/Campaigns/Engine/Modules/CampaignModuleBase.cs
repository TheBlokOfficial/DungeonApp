using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DungeonApp.Models.Campaigns.Engine.Events;
using DungeonApp.Services;
using System.IO;

namespace DungeonApp.Models.Campaigns.Engine.Modules;

/// <summary>
/// Abstrakcyjna klasa bazowa dla modułów kampanii.
/// Udostępnia wygodne metody do komunikacji przez Event Bus oraz API do zapisu stanu.
/// </summary>
public abstract class CampaignModuleBase : ObservableObject, ICampaignModule
{
    public abstract string ModuleId { get; }

    protected Campaign? CurrentCampaign { get; private set; }
    protected IMessenger? Messenger { get; private set; }

    /// <summary>
    /// Serwis do zapisu/odczytu plików JSON przekazany przez CampaignEngine przy inicjalizacji.
    /// </summary>
    protected IStorageService? Storage { get; private set; }

    /// <summary>
    /// Ścieżka do folderu kampanii na dysku (np. .../campaigns/moja-kampania/).
    /// Moduł powinien zapisywać swój stan w podfolderze modules/ tej ścieżki.
    /// </summary>
    protected string CampaignDataPath { get; private set; } = string.Empty;

    public virtual void Initialize(Campaign campaign, IMessenger messenger, IStorageService storage, string campaignDataPath)
    {
        CurrentCampaign = campaign;
        Messenger = messenger;
        Storage = storage;
        CampaignDataPath = campaignDataPath;
        OnInitialize();
    }

    public virtual void Shutdown()
    {
        // Automatyczne wyrejestrowanie ze wszystkich zdarzeń
        Messenger?.UnregisterAll(this);
        
        OnShutdown();
        
        CurrentCampaign = null;
        Messenger = null;
        Storage = null;
        CampaignDataPath = string.Empty;
    }

    /// <summary>
    /// Metoda wywoływana po przypisaniu kampanii i messengera.
    /// Idealne miejsce na subskrypcję zdarzeń (np. Messenger.Register(...)).
    /// </summary>
    protected virtual void OnInitialize() { }

    /// <summary>
    /// Metoda wywoływana przed zamknięciem modułu. Idealne miejsce na SaveState().
    /// </summary>
    protected virtual void OnShutdown() { }

    /// <summary>
    /// Zwraca pełną ścieżkę do pliku stanu tego modułu.
    /// Konwencja: {campaignDataPath}/modules/{ModuleId}.json
    /// </summary>
    protected string GetModuleStatePath()
        => Path.Combine(CampaignDataPath, "modules", $"{ModuleId}.json");

    /// <summary>
    /// Ułatwia publikowanie wiadomości na EventBusie.
    /// </summary>
    protected void Publish<T>(T message) where T : CampaignEventBase
    {
        Messenger?.Send(message);
    }

    /// <summary>
    /// Pobiera przetłumaczony tekst z systemu I18n dla podanego klucza.
    /// Jeśli klucz nie istnieje, zwraca sam klucz.
    /// </summary>
    protected string Translate(string key)
    {
        var translationService = App.Current?.Services?.GetService(typeof(ITranslationService)) as ITranslationService;
        return translationService?.Translate(key) ?? key;
    }
}
