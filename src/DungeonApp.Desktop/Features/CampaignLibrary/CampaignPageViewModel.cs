using System;
using System.Globalization;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignLibrary;

/// <summary>
/// What the campaign position on the sidebar turns into once a campaign is open: what the shell
/// knows about it - name, creation date - and the one action that changes that, closing it
/// (docs/architecture.md, "Kampanię zamyka się z wnętrza strony kampanii, nie z paska").
/// </summary>
public sealed class CampaignPageViewModel(Campaign campaign, Func<Task> closeCampaign) : ObservableObject
{
    public string Name => campaign.Name.Value;

    // Stored in UTC, read by a person sitting in their own timezone - same convention as CampaignRowViewModel.
    public string CreatedAt => campaign.CreatedAt.ToLocalTime().ToString("d MMM yyyy", CultureInfo.CurrentCulture);

    public AsyncCommand CloseCampaignCommand { get; } = new(closeCampaign);
}
