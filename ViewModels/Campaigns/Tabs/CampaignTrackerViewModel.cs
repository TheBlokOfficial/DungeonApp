using CommunityToolkit.Mvvm.ComponentModel;
using DungeonApp.Models;

namespace DungeonApp.ViewModels.Campaigns.Tabs;

public partial class CampaignTrackerViewModel : ViewModelBase
{
    public Campaign Campaign { get; }

    public CampaignTrackerViewModel(Campaign campaign)
    {
        Campaign = campaign;
    }
}
