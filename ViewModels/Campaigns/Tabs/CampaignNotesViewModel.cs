using CommunityToolkit.Mvvm.ComponentModel;
using DungeonApp.Models;

namespace DungeonApp.ViewModels.Campaigns.Tabs;

public partial class CampaignNotesViewModel : ViewModelBase
{
    public Campaign Campaign { get; }

    public CampaignNotesViewModel(Campaign campaign)
    {
        Campaign = campaign;
    }
}
