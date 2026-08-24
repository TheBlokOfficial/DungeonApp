using System;
using DungeonApp.Application.Campaigns;

namespace DungeonApp.Desktop.Features.CampaignLibrary;

public sealed class CampaignListItemViewModel(CampaignSummary summary)
{
    public Guid Id => summary.Id;

    public string Name => summary.Name;

    public string Initials
    {
        get
        {
            var words = summary.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return words.Length switch
            {
                0 => "?",
                1 => words[0][..1].ToUpperInvariant(),
                _ => string.Concat(words[0][0], words[^1][0]).ToUpperInvariant()
            };
        }
    }

    public int ModuleCount => summary.EnabledModuleIds.Count;

    public string Metadata => ModuleCount switch
    {
        0 => "Zapis lokalny · bez aktywnych modułów",
        1 => "Zapis lokalny · 1 aktywny moduł",
        2 or 3 or 4 => $"Zapis lokalny · {ModuleCount} aktywne moduły",
        _ => $"Zapis lokalny · {ModuleCount} aktywnych modułów"
    };

    public string ModuleSummary => ModuleCount == 0
        ? "Brak aktywnych modułów"
        : string.Join(", ", summary.EnabledModuleIds);

    public string StatusLabel => "LOKALNA";
}
