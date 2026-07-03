using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using DungeonApp.Models;

namespace DungeonApp.Services;

public class CampaignService
{
    private readonly string _baseDirectory;

    public CampaignService()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _baseDirectory = Path.Combine(documents, "DungeonSessionManager", "Campaigns");

        Directory.CreateDirectory(_baseDirectory);
    }

    public List<Campaign> LoadAllCampaigns()
    {
        var campaigns = new List<Campaign>();

        foreach (var dir in Directory.GetDirectories(_baseDirectory))
        {
            string campaignFile = Path.Combine(dir, "campaign.json");

            if (File.Exists(campaignFile))
            {
                try
                {
                    string json = File.ReadAllText(campaignFile);
                    var campaign = JsonSerializer.Deserialize<Campaign>(json);
                    if (campaign != null)
                    {
                        campaign.Id = Path.GetFileName(dir); // nazwa folderu = Id
                        campaigns.Add(campaign);
                    }
                }
                catch { /* pomijamy uszkodzone kampanie */ }
            }
        }

        return campaigns.OrderByDescending(c => c.LastSession).ToList();
    }

    public void SaveCampaign(Campaign campaign)
    {
        // Nowa kampania (brak Id) -> generujemy czytelny folder ze slugiem
        if (string.IsNullOrEmpty(campaign.Id))
        {
            campaign.Id = GenerateUniqueSlug(campaign.Name);
        }

        string campaignFolder = Path.Combine(_baseDirectory, campaign.Id);
        Directory.CreateDirectory(campaignFolder);

        string filePath = Path.Combine(campaignFolder, "campaign.json");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(campaign, options);
        File.WriteAllText(filePath, json);
    }

    public void DeleteCampaign(string campaignId)
    {
        if (string.IsNullOrEmpty(campaignId)) return;

        string campaignFolder = Path.Combine(_baseDirectory, campaignId);

        if (Directory.Exists(campaignFolder))
        {
            try
            {
                Directory.Delete(campaignFolder, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd podczas usuwania kampanii: {ex.Message}");
            }
        }
    }

    // Tworzy czytelną, unikalną nazwę folderu na podstawie nazwy kampanii,
    // np. "Klątwa Strahda" -> "klatwa-strahda", a przy kolizji "klatwa-strahda-2"
    private string GenerateUniqueSlug(string name)
    {
        string baseSlug = Slugify(name);
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "kampania";

        string candidate = baseSlug;
        int counter = 2;

        while (Directory.Exists(Path.Combine(_baseDirectory, candidate)))
        {
            candidate = $"{baseSlug}-{counter}";
            counter++;
        }

        return candidate;
    }

    private static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Usuwamy polskie znaki diakrytyczne (ą, ć, ę, ł, ń, ó, ś, ź, ż)
        string normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (char c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        string cleaned = sb.ToString().Normalize(NormalizationForm.FormC);

        // Polskie "ł" nie rozkłada się przez NormalizationForm.FormD, więc podmieniamy ręcznie
        cleaned = cleaned.Replace("ł", "l").Replace("Ł", "L");

        cleaned = cleaned.ToLowerInvariant();

        // Spacje i podkreślniki -> myślnik, reszta niedozwolonych znaków -> usuwamy
        var result = new StringBuilder();
        foreach (char c in cleaned)
        {
            if (char.IsLetterOrDigit(c))
                result.Append(c);
            else if (c is ' ' or '_' or '-')
                result.Append('-');
        }

        // Zwijamy wielokrotne myślniki i przycinamy z krawędzi
        string slug = string.Join("-", result.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));

        return slug;
    }
}