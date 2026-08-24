namespace DungeonApp.Domain.Campaigns.Modules.Scheduler;

public sealed class SchedulerModuleFactory : ICampaignModuleFactory
{
    public ModuleId ModuleId => SchedulerModule.Id;

    public ICampaignModule Create() => new SchedulerModule();
}
