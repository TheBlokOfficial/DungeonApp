using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DungeonApp.Models;

namespace DungeonApp.Services;

public interface ICampaignService
{
    List<Campaign> LoadAllCampaigns();
    void SaveCampaign(Campaign campaign);
    void DeleteCampaign(string campaignId);
    List<Session> LoadSessions(string campaignId);
    void SaveSession(string campaignId, Session session);
    void DeleteSession(string campaignId, string sessionId);
}

public class CampaignService : ICampaignService
{
    private const string AppDirectoryName = "DungeonSessionManager";
    private const string CampaignsDirectoryName = "Campaigns";
    private const string SessionsDirectoryName = "Sessions";
    private const string CampaignFileName = "campaign.json";
    private const string DefaultCampaignSlug = "kampania";

    private readonly string _baseDirectory;
    private readonly IStorageService _storageService;

    public CampaignService(IStorageService storageService)
    {
        _storageService = storageService;
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _baseDirectory = Path.Combine(documents, AppDirectoryName, CampaignsDirectoryName);
        Directory.CreateDirectory(_baseDirectory);
    }

    public List<Campaign> LoadAllCampaigns()
    {
        var campaigns = new List<Campaign>();

        if (!Directory.Exists(_baseDirectory)) return campaigns;

        foreach (string campaignDirectory in Directory.GetDirectories(_baseDirectory))
        {
            string campaignFilePath = Path.Combine(campaignDirectory, CampaignFileName);
            Campaign? campaign = _storageService.Load<Campaign>(campaignFilePath);

            if (campaign is null)
                continue;

            campaign.Id = Path.GetFileName(campaignDirectory);
            campaigns.Add(campaign);
        }

        return campaigns.OrderByDescending(c => c.LastSession).ToList();
    }

    public void SaveCampaign(Campaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        if (string.IsNullOrEmpty(campaign.Id))
            campaign.Id = GenerateUniqueSlug(campaign.Name);

        string campaignDirectory = GetCampaignDirectory(campaign.Id);
        string filePath = Path.Combine(campaignDirectory, CampaignFileName);
        
        _storageService.Save(filePath, campaign);
    }

    public void DeleteCampaign(string campaignId)
    {
        if (string.IsNullOrWhiteSpace(campaignId)) return;
        string campaignDirectory = GetCampaignDirectory(campaignId);
        _storageService.DeleteDirectory(campaignDirectory);
    }

    public List<Session> LoadSessions(string campaignId)
    {
        string sessionsDirectory = GetSessionsDirectory(campaignId);
        var sessions = _storageService.LoadAll<Session>(sessionsDirectory);
        
        foreach(var session in sessions)
        {
            if(string.IsNullOrEmpty(session.Id))
            {
                // In generic load we don't have file name, so we could theoretically set it based on file name if needed,
                // but usually the ID should be inside the JSON.
                // If it's missing, we might have an issue mapping.
                // The previous implementation mapped ID to file name without extension.
                // To maintain parity, we actually need to do this manually or assume ID is in the JSON.
                // Since generic `LoadAll` doesn't set ID, I'll fallback to manual iteration.
            }
        }
        
        // Manual iteration to preserve filename as ID logic:
        var realSessions = new List<Session>();
        if (Directory.Exists(sessionsDirectory))
        {
            foreach (string filePath in Directory.GetFiles(sessionsDirectory, "*.json"))
            {
                Session? session = _storageService.Load<Session>(filePath);
                if (session is null) continue;
                session.Id = Path.GetFileNameWithoutExtension(filePath);
                realSessions.Add(session);
            }
        }

        return realSessions.OrderByDescending(s => s.Date).ToList();
    }

    public void SaveSession(string campaignId, Session session)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrEmpty(session.Id))
            session.Id = Guid.NewGuid().ToString();

        string filePath = GetSessionFilePath(campaignId, session.Id);
        _storageService.Save(filePath, session);
    }

    public void DeleteSession(string campaignId, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(campaignId) || string.IsNullOrWhiteSpace(sessionId)) return;
        string filePath = GetSessionFilePath(campaignId, sessionId);
        _storageService.Delete(filePath);
    }

    private string GenerateUniqueSlug(string name)
    {
        string baseSlug = Slugify(name);
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = DefaultCampaignSlug;

        string candidate = baseSlug;
        int counter = 2;

        while (Directory.Exists(GetCampaignDirectory(candidate)))
        {
            candidate = $"{baseSlug}-{counter}";
            counter++;
        }

        return candidate;
    }

    private static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string cleaned = RemoveDiacritics(input).ToLowerInvariant();
        var result = new StringBuilder();

        foreach (char c in cleaned)
        {
            if (char.IsLetterOrDigit(c)) result.Append(c);
            else if (c is ' ' or '_' or '-') result.Append('-');
        }

        return string.Join("-", result.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string RemoveDiacritics(string input)
    {
        string normalized = input.Replace('ł', 'l').Replace('Ł', 'L').Normalize(NormalizationForm.FormD);
        var result = new StringBuilder();

        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                result.Append(c);
        }

        return result.ToString().Normalize(NormalizationForm.FormC);
    }

    private string GetCampaignDirectory(string campaignId) => Path.Combine(_baseDirectory, campaignId);
    private string GetSessionsDirectory(string campaignId) => Path.Combine(GetCampaignDirectory(campaignId), SessionsDirectoryName);
    private string GetSessionFilePath(string campaignId, string sessionId) => Path.Combine(GetSessionsDirectory(campaignId), $"{sessionId}.json");
}
