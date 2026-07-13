using CommunityToolkit.Mvvm.ComponentModel;
using DungeonApp.Models;

namespace DungeonApp.ViewModels.Campaigns.Tabs;

public partial class CampaignDashboardViewModel : ViewModelBase
{
    public Campaign Campaign { get; }

    public CampaignDashboardViewModel(Campaign campaign)
    {
        Campaign = campaign;
    }
}
