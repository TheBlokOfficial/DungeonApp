using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Models.Campaigns.Engine;
using DungeonApp.Models.Campaigns.Engine.Events;
using DungeonApp.Models.Campaigns.Engine.Modules.Core;
using DungeonApp.Models.Campaigns.Engine.Modules.Timekeeper;

namespace DungeonApp.ViewModels.Campaigns.Tabs;

public partial class CampaignDashboardViewModel : ViewModelBase
{
    private readonly ICampaignEngine _engine;

    public Campaign Campaign { get; }

    /// <summary>
    /// Moduł Konsoli, z hermetycznym stanem i logiką komend.
    /// </summary>
    public ConsoleModule? ConsoleModule { get; }
    
    /// <summary>
    /// Moduł Timekeepera eksponowany bezpośrednio jako DataContext dla TimekeeperView.
    /// Widok binduje się do modułu, a nie do ViewModelu — dzięki czemu moduł jest hermetyczny.
    /// </summary>
    public TimekeeperModule? TimekeeperModule { get; }

    public CampaignDashboardViewModel(Campaign campaign, ICampaignEngine engine)
    {
        Campaign = campaign;
        _engine = engine;

        ConsoleModule = _engine.GetModule<ConsoleModule>();
        TimekeeperModule = _engine.GetModule<TimekeeperModule>();
    }
}
