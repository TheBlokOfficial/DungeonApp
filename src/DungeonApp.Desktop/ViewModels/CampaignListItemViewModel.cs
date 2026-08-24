using System;
using DungeonApp.Application.Campaigns;

namespace DungeonApp.Desktop.ViewModels;

public sealed class CampaignListItemViewModel(CampaignSummary summary)
{
    public Guid Id => summary.Id;

    public string Name => summary.Name;

    public string ModuleSummary => summary.EnabledModuleIds.Count == 0
        ? "Brak aktywnych modułów"
        : string.Join(", ", summary.EnabledModuleIds);
}
