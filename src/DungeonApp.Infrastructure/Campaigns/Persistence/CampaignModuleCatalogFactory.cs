using DungeonApp.Application.Campaigns;
using DungeonApp.Domain.Campaigns.Modules.Clock;
using DungeonApp.Domain.Campaigns.Modules.Scheduler;

namespace DungeonApp.Infrastructure.Campaigns.Persistence;

/// <summary>
/// Default composition of modules shipped with the local application.
/// </summary>
public static class CampaignModuleCatalogFactory
{
    public static CampaignModuleCatalog CreateDefault() => new([new ClockModuleFactory(), new SchedulerModuleFactory()]);
}
