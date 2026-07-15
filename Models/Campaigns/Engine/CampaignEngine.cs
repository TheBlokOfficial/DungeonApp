using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using DungeonApp.Models.Campaigns.Engine.Modules;
using DungeonApp.Services;

namespace DungeonApp.Models.Campaigns.Engine;

/// <summary>
/// Implementacja silnika kampanii. Zarządza cyklem życia wszystkich modułów,
/// dostarcza im izolowaną szynę komunikatów oraz infrastrukturę do zapisu stanu.
/// </summary>
public class CampaignEngine : ICampaignEngine
{
    private readonly List<ICampaignModule> _modules = new();
    private readonly IStorageService _storage;
    private Campaign? _currentCampaign;
    private bool _isRunning;

    // Używamy izolowanego Messengera dla każdej kampanii — zapobiega wyciekowi zdarzeń
    // między równolegle otwartymi kampaniami.
    public IMessenger Messenger { get; } = new StrongReferenceMessenger();

    public CampaignEngine(IEnumerable<ICampaignModule> availableModules, IStorageService storage)
    {
        _modules.AddRange(availableModules);
        _storage = storage;
    }

    public void StartEngine(Campaign campaign)
    {
        if (_isRunning)
            throw new InvalidOperationException("CampaignEngine is already running.");

        _currentCampaign = campaign;

        // Ścieżka do folderu kampanii na dysku (np. .../campaigns/moja-kampania/).
        // Przekazywana modułom, aby mogły zapisywać swoje stany w podfolderze modules/.
        string campaignDataPath = GetCampaignDataPath(campaign.Id);

        foreach (var module in _modules)
        {
            module.Initialize(_currentCampaign, Messenger, _storage, campaignDataPath);
        }

        _isRunning = true;
    }

    public void StopEngine()
    {
        if (!_isRunning) return;

        foreach (var module in _modules)
        {
            module.Shutdown();
        }

        _currentCampaign = null;
        _isRunning = false;
    }

    public T? GetModule<T>() where T : class, ICampaignModule
    {
        return _modules.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Wyznacza ścieżkę do folderu danych konkretnej kampanii.
    /// Spójna z logiką CampaignService, który używa tej samej struktury katalogów.
    /// </summary>
    private static string GetCampaignDataPath(string campaignId)
    {
        return Path.Combine(
            AppPaths.UserDataPath,
            "campaigns",
            string.IsNullOrEmpty(campaignId) ? "_sandbox" : campaignId
        );
    }
}
