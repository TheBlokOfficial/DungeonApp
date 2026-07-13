using CommunityToolkit.Mvvm.ComponentModel;
using DungeonApp.Models;

namespace DungeonApp.ViewModels.Campaigns.Tabs;

public partial class CampaignStoryViewModel : ViewModelBase
{
    public Campaign Campaign { get; }

    public CampaignStoryViewModel(Campaign campaign)
    {
        Campaign = campaign;
    }
}
