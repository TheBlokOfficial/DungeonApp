namespace DungeonApp.Domain.Campaigns.Modules.Clock;

public sealed class ClockModuleFactory : ICampaignModuleFactory
{
    public ModuleId ModuleId => ClockModule.Id;

    public ICampaignModule Create() => new ClockModule();
}
